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
        private class Condition {

            //--- Properties ---
            public string Name { get; set; }
            public string ConditionDefinition { get; set; }
            public ExpressionParser<ModeInfoDetails>.ExpressionDelegate Function { get; set; }
            public HashSet<string> Dependencies { get; set; }
        }

        private class Rule {

            //--- Properties ---
            public string Name { get; set; }
            public string ConditionDefinition { get; set; }
            public ExpressionParser<ModeInfoDetails>.ExpressionDelegate ConditionFunction { get; set; }
            public IEnumerable<ModelChangedAction> Actions { get; set; }
            public HashSet<string> Dependencies { get; set; }
        }

        //--- Fields ---
        private IRadiancePro _radianceProClient;
        private ISonyCledis _cledisClient;
        private ILogger _logger;
        private Dictionary<string, Condition> _conditions = new();
        private List<Rule> _rules = new();
        private ModeInfoDetails _lastModeInfo = new();

        //--- Constructors ---
        public AutomationController(IRadiancePro client, ISonyCledis cledisClient, AutomationConfig config, ILogger logger = null) {
            _radianceProClient = client ?? throw new ArgumentNullException(nameof(client));
            _cledisClient = cledisClient ?? throw new ArgumentNullException(nameof(cledisClient));
            _logger = logger;

            // process configuration
            CompileConditions(config?.Conditions);
            CompileRules(config?.Rules);

            // subscribe to mode-changed events
            if(_rules.Any()) {
                client.ModeInfoChanged += OnModeInfoChanged;
            }
        }

        //--- Methods ---
        private void CompileConditions(Dictionary<string, string> conditions) {
            if(!(conditions?.Any() ?? false)) {
                return;
            }

            // parse condition expressions
            foreach(var (conditionName, conditionDefinition) in conditions) {
                try {
                    var expression = ExpressionParser<ModeInfoDetails>.ParseExpression(conditionName, conditionDefinition, out var dependencies);

                    // verify that the condition does not depend on other conditions
                    foreach(var dependency in dependencies.Where(dependency => dependency.StartsWith("$"))) {
                        _logger?.LogError($"condition '{conditionName}' cannot depend on another condition: '{dependency}'");
                    }

                    // add compiled condition
                    _logger?.LogDebug($"compiled '{conditionName}' => {expression}");
                    _conditions.Add(conditionName, new() {
                        Name = conditionName,
                        ConditionDefinition = conditionDefinition,
                        Function = (ExpressionParser<ModeInfoDetails>.ExpressionDelegate)expression.Compile(),
                        Dependencies = dependencies
                    });
                } catch(Exception e) {
                    _logger?.LogError(e, $"error while adding condition '{conditionName}'");
                }
            }
        }

        private void CompileRules(List<ModeChangedRule> rules) {
            if(!(rules?.Any() ?? false)) {
                return;
            }

            // parse rules
            var ruleIndex = 0;
            foreach(var rule in rules) {
                ++ruleIndex;
                var ruleName = rule.Name ?? $"Rule #{ruleIndex}";
                try {
                    var expression = ExpressionParser<ModeInfoDetails>.ParseExpression(ruleName, rule.Condition, out var dependencies);

                    // flatten condition dependencies
                    var flattenedDependencies = new HashSet<string>();
                    foreach(var dependency in dependencies) {

                        // check if dependency is a condition
                        if(dependency.StartsWith("$")) {

                            // resolve dependencies for the condition
                            if(_conditions.TryGetValue(dependency, out var condition)) {

                                // add condition dependencies to rule dependencies
                                foreach(var conditionDependency in condition.Dependencies) {
                                    flattenedDependencies.Add(conditionDependency);
                                }
                            } else {
                                _logger?.LogError($"rule '{ruleName}' uses undefined condition: '{dependency}'");
                            }
                        } else {
                            flattenedDependencies.Add(dependency);
                        }
                    }

                    // add compiled rule
                    _logger?.LogDebug($"compiled '{rule.Condition}' => {expression}");
                    _rules.Add(new() {
                        Name = ruleName,
                        ConditionDefinition = rule.Condition,
                        ConditionFunction = (ExpressionParser<ModeInfoDetails>.ExpressionDelegate)expression.Compile(),
                        Actions = rule.Actions,
                        Dependencies = dependencies
                    });
                } catch(Exception e) {
                    _logger?.LogError(e, $"error while adding rule '{ruleName}'");
                }
            }
        }

        private async void OnModeInfoChanged(object sender, ModeInfoDetails modeInfo) {
            _logger?.LogDebug("event received");

            // evaluate all conditions
            var conditions = new Dictionary<string, bool>();
            conditions = _conditions
                .Select(condition => (Name: condition.Key, Value: condition.Value.Function(modeInfo, conditions)))
                .ToDictionary(kv => kv.Name, kv => kv.Value);
            var options = new JsonSerializerOptions {
                WriteIndented = true
            };
            _logger?.LogDebug($"conditions: {JsonSerializer.Serialize(conditions, options)}");

            // detect which properties changed from last mode-info change
            var changed = DetectChangedProperties(modeInfo, _lastModeInfo);
            if(!changed.Any()) {
                _logger?.LogDebug("no changes detected in event");
                return;
            }
            _logger?.LogDebug($"changed event properties: {string.Join(", ", changed.OrderBy(dependency => dependency))}");

            // evaluate all rules
            foreach(var rule in _rules) {

                // only evaluate a rule if it depends on changed property
                if(!rule.Dependencies.Any(dependency => changed.Contains(dependency))) {
                    continue;
                }

                // evaluate rule and run actions if the condition passes
                try {
                    var eval = rule.ConditionFunction(modeInfo, conditions);
                    _logger?.LogDebug($"rule '{rule.Name}': {rule.ConditionDefinition} ==> {eval}");
                    if(eval) {

                        // run all actions
                        _logger?.LogInformation($"matched rule '{rule.Name}'");
                        await EvaluateActions(rule.Name, rule.Actions).ConfigureAwait(false);
                    }
                } catch(Exception e) {
                    _logger?.LogError(e, $"error while evaluating rule '{rule.Name}'");
                    break;
                }
            }
            _lastModeInfo = modeInfo;
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

        private HashSet<string> DetectChangedProperties(ModeInfoDetails current, ModeInfoDetails last) {
            var result = new HashSet<string>();
            foreach(var property in typeof(ModeInfoDetails).GetProperties()) {
                var currentPropertyValue = property.GetValue(current);
                var lastPropertyValue = property.GetValue(last);
                if(!object.Equals(currentPropertyValue, lastPropertyValue)) {
                    result.Add(property.Name);
                }
            }
            return result;
        }

        //--- IDisposable Members ---
        void IDisposable.Dispose() {
            if(_rules.Any()) {
                _radianceProClient.ModeInfoChanged -= OnModeInfoChanged;
            }
        }
    }
}
