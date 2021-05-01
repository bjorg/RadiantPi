/*
 * RadiantPi.Lumagen - Communication client for Lumagen RadiancePro
 * Copyright (C) 2020-2021 - Steve G. Bjorg
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

            // NOTE (2021-05-01, bjorg): ZQI23 keeps locking up the RadiancePro, using ZQI22 instead
            var response = await SendAsync("ZQI22", expectResponse: true);
            return ParseModeInfoResponse(response);
        }

        public async Task<string> GetInputLabelAsync(RadianceProMemory memory, RadianceProInput input)
            => SanitizeText(await SendAsync($"ZQS1{ToCommandCode(memory, allowAll: false)}{ToCommandCode(input)}", expectResponse: true), maxLength: 10);

        public Task SetInputLabelAsync(RadianceProMemory memory, RadianceProInput input, string value)
            => SendAsync("ZY524" + $"{ToCommandCode(memory, allowAll: true)}{ToCommandCode(input)}{SanitizeText(value, maxLength: 10)}" + "\r", expectResponse: false);

        public async Task<string> GetCustomModeLabelAsync(RadianceProCustomMode customMode)
            => SanitizeText(await SendAsync($"ZQS11{ToCommandCode(customMode)}", expectResponse: true), maxLength: 7);

        public Task SetCustomModeLabelAsync(RadianceProCustomMode customMode, string value)
            => SendAsync("ZY524" + $"1{ToCommandCode(customMode)}{SanitizeText(value, maxLength: 7)}" + "\r", expectResponse: false);

        public async Task<string> GetCmsLabelAsync(RadianceProCms cms)
            => SanitizeText(await SendAsync($"ZQS12{ToCommandCode(cms)}", expectResponse: true), maxLength: 8);

        public Task SetCmsLabelAsync(RadianceProCms cms, string value)
            => SendAsync("ZY524" + $"2{ToCommandCode(cms)}{SanitizeText(value, maxLength: 8)}" + "\r", expectResponse: false);

        public async Task<string> GetStyleLabelAsync(RadianceProStyle style)
            => SanitizeText(await SendAsync($"ZQS13{ToCommandCode(style)}", expectResponse: true), maxLength: 8);

        public Task SetStyleLabelAsync(RadianceProStyle style, string value)
            => SendAsync("ZY524" + $"3{ToCommandCode(style)}{SanitizeText(value, maxLength: 8)}" + "\r", expectResponse: false);

        // TODO:

        // ZY0M<CR>
        //  Set zoom factor to M: Where M can be 0-2 (or 0-7 if zoom is set for 5% steps)

        // ZY2MMMNNNOOOPPP<CR>
        //  Set output shrink parameters: MMM=top, NNN=left, OOO=bottom, PPP=right edge. Range is 0-255 for each.

        // ZQO03
        //  Output shrink: Returns (top,left,bottom,right) 000-255 pixels (decimal)

        public void Dispose() {
            Log("Dispose");
            _serialPort.DataReceived -= SerialDataReceived;
            _mutex.Dispose();
            if(_serialPort.IsOpen) {
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();
                _serialPort.Close();
            }
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

            // parse mode information event
            const string MODE_INFO_RESPONSE_V1 = "!I21,";
            const string MODE_INFO_RESPONSE_V2 = "!I22,";
            const string MODE_INFO_RESPONSE_V3 = "!I23,";
            if(
                response.StartsWith(MODE_INFO_RESPONSE_V1, StringComparison.Ordinal)
                || response.StartsWith(MODE_INFO_RESPONSE_V2, StringComparison.Ordinal)
                || response.StartsWith(MODE_INFO_RESPONSE_V3, StringComparison.Ordinal)
            ) {
                var modeInfoResponse = ParseModeInfoResponse(response.Substring(MODE_INFO_RESPONSE_V1.Length));
                if(modeInfoResponse != null) {
                    Log("event: GetModeInfoResponse");
                    ModeInfoChanged?.Invoke(this, modeInfoResponse);
                }
            }
        }

        private GetModeInfoResponse ParseModeInfoResponse(string response) {
            var data = response.Split(",");
            GetModeInfoResponse info = new();
            switch(data.Length) {
            case 21:

                // v3 data fields
                info.VirtualInputSelected = uint.Parse(data[19], NumberStyles.Integer, CultureInfo.InvariantCulture);
                info.PhysicalInputSelected = uint.Parse(data[20], NumberStyles.Integer, CultureInfo.InvariantCulture);
                goto case 19;
            case 19:

                // v2 data fields
                info.OutputColorSpace = data[15] switch {
                    "0" => RadianceProColorSpace.CS601,
                    "1" => RadianceProColorSpace.CS709,
                    "2" => RadianceProColorSpace.CS2020,
                    "3" => RadianceProColorSpace.CS2100,
                    string invalid => throw new InvalidDataException($"invalid output color space: {invalid}")
                };
                info.SourceDynamicRange = data[16] switch {
                    "0" => RadianceProDynamicRange.SDR,
                    "1" => RadianceProDynamicRange.HDR,
                    string invalid => throw new InvalidDataException($"invalid source dynamic range: {invalid}")
                };
                info.SourceVideoMode = data[17] switch {
                    "i" => RadianceProVideoMode.Interlaced,
                    "p" => RadianceProVideoMode.Progressive,
                    "-" => RadianceProVideoMode.NoVideo,

                    // TODO: waiting for response on what "n" actually means
                    "n" => RadianceProVideoMode.NoVideo,
                    string invalid => throw new InvalidDataException($"invalid source video mode: {invalid}")
                };
                info.OutputVideoMode = data[18] switch {
                    "I" => RadianceProVideoMode.Interlaced,
                    "P" => RadianceProVideoMode.Progressive,
                    string invalid => throw new InvalidDataException($"invalid source video mode: {invalid}")
                };
                goto case 15;
            case 15:

                // v1 data fields
                info.InputStatus = data[0] switch {
                    "0" => RadianceProInputStatus.NoSource,
                    "1" => RadianceProInputStatus.ActiveVideo,
                    "2" => RadianceProInputStatus.InternalPattern,
                    string invalid => throw new InvalidDataException($"invalid input status: {invalid}")
                };
                info.SourceVerticalRate = data[1];
                info.SourceVerticalResolution = data[2];
                info.Source3DMode = data[3] switch {
                    "0" => RadiancePro3D.Off,
                    "1" => RadiancePro3D.FrameSequential,
                    "2" => RadiancePro3D.FramePacked,
                    "4" => RadiancePro3D.TopBottom,
                    "8" => RadiancePro3D.SideBySide,
                    string invalid => throw new InvalidDataException($"invalid source 3D mode: {invalid}")
                };
                info.ActiveInputConfigNumber = data[4];
                info.SourceRasterAspectRatio = data[5];
                info.SourceContentAspectRatio = data[6];
                info.OutputNonLinearStretchActive = data[7] switch {
                    "-" => false,
                    "N" => true,
                    string invalid => throw new InvalidDataException($"invalid NLS mode: {invalid}")
                };
                info.Output3DMode = data[8] switch {
                    "0" => RadiancePro3D.Off,
                    "1" => RadiancePro3D.FrameSequential,
                    "2" => RadiancePro3D.FramePacked,
                    "4" => RadiancePro3D.TopBottom,
                    "8" => RadiancePro3D.SideBySide,
                    string invalid => throw new InvalidDataException($"invalid source 3D mode: {invalid}")
                };
                info.OutputEnabled = ushort.Parse(data[9], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                info.OutputCms = data[10] switch {
                    "0" => RadianceProCms.Cms0,
                    "1" => RadianceProCms.Cms1,
                    "2" => RadianceProCms.Cms2,
                    "3" => RadianceProCms.Cms3,
                    "4" => RadianceProCms.Cms4,
                    "5" => RadianceProCms.Cms5,
                    "6" => RadianceProCms.Cms6,
                    "7" => RadianceProCms.Cms7,
                    string invalid => throw new InvalidDataException($"invalid output cms: {invalid}")
                };
                info.OutputStyle = data[11] switch {
                    "0" => RadianceProStyle.Style0,
                    "1" => RadianceProStyle.Style1,
                    "2" => RadianceProStyle.Style2,
                    "3" => RadianceProStyle.Style3,
                    "4" => RadianceProStyle.Style4,
                    "5" => RadianceProStyle.Style5,
                    "6" => RadianceProStyle.Style6,
                    "7" => RadianceProStyle.Style7,
                    string invalid => throw new InvalidDataException($"invalid output style: {invalid}")
                };
                info.OutputVerticalRate = data[12];
                info.OutputVerticalResolution = data[13];
                info.OutputAspectRatio = data[14];
                break;
            default:
                throw new InvalidDataException("invalid GetModeInfoResponse");
            }
            return LogResponse(info);
        }

        private void Log(string message) {
            if(Verbose) {
                var escapedMessage = string.Join("", message.Select(c => c switch {
                    >= (char)32 and < (char)127 => ((char)c).ToString(),
                    '\n' => "\\n",
                    '\r' => "\\r",
                    _ => $"\\u{(int)c:X4}"
                }));

                // TODO: make configurable
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

                // TODO: make configurable
                Console.WriteLine($"{typeof(RadianceProClient).Name} response: {serializedResponse}");
            }
            return response;
        }
    }
}
