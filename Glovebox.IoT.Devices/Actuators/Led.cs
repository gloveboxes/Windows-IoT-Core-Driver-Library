using Glovebox.IoT.Devices.HATs;
using System;
using System.Threading;
using Windows.Devices.Gpio;

namespace Glovebox.IoT.Devices.Actuators {
    public class Led:  IDisposable  {

        GpioController gpio = GpioController.GetDefault();

        ledState ts = new ledState();

        class ledState {
            public uint blinkMilliseconds = 0;
            public int BlinkMillisecondsToDate;
            public bool ledOn = false;
            public GpioPin led;
            public Timer tmr;
            public int blinkRateMilliseconds;
            public bool running = false;
        }

        public enum BlinkRate {
            Slow = 1000,
            Medium = 500,
            Fast = 75,
            VeryFast = 25,
        }

        /// <summary>
        /// Simnple Led control
        /// </summary>
        /// <param name="pin">From the SecretLabs.NETMF.Hardware.NetduinoPlus.Pins namespace</param>
        /// <param name="name">Unique identifying name for command and control</param>
        public Led(int pinNumber) {
            InitLed(pinNumber);
        }

        private void InitLed(int pinNumber) {
            ts.led = gpio.OpenPin(pinNumber, GpioSharingMode.Exclusive);
            ts.led.SetDriveMode(GpioPinDriveMode.Output);
            ts.led.Write(GpioPinValue.Low);

            ts.tmr = new Timer(BlinkTime_Tick, ts, Timeout.Infinite, Timeout.Infinite);
        }


        public void On() {
            if (ts.running) { return; }
            ts.led.Write(GpioPinValue.High);
        }

        public void Off() {
            if (ts.running) { return; }
            ts.led.Write(GpioPinValue.Low);
        }

        public void BlinkOn(uint Milliseconds, BlinkRate blinkRate) {

            if (ts.running) { return; }
            ts.running = true;

            ts.blinkMilliseconds = Milliseconds;
            ts.BlinkMillisecondsToDate = 0;
            ts.blinkRateMilliseconds = (int)blinkRate;
            ts.tmr.Change(0, ts.blinkRateMilliseconds);
        }

        void BlinkTime_Tick(object state) {
            var ts = (ledState)state;

            ts.led.Write(!ts.ledOn ? GpioPinValue.High : GpioPinValue.Low);
            ts.ledOn = !ts.ledOn;

            ts.BlinkMillisecondsToDate += ts.blinkRateMilliseconds;
            if (ts.BlinkMillisecondsToDate >= ts.blinkMilliseconds) {
                // turn off blink
                ts.tmr.Change(Timeout.Infinite, Timeout.Infinite);
                ts.led.Write(GpioPinValue.Low);
                ts.running = false;
            }
        }

        public void Dispose() {
            ts.led.Dispose();
        }
    }
}
