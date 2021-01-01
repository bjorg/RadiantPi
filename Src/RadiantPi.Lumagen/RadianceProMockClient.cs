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
using System.Collections.Generic;
using System.Threading.Tasks;
using RadiantPi.Lumagen.Model;

namespace RadiantPi.Lumagen {
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
            [$"{RadianceProCustomMode.CustomMode0}"] = "Custom0",
            [$"{RadianceProCustomMode.CustomMode1}"] = "Custom1",
            [$"{RadianceProCustomMode.CustomMode2}"] = "Custom2",
            [$"{RadianceProCustomMode.CustomMode3}"] = "Custom3",
            [$"{RadianceProCustomMode.CustomMode4}"] = "Custom4",
            [$"{RadianceProCustomMode.CustomMode5}"] = "Custom5",
            [$"{RadianceProCustomMode.CustomMode6}"] = "Custom6",
            [$"{RadianceProCustomMode.CustomMode7}"] = "Custom7",

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
            [$"{RadianceProStyle.Style0}"] = "Style0",
            [$"{RadianceProStyle.Style1}"] = "Style1",
            [$"{RadianceProStyle.Style2}"] = "Style2",
            [$"{RadianceProStyle.Style3}"] = "Style3",
            [$"{RadianceProStyle.Style4}"] = "Style4",
            [$"{RadianceProStyle.Style5}"] = "Style5",
            [$"{RadianceProStyle.Style6}"] = "Style6",
            [$"{RadianceProStyle.Style7}"] = "Style7"
        };

        //--- Methods ---
        public async Task<GetInfoResponse> GetInfoAsync()
            => new GetInfoResponse {
                ModelName = "RadianceXD",
                SoftwareRevision = "102308",
                ModelNumber = "1009",
                SerialNumber = "745"
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

        public void Dispose() => _disposed = true;

        private void CheckNotDisposed() {
            if(_disposed) {
                throw new ObjectDisposedException("client was disposed");
            }
        }
    }
}
