﻿using System;
using System.Threading;
using static MyStealer.AntiDebug.NativeCalls;

namespace MyStealer.AntiDebug.Check.Debug
{
    /// <summary>
    /// https://github.com/AdvDebug/AntiCrack-DotNet/blob/91872f71c5601e4b037b713f31327dfde1662481/AntiCrack-DotNet/AntiDebug.cs
    /// </summary>
    public class SleepDurationDecreased : CheckBase
    {
        public override string Name => "Sleep Ignorance - TickCount delta too short";

        public override bool CheckActive()
        {
            var prev = Environment.TickCount;
            Thread.Sleep(500);
            return Environment.TickCount - prev < 500L;
        }
    }
}