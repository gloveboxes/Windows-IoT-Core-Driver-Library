using Glovebox.IoT.Devices.Actuators;
using Glovebox.IoT.Devices.Converters;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Adc;
using Windows.Devices.Gpio;

namespace Glovebox.IoT.Devices.HATs {
    public class ExplorerHatPro : IDisposable {
        public class Pin {
            public enum Output : ushort {
                One = 6,
                Two = 12,
                Three = 13,
                Four = 16,
            }

            public enum Input : ushort {
                One = 23,
                Two = 22,
                Three = 24,
                Four = 25,
            }

            public enum Led : byte {
                Blue,
                Yellow,
                Red,
                Green,
            }

            public enum Spi : ushort {
                MOSI = 10,
                MISO = 9,
                SCK = 11,
                CS = 8
            }

            public enum Serial : ushort {
                TX = 14,
                RX = 15
            }

            public enum I2C : byte {
                SDA = 2,
                SCL = 3
            }

            //// Map phycical channels to location on bread board
            //public enum Analog : byte {
            //    A1 = 3,
            //    A2 = 2,
            //    A3 = 1,
            //    A4 = 0
            //}

            public enum Motor : byte {
                MotorOne,
                MotorTwo
            }
        }

        public enum State {
            Off = 0,
            On = 1
        }

        //enum LedMap : byte {
        //    Blue = 4,
        //    Yellow = 17,
        //    Red = 27,
        //    Green = 5,
        //}

        public enum AnalogPin : byte {
            Ain1 = 3,
            Ain2 = 2,
            Ain3 = 1,
            Ain4 = 0
        }

        public enum DigitalPin : byte {
            Din1,
            Din2,
            Din3,
            Din4,
            Dout1,
            Dout2,
            Dout3,
            Dout4
        }

        byte[] LedPinMap = new byte[] { 4, 17, 27, 5 };
        public int LedCount => LedPinMap.Length;

        enum MotorMap : ushort {
            TwoPlus = 21,
            TwoMinus = 26,
            OnePlus = 19,
            OneMinus = 20
        }

        public bool IsAdcInitalised { get; private set; } = false;

        AdcProviderManager adcManager;
        IReadOnlyList<AdcController> adcControllers;
        AdcController Adc { get; set; }
        ADS1015.Gain gain = ADS1015.Gain.Volt33;

        Windows.Devices.Adc.AdcChannel[] channels = new AdcChannel[4];

        Led[] Leds = new Led[4];
        Glovebox.IoT.Devices.Actuators.Motor[] motors = new Actuators.Motor[2];


        public async Task InitaliseAdcAsync() {
            adcManager = new AdcProviderManager();
            adcManager.Providers.Add(new ADS1015(gain));
            adcControllers = await adcManager.GetControllersAsync();

            Adc = adcControllers[0];

            IsAdcInitalised = true;
        }

        public ExplorerHatPro(ADS1015.Gain gain = ADS1015.Gain.Volt33) {
            this.gain = gain;
            for (int l = 0; l < LedCount; l++) {  // turn off the leds at startup time
                this.Led((Pin.Led)l).Off();
            }
        }


        public Motor Motor(Pin.Motor motor) {
            switch (motor) {
                case Pin.Motor.MotorOne:
                    if (motors[0] == null) { motors[0] = new Motor((int)MotorMap.OnePlus, (int)MotorMap.OneMinus); }
                    return motors[0];
                case Pin.Motor.MotorTwo:
                    if (motors[1] == null) { motors[1] = new Motor((int)MotorMap.TwoPlus, (int)MotorMap.TwoMinus); }
                    return motors[1];
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        public Led Led(Pin.Led pin) {
            int lpin = (int)pin;
            if (lpin < 0 || lpin >= LedCount) { throw new ArgumentOutOfRangeException(); }

            int mappedPin = LedPinMap[lpin];
            if (Leds[lpin] == null) { Leds[lpin] = new Led(mappedPin); }
            return Leds[lpin];
        }

        public AdcChannel AnalogRead(AnalogPin pin) {
            int aPin = (int)pin;
            if (aPin < 0 || aPin >=4) { throw new ArgumentOutOfRangeException(); }

            if (!IsAdcInitalised) { InitaliseAdcAsync().Wait(); }
            if (channels[aPin] == null) { channels[aPin] = Adc.OpenChannel(aPin); }
            return channels[aPin];
        }

        public void Dispose() {
            for (int l = 0; l < LedCount; l++) {  // turn off the leds at startup time
                if (Leds[l] != null) {
                    Leds[l].Dispose();
                    Leds[l] = null;
                }
            }
        }
    }
}
