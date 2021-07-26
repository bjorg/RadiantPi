using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RadiantPi.Sony.Cledis {

    public enum SonyCledisPower {
        On,
        Off
    }

    public enum SonyCledisPowerStatus {
        StandBy,
        On,
        Updating,
        Startup,
        ShuttingDown,
        Initializing
    }

    public enum SonyCledisInput {
        Hdmi1,
        Hdmi2,
        DisplayPort1,
        DisplayPort2,
        DisplayPortBoth
    }

    public enum SonyCledisPictureMode {
        Mode1,
        Mode2,
        Mode3,
        Mode4,
        Mode5,
        Mode6,
        Mode7,
        Mode8,
        Mode9,
        Mode10
    }

    public enum SonyCledisLightOutput {
        Full,
        High,
        Medium,
        Low
    }

    public class SonyCledisTemperatures {

        //--- Properties ---
        public float ControllerTemperature { get; set; }
        public SonyCledisModuleTemperature[,] Modules { get; set; }
        public int RowCount => Modules.GetLength(1);
        public int ColumnCount => Modules.GetLength(0);
    }

    public class SonyCledisModuleTemperature {

        //--- Properties ---
        public string Id { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public float AmbientTemperature { get; set; }
        public float BoardTemperature { get; set; }
        public float[] CellTemperatures { get; set; } = new float[12];
    }

    public interface ISonyCledis : IDisposable {

        //--- Methods ---
        Task<string> GetModelNameAsync();
        Task<IEnumerable<string>> GetModelNameListAsync();
        Task<long> GetSerialNumberAsync();
        Task<IEnumerable<string>> GetSerialNumberListAsync();
        Task<IEnumerable<Dictionary<string, string>>> GetVersionAsync();
        Task<SonyCledisTemperatures> GetTemperatureAsync();
        Task<SonyCledisPowerStatus> GetPowerStatusAsync();
        Task SetPowerAsync(SonyCledisPower power);
        Task<SonyCledisInput> GetInputAsync();
        Task SetInputAsync(SonyCledisInput input);
        Task<SonyCledisPictureMode> GetPictureModeAsync();
        Task SetPictureModeAsync(SonyCledisPictureMode mode);
        Task<SonyCledisLightOutput> GetLightOutputAsync();
        Task SetLightOutputAsync(SonyCledisLightOutput light);
        Task<IEnumerable<string>> GetErrorsAsync();
    }
}
