using System;
using Windows.Devices.Gpio;

namespace Glovebox.IoT.Devices.Sensors.Distance
{
    public class InfraRed
    {
        private GpioController gpio = GpioController.GetDefault();

        private GpioPin input;
        byte IR_Pin;


        public InfraRed(byte IR_Pin)
        {
            this.IR_Pin = IR_Pin;

            input = gpio.OpenPin(IR_Pin);
            input.SetDriveMode(GpioPinDriveMode.Input);
        }

        public bool GetDistanceToObstacle(ref double distance) => input.Read() == GpioPinValue.Low;
    }
}
