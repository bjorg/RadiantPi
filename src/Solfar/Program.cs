using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RadiantPi.Lumagen;
using RadiantPi.Sony.Cledis;
using RadiantPi.Trinnov.Altitude;

namespace Solfar {

    public static class Program {

        //--- Class Methods ---
        public static async Task Main(string[] args) {
            var logger = new ConsoleLogger();

            // initialize clients
            logger.LogInformation("Initializing clients");
            using RadianceProClient radianceProClient = new(new() {
                PortName = "/dev/ttyUSB0",
                BaudRate = 9600
            }, logger);
            using SonyCledisClient cledisClient = new(new SonyCledisClientConfig {
                Host = "192.168.1.190",
                Port = 53595
            }, logger);
            using TrinnovAltitudeClient trinnovClient = new(new TrinnovAltitudeClientConfig {
                Host = "192.168.1.180",
                Port = 44100
            }, logger);

            // run orchestrator
            logger.LogInformation("Run orchestrator");
            SolfarController orchestrator = new(
                radianceProClient,
                cledisClient,
                trinnovClient,
                logger
            );
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
