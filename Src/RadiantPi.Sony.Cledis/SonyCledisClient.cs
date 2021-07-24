using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RadiantPi.Sony.Cledis.Exceptions;
using RadiantPi.Telnet;

namespace RadiantPi.Sony.Cledis {

    public class SonyCledisClientConfig {

        //--- Properties ---
        public string Host { get; set; }
        public ushort Port { get; set; }
        public bool? Mock { get; set; }
    }

    public class SonyCledisClient : ISonyCledis {

        //--- Fields ---
        private readonly ITelnet _telnet;
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);
        private readonly ILogger _logger;

        //--- Constructors ---
        public SonyCledisClient(SonyCledisClientConfig config, ILogger logger = null) : this(new TelnetClient(config.Host, config.Port, logger), logger) { }

        public SonyCledisClient(ITelnet telnet, ILogger logger) {
            _telnet = telnet ?? throw new ArgumentNullException(nameof(telnet));
            _telnet.ConfirmConnectionAsync = ConfirmConnectionAsync;
            _logger = logger;
        }

        //--- Methods ---
        public async Task<string> GetModelNameAsync()
            => ConvertResponse<string>(await SendAsync("modelname ?"));

        public async Task<IEnumerable<string>> GetModelNameListAsync() => throw new NotImplementedException();

        public async Task<long> GetSerialNumberAsync()
            => ConvertResponse<long>(await SendAsync("serialnum ?"));

        public async Task<IEnumerable<string>> GetSerialNumberListAsync() => throw new NotImplementedException();

        public async Task<IEnumerable<Dictionary<string, string>>> GetVersionAsync() => throw new NotImplementedException();

        public async Task<SonyCledisTemperatures> GetTemperatureAsync()
            => ConvertTemperatureFromJson(await SendAsync("temperature ?"));

        public async Task<SonyCledisPowerStatus> GetPowerStatusAsync()
            => ConvertResponse<string>(await SendAsync("power_status ?")) switch {
                "standby" => SonyCledisPowerStatus.StandBy,
                "on" => SonyCledisPowerStatus.On,
                "updating" => SonyCledisPowerStatus.Updating,
                "startup" => SonyCledisPowerStatus.Startup,
                "shutting_down" => SonyCledisPowerStatus.ShuttingDown,
                "initializing" => SonyCledisPowerStatus.Initializing,
                var value => throw new SonyCledisUnrecognizedResponseException(value)
            };

        public Task SetPowerAsync(SonyCledisPower power)
            => power switch {
                SonyCledisPower.On => SendCommandAsync("power \"on\""),
                SonyCledisPower.Off => SendCommandAsync("power \"off\""),
                _ => throw new ArgumentException("invalid value", nameof(power))
            };

        public async Task<SonyCledisInput> GetInputAsync()
            => ConvertResponse<string>(await SendAsync("input ?")) switch {
                "dp1" => SonyCledisInput.DisplayPort1,
                "dp2" => SonyCledisInput.DisplayPort2,
                "dp1_2" => SonyCledisInput.DisplayPortBoth,
                "hdmi1" => SonyCledisInput.Hdmi1,
                "hdmi2" => SonyCledisInput.Hdmi2,
                var value => throw new SonyCledisUnrecognizedResponseException(value)
            };

        public Task SetInputAsync(SonyCledisInput input)
            => input switch {
                SonyCledisInput.DisplayPort1 => SendCommandAsync("input \"dp1\""),
                SonyCledisInput.DisplayPort2 => SendCommandAsync("input \"dp2\""),
                SonyCledisInput.DisplayPortBoth => SendCommandAsync("input \"dp1_2\""),
                SonyCledisInput.Hdmi1 => SendCommandAsync("input \"hdmi1\""),
                SonyCledisInput.Hdmi2 => SendCommandAsync("input \"hdmi2\""),
                _ => throw new ArgumentException("invalid value", nameof(input))
            };

        public async Task<SonyCledisPictureMode> GetPictureModeAsync()
            => ConvertResponse<string>(await SendAsync("picture_mode ?")) switch {
                "mode1" => SonyCledisPictureMode.Mode1,
                "mode2" => SonyCledisPictureMode.Mode2,
                "mode3" => SonyCledisPictureMode.Mode3,
                "mode4" => SonyCledisPictureMode.Mode4,
                "mode5" => SonyCledisPictureMode.Mode5,
                "mode6" => SonyCledisPictureMode.Mode6,
                "mode7" => SonyCledisPictureMode.Mode7,
                "mode8" => SonyCledisPictureMode.Mode8,
                "mode9" => SonyCledisPictureMode.Mode9,
                "mode10" => SonyCledisPictureMode.Mode10,
                var value => throw new SonyCledisUnrecognizedResponseException(value)
            };

