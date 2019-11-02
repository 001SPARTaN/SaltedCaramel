using System;
using System.Diagnostics;

namespace SaltedCaramel.Tasks
{
    class Kill
    {
        internal static void Execute(SCTaskObject task)
        {
            int pid = Convert.ToInt32(task.@params);
            try
            {
                Debug.WriteLine("[-] Kill - Killing process with PID " + pid);
                Process target = Process.GetProcessById(pid);
                target.Kill();
                task.status = "complete";
                task.message = $"Killed process with PID {pid}";
            }
            catch (Exception e)
            {
                Debug.WriteLine("[-] Kill - ERROR killing process " + pid + ": " + e.Message);
                task.status = "error";
                task.message = e.Message;
            }
        }
    }
}
