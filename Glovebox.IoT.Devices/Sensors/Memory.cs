using Windows.System;


namespace Glovebox.IoT.Devices.Sensors {
    public class AppMemoryUsage {     

        public double Current {
            get {
                return UnitsNet.Information.FromBytes(MemoryManager.AppMemoryUsage).Kilobytes;
            }
        }
    }
}
