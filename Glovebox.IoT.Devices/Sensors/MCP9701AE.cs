using UnitsNet;
using Windows.Devices.Adc;

namespace Glovebox.IoT.Devices.Sensors {
    public class MCP9700AE : MCP970XBase {

        public MCP9700AE(AdcChannel channel, int referenceMilliVolts = 3300) : base(channel, referenceMilliVolts, 400, 19.5, -4)
        {
        }
    }
}
