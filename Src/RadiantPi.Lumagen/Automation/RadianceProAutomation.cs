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
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RadiantPi.Lumagen.Automation.Internal;
using RadiantPi.Lumagen.Automation.Model;
using RadiantPi.Lumagen.Model;

namespace RadiantPi.Lumagen.Automation {

    public class RadianceProAutomation : IDisposable {

        //--- Types ---
        private class Rule {

            //--- Properties ---
            public string Name { get; set; }
            public string ConditionDefinition { get; set; }
            public ExpressionParser<ModeInfoDetails>.ExpressionDelegate Condition { get; set; }
            public IEnumerable<ModelChangedAction> Actions { get; set; }
        }

        //--- Fields ---
        private IRadiancePro _client;
        private ILogger _logger;
        private Dictionary<string, ExpressionParser<ModeInfoDetails>.ExpressionDelegate> _variables = new();
        private List<Rule> _rules = new();

        //--- Constructors ---
        public RadianceProAutomation(IRadiancePro client, AutomationConfig config, ILogger logger) {
            _client = client ?? throw new System.ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // process configuration
            var variables = config?.Variables;
            var rules = config?.Rules;

            // parse variable expressions
            if(variables?.Any() ?? false) {
                foreach(var (variableName, variableDefinition) in variables) {
                    try {
                        _variables.Add(variableName, ExpressionParser<ModeInfoDetails>.ParseExpression(variableName, variableDefinition));
                    } catch(Exception e) {
                        _logger.LogError(e, $"error while adding variable '{variableName}'");
                    }
                }
            }

            // parse rules
            if(rules?.Any() ?? false) {
                var ruleIndex = 0;
                foreach(var rule in rules) {
                    ++ruleIndex;
                    var ruleName = rule.Name ?? $"Rule #{ruleIndex}";
                    try {
                        _rules.Add(new() {
                            Name = ruleName,
                            ConditionDefinition = rule.Condition,
                            Condition = ExpressionParser<ModeInfoDetails>.ParseExpression(ruleName, rule.Condition),
                            Actions = rule.Actions
                        });
                    } catch(Exception e) {
                        _logger.LogError(e, $"error while adding rule '{ruleName}'");
                    }
                }
            }

            // subscribe to mode-changed events
            if(_rules.Any()) {
                client.ModeInfoChanged += OnModeInfoChanged;
            }
        }

        //--- Methods ---
        public async void OnModeInfoChanged(object sender, ModeInfoDetails modeInfo) {
            _logger.LogDebug("event received");

            // create environment by evaluating all variables
            var environment = new Dictionary<string, bool>();
            environment = _variables
                .Select(variable => (Name: variable.Key, Value: variable.Value(modeInfo, environment)))
                .ToDictionary(kv => kv.Name, kv => kv.Value);
            var options = new JsonSerializerOptions {
                WriteIndented = true
            };
            _logger.LogDebug($"environment: {JsonSerializer.Serialize(environment, options)}");

            // find first rule that matches
            foreach(var rule in _rules) {
                try {
                    var eval = rule.Condition(modeInfo, environment);
                    _logger.LogDebug($"rule '{rule.Name}': {rule.ConditionDefinition} ==> {eval}");
                    if(eval) {

                        // apply all actions
                        _logger.LogInformation($"matched rule '{rule.Name}'");
                        await EvaluateActions(rule.Name, rule.Actions);
                        break;
                    }
                } catch(Exception e) {
                    _logger.LogError(e, $"error while evaluating rule '{rule.Name}'");
                    break;
                }
            }
        }

        private async Task EvaluateActions(string ruleName, IEnumerable<ModelChangedAction> actions) {
            if(actions?.Any() ?? false) {
                var actionIndex = 0;
                foreach(var action in actions) {
                    ++actionIndex;
                    if(action.Send == null) {
                        _logger.LogInformation($"{ruleName}, action {actionIndex} skipped: missing 'Send' value'");
                        continue;
                    }
                    switch(action.Target) {
                    case "RadiancePro":
                    case null:
                        _logger.LogInformation($"{ruleName}, action {actionIndex} sending command to 'RadiancePro': '{action.Send}'");
                        await _client.SendAsync(action.Send, expectResponse: false);
                        break;
                    default:
                        _logger.LogInformation($"{ruleName}, action {actionIndex} skipped: unrecognized target '{action.Target ?? "<null>"}'");
                        return;
                    }
                }
            } else {
                _logger.LogInformation($"{ruleName}: no actions");
            }
        }

        //--- IDisposable Members ---
        void IDisposable.Dispose() {
            if(_rules.Any()) {
                _client.ModeInfoChanged -= OnModeInfoChanged;
            }
        }
    }
}
