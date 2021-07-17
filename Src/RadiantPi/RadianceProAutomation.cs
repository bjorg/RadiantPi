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
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RadiantPi.Lumagen;
using RadiantPi.Lumagen.Model;
using RadiantPi.Model;

namespace RadiantPi {

    internal class RadianceProAutomation : IDisposable {

        //--- Types ---
        private class StringConverter : JsonConverter<string> {

            //--- Methods ---
            public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                switch(reader.TokenType) {
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.Number:
                    var stringValue = reader.GetInt32();
                    return stringValue.ToString();
                case JsonTokenType.True:
                    return "true";
                case JsonTokenType.False:
                    return "false";
                case JsonTokenType.Null:
                    return "null";
                default:
                    throw new System.Text.Json.JsonException();
                }
            }

            public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
                => throw new NotSupportedException();
        }

        //--- Class Methods ---
        public static RadianceProAutomation New(IRadiancePro client, RadianceProAutomationConfig config, ILogger logger) {
            var automation = new RadianceProAutomation(client, config, logger);

            // subscribe to mode-changed events
            if(config.ModeChangedRules?.Any() ?? false) {
                client.ModeInfoChanged += automation.OnModeInfoChanged;
            }
            return automation;
        }

        //--- Fields ---
        private IRadiancePro _client;
        private RadianceProAutomationConfig _config;
        private ILogger _logger;

        //--- Constructors ---
        private RadianceProAutomation(IRadiancePro client, RadianceProAutomationConfig config, ILogger logger) {
            _client = client ?? throw new System.ArgumentNullException(nameof(client));
            _config = ValidateConfig(config ?? throw new System.ArgumentNullException(nameof(config)));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        //--- Methods ---
        public async void OnModeInfoChanged(object sender, GetModeInfoResponse modeInfo) {

            // convert event details into a dictionary
            var modeChangedEvent = JsonSerializer.Deserialize<Dictionary<string, string>>(
                JsonSerializer.Serialize(modeInfo),
                new JsonSerializerOptions {
                    Converters = {
                        new StringConverter()
                    }
                }
            );
            var index = 0;
            foreach(var rule in _config.ModeChangedRules) {
                ++index;
                var ruleName = rule.Name ?? $"Rule {index:N0}";
                await EvaluateRule(ruleName, rule, modeChangedEvent);
            }
        }

        private RadianceProAutomationConfig ValidateConfig(RadianceProAutomationConfig config) {

            // TODO: missing
            //  check Condition.Key is not null
            return config;
        }

        private async Task EvaluateRule(string ruleName, ModeChangedRule rule, Dictionary<string, string> modeChangedEvent) {
            List<string> conditionsMatched = new();

            // check if all conditions are met
            if(rule.Conditions != null) {

                // check if all conditions are met
                var conditionIndex = 0;
                foreach(var condition in rule.Conditions) {
                    ++conditionIndex;

                    // check if condition field can be found
                    if(condition.Field == null) {
                        LogInformation($"{ruleName}, condition {conditionIndex} failed: missing 'Field' value");
                        return;
                    }
                    if(!modeChangedEvent.TryGetValue(condition.Field, out var value)) {
                        LogInformation($"{ruleName}, condition {conditionIndex} failed: field '{condition.Field}' not found in event");
                        return;
                    }

                    // check if value matches operation condition
                    switch(condition.Operation) {
                    case "Equal":
                    case null:
                        if(string.Compare(value, condition.Value, StringComparison.Ordinal) != 0) {
                            LogInformation($"{ruleName}, condition {conditionIndex} failed: field '{condition.Field}'({value}) is not equal to '{condition.Value}'");
                            return;
                        }
                        conditionsMatched.Add($"'{condition.Field}' == '{condition.Value}'");
                        break;
                    case "NotEqual":
                        if(string.Compare(value, condition.Value, StringComparison.Ordinal) == 0) {
                            LogInformation($"{ruleName}, condition {conditionIndex} failed: field '{condition.Field}'({value}) is not equal to '{condition.Value}'");
                            return;
                        }
                        conditionsMatched.Add($"'{condition.Field}' == '{condition.Value}'");
                        break;
                    case "LessThan":
                        if(string.Compare(value, condition.Value, StringComparison.Ordinal) >= 0) {
                            LogInformation($"{ruleName}, condition {conditionIndex} failed: field '{condition.Field}'({value}) is not less than '{condition.Value}'");
                            return;
                        }
                        conditionsMatched.Add($"'{condition.Field}' < '{condition.Value}'");
                        break;
                    case "LessThanOrEqual":
                        if(string.Compare(value, condition.Value, StringComparison.Ordinal) > 0) {
                            LogInformation($"{ruleName}, condition {conditionIndex} failed: field '{condition.Field}'({value}) is not less than or equal to '{condition.Value}'");
                            return;
                        }
                        conditionsMatched.Add($"'{condition.Field}' <= '{condition.Value}'");
                        break;
                    case "GreaterThan":
                        if(string.Compare(value, condition.Value, StringComparison.Ordinal) <= 0) {
                            LogInformation($"{ruleName}, condition {conditionIndex} failed: field '{condition.Field}'({value}) is not greater than '{condition.Value}'");
                            return;
                        }
                        conditionsMatched.Add($"'{condition.Field}' > '{condition.Value}'");
                        break;
                    case "GreaterThanOrEqual":
                        if(string.Compare(value, condition.Value, StringComparison.Ordinal) < 0) {
                            LogInformation($"{ruleName}, condition {conditionIndex} failed: field '{condition.Field}'({value}) is not greater than or equal to '{condition.Value}'");
                            return;
                        }
                        conditionsMatched.Add($"'{condition.Field}' >= '{condition.Value}'");
                        break;
                    default:
                        LogInformation($"{ruleName}, condition {conditionIndex} failed: unrecognized operation '{condition.Operation ?? "<null>"}'");
                        return;
                    }
                }
            }

            // apply all actions
            if(rule.Actions != null) {
                var actionIndex = 0;
                if(conditionsMatched.Any()) {
                    LogInformation($"{ruleName} matched: {string.Join(", ", conditionsMatched)}");
                }
                foreach(var action in rule.Actions) {
                    ++actionIndex;
                    if(action.Send == null) {
                        LogInformation($"{ruleName}, action {actionIndex} skipped: missing 'Send' value'");
                        continue;
                    }
                    switch(action.Target) {
                    case "RadiancePro":
                    case null:
                        LogInformation($"{ruleName}, action {actionIndex} sending command to 'RadiancePro': '{action.Send}'");
                        await _client.SendAsync(action.Send, expectResponse: false);
                        break;
                    default:
                        LogInformation($"{ruleName}, action {actionIndex} skipped: unrecognized target '{action.Target ?? "<null>"}'");
                        return;
                    }
                }
            } else {
                LogInformation($"{ruleName}: no actions");
            }
        }

        private void LogInformation(string message) => _logger.LogInformation(message);

        //--- IDisposable Members ---
        void IDisposable.Dispose() {
            if(_config.ModeChangedRules?.Any() ?? false) {
                _client.ModeInfoChanged -= OnModeInfoChanged;
            }
        }
    }
}
