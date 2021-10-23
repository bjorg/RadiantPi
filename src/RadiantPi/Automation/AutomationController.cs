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
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RadiantPi.Automation.Internal;
using RadiantPi.Automation.Model;
using RadiantPi.Lumagen;
using RadiantPi.Lumagen.Model;
using RadiantPi.Sony.Cledis;
using RadiantPi.Sony.Cledis.Exceptions;

namespace RadiantPi.Automation {

    public class AutomationController : IDisposable {

        //--- Types ---
        private class Condition {

            //--- Properties ---
            public string Name { get; set; }
            public string ConditionDefinition { get; set; }
            public ExpressionParser<RadianceProModeInfo>.ExpressionDelegate Function { get; set; }
            public HashSet<string> Dependencies { get; set; }
        }

        private class Rule {

            //--- Properties ---
            public string Name { get; set; }
            public bool Enabled { get; set; }
            public string ConditionDefinition { get; set; }
            public ExpressionParser<RadianceProModeInfo>.ExpressionDelegate ConditionFunction { get; set; }
            public IEnumerable<AutomationAction> Actions { get; set; }
            public HashSet<string> Dependencies { get; set; }
        }

        //--- Class Methods ---
        private HashSet<string> DetectChangedProperties(RadianceProModeInfo current, RadianceProModeInfo last) {
            var result = new HashSet<string>();
            foreach(var property in typeof(RadianceProModeInfo).GetProperties()) {
                var currentPropertyValue = property.GetValue(current);
                var lastPropertyValue = property.GetValue(last);
                if(currentPropertyValue is not null) {
                    if(!currentPropertyValue.Equals(lastPropertyValue)) {
                        result.Add(property.Name);
                    }
                } else if(lastPropertyValue is not null) {
                    result.Add(property.Name);
                }
            }
            return result;
        }

        private static string DependenciesToString(IEnumerable<string> dependencies)
            => string.Join(", ", dependencies.OrderBy(dependency => dependency));

        //--- Fields ---
        private IRadiancePro _radianceProClient;
        private ISonyCledis _cledisClient;
        private ILogger _logger;
        private Dictionary<string, Condition> _conditions = new();
        private List<Rule> _rules = new();
        private RadianceProModeInfo _lastModeInfo = new();

