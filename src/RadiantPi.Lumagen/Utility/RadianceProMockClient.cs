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

// disable warning CS0067: The event 'RadianceProMockClient.ModeInfoChanged' is never used
#pragma warning disable CS0067

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RadiantPi.Lumagen.Model;

namespace RadiantPi.Lumagen.Utility {

    public sealed class RadianceProMockClient : IRadiancePro {

        //--- Class Methods ---
        private static string Truncate(string value, int maxLength) => value.Substring(0, Math.Min(value.Length, maxLength));

        //--- Fields ---
        private bool _disposed;
        private Dictionary<string, string> _labels = new Dictionary<string, string>() {

            // default CMS labels
            [$"{RadianceProCms.Cms0}"] = "CMS0",
            [$"{RadianceProCms.Cms1}"] = "CMS1",
            [$"{RadianceProCms.Cms2}"] = "CMS2",
            [$"{RadianceProCms.Cms3}"] = "CMS3",
            [$"{RadianceProCms.Cms4}"] = "CMS4",
            [$"{RadianceProCms.Cms5}"] = "CMS5",
            [$"{RadianceProCms.Cms6}"] = "CMS6",
            [$"{RadianceProCms.Cms7}"] = "CMS7",

            // default custom mode labels
            [$"{RadianceProCustomMode.CustomMode0}"] = "CUSTOM0",
            [$"{RadianceProCustomMode.CustomMode1}"] = "CUSTOM1",
            [$"{RadianceProCustomMode.CustomMode2}"] = "CUSTOM2",
            [$"{RadianceProCustomMode.CustomMode3}"] = "CUSTOM3",
            [$"{RadianceProCustomMode.CustomMode4}"] = "CUSTOM4",
            [$"{RadianceProCustomMode.CustomMode5}"] = "CUSTOM5",
            [$"{RadianceProCustomMode.CustomMode6}"] = "CUSTOM6",
            [$"{RadianceProCustomMode.CustomMode7}"] = "CUSTOM7",

            // default input labels
            [$"{RadianceProMemory.MemoryA}-{RadianceProInput.Input1}"] = "Input 1A",
            [$"{RadianceProMemory.MemoryB}-{RadianceProInput.Input1}"] = "Input 1B",
            [$"{RadianceProMemory.MemoryC}-{RadianceProInput.Input1}"] = "Input 1C",
            [$"{RadianceProMemory.MemoryD}-{RadianceProInput.Input1}"] = "Input 1D",
            [$"{RadianceProMemory.MemoryA}-{RadianceProInput.Input2}"] = "Input 2A",
            [$"{RadianceProMemory.MemoryB}-{RadianceProInput.Input2}"] = "Input 2B",
            [$"{RadianceProMemory.MemoryC}-{RadianceProInput.Input2}"] = "Input 2C",
            [$"{RadianceProMemory.MemoryD}-{RadianceProInput.Input2}"] = "Input 2D",
            [$"{RadianceProMemory.MemoryA}-{RadianceProInput.Input3}"] = "Input 3A",
            [$"{RadianceProMemory.MemoryB}-{RadianceProInput.Input3}"] = "Input 3B",
            [$"{RadianceProMemory.MemoryC}-{RadianceProInput.Input3}"] = "Input 3C",
            [$"{RadianceProMemory.MemoryD}-{RadianceProInput.Input3}"] = "Input 3D",
            [$"{RadianceProMemory.MemoryA}-{RadianceProInput.Input4}"] = "Input 4A",
            [$"{RadianceProMemory.MemoryB}-{RadianceProInput.Input4}"] = "Input 4B",
            [$"{RadianceProMemory.MemoryC}-{RadianceProInput.Input4}"] = "Input 4C",
            [$"{RadianceProMemory.MemoryD}-{RadianceProInput.Input4}"] = "Input 4D",
            [$"{RadianceProMemory.MemoryA}-{RadianceProInput.Input5}"] = "Input 5A",
            [$"{RadianceProMemory.MemoryB}-{RadianceProInput.Input5}"] = "Input 5B",
            [$"{RadianceProMemory.MemoryC}-{RadianceProInput.Input5}"] = "Input 5C",
            [$"{RadianceProMemory.MemoryD}-{RadianceProInput.Input5}"] = "Input 5D",
            [$"{RadianceProMemory.MemoryA}-{RadianceProInput.Input6}"] = "Input 6A",
            [$"{RadianceProMemory.MemoryB}-{RadianceProInput.Input6}"] = "Input 6B",
            [$"{RadianceProMemory.MemoryC}-{RadianceProInput.Input6}"] = "Input 6C",
            [$"{RadianceProMemory.MemoryD}-{RadianceProInput.Input6}"] = "Input 6D",
            [$"{RadianceProMemory.MemoryA}-{RadianceProInput.Input7}"] = "Input 7A",
            [$"{RadianceProMemory.MemoryB}-{RadianceProInput.Input7}"] = "Input 7B",
            [$"{RadianceProMemory.MemoryC}-{RadianceProInput.Input7}"] = "Input 7C",
            [$"{RadianceProMemory.MemoryD}-{RadianceProInput.Input7}"] = "Input 7D",
            [$"{RadianceProMemory.MemoryA}-{RadianceProInput.Input8}"] = "Input 8A",
            [$"{RadianceProMemory.MemoryB}-{RadianceProInput.Input8}"] = "Input 8B",
            [$"{RadianceProMemory.MemoryC}-{RadianceProInput.Input8}"] = "Input 8C",
            [$"{RadianceProMemory.MemoryD}-{RadianceProInput.Input8}"] = "Input 8D",

            // default style labels
            [$"{RadianceProStyle.Style0}"] = "STYLE0",
            [$"{RadianceProStyle.Style1}"] = "STYLE1",
            [$"{RadianceProStyle.Style2}"] = "STYLE2",
            [$"{RadianceProStyle.Style3}"] = "STYLE3",
            [$"{RadianceProStyle.Style4}"] = "STYLE4",
            [$"{RadianceProStyle.Style5}"] = "STYLE5",
            [$"{RadianceProStyle.Style6}"] = "STYLE6",
            [$"{RadianceProStyle.Style7}"] = "STYLE7"
        };

