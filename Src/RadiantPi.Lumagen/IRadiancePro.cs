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
using System.Threading.Tasks;
using RadiantPi.Lumagen.Model;

namespace RadiantPi.Lumagen {

    public interface IRadiancePro : IDisposable {

        //--- Methods ---
        Task<GetInfoResponse> GetInfoAsync();
        Task<string> GetInputLabelAsync(RadianceProMemory memory, RadianceProInput input);
        Task SetInputLabelAsync(RadianceProMemory memory, RadianceProInput input, string value);
        Task<string> GetCustomModeLabelAsync(RadianceProCustomMode customMode);
        Task SetCustomModeLabelAsync(RadianceProCustomMode customMode, string value);
        Task<string> GetCmsLabelAsync(RadianceProCms cms);
        Task SetCmsLabelAsync(RadianceProCms cms, string value);
        Task<string> GetStyleLabelAsync(RadianceProStyle style);
        Task SetStyleLabelAsync(RadianceProStyle style, string value);
    }
}
