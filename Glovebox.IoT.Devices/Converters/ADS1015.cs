using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Adc.Provider;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

// ADS1015 ADC on I2C Bus
// http://www.ti.com/product/ads1015

namespace Glovebox.IoT.Devices.Converters
{
    public sealed class ADS1015 : IAdcControllerProvider, IDisposable
    {
        private static object deviceLock = new object();
        private static bool isInitialized;
        private static I2cDevice I2CDevice;
        
        public int I2C_ADDRESS { get; set; } = 0x48;
        public string I2C_CONTROLLER_NAME { get; set; } = "I2C1";        /* For Raspberry Pi 2, use I2C1 */

        bool[] ChannelState = new bool[4];

        ushort config = 0;
        Gain gain = Gain.Volt33;


        #region Register Constants
        const int REG_CONV = 0x00;
        const int REG_CFG = 0x01;

        const ushort ADS1015_REG_CONFIG_MUX_SINGLE_0 = (0x4000); // Single-ended AIN0
        const ushort ADS1015_REG_CONFIG_MUX_SINGLE_1 = (0x5000); // Single-ended AIN1
        const ushort ADS1015_REG_CONFIG_MUX_SINGLE_2 = (0x6000); // Single-ended AIN2
        const ushort ADS1015_REG_CONFIG_MUX_SINGLE_3 = (0x7000); // Single-ended AIN3

        #endregion Register constance


        public enum SamplesPerSecond
        {
            SPS128, SPS250, SPS490, SPS920, SPS1600, SPS2400, SPS3300
        }

        private ushort[] SamplePerSecondMap = { 0x0000, 0x0020, 0x0040, 0x0060, 0x0080, 0x00A0, 0x00C0 };
        private ushort[] SamplesPerSecondRate = { 128, 250, 490, 920, 1600, 2400, 3300 };

        public enum Channel
        {
            A4 = 0x4000, A3 = 0x5000, A2 = 0x6000, A1 = 0x7000
        }

        //https://learn.adafruit.com/adafruit-4-channel-adc-breakouts/programming
        ushort[] programmableGainMap = { 0x0000, 0x0200, 0x0400, 0x0600, 0x0800, 0x0A00 };
        ushort[] programmableGain_Scaler = { 6144, 4096, 2048, 1024, 512, 256 };

        //     ProviderAdcChannelMode channelMode = ProviderAdcChannelMode.SingleEnded;



        public int ChannelCount => 4;

        public int MaxValue => gain == Gain.Volt33 ? 3300 : 5000;

        public int MinValue => 0;

        public int ResolutionInBits => 12;

        public ProviderAdcChannelMode ChannelMode { get; set; } = ProviderAdcChannelMode.SingleEnded;


        public enum Gain
        {
            Volt5 = 0,  //   PGA_6_144V = 0, //6144,  //0
            Volt33 = 1, //   PGA_4_096V = 1, //4096,  //1
            //PGA_2_048V = 2, //2048,  //2
            //PGA_1_024V = 3, //1024,  //3
            //PGA_0_512V = 4, //512,   //4
            //PGA_0_256V = 5, //256,   //5
        }

        public ADS1015(Gain refVoltage = Gain.Volt33)
        {
            this.gain = refVoltage;
        }

        public async Task EnsureInitializedAsync()
        {
            if (isInitialized) { return; }

            try
            {
                var settings = new I2cConnectionSettings(I2C_ADDRESS);
                settings.BusSpeed = I2cBusSpeed.FastMode;

                string aqs = I2cDevice.GetDeviceSelector(I2C_CONTROLLER_NAME);  /* Find the selector string for the I2C bus controller                   */
                var dis = await DeviceInformation.FindAllAsync(aqs);            /* Find the I2C bus controller device with our selector string           */
                I2CDevice = await I2cDevice.FromIdAsync(dis[0].Id, settings);    /* Create an I2cDevice with our selected bus controller and I2C settings */
            }
            catch (Exception ex)
            {
                throw new Exception("I2C Initialization Failed", ex);
            }

            isInitialized = true;
        }

        private int GetMillivolts(int channel)
        {

            SamplesPerSecond sps = SamplesPerSecond.SPS1600;


            byte[] data = new byte[3];
            byte[] result = new byte[2];


            // Set disable comparator and set "single shot" mode	
            config = 0x0003 | 0x8000; // | 0x100;
            config |= (ushort)SamplePerSecondMap[(int)sps];
            config |= (ushort)programmableGainMap[(int)gain];


            switch (channel)
            {
                case (0):
                    config |= ADS1015_REG_CONFIG_MUX_SINGLE_0;
                    break;
                case (1):
                    config |= ADS1015_REG_CONFIG_MUX_SINGLE_1;
                    break;
                case (2):
                    config |= ADS1015_REG_CONFIG_MUX_SINGLE_2;
                    break;
                case (3):
                    config |= ADS1015_REG_CONFIG_MUX_SINGLE_3;
                    break;
            }


            data[0] = REG_CFG;
            data[1] = (byte)((config >> 8) & 0xFF);
            data[2] = (byte)(config & 0xFF);


            I2CDevice.Write(data);
            // delay in milliseconds
            //int delay = (1000.0 / SamplesPerSecondRate[(int)sps] + .1;
            //    int delay = 1;
            //Task.Delay(TimeSpan.FromMilliseconds(.5)).Wait();
            Task.Delay(TimeSpan.FromMilliseconds(.5)).Wait();

            I2CDevice.WriteRead(new byte[] { (byte)REG_CONV, 0x00 }, result);

            return (((result[0] << 8) | result[1]) >> 4) * programmableGain_Scaler[(int)gain] / 2048;
        }

        void IDisposable.Dispose()
        {
            I2CDevice?.Dispose();
            I2CDevice = null;
            isInitialized = false;
        }

        public bool IsChannelModeSupported(ProviderAdcChannelMode channelMode)
        {
            return channelMode == ProviderAdcChannelMode.SingleEnded ? true : false;
        }

        public void AcquireChannel(int channel)
        {
            if (channel < 0 || channel >= ChannelCount)
            {
                throw new IndexOutOfRangeException("Channel number out of range");
            }

            if (ChannelState[channel])
            {
                throw new UnauthorizedAccessException("Channel in use");
            }
            ChannelState[channel] = true;
        }

        public void ReleaseChannel(int channel)
        {
            if (channel < 0 || channel >= ChannelCount)
            {
                throw new IndexOutOfRangeException("Channel number out of range");
            }
            ChannelState[channel] = false;
        }

        public int ReadValue(int channelNumber)
        {
            lock (deviceLock)
            {
                if (!isInitialized) { EnsureInitializedAsync().Wait(); }

                if ((channelNumber < 0) || (channelNumber > ChannelCount)) throw new ArgumentOutOfRangeException("channelNumber");

                if (!isInitialized) { EnsureInitializedAsync().Wait(); }    // Make sure we're initialized

                return GetMillivolts(channelNumber);
            }
        }
    }
}
