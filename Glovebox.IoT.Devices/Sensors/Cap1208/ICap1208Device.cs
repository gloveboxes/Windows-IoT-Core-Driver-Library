using Windows.Foundation;

namespace Glovebox.IoT.Devices.Sensors
{
    public interface ICap1208Device
    {
        event TypedEventHandler<ICap1208Device, TouchChannel> PadTouchedEvent;

        void Start();
        void Stop();
    }
}
