using System;
using System.Threading.Tasks;
using Windows.Devices.Adc.Provider;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;
using System.ComponentModel;
using System.Linq;

namespace Glovebox.IoT.Devices.Converters {

    public sealed class MCP3002 : IAdcControllerProvider, IDisposable {

        #region Constants
        private const byte SINGLE_ENDED_MODE = 0x60;
        private const byte PSEUDO_DIFFERENTIAL_MODE = 0x00;
        private const byte ChipMode = SINGLE_ENDED_MODE;
        #endregion // Constants

        #region Member Variables
        static private object DeviceLock = new object();
        static private bool isInitialized;
        static private SpiDevice spiDevice;            // The SPI device the display is connected to
        #endregion // Member Variables



        public string ControllerName { get; set; } = "SPI0";

        public int ChipSelectLine { get; set; } = 0;

        public int ChannelCount => 2;

        public int MaxValue => 1023;

        public int MinValue => 0;

        public int ResolutionInBits => 10;

        public ProviderAdcChannelMode ChannelMode { get; set; } = ProviderAdcChannelMode.SingleEnded;



        #region Internal Methods
        private async Task EnsureInitializedAsync() {
            if (isInitialized) { return; }

                try {
                var settings = new SpiConnectionSettings(ChipSelectLine);
                settings.ClockFrequency = 500000;// 10000000;
                settings.Mode = SpiMode.Mode0; //Mode3;

                string spiAqs = SpiDevice.GetDeviceSelector(ControllerName);
                var deviceInfo = await DeviceInformation.FindAllAsync(spiAqs);
                spiDevice = await SpiDevice.FromIdAsync(deviceInfo[0].Id, settings);

                isInitialized = true;
            }
            /* If initialization fails, display the exception and stop running */
            catch (Exception ex) {
                throw new Exception("SPI Initialization Failed", ex);
            }

        }
        #endregion // Internal Methods



        public bool IsChannelModeSupported(ProviderAdcChannelMode channelMode) {
            return channelMode == ProviderAdcChannelMode.SingleEnded ? true : false;
        }


        public int ReadValue(int channelNumber) {
            lock (DeviceLock) {                

                if ((channelNumber < 0) || (channelNumber > ChannelCount)) { throw new ArgumentOutOfRangeException("channelNumber"); }

                if (!isInitialized) { EnsureInitializedAsync().Wait(); }

                byte[] data = new byte[2]; /*this is defined to hold the output data*/
                byte[] SpiControlFrame = new byte[2] { 0x00, 0x00 }; // SPI Config and must be the same length and the readbuffer

                var cn = (byte)(0x08 << channelNumber);

                SpiControlFrame[0] = (byte)(ChipMode | (byte)(0x08 << channelNumber));
                spiDevice.TransferFullDuplex(SpiControlFrame, data);

                return (data[0] & 0x03) << 8 | data[1];
            }
        }

        public void AcquireChannel(int channel) {
            if ((channel < 0) || (channel > ChannelCount)) throw new ArgumentOutOfRangeException("channel");
        }

        public void ReleaseChannel(int channel) {
            if ((channel < 0) || (channel > ChannelCount)) throw new ArgumentOutOfRangeException("channel");
        }

        public void Dispose() {
            spiDevice?.Dispose();
            spiDevice = null;
            isInitialized = false;
        }
    }
}