        public Task SetPictureModeAsync(SonyCledisPictureMode mode)
            => mode switch {
                SonyCledisPictureMode.Mode1 => SendCommandAsync("picture_mode \"mode1\""),
                SonyCledisPictureMode.Mode2 => SendCommandAsync("picture_mode \"mode2\""),
                SonyCledisPictureMode.Mode3 => SendCommandAsync("picture_mode \"mode3\""),
                SonyCledisPictureMode.Mode4 => SendCommandAsync("picture_mode \"mode4\""),
                SonyCledisPictureMode.Mode5 => SendCommandAsync("picture_mode \"mode5\""),
                SonyCledisPictureMode.Mode6 => SendCommandAsync("picture_mode \"mode6\""),
                SonyCledisPictureMode.Mode7 => SendCommandAsync("picture_mode \"mode7\""),
                SonyCledisPictureMode.Mode8 => SendCommandAsync("picture_mode \"mode8\""),
                SonyCledisPictureMode.Mode9 => SendCommandAsync("picture_mode \"mode9\""),
                SonyCledisPictureMode.Mode10 => SendCommandAsync("picture_mode \"mode10\""),
                _ => throw new ArgumentException("invalid value", nameof(mode))
            };

        public async Task<SonyCledisLightOutput> GetLightOutputAsync() => throw new NotImplementedException();

        public async Task SetLightOutputAsync(SonyCledisLightOutput light) => throw new NotImplementedException();

        public async Task<IEnumerable<string>> GetErrorsAsync() => throw new NotImplementedException();

        public void Dispose() {
            _mutex.Dispose();
            _telnet.Dispose();
        }

        private void ValidateResponse(string response) {
            _logger?.LogDebug($"response: {response}");

            // check if response is an error code
            switch(response) {
            case "ok":
                return;
            case "err_cmd":
                throw new SonyCledisCommandUnrecognizedException();
            case "err_option":
                throw new SonyCledisCommandOptionaException();
            case "err_inactive":
                throw new SonyCledisCommandInactiveException();
            case "err_val":
                throw new SonyCledisCommandValueException();
            case "err_auth":
                throw new SonyCledisAuthenticationException();
            case "err_internal1":
                throw new SonyCledisInternal1Exception();
            case "err_internal2":
                throw new SonyCledisInternal2Exception();
            default:
                throw new SonyCledisUnrecognizedResponseException(response);
            }
        }

        private async Task SendCommandAsync(string message)
            => ValidateResponse(await SendAsync(message + "\r\n"));

        private async Task<string> SendAsync(string message) {
            await _mutex.WaitAsync();
            try {
                TaskCompletionSource<string> responseSource = new();

                // send message and await response, and validate
                try {
                    _telnet.MessageReceived += ResponseHandler;
                    await _telnet.SendAsync(message);
                    var response = await responseSource.Task;
                    return response;
                } finally {

                    // remove response handler no matter what
                    _telnet.MessageReceived -= ResponseHandler;
                }

                // local functions
                void ResponseHandler(object sender, TelnetMessageReceivedEventArgs args)
                    => responseSource.SetResult(args.Message);
            } finally {
                _mutex.Release();
            }
        }
        private async Task ConfirmConnectionAsync(ITelnet client, TextReader reader, TextWriter writer) {
            var handshake = await reader.ReadLineAsync();

            // the controller sends 'NOKEY' when there is no need for authentication
            if(handshake != "NOKEY") {
                throw new NotSupportedException("Sony C-LED requires authentication");
            }
            _logger?.LogDebug("unsecured Sony C-LED connection established");
        }

