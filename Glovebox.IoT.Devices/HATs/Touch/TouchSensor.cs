
using Glovebox.IoT.Devices.Sensors;
using Windows.Foundation;

namespace Glovebox.IoT.Devices.HATs
{
    public sealed class TouchSensor : ITouchSensor
    {
        ICap1208Device _cap1208;

        public  TouchChannel Channel { get; private set; }

        public event TypedEventHandler<ITouchSensor, TouchChannel> ButtonPressedEvent;

        public TouchSensor(ICap1208Device cap1208, TouchChannel channel)
        {
            _cap1208 = cap1208;
            Channel = channel;

            _cap1208.PadTouchedEvent += _cap1208_PadTouchedEvent;
        }

        private void _cap1208_PadTouchedEvent1(ICap1208Device sender, TouchChannel args)
        {
            throw new System.NotImplementedException();
        }

        private void _cap1208_PadTouchedEvent(ICap1208Device sender, TouchChannel args)
        {
            if ((args & Channel) > 0)
            {
                var evt = ButtonPressedEvent;
                if (evt != null)
                {
                    evt(this, args);
                }
            }
        }
    }
}
