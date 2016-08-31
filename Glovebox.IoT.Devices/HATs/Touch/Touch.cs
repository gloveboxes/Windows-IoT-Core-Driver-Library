
//namespace Glovebox.IoT.Devices.Sensors.Cap1208.Touch
//{
//    public static class Touch
//    {
//        private const int TouchAddress = 0x28;

//        public static TouchSensor One { get; private set; }
//        public static TouchSensor Two { get; private set; }
//        public static TouchSensor Three { get; private set; }
//        public static TouchSensor Four { get; private set; }
//        public static TouchSensor Five { get; private set; }
//        public static TouchSensor Six { get; private set; }
//        public static TouchSensor Seven { get; private set; }
//        public static TouchSensor Eight { get; private set; }

//        static Touch()
//        {

//            var cap1208 = new Cap1208Device();

//            One = new TouchSensor(cap1208, TouchChannel.One);
//            Two = new TouchSensor(cap1208, TouchChannel.Two);
//            Three = new TouchSensor(cap1208, TouchChannel.Three);
//            Four = new TouchSensor(cap1208, TouchChannel.Four);
//            Five = new TouchSensor(cap1208, TouchChannel.Five);
//            Six = new TouchSensor(cap1208, TouchChannel.Six);
//            Seven = new TouchSensor(cap1208, TouchChannel.Seven);
//            Eight = new TouchSensor(cap1208, TouchChannel.Eight);

//            cap1208.Start();
//        }
//    }
//}
