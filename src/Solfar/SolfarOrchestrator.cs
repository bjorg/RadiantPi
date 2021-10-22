using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RadiantPi.Lumagen;
using RadiantPi.Lumagen.Model;
using RadiantPi.Sony.Cledis;

namespace Solfar {

    public class SolfarOrchestrator : AOrchestrator {

        //--- Fields ---
        private IRadiancePro _radianceProClient;
        private ISonyCledis _cledisClient;
        private ModeInfoDetails _radiancePro;

        //--- Constructors ---
        public SolfarOrchestrator(IRadiancePro radianceProClient, ISonyCledis cledisClient, ILogger logger = null) : base(logger) {
            _radianceProClient = radianceProClient ?? throw new ArgumentNullException(nameof(radianceProClient));
            _cledisClient = cledisClient ?? throw new ArgumentNullException(nameof(cledisClient));

            // subscribing to events
            _radianceProClient.ModeInfoChanged += OnModeInfoChanged;
        }

        //--- Methods ---
        public override void Stop() {
            _radianceProClient.ModeInfoChanged -= OnModeInfoChanged;
            base.Stop();
        }

        protected override bool ProcessChanges(object change) {
            switch(change) {
            case ModeInfoDetails modeInfoDetails:
                _radiancePro = modeInfoDetails;
                return true;
            default:
                Logger.LogWarning($"unrecognized channel event: {change?.GetType().FullName}");
                return false;
            }
        }

        protected override async Task EvaluateChangeAsync() {

            // fetch values
            var fitHeight = StringComparer.Ordinal.Compare(_radiancePro.DetectedAspectRatio, "178") < 0;
            var fitWidth = (StringComparer.Ordinal.Compare(_radiancePro.DetectedAspectRatio, "178") >= 0)
                && (StringComparer.Ordinal.Compare(_radiancePro.DetectedAspectRatio, "200") <= 0);
            var fitNative = StringComparer.Ordinal.Compare(_radiancePro.DetectedAspectRatio, "200") > 0;
            var isHdr = _radiancePro.SourceDynamicRange == RadianceProDynamicRange.HDR;
            var is3D = (_radiancePro.Source3DMode != RadiancePro3D.Undefined) && (_radiancePro.Source3DMode != RadiancePro3D.Off);
            var isGameSource = _radiancePro.PhysicalInputSelected is 2 or 4 or 6 or 8;
            var isGui = _radiancePro.SourceVerticalRate == "050";

            // evaluate rules
            await DoAsync("Switch to 3D", is3D, SwitchTo3DAsync);
            await DoAsync("Switch to 2D", !is3D, SwitchTo2DAsync);
            await DoAsync("Fit Height", !is3D && !isGameSource && (fitHeight || isGui), FitHeightAsync);
            await DoAsync("Fit Width", !is3D && !isGameSource && fitWidth && !isGui, FitWidthAsync);
            await DoAsync("Fit Native", !is3D && !isGameSource && fitNative && !isGui, FitNativeAsync);
        }

        private void OnModeInfoChanged(object sender, ModeInfoDetailsEventArgs args)
            => NotifyOfChanges(args.ModeInfoDetails);

        private async Task SwitchTo3DAsync() {
            await _cledisClient.SetInputAsync(SonyCledisInput.Hdmi2);
            await _cledisClient.SetPictureModeAsync(SonyCledisPictureMode.Mode3);
            await _radianceProClient.SelectMemory(RadianceProMemory.MemoryA);
        }

        private Task SwitchTo2DAsync() => _cledisClient.SetInputAsync(SonyCledisInput.Hdmi1);
        private Task SwitchToHDRAsync() => _cledisClient.SetPictureModeAsync(SonyCledisPictureMode.Mode2);
        private Task SwitchToSDRAsync() => _cledisClient.SetPictureModeAsync(SonyCledisPictureMode.Mode1);
        private Task FitHeightAsync() => _radianceProClient.SelectMemory(RadianceProMemory.MemoryC);
        private Task FitWidthAsync() => _radianceProClient.SelectMemory(RadianceProMemory.MemoryB);
        private Task FitNativeAsync() => _radianceProClient.SelectMemory(RadianceProMemory.MemoryA);
    }
}
