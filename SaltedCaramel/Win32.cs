using System;
using System.Runtime.InteropServices;

namespace SaltedCaramel
{
    internal class Win32
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenProcessToken(IntPtr ProcessHandle,
            uint desiredAccess, out IntPtr TokenHandle);


        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal extern static bool DuplicateTokenEx(
            IntPtr hExistingToken,
            uint dwDesiredAccess,
            IntPtr lpTokenAttributes,
            uint ImpersonationLevel,
            TOKEN_TYPE TokenType,
            out IntPtr phNewToken);

        internal enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool CreateProcessWithTokenW(IntPtr hToken, IntPtr dwLogonFlags,
            string lpApplicationName, string lpCommandLine, IntPtr dwCreationFlags,
            IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        internal enum CreationFlags
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
        internal struct PROCESS_INFORMATION
        {
            internal IntPtr hProcess;
            internal IntPtr hThread;
            internal int dwProcessId;
            internal int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct STARTUPINFO
        {
             internal Int32 cb;
             internal IntPtr lpReserved;
             internal IntPtr lpDesktop;
             internal IntPtr lpTitle;
             internal Int32 dwX;
             internal Int32 dwY;
             internal Int32 dwXSize;
             internal Int32 dwYSize;
             internal Int32 dwXCountChars;
             internal Int32 dwYCountChars;
             internal Int32 dwFillAttribute;
             internal Int32 dwFlags;
             internal Int16 wShowWindow;
             internal Int16 cbReserved2;
             internal IntPtr lpReserved2;
             internal IntPtr hStdInput;
             internal IntPtr hStdOutput;
             internal IntPtr hStdError;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        [DllImport("kernel32.dll")]
        internal static extern int GetProcessId(IntPtr handle);
    }
}
