using System;
using System.Runtime.InteropServices;

namespace SaltedCaramel.Win32
{
    internal class Ntdll
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct PROCESS_BASIC_INFORMATION
        {
            internal IntPtr ExitStatus;
            internal IntPtr PebBaseAddress;
            internal IntPtr AffinityMask;
            internal IntPtr BasePriority;
            internal UIntPtr UniqueProcessId;
            internal IntPtr InheritedFromUniqueProcessId;
        }

        [DllImport("ntdll.dll")]
        internal static extern int NtQueryInformationProcess(
            IntPtr processHandle,
            int processInformationClass,
            ref PROCESS_BASIC_INFORMATION ProcessBasicInfo,
            int processInformationLength,
            out int returnLength
        );
    }
}
