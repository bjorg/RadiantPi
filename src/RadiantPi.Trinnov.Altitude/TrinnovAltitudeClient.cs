using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RadiantPi.Telnet;

namespace RadiantPi.Trinnov.Altitude {

    public class TrinnovAltitudeClientConfig {

        //--- Properties ---
        public string Host { get; set; }
        public ushort? Port { get; set; }
        public bool? Mock { get; set; }
    }

    public sealed class AudioDecoderChangedEventArgs : EventArgs {

        //--- Properties ---
        public string Decoder { get; init; }
        public string Upmixer { get; init; }
    }

    public class TrinnovAltitudeClient : ITrinnovAltitude {

        //--- Class Fields ---
        private static Regex _audioModeRegex = new Regex(@"DECODER NONAUDIO [01] PLAYABLE (?<playable>[01]) DECODER (?<decoder>.+) UPMIXER (?<upmixer>.+)", RegexOptions.Compiled);

        //--- Fields ---
        private readonly ILogger _logger;
        private readonly ITelnet _telnet;
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);

        //--- Constructors ---
        public TrinnovAltitudeClient(TrinnovAltitudeClientConfig config, ILogger logger = null) : this(new TelnetClient(config.Host, config.Port ?? 53595, logger), logger) { }

        public TrinnovAltitudeClient(ITelnet telnet, ILogger logger) {
            _logger = logger;
            _telnet = telnet ?? throw new ArgumentNullException(nameof(telnet));
            _telnet.ConfirmConnectionAsync = ConfirmConnectionAsync;
            _telnet.MessageReceived += MessageReceived;
        }

        //--- Events ---
        public event EventHandler<AudioDecoderChangedEventArgs> AudioDecoderChanged;

        //--- Methods ---
        public async Task ConnectAsync() {
            if(await _telnet.ConnectAsync().ConfigureAwait(false)) {
                await _telnet.SendAsync($"id radiant_pi_{DateTimeOffset.UtcNow.Ticks}").ConfigureAwait(false);
            }
        }

        public Task SetVolumeAsync(float volume) => _telnet.SendAsync($"volume {volume}");
        public Task AdjustVolumeAsync(float delta) => _telnet.SendAsync($"dvolume {delta}");

        public void Dispose() {
            _mutex.Dispose();
            _telnet.Dispose();
        }

        private async Task ConfirmConnectionAsync(ITelnet client, TextReader reader, TextWriter writer) {
            var handshake = await reader.ReadLineAsync();

            // the device sends a welcome text to identify it
            if(!handshake.StartsWith("Welcome on Trinnov Optimizer (", StringComparison.Ordinal)) {
                throw new NotSupportedException("Unrecognized device");
            }
            _logger?.LogDebug("Trinnov Altitude connection established");
        }

        private void MessageReceived(object sender, TelnetMessageReceivedEventArgs args) {
            _logger?.LogDebug($"received: {args.Message}");

            // check for audio event
            var match = _audioModeRegex.Match(args.Message);
            if(match.Success) {
                var playable = match.Groups["playable"].Value;
                if(playable == "0") {

                    // we can't use this signal, ignore it
                    return;
                }

                // emit event
                AudioDecoderChanged?.Invoke(this, new AudioDecoderChangedEventArgs {
                    Decoder = match.Groups["decoder"].Value,
                    Upmixer = match.Groups["upmixer"].Value
                });
            }
        }
    }
}
