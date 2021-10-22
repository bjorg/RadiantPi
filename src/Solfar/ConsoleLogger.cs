using System;
using Microsoft.Extensions.Logging;

namespace Solfar {

    internal class ConsoleLogger : ILogger {

        //--- ILogger Members ---
        IDisposable ILogger.BeginScope<TState>(TState state) {
            throw new NotImplementedException();
        }

        bool ILogger.IsEnabled(LogLevel logLevel) {
            return logLevel >= LogLevel.Information;
        }

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
            if(logLevel >= LogLevel.Debug) {
                var foregroundColor = Console.ForegroundColor;
                try {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"{logLevel.ToString().ToUpper()}: {formatter(state, exception)}");
                } finally {
                    Console.ForegroundColor = foregroundColor;
                }
            }
        }
    }
}
