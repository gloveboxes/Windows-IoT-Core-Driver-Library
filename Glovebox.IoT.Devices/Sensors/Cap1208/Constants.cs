using System;

namespace Glovebox.IoT.Devices.Sensors
{


    [Flags]
    public enum TouchChannel
    {
        None = 0x00,
        One = 0x10,
        Two = 0x20,
        Three = 0x40,
        Four = 0x80,
        Five = 0x01,
        Six = 0x02,
        Seven = 0x04,
        Eight = 0x08
    }

    public enum TouchEventType
    {
        Unknown = 0x00,
        Pressed = 0x01,
        Released = 0x02,
        Held = 0x04
    }

    public enum I2CAddr
    {
        Cap1208 = 0x28
    }
}
