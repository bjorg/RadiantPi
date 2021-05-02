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
using RadiantPi.Lumagen;
using RadiantPi.Model;

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
                var radiancePro = Configuration.GetSection("RadiancePro");
                var config = radiancePro.Get<RadianceProClientConfig>();
                if((config == null) || (config.PortName == null) || config.Mock.GetValueOrDefault()) {

                    // default to mock configuration when no configuration is found
                    LogWarn("using RadiancePro mock client configuration");
                    config = new RadianceProClientConfig(
                        PortName: null,
                        BaudRate: null,
                        Mock: true,
                        Verbose: null
                    );
                }

                // initialize client
                var client = RadianceProClient.Initialize(config);

                // initialize client automation
                var automation = radiancePro.GetSection("Automation").Get<RadianceProAutomationConfig>();
                if(automation != null) {
                    RadianceProAutomation.New(client, automation);
                }
                return client;
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
