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

            public enum Led : byte
            {
                Blue = 4,
                Yellow = 17,
                Red = 27,
                Green = 5,
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

            public enum Motor : ushort
            {
                TwoPlus = 21,
                TwoMinus = 26,
                OnePlus = 19,
                OneMinus = 20
            }

            // Map phycical channels to location on bread board
            public enum Analog : byte
            {
                A1 = 3,
                A2 = 2,
                A3 = 1,
                A4 = 0
            }
        }

        public bool IsAdcInitalised { get; private set; } = false;
        public bool AreLedsInitalised { get; private set; } = false;
        public bool AreMotorsInitalised { get; private set; } = false;

        AdcProviderManager adcManager;
        public IReadOnlyList<AdcController> adcControllers;
        public AdcController Adc { get; private set; }


        public Led[] Leds;
        public Motor[] motors;


        public Led ledBlue { get { return Leds[0]; } }
        public Led ledYellow { get { return Leds[1]; } }
        public Led ledRed { get { return Leds[2]; } }
        public Led ledGreen { get { return Leds[3]; } }

        public Motor Motor1 { get { return motors[0]; } }
        public Motor Motor2 { get { return motors[1]; } }



        public async Task InitaliseAdcAsync() {
            adcManager = new AdcProviderManager();
            adcManager.Providers.Add(new ADS1015(ADS1015.Gain.Volt33));
            adcControllers = await adcManager.GetControllersAsync();

            Adc = adcControllers[0];

            IsAdcInitalised = true;
        }


        public void InitaliseLeds()
        {
            Leds = new Led[] {
                    new Led(Pin.Led.Blue),
                    new Led(Pin.Led.Yellow),
                    new Led(Pin.Led.Red),
                    new Led(Pin.Led.Green)
                };

            AreLedsInitalised = true;
        }


        public void InitaliseMotors()
        {
            motors = new Motor[]
            {
                    new Motor((int)Pin.Motor.OnePlus, (int)Pin.Motor.OneMinus),
                    new Motor((int)Pin.Motor.TwoPlus, (int)Pin.Motor.TwoMinus)
            };

            AreMotorsInitalised = true;
        }

        public async Task InitialiseHatAsync(bool initAdc = true, bool initLeds = true, bool initMotors = true)
        {
            if (initAdc)
            {
                await InitaliseAdcAsync();
            }

            if (initMotors)
            {
                InitaliseMotors();
       
            }

            if (initLeds)
            {
                InitaliseLeds();
            }
        }
    }
}
