using Glovebox.IoT.Devices.Converters;
using UnitsNet;
using Windows.Devices.Adc;

namespace Glovebox.IoT.Devices.Sensors {

    // http://www.microchip.com/wwwproducts/Devices.aspx?dDocName=en022289
    public class MCP9700A : MCP970XBase {

        public MCP9700A(AdcChannel channel, int referenceMilliVolts = 3300) : base(channel, referenceMilliVolts, 530, 11, -2)
        {
        }
    }
}
