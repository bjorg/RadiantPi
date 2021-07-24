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
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RadiantPi.Automation.Internal;
using RadiantPi.Automation.Model;
using RadiantPi.Lumagen;
using RadiantPi.Lumagen.Model;
using RadiantPi.Sony.Cledis;

namespace RadiantPi.Automation {

    public class AutomationController : IDisposable {

        //--- Types ---
        private class Rule {

            //--- Properties ---
            public string Name { get; set; }
            public string ConditionDefinition { get; set; }
            public ExpressionParser<ModeInfoDetails>.ExpressionDelegate Condition { get; set; }
            public IEnumerable<ModelChangedAction> Actions { get; set; }
        }

        //--- Fields ---
        private IRadiancePro _radianceProClient;
        private ISonyCledis _cledisClient;
        private ILogger _logger;
        private Dictionary<string, ExpressionParser<ModeInfoDetails>.ExpressionDelegate> _variables = new();
        private List<Rule> _rules = new();

        //--- Constructors ---
        public AutomationController(IRadiancePro client, ISonyCledis cledisClient, AutomationConfig config, ILogger logger = null) {
            _radianceProClient = client ?? throw new ArgumentNullException(nameof(client));
            _cledisClient = cledisClient ?? throw new ArgumentNullException(nameof(cledisClient));
            _logger = logger;

            // process configuration
            var variables = config?.Conditions;
            var rules = config?.Rules;

            // parse variable expressions
            if(variables?.Any() ?? false) {
                foreach(var (variableName, variableDefinition) in variables) {
                    try {
                        var expression = ExpressionParser<ModeInfoDetails>.ParseExpression(variableName, variableDefinition);
                        _logger?.LogDebug($"compiled '{variableName}' => {expression}");
                        _variables.Add(variableName, (ExpressionParser<ModeInfoDetails>.ExpressionDelegate)expression.Compile());
                    } catch(Exception e) {
                        _logger?.LogError(e, $"error while adding variable '{variableName}'");
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
                        var expression = ExpressionParser<ModeInfoDetails>.ParseExpression(ruleName, rule.Condition);
                        _logger?.LogDebug($"compiled '{rule.Condition}' => {expression}");
                        _rules.Add(new() {
                            Name = ruleName,
                            ConditionDefinition = rule.Condition,
                            Condition = (ExpressionParser<ModeInfoDetails>.ExpressionDelegate)expression.Compile(),
                            Actions = rule.Actions
                        });
                    } catch(Exception e) {
                        _logger?.LogError(e, $"error while adding rule '{ruleName}'");
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
            _logger?.LogDebug("event received");

            // create environment by evaluating all variables
            var environment = new Dictionary<string, bool>();
            environment = _variables
                .Select(variable => (Name: variable.Key, Value: variable.Value(modeInfo, environment)))
                .ToDictionary(kv => kv.Name, kv => kv.Value);
            var options = new JsonSerializerOptions {
                WriteIndented = true
            };
            _logger?.LogDebug($"environment: {JsonSerializer.Serialize(environment, options)}");

            // find first rule that matches
            foreach(var rule in _rules) {
                try {
                    var eval = rule.Condition(modeInfo, environment);
                    _logger?.LogDebug($"rule '{rule.Name}': {rule.ConditionDefinition} ==> {eval}");
                    if(eval) {

                        // apply all actions
                        _logger?.LogInformation($"matched rule '{rule.Name}'");
                        await EvaluateActions(rule.Name, rule.Actions).ConfigureAwait(false);
                    }
                } catch(Exception e) {
                    _logger?.LogError(e, $"error while evaluating rule '{rule.Name}'");
                    break;
                }
            }
        }

        private async Task EvaluateActions(string ruleName, IEnumerable<ModelChangedAction> actions) {
            if(actions?.Any() ?? false) {
                var actionIndex = 0;
                foreach(var action in actions) {
                    ++actionIndex;

                    // check what command is requested
                    if(action.RadianceProSend is not null) {
                        await _radianceProClient.SendAsync(Unescape(action.RadianceProSend), expectResponse: false).ConfigureAwait(false);
                    } else if(action.SonyCledisPictureMode is not null) {
                        await _cledisClient.SetPictureModeAsync(Enum.Parse<SonyCledisPictureMode>(action.SonyCledisPictureMode)).ConfigureAwait(false);
                    } else if(action.ShellRun is not null) {
                        if(action.ShellRun.WaitUntilFinished ?? true) {
                            await ShellRunAsync(action.ShellRun.App, action.ShellRun.Arguments).ConfigureAwait(false);
                        } else {
                            _ = ShellRunAsync(action.ShellRun.App, action.ShellRun.Arguments);
                        }
                    } else {
                        _logger?.LogWarning($"{ruleName}, action {actionIndex} skipped: unrecognized command");
                    }

                    // optional wait after command was run
                    if(action.Wait is not null) {
                        await Task.Delay(TimeSpan.FromSeconds(action.Wait.Value)).ConfigureAwait(false);
                    }
                }
            } else {
                _logger?.LogInformation($"{ruleName}: no actions");
            }

            // local functions
            string Unescape(string command) => Regex.Unescape(command);
        }

        private async Task ShellRunAsync(string app, string arguments) {
            var process = new Process {
                StartInfo = {
                    FileName = app,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };
            process.OutputDataReceived += (_, args) => _logger?.LogDebug($"shell [{app} {arguments}]: {args.Data}");
            process.ErrorDataReceived += (_, args) => _logger?.LogDebug($"shell [{app} {arguments}]: {args.Data}");
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync().ConfigureAwait(false);
        }

        //--- IDisposable Members ---
        void IDisposable.Dispose() {
            if(_rules.Any()) {
                _radianceProClient.ModeInfoChanged -= OnModeInfoChanged;
            }
        }
    }
}
