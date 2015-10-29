using UnitsNet;
using Windows.Devices.Adc;

namespace Glovebox.IoT.Devices.Sensors {

    public class MCP9701A : MCP970XBase
	{
        public MCP9701A(AdcChannel channel, int referenceMilliVolts = 3300) : base(channel, referenceMilliVolts, 400, 19.53, -6)
        {
        }
    }
}
