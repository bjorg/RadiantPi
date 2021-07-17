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
using RadiantPi.Lumagen.Automation;
using RadiantPi.Lumagen.Automation.Model;

namespace RadiantPi {

    public class RadianceProAutomationService : BackgroundService {

        //--- Fields ---
        private readonly ILogger<RadianceProAutomationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IRadiancePro _client;

        //--- Constructors ---
        public RadianceProAutomationService(ILogger<RadianceProAutomationService> logger, IConfiguration configuration, IRadiancePro client) {
            _logger = logger;
            _configuration = configuration;
            _client = client;
        }

        //--- Methods ---
        protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
            _logger.LogInformation("starting");

            // initialize client automation
            var automationConfig = _configuration
                .GetSection("RadiancePro")
                .GetSection("Automation")
                .Get<AutomationConfig>();
            if(automationConfig != null) {
                using var automation = new RadianceProAutomation(_client, automationConfig, _logger);

                // initiate client events
                _logger.LogInformation("get current video mode");
                await _client.GetModeInfoAsync();

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
