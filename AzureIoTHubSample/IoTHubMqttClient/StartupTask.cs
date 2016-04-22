using Glovebox.Graphics.Components;
using Glovebox.Graphics.Drivers;
using Glovebox.IoT.Devices.Converters;
using Glovebox.IoT.Devices.HATs;
using Glovebox.IoT.Devices.Sensors;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using Windows.ApplicationModel.Background;
using static Glovebox.IoT.Devices.HATs.ExplorerHatPro.Pin;

namespace IoTHubMqttClient {
    public sealed class StartupTask : IBackgroundTask {

        SecurityManager cm = new SecurityManager("HostName=glovebox-iot-hub.azure-devices.net;DeviceId=RPi3DG;SharedAccessKey=Y7KaNlIPwYOf7S70Gc/zeo0pOFcQww5OO/hZ7uAiEh0=");

        BackgroundTaskDeferral deferral;

        ExplorerHatPro hat = new ExplorerHatPro(ADS1015.Gain.Volt5);
        BME280 bme280 = new BME280();
        LED8x8Matrix matrix = new LED8x8Matrix(new Ht16K33());

        Telemetry telemetry;

        string hubUser;
        string hubTopicPublish;
        string hubTopicSubscribe;

        private MqttClient client;

        public void Run(IBackgroundTaskInstance taskInstance) {
            deferral = taskInstance.GetDeferral();

            telemetry = new IoTHubMqttClient.Telemetry("Sydney", cm.hubName);

            hubUser = $"{cm.hubAddress}/{cm.hubName}";
            hubTopicPublish = $"devices/{cm.hubName}/messages/events/";
            hubTopicSubscribe = $"devices/{cm.hubName}/messages/devicebound/#";


            // https://m2mqtt.wordpress.com/m2mqtt_doc/
            client = new MqttClient(cm.hubAddress, 8883, true, MqttSslProtocols.TLSv1_2);
            client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
            client.Subscribe(new string[] { hubTopicSubscribe }, new byte[] { 0 });

            var result = Task.Run(async () => {
                while (true) {
                    matrix.DrawSymbol(Glovebox.Graphics.Grid.Grid8x8.Symbols.HourGlass);
                    matrix.FrameDraw();

                    if (!client.IsConnected) { client.Connect(cm.hubName, hubUser, cm.hubPass); }
                    if (client.IsConnected) {
                        client.Publish(hubTopicPublish, telemetry.ToJson(bme280.Temperature.DegreesCelsius, hat.AnalogRead(ExplorerHatPro.AnalogPin.Ain2).ReadRatio(), bme280.Pressure.Hectopascals, bme280.Humidity));
                    }

                    matrix.FrameClear();
                    matrix.FrameDraw();

                    await Task.Delay(30000); // don't leave this running for too long at this rate as you'll quickly consume your free daily Iot Hub Message limit
                }
            });
        }

        private void Client_MqttMsgPublishReceived(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishEventArgs e) {
            string message = System.Text.Encoding.UTF8.GetString(e.Message).ToUpperInvariant();
            Debug.WriteLine($"Command Received: {message}");

            switch (message) {
                case "RED":
                    hat.Led(Led.Red).On();
                    break;
                case "GREEN":
                    hat.Led(Led.Green).On();
                    break;
                case "BLUE":
                    hat.Led(Led.Blue).On();
                    break;
                case "YELLOW":
                    hat.Led(Led.Yellow).On();
                    break;
                case "OFF":
                    for (int l = 0; l < hat.LedCount; l++) {
                        hat.Led((Led)l).Off();
                    }
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine("Unrecognized command: {0}", message);
                    break;
            }
        }
    }
}
