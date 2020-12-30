/*
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
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RadiantPi.Lumagen {

    public sealed class RadianceProClient : IRadiancePro {

        //--- Class Methods ---
        private static string ToCommandCode(RadianceProMemory memory)
            => memory switch {
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
                RadianceProCustomMode.CustomMode1 => "0",
                RadianceProCustomMode.CustomMode2 => "1",
                RadianceProCustomMode.CustomMode3 => "2",
                RadianceProCustomMode.CustomMode4 => "3",
                RadianceProCustomMode.CustomMode5 => "4",
                RadianceProCustomMode.CustomMode6 => "5",
                RadianceProCustomMode.CustomMode7 => "6",
                RadianceProCustomMode.CustomMode8 => "7",
               _ => throw new ArgumentException("invalid custom mode selection")
            };

        private static string ToCommandCode(RadianceProCms cms)
            => cms switch {
                RadianceProCms.Cms1 => "0",
                RadianceProCms.Cms2 => "1",
                RadianceProCms.Cms3 => "2",
                RadianceProCms.Cms4 => "3",
                RadianceProCms.Cms5 => "4",
                RadianceProCms.Cms6 => "5",
                RadianceProCms.Cms7 => "6",
                RadianceProCms.Cms8 => "7",
               _ => throw new ArgumentException("invalid cms selection")
            };

        private static string ToCommandCode(RadianceProStyle style)
            => style switch {
                RadianceProStyle.Style1 => "0",
                RadianceProStyle.Style2 => "1",
                RadianceProStyle.Style3 => "2",
                RadianceProStyle.Style4 => "3",
                RadianceProStyle.Style5 => "4",
                RadianceProStyle.Style6 => "5",
                RadianceProStyle.Style7 => "6",
                RadianceProStyle.Style8 => "7",
               _ => throw new ArgumentException("invalid style selection")
            };

        //--- Fields ---
        private readonly SerialPort _serialPort;
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);

        //--- Constructors ---
        public RadianceProClient(SerialPort serialPort) {
            _serialPort = serialPort ?? throw new ArgumentNullException(nameof(serialPort));
            _serialPort.Open();
        }

        public RadianceProClient(string portName, int baudRate = 9600) : this(new SerialPort {
            PortName = portName,
            BaudRate = baudRate,
            DataBits = 8,
            Parity = Parity.None,
            StopBits = StopBits.One,
            Handshake = Handshake.None
        }) { }

        //--- Methods ---
        public Task PowerOnAsync() => SendAsync("%", expectResponse: false);
        public Task PowerOffAsync() => SendAsync("$", expectResponse: false);
        public Task<string> IsPoweredOnAsync() => SendAsync("ZQS02", expectResponse: true);

        public Task<string> ReadInputLabel(RadianceProMemory memory, RadianceProInput input) => SendAsync($"ZQS1{ToCommandCode(memory)}{ToCommandCode(input)}", expectResponse: true);
        public Task<string> ReadCustomModeLabel(RadianceProCustomMode customMode) => SendAsync($"ZQS11{ToCommandCode(customMode)}", expectResponse: true);
        public Task<string> ReadCmsLabel(RadianceProCms cms) => SendAsync($"ZQS12{ToCommandCode(cms)}", expectResponse: true);
        public Task<string> ReadStyleLabel(RadianceProStyle style) => SendAsync($"ZQS13{ToCommandCode(style)}", expectResponse: true);

        public void Dispose() {
            _mutex.Dispose();
            _serialPort.Dispose();
        }

        private async Task<string> SendAsync(string command, bool expectResponse) {
            var buffer = Encoding.UTF8.GetBytes(command);
            await _mutex.WaitAsync();
            try {
                var echo = command;
                TaskCompletionSource<string> responseSource = new();
                StringBuilder receiveBuffer = new();

                // send message, await echo response and optional response message
                try {
                    _serialPort.DataReceived += DataReceived;
                    await _serialPort.BaseStream.WriteAsync(buffer, 0, buffer.Length);
                    return await responseSource.Task;
                } finally {
                    _serialPort.DataReceived -= DataReceived;
                }

                // local functions
                void DataReceived(object sender, SerialDataReceivedEventArgs args) {
                    foreach(var c in _serialPort.ReadExisting()) {

                        // check if we're matching echoed characters
                        if(echo.Any()) {
                            if(c == echo.First()) {

                                // remove matched character
                                echo = echo.Substring(1);
                                continue;
                            }

                            // unexpected charater
                            responseSource.SetException(new IOException($"unexpected charater: expected '{echo[0]}', received '{c}'"));
                            return;
                        } else if(!expectResponse) {

                            // unexpected extra charater, that's not good!
                            responseSource.SetException(new IOException($"unexpected extra charater: '{c}'"));
                            return;
                        }

                        // add character to receive buffer
                        receiveBuffer.Append(c);
                    }

                    // check if we're done receiving data
                    if(echo.Any()) {

                        // more echo characters expected; keep going
                    } else if(!expectResponse) {

                        // all echo characters were matched; no additional data expected; indicate we're done with a 'null' response
                        responseSource.SetResult(null);
                    } else if(
                        (receiveBuffer.Length >= 2)
                        && (receiveBuffer[receiveBuffer.Length - 2] == '\r')
                        && (receiveBuffer[receiveBuffer.Length - 1] == '\n')
                    ) {

                        // remove trailing line terminator
                        receiveBuffer.Remove(receiveBuffer.Length - 2, 2);

                        // check if the response has a prologue
                        if(receiveBuffer[0] == '!') {
                            for(var i = 0; i < receiveBuffer.Length; ++i) {
                                if(receiveBuffer[i] == ',') {
                                    receiveBuffer.Remove(0, i + 1);
                                    break;
                                }
                            }
                        }

                        // terminator characters were received; indicate we're done by setting response
                        responseSource.SetResult(receiveBuffer.ToString());
                    }
                }
            } finally {
                _mutex.Release();
            }
        }
    }
}
