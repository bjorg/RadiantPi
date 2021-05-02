/*
 * RadiantPi - Web app for controlling a Lumagen RadiancePro from a RaspberryPi device
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
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using RadiantPi.Lumagen;
using RadiantPi.Lumagen.Model;
using RadiantPi.Model;

namespace RadiantPi {

    internal class RadianceProAutomation : IDisposable {

        //--- Class Methods ---
        public static RadianceProAutomation New(IRadiancePro client, RadianceProAutomationConfig config) {
            var automation = new RadianceProAutomation(client, config);

            // subscribe to mode-changed events
            if(config.ModeChangedRules != null) {
                client.ModeInfoChanged += automation.OnModeInfoChanged;
            }
            return automation;
        }

        //--- Fields ---
        private IRadiancePro _client;
        private RadianceProAutomationConfig _config;

        //--- Constructors ---
        private RadianceProAutomation(IRadiancePro client, RadianceProAutomationConfig config) {
            _client = client ?? throw new System.ArgumentNullException(nameof(client));
            _config = ValidateConfig(config ?? throw new System.ArgumentNullException(nameof(config)));
        }

        //--- Methods ---
        public async void OnModeInfoChanged(object sender, GetModeInfoResponse modeInfo) {

            // convert event details into a dictionary
            var modeChangedEvent = JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(modeInfo));
            var index = 0;
            foreach(var rule in _config.ModeChangedRules) {
                ++index;
                var ruleName = rule.Name ?? $"Rule {index:N0}";
                Log($"Evaluating rule '{ruleName}'");
                await EvaluateRule(ruleName, rule, modeChangedEvent);
            }
        }

        private RadianceProAutomationConfig ValidateConfig(RadianceProAutomationConfig config) {

            // TODO: missing
            //  check Condition.Key is not null
            return config;
        }

        private async Task EvaluateRule(string ruleName, ModeChangedRule rule, Dictionary<string, string> modeChangedEvent) {

            // check if all conditions are met
            if(rule.Conditions != null) {

                // check if all conditions are met
                var conditionIndex = 0;
                foreach(var condition in rule.Conditions) {
                    ++conditionIndex;

                    // check if condition field can be found
                    if(condition.Field == null) {
                        Log($"{ruleName}, condition {conditionIndex} failed: missing 'Field' value");
                        return;
                    }
                    if(!modeChangedEvent.TryGetValue(condition.Field, out var value)) {
                        Log($"{ruleName}, condition {conditionIndex} failed: field '{condition.Field}' not found in event");
                        return;
                    }

                    // check if value matches operation condition
                    switch(condition.Operation) {
                    case "Equals":
                    case null:
                        if(value != condition.Value) {
                            Log($"{ruleName}, condition {conditionIndex} failed: field '{condition.Field}' is not equal '{condition.Value}' (actual: '{value}')");
                            return;
                        }
                        break;
                    default:
                        Log($"{ruleName}, condition {conditionIndex} failed: unrecognized operation '{condition.Operation ?? "<null>"}'");
                        return;
                    }
                }
            }

            // apply all actions
            if(rule.Actions != null) {
                var actionIndex = 0;
                foreach(var action in rule.Actions) {
                    ++actionIndex;
                    if(action.Send == null) {
                        Log($"{ruleName}, action {actionIndex} skipped: missing 'Send' value'");
                        continue;
                    }
                    switch(action.Target) {
                    case "RadiancePro":
                    case null:
                        await _client.SendAsync(action.Send, expectResponse: false);
                        break;
                    default:
                        Log($"{ruleName}, action {actionIndex} skipped: unrecognized target '{action.Target ?? "<null>"}'");
                        return;
                    }
                }
            }
        }

        // TODO: make logging configurable
        private void Log(string message) => Console.WriteLine(message);

        //--- IDisposable Members ---
        void IDisposable.Dispose() {
            if(_config.ModeChangedRules != null) {
                _client.ModeInfoChanged -= OnModeInfoChanged;
            }
        }
    }
}
