using Glovebox.IoT.Devices.Actuators;
using Glovebox.IoT.Devices.Converters;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Adc;
using Windows.Devices.Gpio;

namespace Glovebox.IoT.Devices.HATs
{
    public class ExplorerHatPro
    {
        public class Pin
        {

            public enum Output : ushort
            {
                One = 6,
                Two = 12,
                Three = 13,
                Four = 16,
            }

            public enum Input : ushort
            {
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



            public enum Spi : ushort
            {
                MOSI = 10,
                MISO = 9,
                SCK = 11,
                CS = 8
            }

            public enum Serial : ushort
            {
                TX = 14,
                RX = 15
            }

            public enum I2C : byte
            {
                SDA = 2,
                SCL = 3
            }

            //public enum Motor : ushort
            //{
            //    TwoPlus,
            //    TwoMinus,
            //    OnePlus,
            //    OneMinus
            //}


            // Map phycical channels to location on bread board
            public enum Analog : byte
            {
                A1 = 3,
                A2 = 2,
                A3 = 1,
                A4 = 0
            }

            public enum Motor : byte {
                MotorOne,
                MotorTwo
            }
        }

        public enum State {
            Off = 0,
            On = 1
        }

        enum LedMap : byte {
            Blue = 4,
            Yellow = 17,
            Red = 27,
            Green = 5,
        }

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

        Windows.Devices.Adc.AdcChannel[] channels = new AdcChannel[4];

        Led[] Leds = new Led[4];
        Glovebox.IoT.Devices.Actuators.Motor[] motors = new Actuators.Motor[2];


        bool InitaliseAdc() {
            adcManager = new AdcProviderManager();
            adcManager.Providers.Add(new ADS1015(ADS1015.Gain.Volt33));
            adcControllers =  adcManager.GetControllersAsync().GetResults();

            Adc = adcControllers[0];

            IsAdcInitalised = true;
            return IsAdcInitalised;
        }


        public Motor Motor(Pin.Motor m) {
            switch (m) {
                case Pin.Motor.MotorOne:
                    if (motors[0] == null) { motors[0] = new Motor((int)MotorMap.OnePlus, (int)MotorMap.OneMinus); }
                    return motors[0];
                case Pin.Motor.MotorTwo:
                    if (motors[1] == null) { motors[1] = new Motor((int)MotorMap.TwoPlus, (int)MotorMap.TwoMinus); }
                    return motors[1];
                default:
                    break;
            }
            return null;
        }


        public Led Led(Pin.Led pin) {
            int mappedPin = (int)Enum.GetNames(typeof(LedMap)).GetValue((int)pin);
            if (Leds[(int)pin] == null) { Leds[(int)pin] = new Led(mappedPin); }
            return Leds[(int)pin];
        }

        public AdcChannel AnalogRead(Pin.Analog pin) {
            if (!IsAdcInitalised) { InitaliseAdc(); }
            if (channels[(int)pin] == null) { channels[(int)pin] = Adc.OpenChannel((int)pin); }
            return channels[(int)pin];
        }


        public double AnalogReadRatio(Pin.Analog pin) {
            if (!IsAdcInitalised) { InitaliseAdc(); }
            if (channels[(int)pin] == null) { channels[(int)pin] = Adc.OpenChannel((int)pin); }
            return channels[(int)pin].ReadRatio();
        }
    }
}
