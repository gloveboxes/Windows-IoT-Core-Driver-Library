using Glovebox.IoT.Devices.Converters;
using Glovebox.IoT.Devices.HATs;
using Glovebox.IoT.Devices.Sensors;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using static Glovebox.IoT.Devices.HATs.ExplorerHatPro;
using static Glovebox.IoT.Devices.HATs.ExplorerHatPro.Pin;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace ExplorerHatProSample {
    public sealed class StartupTask : IBackgroundTask {

        public void Run(IBackgroundTaskInstance taskInstance) {

            using (ExplorerHatPro hat = new ExplorerHatPro(ADS1015.Gain.Volt5))
            using (BMP280 bmp280 = new BMP280() { I2C_ADDRESS = 0x76 }) {

                while (true) {
                    Debug.WriteLine($"Temperature {bmp280.Temperature.DegreesCelsius}C, Pressure {bmp280.Pressure.Hectopascals}, Light ratio {hat.AnalogRead(AnalogPin.Ain2).ReadRatio()} ");

                    for (int l = 0; l < hat.ColourCount; l++) {
                        hat.Led((Light)l).On();
                        Task.Delay(20).Wait();
                    }

                    for (int l = 0; l < hat.ColourCount; l++) {
                        hat.Led((Light)l).Off();
                        Task.Delay(20).Wait();
                    }
                }

                //hat.Motor(MotorId.MotorOne).Forward();
                //hat.Motor(MotorId.MotorTwo).Backward();
            }

            // 
            // TODO: Insert code to perform background work
            //
            // If you start any asynchronous methods here, prevent the task
            // from closing prematurely by using BackgroundTaskDeferral as
            // described in http://aka.ms/backgroundtaskdeferral
            //
        }
    }
}
