using System;
using System.Runtime.InteropServices;

namespace SaltedCaramel
{
    internal class Win32
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr hObject);

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

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal extern static bool DuplicateTokenEx(
            IntPtr hExistingToken,
            uint dwDesiredAccess,
            IntPtr lpTokenAttributes,
            uint ImpersonationLevel,
            TOKEN_TYPE TokenType,
            out IntPtr phNewToken);

        [DllImport("kernel32.dll")]
        internal static extern int GetProcessId(IntPtr handle);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenProcessToken(IntPtr ProcessHandle,
            uint desiredAccess, out IntPtr TokenHandle);

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            internal IntPtr hProcess;
            internal IntPtr hThread;
            internal int dwProcessId;
            internal int dwThreadId;
        }

        [Flags]
        public enum STARTF : uint
        {
            STARTF_USESHOWWINDOW = 0x00000001,
            STARTF_USESIZE = 0x00000002,
            STARTF_USEPOSITION = 0x00000004,
            STARTF_USECOUNTCHARS = 0x00000008,
            STARTF_USEFILLATTRIBUTE = 0x00000010,
            STARTF_RUNFULLSCREEN = 0x00000020,  // ignored for non-x86 platforms
            STARTF_FORCEONFEEDBACK = 0x00000040,
            STARTF_FORCEOFFFEEDBACK = 0x00000080,
            STARTF_USESTDHANDLES = 0x00000100,
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
             internal uint dwFlags;
             internal Int16 wShowWindow;
             internal Int16 cbReserved2;
             internal IntPtr lpReserved2;
             internal IntPtr hStdInput;
             internal IntPtr hStdOutput;
             internal IntPtr hStdError;
        }

        internal enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);
    }
}
