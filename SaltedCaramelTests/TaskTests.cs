using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace SaltedCaramel.Tests
{
    [TestClass()]
    public class TaskTests
    {
        SCImplant implant = new SCImplant();
        [TestMethod()]
        public void TaskChangeDirValid()
        {
            SCTask task = new SCTask("cd", "C:\\Temp", "1");
            Tasks.ChangeDir.Execute(task);
            Assert.AreEqual("complete", task.status);
        }

        [TestMethod()]
        public void TaskChangeDirInvalid()
        {
            SCTask task = new SCTask("cd", "C:\\asdf", "1");
            Tasks.ChangeDir.Execute(task);
            Assert.AreEqual("error", task.status);
        }

        [TestMethod()]
        public void DirectoryListValid()
        {
            SCTask task = new SCTask("ls", "C:\\Temp", "1");
            Tasks.DirectoryList.Execute(task, implant);
            Assert.AreEqual("complete", task.status);
            Assert.IsNotNull(task.message);
        }

        [TestMethod()]
        public void DirectoryListInvalid()
        {
            SCTask task = new SCTask("ls", "C:\\asdf", "1");
            Tasks.DirectoryList.Execute(task, implant);
            Assert.AreEqual("error", task.status);
            Assert.IsNotNull(task.message);
        }

        [TestMethod()]
        public void KillValid()
        {
            Process proc = new Process();
            ProcessStartInfo si = new ProcessStartInfo();
            si.FileName = "C:\\Windows\\System32\\notepad.exe";
            proc.StartInfo = si;
            proc.Start();
            SCTask task = new SCTask("kill", proc.Id.ToString(), "1");
            Tasks.Kill.Execute(task);
            Assert.AreEqual("complete", task.status);
            Assert.IsNotNull(task.message);
        }

        [TestMethod()]
        public void KillInvalid()
        {
            SCTask task = new SCTask("kill", "1234567", "1");
            Tasks.Kill.Execute(task);
            Assert.AreEqual("error", task.status);
            Assert.IsNotNull(task.message);
        }

        [TestMethod()]
        public void PowerShellValid()
        {
            SCTask task = new SCTask("powershell", "Get-Process", "1");
            Tasks.Powershell.Execute(task);
            Assert.AreEqual("complete", task.status);
            Assert.IsNotNull(task.message);
        }

        [TestMethod()]
        public void ProcValid()
        {
            Tasks.Token.stolenHandle = IntPtr.Zero;
            SCTask task = new SCTask("run", "whoami /priv", "1");
            Tasks.Proc.Execute(task, implant);
            Assert.AreEqual("complete", task.status);
            Assert.IsNotNull(task.message);
        }

        [TestMethod()]
        public void ProcInvalid()
        {
            Tasks.Token.stolenHandle = IntPtr.Zero;
            SCTask task = new SCTask("run", "asdf", "1");
            Tasks.Proc.Execute(task, implant);
            Assert.AreEqual("error", task.status);
            Assert.IsNotNull(task.message);
        }
        
        [TestMethod()]
        public void ProcWithCredsValid()
        {
            Tasks.Token.Revert();
            SCTask task = new SCTask("make_token", "lowpriv Passw0rd!", "1");
            task.DispatchTask(implant);

            task.command = "run";
            task.@params = "whoami /priv";
            task.status = "";
            task.message = "";
            Tasks.Proc.Execute(task, implant);
            Assert.AreEqual("complete", task.status);
            Assert.IsNotNull(task.message);
            Assert.IsTrue(task.message.Contains("Privilege") || task.message.Contains("Execution"));
            Tasks.Token.Revert();
        }

        [TestMethod()]
        public void ProcWithLogonValid()
        {
            Tasks.Token.Revert();
            SCTask task = new SCTask("make_token", "lowpriv Passw0rd! netonly", "1");
            task.DispatchTask(implant);

            task.command = "run";
            task.@params = "whoami /priv";
            task.status = "";
            task.message = "";
            Tasks.Proc.Execute(task, implant);
            Assert.AreEqual("complete", task.status);
            Assert.IsNotNull(task.message);
            Assert.IsTrue(task.message.Contains("Privilege") || task.message.Contains("Execution"));
            Tasks.Token.Revert();
        }

        [TestMethod()]
        public void ProcWithCredsInvalid()
        {
            Tasks.Token.Revert();
            SCTask task = new SCTask("make_token", "lowpriv Passw0rd!", "1");
            task.DispatchTask(implant);

            task.command = "run";
            task.@params = "asdf";
            task.status = "";
            task.message = "";
            Tasks.Proc.Execute(task, implant);
            Assert.AreEqual("error", task.status);
            Assert.IsNotNull(task.message);
            Assert.IsTrue(task.message.Contains("specified"));
            Tasks.Token.Revert();
        }

        [TestMethod()]
        public void ProcWithTokenValid()
        {
            Tasks.Token.Revert();
            SCTask task = new SCTask("steal_token", "", "1");
            task.DispatchTask(implant);

            task.command = "run";
            task.@params = "whoami /priv";
            task.status = "";
            task.message = "";
            Tasks.Proc.Execute(task, implant);
            Assert.AreEqual("complete", task.status);
            Assert.IsNotNull(task.message);
            Assert.IsTrue(task.message.Contains("Privilege") || task.message.Contains("Execution"));
            Tasks.Token.Revert();
        }

        [TestMethod()]
        public void ProcWithTokenInvalid()
        {
            Tasks.Token.Revert();
            SCTask task = new SCTask("steal_token", "", "1");
            task.DispatchTask(implant);

            task.command = "run";
            task.@params = "asdf";
            task.status = "";
            task.message = "";
            Tasks.Proc.Execute(task, implant);
            Assert.AreEqual("error", task.status);
            Assert.IsNotNull(task.message);
            Assert.IsTrue(task.message.Contains("2"));
            Tasks.Token.Revert();
        }

        [TestMethod()]
        public void ProcessList()
        {
            SCTask task = new SCTask("ps", "", "1");
            Tasks.ProcessList.Execute(task);
            Assert.AreEqual("complete", task.status);
            Assert.IsNotNull(task.message);
            Assert.IsTrue(task.message.Contains("explorer"));
        }

        // Relies on being able to communicate with Apfell server
        //[TestMethod()]
        //public void ScreenCapture()
        //{
        //    SCTask task = new SCTask("screencapture", "", "1");
        //    task.DispatchTask(implant);
        //    Assert.AreEqual(task.status, "complete");
        //}

        [TestMethod()]
        public void TokenWinlogon()
        {
            SCTask task = new SCTask("steal_token", "", "1");
            Tasks.Token.Execute(task);
            Assert.AreEqual("complete", task.status);
            Tasks.Token.Revert();
        }

        [TestMethod()]
        public void TokenInvalid()
        {
            SCTask task = new SCTask("steal_token", "12351", "1");
            Tasks.Token.Execute(task);
            Assert.AreEqual("error", task.status);
            Tasks.Token.Revert();
        }
        
        [TestMethod()]
        public void Shellcode()
        {
            SCTask task = new SCTask("shinject", "", "1");
            Tasks.Shellcode.Execute(task);
            Assert.AreEqual("complete", task.status);
        }
    }
}