using System;

namespace Glovebox.IoT.Devices.Sensors
{
    public static class BitTwiddlingExtensions
    {

        public static bool IsSet(this byte target, byte mask)
        {
            return (target & mask) == 1;
        }

        public static byte SetBit(this byte target, byte mask)
        {
            return Convert.ToByte(target | mask);
        }

        public static byte ClearBit(this byte target, byte mask)
        {
            return Convert.ToByte(target & ~mask);
        }
    }
}
