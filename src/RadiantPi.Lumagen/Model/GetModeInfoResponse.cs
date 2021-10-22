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

namespace RadiantPi.Lumagen.Model {

    // TODO: rename to `RadianceProDetails`
    public class ModeInfoDetails {

        // Radiance Pro only(release 041120 and later).
        // Full information query('Fullv4' for unsolicited status output):
        // Response= "!I23,M,RRR,VVVV,D,X,AAA,SSS,Y,T,WWWW,C,B,PPP,QQQQ,ZZZ,E,F,G,H,II,KK":
        // 00: M = Input status (0=no source, 1=active video, 2=internal pattern)
        // 01: RRR = Source vertical rate (e.g.059 for 59.94, 060 for 60.00)
        // 02: VVVV = Source vertical resolution (e.g. 1080 for 1080p)
        // 03: D = Source 3D mode (0,1,2,4,8)
        // 04: X = Active input config number for current input resolution
        // 05: AAA = Source raster aspect(e.g. 178 for HD or UHD)
        // 06: SSS = Source content aspect (e.g. 240 for 2.40)
        // 07: Y = NLS active ('-' for normal, 'N' for NLS)
        // 08: T = 3D output mode (0,1,2,4,8)
        // 09: WWWW = Output on. 16 bit hex, b0 to 15 for out 1 to 16. Bit=1 if on
        // 10: C = Active Output CMS (0 to 7)
        // 11: B = Active Output Style (0 to 7)
        // 12: PPP = Output vertical rate, (e.g.059 for 59.94, 060 for 60.00)
        // 13: QQQQ = Output vertical resolution (e.g. 1080 for 1080p)
        // 14: ZZZ = Output aspect (e.g. 178 for 16:9)
        // 15: E = Output Colorspace (0,1,2,3 for 601, 709, 2020, 2100 respectively)
        // 16: F = Source dynamic range (0 = SDR, 1= HDR)
        // 17: G = Source Mode ("i" = interlaced, "p"= progressive, "-" = no Input)
        // 18: H = Output Mode ("I" = interlaced, "P" = progressive)
        // 19: II = Virtual Input selected by remote/RS232 command (1 to 19)
        // 20: KK = Physical Input selected for current virtual input (1 to 19)
        // 20: JJJ = Detected input aspect, .ie 240 for 2.40

        //--- Properties ---
        public RadianceProInputStatus InputStatus { get; set; }
        public int VirtualInputSelected { get; set; }
        public int PhysicalInputSelected { get; set; }

        // Source Properties
        public string SourceVerticalRate { get; set; }
        public string SourceVerticalResolution { get; set; }
        public RadianceProVideoMode SourceVideoMode { get; set; }
        public RadiancePro3D Source3DMode { get; set; }
        public string SourceRasterAspectRatio { get; set; }
        public string SourceContentAspectRatio { get; set; }
        public RadianceProDynamicRange SourceDynamicRange { get; set; }

        // TODO: what type should this be?
        public string ActiveInputConfigNumber { get; set; }

        // Output Properties
        public bool OutputNonLinearStretchActive { get; set; }
        public RadiancePro3D Output3DMode { get; set; }
        public int OutputEnabled { get; set; }
        public RadianceProCms OutputCms { get; set; }
        public RadianceProStyle OutputStyle { get; set; }
        public string OutputVerticalRate { get; set; }
        public string OutputVerticalResolution { get; set; }
        public RadianceProVideoMode OutputVideoMode { get; set; }
        public string OutputAspectRatio { get; set; }
        public RadianceProColorSpace OutputColorSpace { get; set; }

        // Detected Properties
        public string DetectedAspectRatio { get; set; }

        // TODO: determine what this field really is in the v4 response
        public string DetectedRasterAspectRatio { get; set; }
    }

    public sealed class GetModeInfoResponse : ModeInfoDetails { }
}
