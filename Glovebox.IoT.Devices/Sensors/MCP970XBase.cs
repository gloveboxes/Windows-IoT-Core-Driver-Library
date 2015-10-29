using System;
using System.ComponentModel;
using System.Threading.Tasks;
using UnitsNet;
using Windows.Devices.Adc;

namespace Glovebox.IoT.Devices.Sensors {

    public class MCP970XBase : IDisposable {

        static object deviceLock = new object();
        AdcChannel channel;


        public int ReferenceMilliVolts { get; set; } = 3300;

        public double ZeroDegreeOffset { get; set; } = 400;

        public double MillivoltsPerDegree { get; set; } = 20;

        public double CalibrationOffset { get; set; } = 0;


        public MCP970XBase(AdcChannel channel, int referenceMilliVolts, double zeroDegreeOffset, double millivoltsPerDegree, double calibrationOffset)
        {
            this.channel = channel;
            this.ReferenceMilliVolts = referenceMilliVolts;
            this.ZeroDegreeOffset = zeroDegreeOffset;
            this.MillivoltsPerDegree = millivoltsPerDegree;
            this.CalibrationOffset = calibrationOffset;
        }

        public Temperature Temperature {
            get {
                // ensure thread safe
                lock (deviceLock)
                {

                    double AverageRatio = 0;
                    for (int i = 0; i < 6; i++)
                    {
                        AverageRatio += channel.ReadRatio();
                        Task.Delay(1).Wait();
                    }

                    var ratio = AverageRatio / 6;
                    double milliVolts = ratio * ReferenceMilliVolts;
                    double celsius = ((milliVolts - ZeroDegreeOffset) / MillivoltsPerDegree) + CalibrationOffset;

                    return Temperature.FromDegreesCelsius(celsius);
                }
            }
        }

        public void Dispose() {
            channel.Dispose();
        }
    }
}
