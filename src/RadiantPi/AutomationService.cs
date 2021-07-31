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

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RadiantPi.Internal;
using RadiantPi.Lumagen;
using RadiantPi.Automation;
using RadiantPi.Automation.Model;
using RadiantPi.Sony.Cledis;
using System.Text.Json;
using System.IO;
using System.Text.Json.Serialization;

namespace RadiantPi {

    public class AutomationService : BackgroundService {

        //--- Fields ---
        private readonly ILogger<AutomationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IRadiancePro _radianceProClient;
        private readonly ISonyCledis _cledisClient;

        //--- Constructors ---
        public AutomationService(IConfiguration configuration, IRadiancePro radianceProClient, ISonyCledis cledisClient, ILogger<AutomationService> logger) {
            _configuration = configuration ?? throw new System.ArgumentNullException(nameof(configuration));
            _radianceProClient = radianceProClient ?? throw new System.ArgumentNullException(nameof(radianceProClient));
            _cledisClient = cledisClient ?? throw new System.ArgumentNullException(nameof(cledisClient));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        //--- Methods ---
        protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
            _logger.LogInformation("starting");

            // initialize client automation
            var automationFile = _configuration.GetValue<string>("Automation");
            var jsonOptions = new JsonSerializerOptions {
                Converters = {
                    new JsonStringEnumConverter()
                }
            };
            var automationConfig = JsonSerializer.Deserialize<AutomationConfig>(File.ReadAllText(automationFile), jsonOptions);
            if(automationConfig is not null) {
                using var automation = new AutomationController(_radianceProClient, _cledisClient, automationConfig, _logger);

                // initiate client events
                _logger.LogInformation("get current video mode");
                await _radianceProClient.GetModeInfoAsync();

                // wait until  we're requested to stop
                cancellationToken.Register(() => _logger.LogInformation("received stop signal"));
                await cancellationToken;
                _logger.LogInformation("stopping");
            } else {

                // nothing else to do, but wait
                _logger.LogInformation("no automation configuration found");
                await cancellationToken;
            }
        }
    }
}
