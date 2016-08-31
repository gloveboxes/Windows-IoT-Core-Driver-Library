using Glovebox.IoT.Devices.HATs;
using Glovebox.IoT.Devices.Sensors;
using System;
using System.Diagnostics;
using Windows.ApplicationModel.Background;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace ExplorerProHatTouchSample
{
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral deferral;
        ExplorerHatPro hat = new ExplorerHatPro();

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            hat.One.ButtonPressedEvent += One_ButtonPressedEvent;
            hat.Two.ButtonPressedEvent += One_ButtonPressedEvent;
            hat.Three.ButtonPressedEvent += One_ButtonPressedEvent;
            hat.Four.ButtonPressedEvent += One_ButtonPressedEvent;
            hat.Five.ButtonPressedEvent += One_ButtonPressedEvent;
            hat.Six.ButtonPressedEvent += One_ButtonPressedEvent;
            hat.Seven.ButtonPressedEvent += One_ButtonPressedEvent;
            hat.Eight.ButtonPressedEvent += One_ButtonPressedEvent;

        }

        private void One_ButtonPressedEvent(ITouchSensor sender, TouchChannel args)
        {
            var e = Enum.GetName(typeof(TouchChannel), args);

            Debug.WriteLine($"{e} Pressed");
        }
    }
}
