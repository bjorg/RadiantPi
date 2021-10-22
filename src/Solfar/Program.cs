using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RadiantPi.Lumagen;
using RadiantPi.Sony.Cledis;

namespace Solfar {

    public static class Program {

        //--- Class Properties ---
        private static ILogger Logger { get; } = new ConsoleLogger();

        //--- Class Methods ---
        public static async Task Main(string[] args) {

            // initialize clients
            Logger.LogInformation("Initializing clients");
            using var radianceProClient = RadianceProClient.Initialize(new() {
                PortName = "/dev/ttyUSB0",
                BaudRate = 9600
            }, Logger);
            using var cledisClient = SonyCledisClient.Initialize(new() {
                Host = "192.168.1.190",
                Port = 53595
            }, Logger);

            // run orchestrator
            Logger.LogInformation("Run orchestrator");
            SolfarOrchestrator orchestrator = new(radianceProClient, cledisClient, Logger);
            orchestrator.Start();
            _ = Task.Run(() => {
                Console.ReadLine();
                orchestrator.Stop();
            });

            // wait until orchestrator finishes
            await orchestrator.WaitAsync();
        }
    }
}
