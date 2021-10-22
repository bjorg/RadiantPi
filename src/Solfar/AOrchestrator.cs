using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Solfar {

    public abstract class AOrchestrator {

        //--- Fields ---
        private Dictionary<Func<Task>, bool> _rules = new();
        private Channel<object> _channel = Channel.CreateUnbounded<object>();
        private TaskCompletionSource _taskCompletionSource = new();

        //--- Constructors ---
        protected AOrchestrator(ILogger logger = null) => Logger = logger;

        //--- Properties ---
        protected ILogger Logger { get; }

        //--- Abstract Methods ---
        protected abstract bool ProcessChanges(object change);
        protected abstract Task EvaluateChangeAsync();

        //--- Methods ---
        public virtual void NotifyOfChanges(object change) => _channel.Writer.TryWrite(change);

        public virtual void Start()
            => Task.Run((Func<Task>)(async () => {

                // process all changes in the channel
                await foreach(var change in _channel.Reader.ReadAllAsync()) {
                    if(ProcessChanges(change)) {
                        await EvaluateChangeAsync();
                    }
                }

                // signal the orchestrator is done
                _taskCompletionSource.SetResult();
            }));

        public virtual void Stop() => _channel.Writer.Complete();
        public virtual Task WaitAsync() => _taskCompletionSource.Task;

        protected virtual async Task DoAsync(string name, bool newState, Func<Task> callback) {
            if(callback is null) {
                throw new ArgumentNullException(nameof(callback));
            }
            if(newState && _rules.TryGetValue(callback, out var oldState) && !oldState) {
                Logger?.LogInformation($"Executing rule: {name}");
                await callback();
            }
            _rules[callback] = newState;
        }
    }
}