        public T ConvertResponse<T>(string response) {

            // check if response is an error code
            switch(response) {
            case "ok":
                _logger?.LogDebug($"response: {response}");
                return default;
            case "err_cmd":
                throw new SonyCledisCommandUnrecognizedException();
            case "err_option":
                throw new SonyCledisCommandOptionaException();
            case "err_inactive":
                throw new SonyCledisCommandInactiveException();
            case "err_val":
                throw new SonyCledisCommandValueException();
            case "err_auth":
                throw new SonyCledisAuthenticationException();
            case "err_internal1":
                throw new SonyCledisInternal1Exception();
            case "err_internal2":
                throw new SonyCledisInternal2Exception();
            default:
                if(_logger?.IsEnabled(LogLevel.Debug) ?? false) {
                    var serializedResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions {
                        WriteIndented = true,
                        Converters = {
                            new JsonStringEnumConverter()
                        }
                    });
                    _logger?.LogDebug($"response: {serializedResponse}");
                }
                return JsonSerializer.Deserialize<T>(response);
            }
        }

        private SonyCledisTemperatures ConvertTemperatureFromJson(string json) {
            SonyCledisTemperatures result = new();
            var data = ConvertResponse<List<Dictionary<string, float>>>(json);
            Dictionary<(int Column,int Row), SonyCledisModuleTemperature> modules = new();

            // loop over all entries in response
            foreach(var entry in data) {
                var (key, value) = entry.First();

                // check what the entry describes
                if(key == "controller") {

                    // set controller temperature
                    result.ControllerTemperature = value;
                } else if(
                    (key.StartsWith("u", StringComparison.Ordinal))
                    && (key.Length >= 6)
                    && (key[5] == '_')
                    && int.TryParse(key.Substring(1, 2), out var column)
                    && int.TryParse(key.Substring(3, 2), out var row)
                ) {

                    // get module entry or create one
                    if(!modules.TryGetValue((column, row), out var module)) {
                        module = new() {
                            Row = row,
                            Column = column
                        };
                        modules.Add((column, row), module);
                    }

                    // check what part of the module the entry describes
                    if(key.EndsWith("_board", StringComparison.Ordinal)) {

                        // set module board temperature
                        module.BoardTemperature = value;
                    } else if(key.EndsWith("_ambient", StringComparison.Ordinal)) {

                        // set module ambient temperature
                        module.AmbientTemperature = value;
                    } else if(key.EndsWith("_cell1")) {

                        // set module cell temperature
                        module.CellTemperatures[0] = value;
                    } else if(key.EndsWith("_cell2")) {

                        // set module cell temperature
                        module.CellTemperatures[1] = value;
                    } else if(key.EndsWith("_cell3")) {

                        // set module cell temperature
                        module.CellTemperatures[2] = value;
                    } else if(key.EndsWith("_cell4")) {

                        // set module cell temperature
                        module.CellTemperatures[3] = value;
                    } else if(key.EndsWith("_cell5")) {

                        // set module cell temperature
                        module.CellTemperatures[4] = value;
                    } else if(key.EndsWith("_cell6")) {

                        // set module cell temperature
                        module.CellTemperatures[5] = value;
                    } else if(key.EndsWith("_cell7")) {

                        // set module cell temperature
                        module.CellTemperatures[6] = value;
                    } else if(key.EndsWith("_cell8")) {

                        // set module cell temperature
                        module.CellTemperatures[7] = value;
                    } else if(key.EndsWith("_cell9")) {

                        // set module cell temperature
                        module.CellTemperatures[8] = value;
                    } else if(key.EndsWith("_cell10")) {

                        // set module cell temperature
                        module.CellTemperatures[9] = value;
                    } else if(key.EndsWith("_cell11")) {

                        // set module cell temperature
                        module.CellTemperatures[10] = value;
                    } else if(key.EndsWith("_cell12")) {

                        // set module cell temperature
                        module.CellTemperatures[11] = value;
                    } else {
                        _logger?.LogWarning($"unrecognized module entry: '{key}' = {value}");
                    }
                } else {
                    _logger?.LogWarning($"unrecognized entry: '{key}' = {value}");
                }
            }

            // check if any module information was found
            if(modules.Any()) {

                // determine the dimensions of the wall
                var maxRow = modules.Keys.Max(location => location.Row);
                var maxColumn = modules.Keys.Max(location => location.Column);
                result.Modules = new SonyCledisModuleTemperature[maxColumn, maxRow];

                // assign module information to corresponding array location
                foreach(var (location, module) in modules) {
                    result.Modules[location.Column - 1, location.Row - 1] = module;
                }
            } else {

                // not modules found; initialize an empty array
                result.Modules = new SonyCledisModuleTemperature[0, 0];
            }
            return result;
        }
    }
}
