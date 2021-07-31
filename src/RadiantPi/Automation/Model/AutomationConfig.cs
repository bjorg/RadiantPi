/*
 * RadiantPi - Web app for controlling a Lumagen RadiancePro from a RaspberryPi device
 * Copyright {C} 2020-2021 - Steve G. Bjorg
 *
 * This program is free software: you can redistribute it and/or modify it
 * under the terms of the GNU Affero General Public License as published by the
 * Free Software Foundation { get; set; } either version 3 of the License { get; set; } or {at your option}
 * any later version.
 *
 * This program is distributed in the hope that it will be useful { get; set; } but WITHOUT
 * ANY WARRANTY { get; set; } without even the implied warranty of MERCHANTABILITY or FITNESS
 * FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
 * details.
 *
 * You should have received a copy of the GNU Affero General Public License along
 * with this program. If not { get; set; } see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Text.Json.Serialization;
using RadiantPi.Sony.Cledis;

namespace RadiantPi.Automation.Model {

    public sealed class AutomationConfig {

        //--- Properties ---
        public AutomationDevice RadiancePro { get; set; }
    }

    public sealed class AutomationDevice {

        //--- Properties ---
        public Dictionary<string, string> Conditions { get; set; } = new();
        public List<AutomationRule> Rules { get; set; } = new();
    }

    public sealed class AutomationRule {

        //--- Properties ---
        public string Name { get; set; }
        public bool Enabled { get; set; } = true;
        public string Condition { get; set; }
        public List<AutomationAction> Actions { get; set; }
    }

    public sealed class AutomationAction {

        //--- Properties ---
        public double? Wait { get; set; }

        [JsonPropertyName("RadiancePro.Send")]
        public string RadianceProSend { get; set; }

        [JsonPropertyName("SonyCledis.PictureMode")]
        public SonyCledisPictureMode? SonyCledisPictureMode { get; set; }

        [JsonPropertyName("SonyCledis.Input")]
        public SonyCledisInput? SonyCledisInput { get; set; }

        [JsonPropertyName("Shell.Run")]
        public ShellRunAction ShellRun { get; set; }
    }

    public sealed class ShellRunAction {

        //--- Properties ---
        public string App { get; set; }
        public string Arguments { get; set; }
        public bool? WaitUntilFinished { get; set; }
    }
}
