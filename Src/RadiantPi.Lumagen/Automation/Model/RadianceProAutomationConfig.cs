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

namespace RadiantPi.Lumagen.Automation.Model {

    public sealed class AutomationConfig {

        //--- Properties ---
        public Dictionary<string, string> Variables { get; set; } = new();
        public Dictionary<string, ModeChangedRule> Rules { get; set; } = new();
    }

    public sealed class ModeChangedRule {

        //--- Properties ---
        public string Condition { get; set; }
        public List<ModelChangedAction> Actions { get; set; }
    }

    public sealed class ModelChangedAction {

        //--- Properties ---
        public string Target { get; set; }
        public string Send { get; set; }
    }
}