using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Solfar {

    public abstract class AOrchestrator {

        //--- Class Methods ---
        protected static bool LessThan(string left, string right) => StringComparer.Ordinal.Compare(left, right) < 0;
        protected static bool LessThanOrEqual(string left, string right) => StringComparer.Ordinal.Compare(left, right) <= 0;
        protected static bool Equal(string left, string right) => StringComparer.Ordinal.Compare(left, right) == 0;
        protected static bool GreaterThanOrEqual(string left, string right) => StringComparer.Ordinal.Compare(left, right) >= 0;
        protected static bool GreaterThan(string left, string right) => StringComparer.Ordinal.Compare(left, right) > 0;

        //--- Fields ---
        private Dictionary<Func<Task>, bool> _rules = new();
        private Channel<(object Sender, EventArgs EventArgs)> _channel = Channel.CreateUnbounded<(object Sender, EventArgs EventArgs)>();
        private TaskCompletionSource _taskCompletionSource = new();
        private List<(string Name, Func<Task> Action)> _triggeredActions = new();

        //--- Constructors ---
        protected AOrchestrator(ILogger logger = null) => Logger = logger;

        //--- Properties ---
        protected ILogger Logger { get; }

        //--- Abstract Methods ---
        protected abstract bool ApplyEvent(object sender, EventArgs change);
        protected abstract void Evaluate();

        //--- Methods ---
        public virtual void EventListener(object sender, EventArgs args) => _channel.Writer.TryWrite((Sender: sender, EventArgs: args));

        public virtual void Start()
            => Task.Run((Func<Task>)(async () => {

                // process all changes in the channel
                await foreach(var change in _channel.Reader.ReadAllAsync()) {
                    if(ApplyEvent(change.Sender, change.EventArgs)) {
                        await EvaluateChangeAsync().ConfigureAwait(false);
                    }
                }

                // signal the orchestrator is done
                _taskCompletionSource.SetResult();
            }));

        protected virtual async Task EvaluateChangeAsync() {
            _triggeredActions.Clear();
            Logger?.LogDebug($"Evaluating changes");
            Evaluate();
            Logger?.LogDebug($"Triggered {_triggeredActions.Count:N0} rules to execute");
            foreach(var triggered in _triggeredActions) {
                Logger?.LogInformation($"Executing rule '{triggered.Name}'");
                try {
                    await triggered.Action().ConfigureAwait(false);
                } catch(Exception e) {
                    Logger?.LogError(e, $"Exception while evaluating rule '{triggered.Name}'");
                }
            }
        }

        public virtual void Stop() => _channel.Writer.Complete();
        public virtual Task WaitAsync() => _taskCompletionSource.Task;

        protected virtual void OnTrue(string name, bool condition, Func<Task> callback) {
            if(callback is null) {
                throw new ArgumentNullException(nameof(callback));
            }

            // check if this rule is being seen for the first time
            if(_rules.TryGetValue(callback, out var oldState)) {
                if(condition) {
                    if(!oldState) {
                        Logger?.LogDebug($"Trigger rule '{name}': {oldState} --> {condition}");
                        _triggeredActions.Add((Name: name, Action: callback));
                    } else {
                        Logger?.LogDebug($"Ignore rule '{name}': {oldState} --> {condition}");
                    }
                } else {
                    Logger?.LogDebug($"Ignore rule '{name}': ??? --> {condition}");
                }
            } else {
                Logger?.LogDebug($"Record rule '{name}' for the first time (condition: {condition})");
            }

            // record current condition state
            _rules[callback] = condition;
        }
    }
}
