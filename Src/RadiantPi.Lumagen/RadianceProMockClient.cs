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

namespace RadiantPi.Lumagen {
    public sealed class RadianceProMockClient : IRadiancePro {

        //--- Fields ---
        private bool _disposed;
        private Dictionary<string, string> _labels = new Dictionary<string, string>() {

            // default CMS labels
            [$"{RadianceProCms.Cms1}"] = "CMS1",
            [$"{RadianceProCms.Cms2}"] = "CMS2",
            [$"{RadianceProCms.Cms3}"] = "CMS3",
            [$"{RadianceProCms.Cms4}"] = "CMS4",
            [$"{RadianceProCms.Cms5}"] = "CMS5",
            [$"{RadianceProCms.Cms6}"] = "CMS6",
            [$"{RadianceProCms.Cms7}"] = "CMS7",
            [$"{RadianceProCms.Cms8}"] = "CMS8",

            // default custom mode labels
            [$"{RadianceProCustomMode.CustomMode1}"] = "Custom1",
            [$"{RadianceProCustomMode.CustomMode2}"] = "Custom2",
            [$"{RadianceProCustomMode.CustomMode3}"] = "Custom3",
            [$"{RadianceProCustomMode.CustomMode4}"] = "Custom4",
            [$"{RadianceProCustomMode.CustomMode5}"] = "Custom5",
            [$"{RadianceProCustomMode.CustomMode6}"] = "Custom6",
            [$"{RadianceProCustomMode.CustomMode7}"] = "Custom7",
            [$"{RadianceProCustomMode.CustomMode8}"] = "Custom8",

            // default input labels
            [$"{RadianceProMemory.MemoryA}-{RadianceProInput.Input1}"] = "Input",
            [$"{RadianceProMemory.MemoryB}-{RadianceProInput.Input1}"] = "Input",
            [$"{RadianceProMemory.MemoryC}-{RadianceProInput.Input1}"] = "Input",
            [$"{RadianceProMemory.MemoryD}-{RadianceProInput.Input1}"] = "Input",
            [$"{RadianceProMemory.MemoryA}-{RadianceProInput.Input2}"] = "Input",
            [$"{RadianceProMemory.MemoryB}-{RadianceProInput.Input2}"] = "Input",
            [$"{RadianceProMemory.MemoryC}-{RadianceProInput.Input2}"] = "Input",
            [$"{RadianceProMemory.MemoryD}-{RadianceProInput.Input2}"] = "Input",
            [$"{RadianceProMemory.MemoryA}-{RadianceProInput.Input3}"] = "Input",
            [$"{RadianceProMemory.MemoryB}-{RadianceProInput.Input3}"] = "Input",
            [$"{RadianceProMemory.MemoryC}-{RadianceProInput.Input3}"] = "Input",
            [$"{RadianceProMemory.MemoryD}-{RadianceProInput.Input3}"] = "Input",
            [$"{RadianceProMemory.MemoryA}-{RadianceProInput.Input4}"] = "Input",
            [$"{RadianceProMemory.MemoryB}-{RadianceProInput.Input4}"] = "Input",
            [$"{RadianceProMemory.MemoryC}-{RadianceProInput.Input4}"] = "Input",
            [$"{RadianceProMemory.MemoryD}-{RadianceProInput.Input4}"] = "Input",
            [$"{RadianceProMemory.MemoryA}-{RadianceProInput.Input5}"] = "Input",
            [$"{RadianceProMemory.MemoryB}-{RadianceProInput.Input5}"] = "Input",
            [$"{RadianceProMemory.MemoryC}-{RadianceProInput.Input5}"] = "Input",
            [$"{RadianceProMemory.MemoryD}-{RadianceProInput.Input5}"] = "Input",
            [$"{RadianceProMemory.MemoryA}-{RadianceProInput.Input6}"] = "Input",
            [$"{RadianceProMemory.MemoryB}-{RadianceProInput.Input6}"] = "Input",
            [$"{RadianceProMemory.MemoryC}-{RadianceProInput.Input6}"] = "Input",
            [$"{RadianceProMemory.MemoryD}-{RadianceProInput.Input6}"] = "Input",
            [$"{RadianceProMemory.MemoryA}-{RadianceProInput.Input7}"] = "Input",
            [$"{RadianceProMemory.MemoryB}-{RadianceProInput.Input7}"] = "Input",
            [$"{RadianceProMemory.MemoryC}-{RadianceProInput.Input7}"] = "Input",
            [$"{RadianceProMemory.MemoryD}-{RadianceProInput.Input7}"] = "Input",
            [$"{RadianceProMemory.MemoryA}-{RadianceProInput.Input8}"] = "Input",
            [$"{RadianceProMemory.MemoryB}-{RadianceProInput.Input8}"] = "Input",
            [$"{RadianceProMemory.MemoryC}-{RadianceProInput.Input8}"] = "Input",
            [$"{RadianceProMemory.MemoryD}-{RadianceProInput.Input8}"] = "Input",

            // default style labels
            [$"{RadianceProStyle.Style1}"] = "Style1",
            [$"{RadianceProStyle.Style2}"] = "Style2",
            [$"{RadianceProStyle.Style3}"] = "Style3",
            [$"{RadianceProStyle.Style4}"] = "Style4",
            [$"{RadianceProStyle.Style5}"] = "Style5",
            [$"{RadianceProStyle.Style6}"] = "Style6",
            [$"{RadianceProStyle.Style7}"] = "Style7",
            [$"{RadianceProStyle.Style8}"] = "Style8"
        };

        //--- Methods ---
        public Task<string> ReadCmsLabel(RadianceProCms cms) {
            CheckNotDisposed();
            return Task.FromResult(_labels[$"{cms}"]);
        }

        public Task<string> ReadCustomModeLabel(RadianceProCustomMode customMode) {
            CheckNotDisposed();
            return Task.FromResult(_labels[$"{customMode}"]);
        }

        public Task<string> ReadInputLabel(RadianceProMemory memory, RadianceProInput input) {
            CheckNotDisposed();
            return Task.FromResult(_labels[$"{memory}-{input}"]);
        }

        public Task<string> ReadStyleLabel(RadianceProStyle style) {
            CheckNotDisposed();
            return Task.FromResult(_labels[$"{style}"]);
        }

        public void Dispose() => _disposed = true;

        private void CheckNotDisposed() {
            if(_disposed) {
                throw new ObjectDisposedException("client was disposed");
            }
        }
    }
}
