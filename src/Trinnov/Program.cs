using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RadiantPi.Trinnov.Altitude;

namespace Trinnov {

    public class Program {

        //--- Class Methods ---
        public static async Task Main(string[] args) {

            // define services
            var services = new ServiceCollection()
                .AddLogging(configure =>configure.AddConsole())
                .AddSingleton(services => new TrinnovAltitudeClientConfig {
                    Host = "192.168.1.180",
                    Port = 44100
                })
                .AddSingleton<ITrinnovAltitude, TrinnovAltitudeClient>()
                .AddSingleton<Program>();

            // execute program
            using var serviceProvider = services.BuildServiceProvider();
            await serviceProvider.GetRequiredService<Program>().Run();
        }

        //--- Fields ---
        private readonly ITrinnovAltitude _trinnovClient;

        //--- Constructors ---
        public Program(ITrinnovAltitude trinnovClient, ILogger<Program> logger = null) {
            _trinnovClient = trinnovClient ?? throw new ArgumentNullException(nameof(trinnovClient));
            Logger = logger;
        }

        //--- Properties ---
        public ILogger<Program> Logger { get; }

        //--- Methods ---
        public async Task Run() {

            // start listening to events
            _trinnovClient.AudioDecoderChanged += AudioCodecChanged;

            // connect and wait until user hits enter key
            await _trinnovClient.ConnectAsync();
            Console.ReadLine();

            // stop listening to events
            _trinnovClient.AudioDecoderChanged -= AudioCodecChanged;
        }

        private void AudioCodecChanged(object sender, AudioDecoderChangedEventArgs args)
            => Logger?.LogInformation($"==> AUDIO: Decoder='{args.Decoder}' Upmixer='{args.Upmixer}'");
    }
}
