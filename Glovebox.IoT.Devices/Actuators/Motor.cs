using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace Glovebox.IoT.Devices.Actuators
{
    public class Motor {

        private GpioController gpio = GpioController.GetDefault();
        private readonly int in1;
        private readonly int in2;
        private readonly GpioPin motorA;
        private readonly GpioPin motorB;


        public Motor(byte in1, byte in2) {
            this.in1 = in1;
            this.in2 = in2;            

            motorA = gpio.OpenPin(in1);
            motorB = gpio.OpenPin(in2);

            motorA.Write(GpioPinValue.Low);
            motorB.Write(GpioPinValue.Low);

            motorA.SetDriveMode(GpioPinDriveMode.Output);
            motorB.SetDriveMode(GpioPinDriveMode.Output);
        }

        public void Forward() {
            motorA.Write(GpioPinValue.Low);
            motorB.Write(GpioPinValue.High);
        }

        public void Backward() {
            motorA.Write(GpioPinValue.High);
            motorB.Write(GpioPinValue.Low);
        }

        public void Stop() {
            motorA.Write(GpioPinValue.Low);
            motorB.Write(GpioPinValue.Low);
        }
    }
}
