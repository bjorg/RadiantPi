using System;
using System.Threading.Tasks;

namespace RadiantPi.Trinnov.Altitude {

    public interface ITrinnovAltitude : IDisposable {

        //--- Events ---
        event EventHandler<AudioDecoderChangedEventArgs> AudioDecoderChanged;

        //--- Methods ---
        Task ConnectAsync();
        Task SetVolumeAsync(float volume);
        Task AdjustVolumeAsync(float delta);
    }
}
