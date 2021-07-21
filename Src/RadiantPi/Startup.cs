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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RadiantPi.Lumagen;
using RadiantPi.Sony.Cledis;
using RadiantPi.Sony.Cledis.Mock;

namespace RadiantPi {

    public class Startup {

        //--- Constructors ---
        public Startup(IConfiguration configuration) {
            Configuration = configuration;

            // create startup logger
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            ConsoleLogger = loggerFactory.CreateLogger<Startup>();
        }

        //--- Properties ---
        public IConfiguration Configuration { get; }
        public ILogger<Startup> ConsoleLogger { get; }

        //--- Methods ---
        public void ConfigureServices(IServiceCollection services) {

            // This method gets called by the runtime. Use this method to add services to the container.
            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
            services.AddRazorPages();
            services.AddServerSideBlazor();

            // add RadiancePro client
            services.AddSingleton<IRadiancePro>(services => {
                var radiancePro = Configuration.GetSection("RadiancePro");
                var config = radiancePro.Get<RadianceProClientConfig>();
                if((config is null) || config.Mock.GetValueOrDefault()) {

                    // default to mock configuration when no configuration is found
                    ConsoleLogger.LogWarning("using RadiancePro mock client configuration");
                    config = new RadianceProClientConfig {
                        Mock = true
                    };
                }
                var clientLogger = services.GetService<ILoggerFactory>().CreateLogger<RadianceProClient>();
                return RadianceProClient.Initialize(config, clientLogger);
            });

            // add Sony Cledis client
            services.AddSingleton<ISonyCledis>(services => {
                var sonyCledis = Configuration.GetSection("SonyCledis");
                var config = sonyCledis.Get<SonyCledisClientConfig>();
                if((config is null) || config.Mock.GetValueOrDefault()) {

                    // default to mock configuration when no configuration is found
                    ConsoleLogger.LogWarning("using Sony Cledis mock client configuration");
                    return new SonyCledisMockClient();
                }
                return new SonyCledisClient(config);
            });

            // add RadiancePro automation service
            services.AddHostedService<RadianceProAutomationService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {

            // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
            if(env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseExceptionHandler("/Error");
            }
            app.UseStaticFiles();
            app.UseRouting();
            app.UseEndpoints(endpoints => {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
