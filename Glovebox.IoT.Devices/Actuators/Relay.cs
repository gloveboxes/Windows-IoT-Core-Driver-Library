namespace Glovebox.IoT.Devices.Actuators {
    public class Relay : OutputPin {


        /// <summary>
        /// Create a relay control
        /// </summary>
        /// <param name="pin">Raspberry Pi 2 pin number</param>
        /// <param name="name">Unique identifying name for command and control</param>
        public Relay(int pinNumber)
            : base(pinNumber) {
        }
    }
}
