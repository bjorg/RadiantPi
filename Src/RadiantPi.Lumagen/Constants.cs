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

namespace RadiantPi.Lumagen {

    public enum RadianceProMemory {
        Undefined,
        MemoryAll,
        MemoryA,
        MemoryB,
        MemoryC,
        MemoryD
    }

    public enum RadianceProInput {
        Undefined,
        Input1,
        Input2,
        Input3,
        Input4,
        Input5,
        Input6,
        Input7,
        Input8
    }

    public enum RadianceProCustomMode {
        Undefined,
        CustomMode0,
        CustomMode1,
        CustomMode2,
        CustomMode3,
        CustomMode4,
        CustomMode5,
        CustomMode6,
        CustomMode7
    }

    public enum RadianceProCms {
        Undefined,
        Cms0,
        Cms1,
        Cms2,
        Cms3,
        Cms4,
        Cms5,
        Cms6,
        Cms7
    }

    public enum RadianceProStyle {
        Undefined,
        Style0,
        Style1,
        Style2,
        Style3,
        Style4,
        Style5,
        Style6,
        Style7
    }

    public enum RadianceProInputStatus {
        Undefined,
        NoSource,
        ActiveVideo,
        InternalPattern
    }

    public enum RadiancePro3D {
        Undefined,
        Off,
        FrameSequential,
        FramePacked,
        TopBottom,
        SideBySide
    }

    public enum RadianceProColorSpace {
        Undefined,
        CS601,
        CS709,
        CS2020,
        CS2100
    }

    public enum RadianceProDynamicRange {
        Undefined,
        SDR,
        HDR
    }

    public enum RadianceProVideoMode {
        Undefined,
        NoVideo,
        Interlaced,
        Progressive,
    }
}
