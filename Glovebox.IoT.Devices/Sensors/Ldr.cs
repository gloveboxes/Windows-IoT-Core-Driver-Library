using System;
using Windows.Devices.Adc;

namespace Glovebox.IoT.Devices.Sensors {
    public class Ldr : IDisposable {

        AdcChannel channel;

        public Ldr(AdcChannel channel) {
            this.channel = channel;
        }

        public static Ldr GetSensor(AdcChannel channel) {
            return new Ldr(channel);
        }

        public void Dispose() {
            channel.Dispose();
        }


        public double ReadRatio => channel.ReadRatio();

        public double ReadValue => channel.ReadValue();

    }
}