        //--- Events ---
        public event EventHandler<ModeInfoChangedEventArgs> ModeInfoChanged;

        //--- Methods ---
        public async Task<GetDeviceInfoResponse> GetDeviceInfoAsync()
            => new GetDeviceInfoResponse {
                ModelName = "RadianceXD",
                SoftwareRevision = "102308",
                ModelNumber = "1009",
                SerialNumber = "745"
            };

        public async Task<GetModeInfoResponse> GetModeInfoAsync()
            => new GetModeInfoResponse {
                InputStatus = RadianceProInputStatus.ActiveVideo,
                SourceVerticalRate = "023",
                SourceVerticalResolution = "1080",
                Source3DMode = RadiancePro3D.Off,
                ActiveInputConfigNumber = "1",
                SourceRasterAspectRatio = "178",
                SourceContentAspectRatio = "220",
                OutputNonLinearStretchActive = true,
                Output3DMode = RadiancePro3D.Off,
                OutputEnabled = 8,
                OutputCms = RadianceProCms.Cms0,
                OutputStyle = RadianceProStyle.Style0,
                OutputVerticalRate = "059",
                OutputVerticalResolution = "2160",
                OutputAspectRatio = "178",
                OutputColorSpace = RadianceProColorSpace.CS709,
                SourceDynamicRange = RadianceProDynamicRange.SDR,
                SourceVideoMode = RadianceProVideoMode.Progressive,
                OutputVideoMode = RadianceProVideoMode.Progressive,
                VirtualInputSelected = 1,
                PhysicalInputSelected = 1,
                DetectedRasterAspectRatio = "178",
                DetectedAspectRatio = "178"
            };

