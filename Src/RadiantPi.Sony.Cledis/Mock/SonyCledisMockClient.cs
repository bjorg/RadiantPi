using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RadiantPi.Core.Utility;
using static RadiantPi.Sony.Cledis.Internal.Converters;

namespace RadiantPi.Sony.Cledis.Mock {

    public class SonyCledisMockClient : ISonyCledis {

        //--- Constants ---
        private const long SERIAL_NUMBER = 1234567L;
        private const string MODEL_NAME = "ZRCT-200 [MOCK]";

        //--- Fields ---
        private SonyCledisPowerStatus _power = SonyCledisPowerStatus.StandBy;
        private SonyCledisInput _input = SonyCledisInput.Hdmi1;
        private SonyCledisPictureMode _mode = SonyCledisPictureMode.Mode1;
        private SonyCledisLightOutput _light = SonyCledisLightOutput.Low;

        //--- Methods ---
        public Task<IEnumerable<string>> GetErrorsAsync() => throw new NotImplementedException();
        public Task<SonyCledisInput> GetInputAsync() => Task.FromResult(_input);
        public Task<SonyCledisLightOutput> GetLightOutputAsync() => Task.FromResult(_light);
        public Task<string> GetModelNameAsync() => Task.FromResult(MODEL_NAME);
        public Task<IEnumerable<string>> GetModelNameListAsync() => throw new NotImplementedException();
        public Task<SonyCledisPictureMode> GetPictureModeAsync() => Task.FromResult(_mode);
        public Task<SonyCledisPowerStatus> GetPowerStatusAsync() => Task.FromResult(_power);
        public Task<long> GetSerialNumberAsync() => Task.FromResult(SERIAL_NUMBER);
        public Task<IEnumerable<string>> GetSerialNumberListAsync() => throw new NotImplementedException();

        public Task<SonyCledisTemperatures> GetTemperatureAsync() {
            if(_power == SonyCledisPowerStatus.On) {
                var json = GetType().Assembly.ReadManifestResource("RadiantPi.Sony.Cledis.Resources.CledisTemperature.json.gz");
                return Task.FromResult(ConvertTemperatureFromJson(json));
            }
            return Task.FromResult(new SonyCledisTemperatures {
                ControllerTemperature = 33f,
                Modules = new SonyCledisModuleTemperature[0, 0]
            });
        }

        public Task<IEnumerable<Dictionary<string, string>>> GetVersionAsync() => throw new NotImplementedException();

        public Task SetInputAsync(SonyCledisInput input) {
            _input = input;
            return Task.CompletedTask;
        }

        public Task SetLightOutputAsync(SonyCledisLightOutput light) {
            _light = light;
            return Task.CompletedTask;
        }

        public Task SetPictureModeAsync(SonyCledisPictureMode mode) {
            _mode = mode;
            return Task.CompletedTask;
        }

        public Task SetPowerAsync(SonyCledisPower power) {
            switch(power) {
            case SonyCledisPower.On:
                _power = SonyCledisPowerStatus.On;
                break;
            case SonyCledisPower.Off:
                _power = SonyCledisPowerStatus.StandBy;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(power));
            }
            return Task.CompletedTask;
        }

        public void Dispose() { }
    }
}