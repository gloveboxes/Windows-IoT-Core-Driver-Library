using Glovebox.IoT.Devices.Converters;
using Glovebox.IoT.Devices.HATs;
using Glovebox.IoT.Devices.Sensors;
using IotServices;
using Microsoft.Azure.Devices.Client;
using System;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using static Glovebox.IoT.Devices.HATs.ExplorerHatPro;

namespace IoTHubExplorerProHat
{
    public sealed class StartupTask : IBackgroundTask
    {
        private DeviceClient deviceClient = DeviceClient.CreateFromConnectionString("HostName=glovebox-iot-hub.azure-devices.net;DeviceId=RPi3DG;SharedAccessKey=Y7KaNlIPwYOf7S70Gc/zeo0pOFcQww5OO/hZ7uAiEh0=");

        BackgroundTaskDeferral deferral;

        ExplorerHatPro hat = new ExplorerHatPro(ADS1015.Gain.Volt5);
        BME280 bme280 = new BME280(0x76);

        Telemetry telemetry;
        IoTHubCommand<String> iotHubCommand;

        public void Run(IBackgroundTaskInstance taskInstance) {
            deferral = taskInstance.GetDeferral();

            telemetry = new Telemetry("Sydney", "RPi3DG", Measure);
            iotHubCommand = new IoTHubCommand<string>(deviceClient, telemetry);
            iotHubCommand.CommandReceived += Commanding_CommandReceived;
        }


        async void Measure() {
            try {

                var content = new Message(telemetry.ToJson(bme280.Temperature.DegreesCelsius, hat.AnalogRead(AnalogPin.Ain2).ReadRatio(), bme280.Pressure.Hectopascals, bme280.Humidity));
                await deviceClient.SendEventAsync(content);

            }
            catch {
                telemetry.Exceptions++;
                hat.Light(ExplorerHatPro.Colour.Red).On();
            }
        }

        private void Commanding_CommandReceived(object sender, CommandEventArgs<string> e)
        {
            #region IoT Hub Command Support

            char cmd = e.Item.Length > 0 ? e.Item.ToUpper()[0] : ' ';  // get command character sent from IoT Hub

            switch (cmd)
            {
                case 'R':
                    hat.Light(Colour.Red).On();
                    break;
                case 'G':
                    hat.Light(Colour.Red).On();
                    break;
                case 'B':
                    hat.Light(Colour.Red).On();
                    break;
                case 'Y':
                    hat.Light(Colour.Red).On();
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine("Unrecognized command: {0}", e.Item);
                    break;
            }

            #endregion
        }
    }
}