        //--- Constructors ---
        public AutomationController(IRadiancePro client, ISonyCledis cledisClient, AutomationConfig config, ILogger logger = null) {
            _radianceProClient = client ?? throw new ArgumentNullException(nameof(client));
            _cledisClient = cledisClient ?? throw new ArgumentNullException(nameof(cledisClient));
            _logger = logger;

            // process configuration
            CompileConditions(config?.RadiancePro?.Conditions);
            CompileRules(config?.RadiancePro?.Rules);

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
                    var expression = ExpressionParser<RadianceProModeInfo>.ParseExpression(conditionName, conditionDefinition, out var dependencies);

                    // verify that the condition does not depend on other conditions
                    foreach(var dependency in dependencies.Where(dependency => dependency.StartsWith("$"))) {
                        _logger?.LogError($"condition '{conditionName}' cannot depend on another condition: '{dependency}'");
                    }

                    // add compiled condition
                    _logger?.LogDebug($"compiled condition '{conditionName}' => {expression}");
                    _logger?.LogDebug($"dependencices: {DependenciesToString(dependencies)}");
                    _conditions.Add(conditionName, new() {
                        Name = conditionName,
                        ConditionDefinition = conditionDefinition,
                        Function = (ExpressionParser<RadianceProModeInfo>.ExpressionDelegate)expression.Compile(),
                        Dependencies = dependencies
                    });
                } catch(Exception e) {
                    _logger?.LogError(e, $"error while adding condition '{conditionName}'");
                }
            }
        }

        private void CompileRules(List<AutomationRule> rules) {
            if(!(rules?.Any() ?? false)) {
                return;
            }

            // parse rules
            var ruleIndex = 0;
            foreach(var rule in rules) {
                ++ruleIndex;
                var ruleName = rule.Name ?? $"Rule #{ruleIndex}";
                try {
                    var expression = ExpressionParser<RadianceProModeInfo>.ParseExpression(ruleName, rule.Condition, out var dependencies);

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
                    _logger?.LogDebug($"compiled rule '{ruleName}' => {expression}");
                    _logger?.LogDebug($"dependencices: {DependenciesToString(flattenedDependencies)}");
                    _rules.Add(new() {
                        Name = ruleName,
                        Enabled = rule.Enabled,
                        ConditionDefinition = rule.Condition,
                        ConditionFunction = (ExpressionParser<RadianceProModeInfo>.ExpressionDelegate)expression.Compile(),
                        Actions = rule.Actions,
                        Dependencies = flattenedDependencies
                    });
                } catch(Exception e) {
                    _logger?.LogError(e, $"error while adding rule '{ruleName}'");
                }
            }
        }

        private async void OnModeInfoChanged(object sender, ModeInfoChangedEventArgs args) {
            var serializerOptions = new JsonSerializerOptions {
                WriteIndented = true,
                Converters = {
                    new JsonStringEnumConverter()
                }
            };
            _logger?.LogDebug($"event received: {JsonSerializer.Serialize(args.ModeInfo, serializerOptions)}");

            // evaluate all conditions
            var conditions = new Dictionary<string, bool>();
            conditions = _conditions
                .Select(condition => (Name: condition.Key, Value: condition.Value.Function(args.ModeInfo, conditions)))
                .ToDictionary(kv => kv.Name, kv => kv.Value);
            _logger?.LogTrace($"conditions: {JsonSerializer.Serialize(conditions, serializerOptions)}");

            // detect which properties changed from last mode-info change
            var changed = DetectChangedProperties(args.ModeInfo, _lastModeInfo);
            _lastModeInfo = args.ModeInfo;
            if(!changed.Any()) {
                _logger?.LogTrace("no changes detected in event");
                return;
            }
            _logger?.LogTrace($"changed event properties: {DependenciesToString(changed)}");

            // evaluate all rules
            foreach(var rule in _rules.Where(rule => rule.Enabled)) {

                // only evaluate a rule if it depends on changed property
                if(!rule.Dependencies.Any(dependency => changed.Contains(dependency))) {
                    _logger?.LogDebug($"rule '{rule.Name}' has no dependencies on changes");
                    continue;
                }

                // evaluate rule and run actions if the condition passes
                try {
                    var eval = rule.ConditionFunction(args.ModeInfo, conditions);
                    _logger?.LogDebug($"rule '{rule.Name}': {rule.ConditionDefinition} ==> {eval}");
                    if(eval) {

                        // dispatch all actions
                        _logger?.LogInformation($"dispatching actions for rule '{rule.Name}'");
                        await DispatchActions(rule.Name, rule.Actions).ConfigureAwait(false);
                    }
                } catch(Exception e) {
                    _logger?.LogError(e, $"error while evaluating rule '{rule.Name}'");
                    break;
                }
            }
        }

        private async Task DispatchActions(string ruleName, IEnumerable<AutomationAction> actions) {
            if(actions?.Any() ?? false) {
                var actionIndex = 0;
                foreach(var action in actions) {
                    ++actionIndex;

                    // check what command is requested
                    if(action.RadianceProSend is not null) {
                        _logger?.LogDebug($"RadiancePro.Send: {action.RadianceProSend}");
                        await _radianceProClient.SendAsync(Unescape(action.RadianceProSend), expectResponse: false).ConfigureAwait(false);
                    } else if(action.SonyCledisPictureMode is not null) {
                        _logger?.LogDebug($"SonyCledis.PictureMode: {action.SonyCledisPictureMode}");
                        try {
                            await _cledisClient.SetPictureModeAsync(action.SonyCledisPictureMode.Value).ConfigureAwait(false);
                        } catch(SonyCledisCommandInactiveException) {
                            _logger?.LogDebug($"Sony Cledis is turned off");
                        }
                    } else if(action.SonyCledisInput is not null) {
                        _logger?.LogDebug($"SonyCledis.Input: {action.SonyCledisInput}");
                        try {
                            await _cledisClient.SetInputAsync(action.SonyCledisInput.Value).ConfigureAwait(false);
                        } catch(SonyCledisCommandInactiveException) {
                            _logger?.LogDebug($"Sony Cledis is turned off");
                        }
                    } else if(action.ShellRun is not null) {
                        _logger?.LogDebug($"Shell.Run: {action.ShellRun}");
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
                        _logger?.LogDebug($"Wait: {action.Wait.Value:#0.00} seconds");
                        await Task.Delay(TimeSpan.FromSeconds(action.Wait.Value)).ConfigureAwait(false);
                    }
                }
                _logger?.LogDebug($"{ruleName}: actions done");
            } else {
                _logger?.LogDebug($"{ruleName}: no actions");
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
