namespace Glovebox.IoT.Devices.HATs
{
    public sealed class TouchEventArgs
    {
        public byte EventType { get; private set; }
        public byte Channel { get; private set; }

        public TouchEventArgs(byte eventType, byte channelStatus)
        {
            EventType = eventType;
            Channel = channelStatus;
        }
    }
}
