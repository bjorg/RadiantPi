using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RadiantPi.Trinnov.Altitude;

namespace Trinnov {

    public class Program {

        //--= Types ---
        public class ConsoleLogger : ILogger {

            public IDisposable BeginScope<TState>(TState state) {
                throw new NotImplementedException();
            }

            public bool IsEnabled(LogLevel logLevel) {
                return logLevel >= LogLevel.Information;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
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

        //--- Class Methods ---
        public static async Task Main(string[] args) {
            var ip = "192.168.1.180";
            ushort port = 44100;
            var logger = new ConsoleLogger();

            // initialize Trinnov client
            using TrinnovAltitudeClient trinnovClient = new(new TrinnovAltitudeClientConfig {
                Host = ip,
                Port = port
            }, logger);
            trinnovClient.AudioDecoderChanged += AudioCodecChanged;
            await trinnovClient.ConnectAsync();

            // wait for keyboard input
            while(true) {
                var command = Console.ReadLine();
                switch(command) {
                case "q":
                case "quit":
                case "bye":
                    goto done;
                case "":
                    continue;
                }
                // await client.SendAsync(command);
            }
        done:
            trinnovClient.AudioDecoderChanged -= AudioCodecChanged;
        }

        private static void AudioCodecChanged(object sender, AudioDecoderChangedEventArgs args) {
            Console.WriteLine($"==> AUDIO: Decoder='{args.Decoder}' Upmixer='{args.Upmixer}'");
        }
    }
}
