using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Glovebox.IoT.Devices.Sensors;
using Glovebox.Graphics.Drivers;
using Glovebox.Graphics.Components;


// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace WeatherStation
{
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral _deferral;
        MAX7219 driver = new MAX7219(4, MAX7219.Rotate.None, MAX7219.Transform.HorizontalFlip);


        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            LED8x8Matrix matrix = new LED8x8Matrix(driver);

            BMP180 tempAndPressure = new BMP180();            


            while (true)
            {
                var message = $"{Math.Round(tempAndPressure.Temperature.DegreesCelsius, 1)}C, {Math.Round(tempAndPressure.Pressure.Hectopascals, 1)}hPa ";

                matrix.ScrollStringInFromRight(message, 70);

            }
        }
    }
}
