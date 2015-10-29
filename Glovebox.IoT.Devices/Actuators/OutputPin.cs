using System;
using Windows.Devices.Gpio;

namespace Glovebox.IoT.Devices.Actuators {
    public class OutputPin : IDisposable{

        GpioController gpio = GpioController.GetDefault();
        GpioPin pin;

        public OutputPin(int pinNumber) {
            pin = gpio.OpenPin(pinNumber, GpioSharingMode.Exclusive);
            pin.SetDriveMode(GpioPinDriveMode.Output);
            pin.Write(GpioPinValue.Low);
        }

        public void On() {
            pin.Write(GpioPinValue.High);
        }

        public void Off() {
            pin.Write(GpioPinValue.Low);
        }

        public void Dispose() {
            if (pin != null) {
                pin.Dispose();
                pin = null;
            }
        }
    }
}
