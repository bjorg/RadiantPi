/*
 * RadiantPi - Web app for controlling a Lumagen RadiancePro from a RaspberryPi device
 * Copyright (C) 2020 - Steve G. Bjorg
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

using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RadiantPi.Lumagen;

namespace RadiantPi {

    public class Startup {

        //--- Constructors ---
        public Startup(IConfiguration configuration) => Configuration = configuration;

        //--- Properties ---
        public IConfiguration Configuration { get; }

        //--- Methods ---
        public void ConfigureServices(IServiceCollection services) {

            // This method gets called by the runtime. Use this method to add services to the container.
            // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
            services.AddRazorPages();
            services.AddServerSideBlazor();

            // add RadiancePro client configuration
            services.AddSingleton<IRadiancePro>(_ => {
                var config = Configuration.GetSection("RadiancePro").Get<RadianceProClientConfig>();
                if(config == null) {

                    // default to mock configuration when no configuration is found
                    LogWarn("no 'RadiancePro' section found in appsettings.json file; defaulting to mock client configuration");
                    config = new RadianceProClientConfig {
                        Mock = true
                    };
                } else if(!config.Mock.GetValueOrDefault() && (config.PortName == null)) {

                    // find first available serial port
                    LogWarn("no 'PortName' property specified for RadiancePro section; defaulting to mock client configuration");
                    config = new RadianceProClientConfig {
                        Mock = true
                    };
                }
                return RadianceProClient.Initialize(config);
            });

            // local functions

            // TODO (12-31-2020, bjorg): would prefer to use ILogger, but it doesn't seem to be availble here
            void LogWarn(string message) => System.Console.WriteLine("WARNING: " + message);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {

            // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
            if(env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseExceptionHandler("/Error");

                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
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
