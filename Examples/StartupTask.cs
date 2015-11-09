using Glovebox.IoT.Devices.Converters;
using System.Collections.Generic;
using Windows.ApplicationModel.Background;
using Windows.Devices.Adc;
using System;
using Glovebox.IoT.Devices.Sensors;
using Glovebox.IoT.Devices.Actuators;
using System.Threading.Tasks;
using Glovebox.IoT.Devices.HATs;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace Examples
{
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral _deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();


            BMP18x tempAndPressure = new BMP18x(BMP18x.Mode.STANDARD);


            while (true)
            {


                var degreesCelsius = tempAndPressure.Temperature.DegreesCelsius;  // read temp in celsius - plenty of other units
                var degreesFahrenheit = tempAndPressure.Temperature.DegreesFahrenheit;

                var bars = tempAndPressure.Pressure.Bars;       // read air pressure in bars - plenty of other units
                var hectopascals = tempAndPressure.Pressure.Hectopascals;  // read air pressure in Hectopascals
                var Atmospheres = tempAndPressure.Pressure.Atmospheres;

            }

            //// How to use the ADS1015 and MCP3002 ADC/Converters

            //AdcProviderManager adcManager = new AdcProviderManager();

            //adcManager.Providers.Add(new ADS1015(ADS1015.Gain.Volt33));     // Load up ADS1015 4 Channel ADC Converter
            //adcManager.Providers.Add(new MCP3002());                       // Load up MCP3002 2 Channel ADC Converter

            //IReadOnlyList<AdcController> adcControllers = await adcManager.GetControllersAsync();  // load ADCs


            ////use the ADCs create above
            //Ldr light = new Ldr(adcControllers[0].OpenChannel(0)); // create new light sensor using the ADS1015 ADC provider
            //MCP9700A temp = new MCP9700A(adcControllers[1].OpenChannel(0)); // create temperature sensor using MCP3002 ADC Provider

            //var lightLevel = light.ReadValue;        // read light level from the first ADC ADS1015
            //var lightRatio = light.ReadRatio;

            //var celsius = temp.Temperature.DegreesCelsius;  // read temp in celsius
            //var fahrenheit = temp.Temperature.DegreesFahrenheit;  // read temp in celsius


            //BMP280 tempAndPressure = new BMP280();

            //var degreesCelsius = tempAndPressure.Temperature.DegreesCelsius;  // read temp in celsius - plenty of other units
            //var degreesFahrenheit = tempAndPressure.Temperature.DegreesFahrenheit;

            //var bars = tempAndPressure.Pressure.Bars;       // read air pressure in bars - plenty of other units
            //var hectopascals = tempAndPressure.Pressure.Hectopascals;  // read air pressure in Hectopascals
            //var Atmospheres = tempAndPressure.Pressure.Atmospheres;


            //// LED demo

            //Led led = new Led(4);  // open led on pin 4
            //led.On();  // turn on
            //await Task.Delay(1000);  // wait for 1 second
            //led.Off(); // turn off

            //// relay Demo
            //Relay relay = new Relay(6);
            //relay.On(); // turn relay on
            //await Task.Delay(1000);  // wait for 1 second
            //led.Off(); // turn relay off


            //// motor demo
            //Motor leftMotor = new Motor(22, 24);
            //Motor rightMotor = new Motor(12, 25);

            ////now do a tight circle
            //leftMotor.Forward();
            //rightMotor.Backward();
            //await Task.Delay(5000);  // wait for 5 second
            //leftMotor.Stop();
            //rightMotor.Stop();

            //// Explorer Hat Pro Demo
            //// note this demo attempts to open ADS1015.  Comment out the ADS1015 create in the first demo else you'll get an access violation

            //ExplorerHatPro explorerHatPro = new ExplorerHatPro();
            //await explorerHatPro.InitialiseHatAsync(true, true, true); // initialise ADC, LEDS and motors

            //if (!explorerHatPro.IsAdcInitalised)// alternatively initialise the ADS1015 on the board
            //{
            //    await explorerHatPro.InitaliseAdcAsync();  
            //}

            //explorerHatPro.ledBlue.On();  // turn on the blue light
            //explorerHatPro.ledYellow.On();
            //explorerHatPro.ledRed.On();
            //explorerHatPro.ledGreen.On();

            //for (int l = 0; l < explorerHatPro.Leds.Length; l++)  // alternatively you can loop through the collection of lights
            //{
            //    explorerHatPro.Leds[l].Off();
            //}


            //explorerHatPro.Motor1.Forward();
            //explorerHatPro.Motor2.Forward();
            //await Task.Delay(5000); // drive forward for 5 seconds
            //explorerHatPro.Motor1.Stop();
            //explorerHatPro.Motor2.Stop();

            //Ldr lightSensor = new Ldr(explorerHatPro.Adc.OpenChannel(0));
            //var lightPercentage = lightSensor.ReadRatio * 100;
        }
    }
}