        public Task<string> GetInputLabelAsync(RadianceProMemory memory, RadianceProInput input) {
            CheckNotDisposed();
            return Task.FromResult(_labels[$"{memory}-{input}"]);
        }

        public Task SetInputLabelAsync(RadianceProMemory memory, RadianceProInput input, string value) {
            CheckNotDisposed();
            value = Truncate(value ?? throw new ArgumentNullException(nameof(value)), maxLength: 10);
            if(memory == RadianceProMemory.MemoryAll) {
                _labels[$"{RadianceProMemory.MemoryA}-{input}"] = value;
                _labels[$"{RadianceProMemory.MemoryB}-{input}"] = value;
                _labels[$"{RadianceProMemory.MemoryC}-{input}"] = value;
                _labels[$"{RadianceProMemory.MemoryD}-{input}"] = value;
            } else {
                _labels[$"{memory}-{input}"] = value;
            }
            return Task.CompletedTask;
        }

        public Task<string> GetCustomModeLabelAsync(RadianceProCustomMode customMode) {
            CheckNotDisposed();
            return Task.FromResult(_labels[$"{customMode}"]);
        }

        public Task SetCustomModeLabelAsync(RadianceProCustomMode customMode, string value) {
            CheckNotDisposed();
            value = Truncate(value ?? throw new ArgumentNullException(nameof(value)), maxLength: 7);
            _labels[$"{customMode}"] = value;
            return Task.CompletedTask;
        }

        public Task<string> GetCmsLabelAsync(RadianceProCms cms) {
            CheckNotDisposed();
            return Task.FromResult(_labels[$"{cms}"]);
        }

        public Task SetCmsLabelAsync(RadianceProCms cms, string value) {
            CheckNotDisposed();
            value = Truncate(value ?? throw new ArgumentNullException(nameof(value)), maxLength: 8);
            _labels[$"{cms}"] = value;
            return Task.CompletedTask;
        }

        public Task<string> GetStyleLabelAsync(RadianceProStyle style) {
            CheckNotDisposed();
            return Task.FromResult(_labels[$"{style}"]);
        }

        public Task SetStyleLabelAsync(RadianceProStyle style, string value) {
            CheckNotDisposed();
            value = Truncate(value ?? throw new ArgumentNullException(nameof(value)), maxLength: 8);
            _labels[$"{style}"] = value;
            return Task.CompletedTask;
        }

        public Task SelectMemoryAsync(RadianceProMemory memory) {
            CheckNotDisposed();
            return Task.CompletedTask;
        }

        public Task ShowMessageAsync(string message, int delay) {

            // TODO: why duplicate logic, can't we just use the non-mock client instead?

            CheckNotDisposed();
            if(message is null) {
                throw new ArgumentNullException(nameof(message));
            }
            if(message.Any(c => (c < 0x20) || (c > 0x7A))) {
                throw new ArgumentOutOfRangeException(nameof(message), "characters must be >= ' ' (0x20) and <= 'z' (0x7A)");
            }
            if(message.Length > 60) {
                throw new ArgumentOutOfRangeException(nameof(message), "string length must be <= 60 characters");
            }
            if((delay < 0) || (delay > 9)) {
                throw new ArgumentOutOfRangeException(nameof(delay), "value must be >= 0 and <= 9");
            }
            return SendAsync($"ZT{delay}{message}\r", expectResponse: false);
        }

        public Task ClearMessageAsync() {
            CheckNotDisposed();
            return SendAsync($"ZC", expectResponse: false);
        }

        public Task<string> SendAsync(string command, bool expectResponse)
            => Task.FromResult((string)null);

        public void Dispose() => _disposed = true;

        private void CheckNotDisposed() {

            // TODO: this check should be in the non-mock client as well

            if(_disposed) {
                throw new ObjectDisposedException("client was disposed");
            }
        }
    }
}
