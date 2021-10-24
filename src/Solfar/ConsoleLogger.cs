using System;
using Microsoft.Extensions.Logging;

namespace Solfar {

    internal class ConsoleLogger : ILogger {

        //--- ILogger Members ---
        IDisposable ILogger.BeginScope<TState>(TState state) {
            throw new NotImplementedException();
        }

        bool ILogger.IsEnabled(LogLevel logLevel) {
            return true;
            // return logLevel >= LogLevel.Information;
        }

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
            var foregroundColor = Console.ForegroundColor;
            try {
                switch(logLevel) {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogLevel.Information:
                default:

                    // nothing to do
                    break;
                }
                Console.WriteLine($"{logLevel.ToString().ToUpper()}: {formatter(state, exception)}");
            } finally {
                Console.ForegroundColor = foregroundColor;
            }
        }
    }
}
