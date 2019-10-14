using System;
using System.Runtime.InteropServices;

namespace SaltedCaramel
{
    class Win32
    {
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CreateProcessWithTokenW(IntPtr hToken, IntPtr dwLogonFlags,
            string lpApplicationName, string lpCommandLine, IntPtr dwCreationFlags,
            IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        public enum CreationFlags
        {
            DefaultErrorMode = 0x04000000,
            NewConsole = 0x00000010,
            NewProcessGroup = 0x00000200,
            SeparateWOWVDM = 0x00000800,
            Suspended = 0x00000004,
            UnicodeEnvironment = 0x00000400,
            ExtendedStartupInfoPresent = 0x00080000
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct STARTUPINFO
        {
             public Int32 cb;
             public IntPtr lpReserved;
             public IntPtr lpDesktop;
             public IntPtr lpTitle;
             public Int32 dwX;
             public Int32 dwY;
             public Int32 dwXSize;
             public Int32 dwYSize;
             public Int32 dwXCountChars;
             public Int32 dwYCountChars;
             public Int32 dwFillAttribute;
             public Int32 dwFlags;
             public Int16 wShowWindow;
             public Int16 cbReserved2;
             public IntPtr lpReserved2;
             public IntPtr hStdInput;
             public IntPtr hStdOutput;
             public IntPtr hStdError;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        [DllImport("kernel32.dll")]
        public static extern int GetProcessId(IntPtr handle);
    }
}
