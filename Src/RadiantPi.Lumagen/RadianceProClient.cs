﻿/*
 * RadiantPi.Lumagen - Communication client for Lumagen RadiancePro
 * Copyright (C) 2020 - Steve G. Bjorg
 *
 * This program is free software: you can redistribute it and/or modify it
 * under the terms of the GNU Affero General Public License as published by the
 * Free Software Foundation, either version 3 of the License, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 * FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
 * details.
 *
 * You should have received a copy of the GNU Affero General Public License along
 * with this program. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using RadiantPi.Lumagen.Model;

namespace RadiantPi.Lumagen {

    public sealed class RadianceProClientConfig {

        //--- Properties ---
        public string PortName { get; set; }
        public int? BaudRate { get; set; }
        public bool? Mock { get; set; }
        public bool? Verbose { get; set; }
    }

    public sealed class RadianceProClient : IRadiancePro {

        //--- Class Methods ---
        public static IRadiancePro Initialize(RadianceProClientConfig config)
            => (config.Mock ?? false)
                ? new RadianceProMockClient()
                : new RadianceProClient(config.PortName, config.BaudRate ?? 9600) {
                    Verbose = config.Verbose ?? false
                };

        private static string ToCommandCode(RadianceProMemory memory, bool allowAll)
            => memory switch {
                RadianceProMemory.MemoryAll => allowAll
                    ? "0"
                    : throw new ArgumentException("all memory is not allowed"),
                RadianceProMemory.MemoryA => "A",
                RadianceProMemory.MemoryB => "B",
                RadianceProMemory.MemoryC => "C",
                RadianceProMemory.MemoryD => "D",
               _ => throw new ArgumentException("invalid memory selection")
            };

        private static string ToCommandCode(RadianceProInput input)
            => input switch {
                RadianceProInput.Input1 => "0",
                RadianceProInput.Input2 => "1",
                RadianceProInput.Input3 => "2",
                RadianceProInput.Input4 => "3",
                RadianceProInput.Input5 => "4",
                RadianceProInput.Input6 => "5",
                RadianceProInput.Input7 => "6",
                RadianceProInput.Input8 => "7",
               _ => throw new ArgumentException("invalid input selection")
            };

        private static string ToCommandCode(RadianceProCustomMode customMode)
            => customMode switch {
                RadianceProCustomMode.CustomMode0 => "0",
                RadianceProCustomMode.CustomMode1 => "1",
                RadianceProCustomMode.CustomMode2 => "2",
                RadianceProCustomMode.CustomMode3 => "3",
                RadianceProCustomMode.CustomMode4 => "4",
                RadianceProCustomMode.CustomMode5 => "5",
                RadianceProCustomMode.CustomMode6 => "6",
                RadianceProCustomMode.CustomMode7 => "7",
                _ => throw new ArgumentException("invalid custom mode selection")
            };

        private static string ToCommandCode(RadianceProCms cms)
            => cms switch {
                RadianceProCms.Cms0 => "0",
                RadianceProCms.Cms1 => "1",
                RadianceProCms.Cms2 => "2",
                RadianceProCms.Cms3 => "3",
                RadianceProCms.Cms4 => "4",
                RadianceProCms.Cms5 => "5",
                RadianceProCms.Cms6 => "6",
                RadianceProCms.Cms7 => "7",
               _ => throw new ArgumentException("invalid cms selection")
            };

        private static string ToCommandCode(RadianceProStyle style)
            => style switch {
                RadianceProStyle.Style0 => "0",
                RadianceProStyle.Style1 => "1",
                RadianceProStyle.Style2 => "2",
                RadianceProStyle.Style3 => "3",
                RadianceProStyle.Style4 => "4",
                RadianceProStyle.Style5 => "5",
                RadianceProStyle.Style6 => "6",
                RadianceProStyle.Style7 => "7",
               _ => throw new ArgumentException("invalid style selection")
            };

        private static string SanitizeText(string value, int maxLength) => new string(value.Take(maxLength).Select(c => (char.IsLetterOrDigit(c) ? c : ' ')).ToArray());

        //--- Fields ---
        public event EventHandler<GetModeInfoResponse> ModeInfoChanged;
        private readonly SerialPort _serialPort;
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);
        private string _accumulator = "";
        private event EventHandler<string> _responseReceivedEvent;

        //--- Constructors ---
        public RadianceProClient(SerialPort serialPort) {
            _responseReceivedEvent += DispatchEvents;
            _serialPort = serialPort ?? throw new ArgumentNullException(nameof(serialPort));
            _serialPort.DataReceived += SerialDataReceived;
            _serialPort.Open();
        }

        public RadianceProClient(string portName, int baudRate = 9600) : this(new SerialPort {
            PortName = portName,
            BaudRate = baudRate,
            DataBits = 8,
            Parity = Parity.None,
            StopBits = StopBits.One,
            Handshake = Handshake.None,
            ReadTimeout = 1_000,
            WriteTimeout = 1_000
        }) { }

        //--- Properties ---
        public bool Verbose { get; set; }

        //--- Methods ---
        public async Task<GetDeviceInfoResponse> GetDeviceInfoAsync() {
            var response = await SendAsync("ZQS01", expectResponse: true);
            var data = response.Split(",");
            if(data.Length < 4) {
                throw new InvalidDataException("invalid response");
            }
            return LogResponse(new GetDeviceInfoResponse {
                ModelName = data[0],
                SoftwareRevision = data[1],
                ModelNumber = data[2],
                SerialNumber = data[3]
            });
        }

        public async Task<GetModeInfoResponse> GetModeInfoAsync() {
            var response = await SendAsync("ZQI23", expectResponse: true);
            return ParseModeInfoResponse(response);
        }

        public Task<string> GetInputLabelAsync(RadianceProMemory memory, RadianceProInput input)
            => SendAsync($"ZQS1{ToCommandCode(memory, allowAll: false)}{ToCommandCode(input)}", expectResponse: true);

        public Task SetInputLabelAsync(RadianceProMemory memory, RadianceProInput input, string value)
            => SendAsync("ZY524" + $"{ToCommandCode(memory, allowAll: true)}{ToCommandCode(input)}{SanitizeText(value, maxLength: 10)}" + "\r", expectResponse: false);

        public Task<string> GetCustomModeLabelAsync(RadianceProCustomMode customMode)
            => SendAsync($"ZQS11{ToCommandCode(customMode)}", expectResponse: true);

        public Task SetCustomModeLabelAsync(RadianceProCustomMode customMode, string value)
            => SendAsync("ZY524" + $"1{ToCommandCode(customMode)}{SanitizeText(value, maxLength: 7)}" + "\r", expectResponse: false);

        public Task<string> GetCmsLabelAsync(RadianceProCms cms)
            => SendAsync($"ZQS12{ToCommandCode(cms)}", expectResponse: true);

        public Task SetCmsLabelAsync(RadianceProCms cms, string value)
            => SendAsync("ZY524" + $"2{ToCommandCode(cms)}{SanitizeText(value, maxLength: 8)}" + "\r", expectResponse: false);

        public Task<string> GetStyleLabelAsync(RadianceProStyle style) => SendAsync($"ZQS13{ToCommandCode(style)}", expectResponse: true);

        public Task SetStyleLabelAsync(RadianceProStyle style, string value)
            => SendAsync("ZY524" + $"3{ToCommandCode(style)}{SanitizeText(value, maxLength: 8)}" + "\r", expectResponse: false);

        public void Dispose() {
            Log("Disponse");
            _serialPort.DataReceived -= SerialDataReceived;
            _mutex.Dispose();
            _serialPort.Dispose();
        }

        private async Task<string> SendAsync(string command, bool expectResponse) {
            var buffer = Encoding.UTF8.GetBytes(command);
            await _mutex.WaitAsync();
            try {

                // send message, await echo response and optional response message
                if(expectResponse) {
                    TaskCompletionSource<string> responseSource = new();
                    try {
                        _responseReceivedEvent += ReadResponse;
                        Log($"sending: '{command}'");
                        await _serialPort.BaseStream.WriteAsync(buffer, 0, buffer.Length);
                        return await responseSource.Task;
                    } finally {
                        _responseReceivedEvent -= ReadResponse;
                    }

                    // local functions
                    void ReadResponse(object sender, string response) {

                        // skip everything until the first comma (',')
                        for(var i = 0; i < response.Length; ++i) {
                            if(response[i] == ',') {
                                response = response.Remove(0, i + 1);
                                break;
                            }
                        }

                        // terminator characters were received; indicate we're done by setting response
                        responseSource.SetResult(response);
                    }
                } else {
                    await _serialPort.BaseStream.WriteAsync(buffer, 0, buffer.Length);
                    return null;
                }
            } finally {
                _mutex.Release();
            }
        }

        private void SerialDataReceived(object sender, SerialDataReceivedEventArgs args) {
            var received = _serialPort.ReadExisting();
            Log($"received: '{string.Join("", received.Select(EscapeChar))}'");

            // loop while there is text to process
            while(received.Length > 0) {
                if(_accumulator.Length == 0) {

                    // check if received text contains a response marker
                    var index = received.IndexOf('!');
                    if(index < 0) {
                        return;
                    }

                    // found beginning of a response
                    _accumulator = "!";

                    // continue by processing remainder of received text
                    received = received.Substring(index + 1);
                } else {

                    // append received text
                    _accumulator += received;

                    // check if we found an end-of-response marker
                    var index = _accumulator.IndexOf("\r\n");
                    if(index < 0) {
                        return;
                    }

                    // process response
                    var message = _accumulator.Substring(0, index);
                    Log($"dispatching: '{message}'");
                    _responseReceivedEvent?.Invoke(this, message);

                    // process remainder of accumulator as newly received text
                    received = _accumulator.Substring(index);
                    _accumulator = "";
                }
            }

            // local functions
            string EscapeChar(char c) => c switch {
                >= (char)32 and < (char)128 => c.ToString(),
                '\r' => "\\r",
                '\n' => "\\n",
                _ => $"\\u{(int)c:X4}"
            };
        }

        private void DispatchEvents(object sender, string response) {
            const string MODEINFORESPONSE = "!I23,";
            if(response.StartsWith(MODEINFORESPONSE, StringComparison.Ordinal)) {
                var modeInfoResponse = ParseModeInfoResponse(response.Substring(MODEINFORESPONSE.Length));
                if(modeInfoResponse != null) {
                    Log("event: GetModeInfoResponse");
                    ModeInfoChanged?.Invoke(this, modeInfoResponse);
                }
            }
        }

        private GetModeInfoResponse ParseModeInfoResponse(string response) {
            var data = response.Split(",");
            if(data.Length < 21) {
                throw new InvalidDataException("invalid GetModeInfoResponse");
            }
            return LogResponse(new GetModeInfoResponse {
                InputStatus = data[0] switch {
                    "0" => RadianceProInputStatus.NoSource,
                    "1" => RadianceProInputStatus.ActiveVideo,
                    "2" => RadianceProInputStatus.InternalPattern,
                    string invalid => throw new InvalidDataException($"invalid input status: {invalid}")
                },
                SourceVerticalRate = data[1],
                SourceVerticalResolution = data[2],
                Source3DMode = data[3] switch {
                    "0" => RadiancePro3D.Off,
                    "1" => RadiancePro3D.FrameSequential,
                    "2" => RadiancePro3D.FramePacked,
                    "4" => RadiancePro3D.TopBottom,
                    "8" => RadiancePro3D.SideBySide,
                    string invalid => throw new InvalidDataException($"invalid source 3D mode: {invalid}")
                },
                ActiveInputConfigNumber = data[4],
                SourceRasterAspectRatio = data[5],
                SourceContentAspectRatio = data[6],
                OutputNonLinearStretchActive = data[7] switch {
                    "-" => false,
                    "N" => true,
                    string invalid => throw new InvalidDataException($"invalid NLS mode: {invalid}")
                },
                Output3DMode = data[8] switch {
                    "0" => RadiancePro3D.Off,
                    "1" => RadiancePro3D.FrameSequential,
                    "2" => RadiancePro3D.FramePacked,
                    "4" => RadiancePro3D.TopBottom,
                    "8" => RadiancePro3D.SideBySide,
                    string invalid => throw new InvalidDataException($"invalid source 3D mode: {invalid}")
                },
                OutputEnabled = ushort.Parse(data[9], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                OutputCms = data[10] switch {
                    "0" => RadianceProCms.Cms0,
                    "1" => RadianceProCms.Cms1,
                    "2" => RadianceProCms.Cms2,
                    "3" => RadianceProCms.Cms3,
                    "4" => RadianceProCms.Cms4,
                    "5" => RadianceProCms.Cms5,
                    "6" => RadianceProCms.Cms6,
                    "7" => RadianceProCms.Cms7,
                    string invalid => throw new InvalidDataException($"invalid output cms: {invalid}")
                },
                OutputStyle = data[11] switch {
                    "0" => RadianceProStyle.Style0,
                    "1" => RadianceProStyle.Style1,
                    "2" => RadianceProStyle.Style2,
                    "3" => RadianceProStyle.Style3,
                    "4" => RadianceProStyle.Style4,
                    "5" => RadianceProStyle.Style5,
                    "6" => RadianceProStyle.Style6,
                    "7" => RadianceProStyle.Style7,
                    string invalid => throw new InvalidDataException($"invalid output style: {invalid}")
                },
                OutputVerticalRate = data[12],
                OutputVerticalResolution = data[13],
                OutputAspectRatio = data[14],
                OutputColorSpace = data[15] switch {
                    "0" => RadianceProColorSpace.CS601,
                    "1" => RadianceProColorSpace.CS709,
                    "2" => RadianceProColorSpace.CS2020,
                    "3" => RadianceProColorSpace.CS2100,
                    string invalid => throw new InvalidDataException($"invalid output color space: {invalid}")
                },
                SourceDynamicRange = data[16] switch {
                    "0" => RadianceProDynamicRange.SDR,
                    "1" => RadianceProDynamicRange.HDR,
                    string invalid => throw new InvalidDataException($"invalid source dynamic range: {invalid}")
                },
                SourceVideoMode = data[17] switch {
                    "i" => RadianceProVideoMode.Interlaced,
                    "p" => RadianceProVideoMode.Progressive,
                    "-" => RadianceProVideoMode.NoVideo,
                    string invalid => throw new InvalidDataException($"invalid source video mode: {invalid}")
                },
                OutputVideoMode = data[18] switch {
                    "I" => RadianceProVideoMode.Interlaced,
                    "P" => RadianceProVideoMode.Progressive,
                    string invalid => throw new InvalidDataException($"invalid source video mode: {invalid}")
                },
                VirtualInputSelected = uint.Parse(data[19], NumberStyles.Integer, CultureInfo.InvariantCulture),
                PhysicalInputSelected = uint.Parse(data[20], NumberStyles.Integer, CultureInfo.InvariantCulture)
            });
        }

        private void Log(string message) {
            if(Verbose) {
                var escapedMessage = string.Join("", message.Select(c => c switch {
                    >= (char)32 and < (char)127 => ((char)c).ToString(),
                    '\n' => "\\n",
                    '\r' => "\\r",
                    _ => $"\\u{(int)c:X4}"
                }));
                Console.WriteLine($"{typeof(RadianceProClient).Name} {escapedMessage}");
            }
        }

        private T LogResponse<T>(T response) {
            if(Verbose) {
                var serializedResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions {
                    WriteIndented = true,
                    Converters = {
                        new JsonStringEnumConverter()
                    }
                });
                Console.WriteLine($"{typeof(RadianceProClient).Name} response: {serializedResponse}");
            }
            return response;
        }
    }
}
