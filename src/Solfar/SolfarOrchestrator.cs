using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RadiantPi.Lumagen;
using RadiantPi.Lumagen.Model;
using RadiantPi.Sony.Cledis;
using RadiantPi.Trinnov.Altitude;

namespace Solfar {

    public class SolfarOrchestrator : AOrchestrator {

        //--- Fields ---
        private IRadiancePro _radianceProClient;
        private ISonyCledis _cledisClient;
        protected ITrinnovAltitude _trinnovClient;
        private ModeInfo _radianceProModeInfo;
        private AudioDecoderChangedEventArgs _altitudeAudioDecoder;

        //--- Constructors ---
        public SolfarOrchestrator(
            IRadiancePro radianceProClient,
            ISonyCledis cledisClient,
            ITrinnovAltitude altitudeClient,
            ILogger logger = null
        ) : base(logger) {
            _radianceProClient = radianceProClient ?? throw new ArgumentNullException(nameof(radianceProClient));
            _cledisClient = cledisClient ?? throw new ArgumentNullException(nameof(cledisClient));
            _trinnovClient = altitudeClient ?? throw new ArgumentNullException(nameof(altitudeClient));
        }

        //--- Methods ---
        public override void Start() {
            base.Start();
            _radianceProClient.ModeInfoChanged += EventListener;
            _trinnovClient.AudioDecoderChanged += EventListener;
        }

        public override void Stop() {
            _trinnovClient.AudioDecoderChanged -= EventListener;
            _radianceProClient.ModeInfoChanged -= EventListener;
            base.Stop();
        }

        protected override bool ApplyEvent(object sender, EventArgs args) {
            switch(args) {
            case ModeInfoChangedEventArgs modeInfoDetailsEventArgs:
                _radianceProModeInfo = modeInfoDetailsEventArgs.ModeInfo;
                return true;
            case AudioDecoderChangedEventArgs audioDecoderChangedEventArgs:
                _altitudeAudioDecoder = audioDecoderChangedEventArgs;
                return true;
            default:
                Logger.LogWarning($"unrecognized channel event: {args?.GetType().FullName}");
                return false;
            }
        }

        protected override void Evaluate() {

            // fetch values
            var fitHeight = LessThan(_radianceProModeInfo.DetectedAspectRatio, "178");
            var fitWidth = GreaterThanOrEqual(_radianceProModeInfo.DetectedAspectRatio, "178")
                && LessThanOrEqual(_radianceProModeInfo.DetectedAspectRatio, "200");
            var fitNative = GreaterThan(_radianceProModeInfo.DetectedAspectRatio, "200");
            var isHdr = _radianceProModeInfo.SourceDynamicRange == RadianceProDynamicRange.HDR;
            var is3D = (_radianceProModeInfo.Source3DMode != RadiancePro3D.Undefined) && (_radianceProModeInfo.Source3DMode != RadiancePro3D.Off);
            var isGameSource = _radianceProModeInfo.PhysicalInputSelected is 2 or 4 or 6 or 8;
            var isGui = _radianceProModeInfo.SourceVerticalRate == "050";

            // evaluate rules
            OnTrue("Switch to 2D", !is3D, SwitchTo2DAsync);
            OnTrue("Switch to 3D", is3D, SwitchTo3DAsync);
            OnTrue("Switch to SDR", !is3D && !isHdr, SwitchToSDRAsync);
            OnTrue("Switch to HDR", !is3D && isHdr, SwitchToHDRAsync);
            OnTrue("Fit Height", !is3D && !isGameSource && (fitHeight || isGui), FitHeightAsync);
            OnTrue("Fit Width", !is3D && !isGameSource && fitWidth && !isGui, FitWidthAsync);
            OnTrue("Fit Native", !is3D && !isGameSource && fitNative && !isGui, FitNativeAsync);
        }

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
