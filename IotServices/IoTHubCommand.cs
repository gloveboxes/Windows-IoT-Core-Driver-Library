using Microsoft.Azure.Devices.Client;
using System;
using System.Text;
using System.Threading.Tasks;

namespace IotServices
{

    public class CommandEventArgs<T> : EventArgs
    {
        public CommandEventArgs(T item) { Item = item; }
        public T Item { get; protected set; }
    }

    public class IoTHubCommand<T>
    {
        public event EventHandler<CommandEventArgs<T>> CommandReceived;

        DeviceClient deviceClient;
        Telemetry telemetry;

        public IoTHubCommand(DeviceClient deviceClient, Telemetry telemetry) {
            this.deviceClient = deviceClient;
            this.telemetry = telemetry;

            Task.Run(new Action(CloudToDeviceAsync));
        }

        private async void CloudToDeviceAsync() {
            while (true) {
                try {
                    Message receivedMessage = await deviceClient.ReceiveAsync();
                    if (receivedMessage == null) {
                        await Task.Delay(2000);
                        continue;
                    }

                    await deviceClient.CompleteAsync(receivedMessage);
                    string command = Encoding.ASCII.GetString(receivedMessage.GetBytes());

                    if (telemetry.SetSampleRateInSeconds(command)) { continue; }

                    T cmd = (T)Convert.ChangeType(command, typeof(T));

                    CommandReceived?.Invoke(this, new CommandEventArgs<T>(cmd));
                }
                catch { telemetry.Exceptions++; }
            }
        }
    }
}
