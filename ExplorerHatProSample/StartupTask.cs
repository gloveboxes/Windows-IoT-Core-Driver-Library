using Glovebox.IoT.Devices.HATs;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using static Glovebox.IoT.Devices.HATs.ExplorerHatPro;
using static Glovebox.IoT.Devices.HATs.ExplorerHatPro.Pin;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace ExplorerHatProSample {
    public sealed class StartupTask : IBackgroundTask
    {
        ExplorerHatPro hat = new ExplorerHatPro();

        public void Run(IBackgroundTaskInstance taskInstance)
        {

            var light = hat.AnalogRead(Analog.A1).ReadRatio();
                
            hat.Led(Led.Blue).On();
            hat.Motor(Motor.MotorOne).Forward();
            hat.Motor(Motor.MotorTwo).Backward();
            Task.Delay(1000);
            hat.Motor(Motor.MotorOne).Stop();
            hat.Led(Led.Blue).On();


            // 
            // TODO: Insert code to perform background work
            //
            // If you start any asynchronous methods here, prevent the task
            // from closing prematurely by using BackgroundTaskDeferral as
            // described in http://aka.ms/backgroundtaskdeferral
            //
        }
    }
}
