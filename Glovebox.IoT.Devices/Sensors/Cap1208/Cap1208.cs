
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.Foundation;

namespace Glovebox.IoT.Devices.Sensors
{
    public class Cap1208Device : ICap1208Device
    {
        const byte LSBMask = 0x01;
        const byte MSBMask = 0x80;

        const byte ThresholdRegister = 0x30;
        const byte ControlRegister = 0x00;
        const byte StatusRegister = 0x03;
        const byte DeltaRegister = 0x10;

        const int PollingPeriod = 10;

        Timer _timer;

        public event TypedEventHandler<ICap1208Device, TouchChannel> PadTouchedEvent;
        public int I2C_ADDRESS { get; set; } = 0x28;

        private static I2cDevice I2CDevice;
        public string I2cControllerName { get; set; } = "I2C1";  /* For Raspberry Pi 2, use I2C1 */

        public static bool IsInitialised { get; private set; } = false;

        public void Initialise()
        {
            if (!IsInitialised)
            {
                EnsureInitializedAsync().Wait();
            }
        }

        private async Task EnsureInitializedAsync()
        {
            if (IsInitialised) { return; }

            try
            {
                var settings = new I2cConnectionSettings(I2C_ADDRESS);
                settings.BusSpeed = I2cBusSpeed.FastMode;

                string aqs = I2cDevice.GetDeviceSelector(I2cControllerName);  /* Find the selector string for the I2C bus controller                   */
                var dis = await DeviceInformation.FindAllAsync(aqs);            /* Find the I2C bus controller device with our selector string           */
                I2CDevice = await I2cDevice.FromIdAsync(dis[0].Id, settings);    /* Create an I2cDevice with our selected bus controller and I2C settings */


                IsInitialised = true;
            }
            catch (Exception ex)
            {
                throw new Exception("I2C Initialization Failed", ex);
            }
        }

        public byte Read(byte register)
        {
            var buffer = new byte[1];
            I2CDevice.WriteRead(new byte[] { register }, buffer);

            return buffer[0];
        }

        public byte[] Read(byte register, int length)
        {
            var buffer = new byte[length];
            I2CDevice.WriteRead(new byte[] { register }, buffer);

            return buffer;
        }

        public void Write(byte register, byte value)
        {
            I2CDevice.Write(new byte[] { register, value });
        }

        public void Write(byte register, [ReadOnlyArray] byte[] buffer)
        {
            var writeBuffer = new byte[buffer.Length + 1];
            writeBuffer[0] = register;
            buffer.CopyTo(writeBuffer, 1);
            I2CDevice.Write(writeBuffer);
        }

        public void Start()
        {
            Initialise();
            _timer = new Timer(OnTick, null, 0, PollingPeriod);
        }

        public void Stop()
        {
            _timer?.Dispose();
        }

        internal async void OnTick(object state)
        {
            var controlReg = Read(ControlRegister);

            if (controlReg.IsSet(LSBMask)) // touch detected
            {
                var status = Read(StatusRegister);
                ClearInterrupt();

                // TODO get thresholds & deltas
                var thresHolds = Read(ThresholdRegister, 8);
                var deltas = Read(DeltaRegister, 8);

                for (var i = 0; i < 8; i++)
                {
                    if (((1 << i & status) == 1) && (deltas[i] >= thresHolds[i]))
                    {
                        status &= (byte)~(1 << i);
                    }
                }

                // TODO figure out the type of interrupt
                await OnPadTouchedEvent((byte)TouchEventType.Unknown, status);
            }
        }

        private void ClearInterrupt()
        {
            var control = Read(ControlRegister);
            control = control.ClearBit(LSBMask);
            Write(ControlRegister, control);
        }

        private async Task OnPadTouchedEvent(byte eventType, byte channelStatus)
        {
            await Task.Run(() =>
            {
                PadTouchedEvent?.Invoke(this, (TouchChannel)channelStatus);
            });
        }
    }
}
