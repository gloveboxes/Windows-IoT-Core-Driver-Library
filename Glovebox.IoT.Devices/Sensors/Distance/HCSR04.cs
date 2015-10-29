using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace Glovebox.IoT.Devices.Sensors.Distance
{
    public class HCSR04
    {
        private GpioController gpio = GpioController.GetDefault();

        private GpioPin trig;
        private GpioPin echo;

        private readonly int trig_Pin;
        private readonly int echo_Pin;


        Object deviceLock = new object();

        Stopwatch sw = new Stopwatch();

        public HCSR04(byte trig_Pin, byte echo_Pin)
        {

            this.trig_Pin = trig_Pin;
            this.echo_Pin = echo_Pin;


            trig = gpio.OpenPin(trig_Pin);
            echo = gpio.OpenPin(echo_Pin);

            trig.SetDriveMode(GpioPinDriveMode.Output);
            echo.SetDriveMode(GpioPinDriveMode.Input);

            trig.Write(GpioPinValue.Low);
        }


        public bool GetDistanceToObstacle(ref double distance)
        {
            lock (deviceLock)
            {

                //http://www.c-sharpcorner.com/UploadFile/167ad2/how-to-use-ultrasonic-sensor-hc-sr04-in-arduino/
                //http://www.modmypi.com/blog/hc-sr04-ultrasonic-range-sensor-on-the-raspberry-pi

                sw.Reset();                                       // reset the stopwatch

                trig.Write(GpioPinValue.Low);                     // ensure the trigger is off
                Task.Delay(TimeSpan.FromMilliseconds(2)).Wait();  // wait for the sensor to settle

                trig.Write(GpioPinValue.High);                          // turn on the pulse
                Task.Delay(TimeSpan.FromMilliseconds(.01)).Wait();      // let the pulse run for 10 microseconds
                trig.Write(GpioPinValue.Low);                           // turn off the pulse

                sw.Start();                                             //start the stopwatch

                while (echo.Read() == GpioPinValue.Low)
                {               // wait until the echo starts
                    if (sw.ElapsedMilliseconds > 500)
                    {
                        return false;                                   // if you have waited for more than a second, then there was a failure in the echo
                    }
                }

                sw.Restart();           // echo is working properly, so restart the stop watch at zero

                while (echo.Read() == GpioPinValue.High) ;

                sw.Stop();  // stop the stopwatch when the echo stops

                // speed of sound is 34300 cm per second or 34.3 cm per millisecond
                // since the sound waves traveled to the obstacle and back to the sensor
                // I am dividing by 2 to represent travel time to the obstacle
                distance = sw.Elapsed.TotalMilliseconds * 34.3 / 2.0;


                return true;
            }
        }
    }
}
