using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace SaltedCaramel.Tests
{
    [TestClass()]
    public class DispatchTaskTests
    {
        SCImplant implant = new SCImplant();
        [TestMethod()]
        public void TaskChangeDirValid()
        {
            SCTask task = new SCTask("cd", "C:\\Temp", "1");
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "complete");
        }
        [TestMethod()]
        public void TaskChangeDirInvalid()
        {
            SCTask task = new SCTask("cd", "C:\\asdf", "1");
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "error");
        }
        [TestMethod()]
        public void DirectoryListValid()
        {
            SCTask task = new SCTask("ls", "C:\\Temp", "1");
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "complete");
            Assert.IsNotNull(task.message);
        }
        [TestMethod()]
        public void DirectoryListInvalid()
        {
            SCTask task = new SCTask("ls", "C:\\asdf", "1");
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "error");
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
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "complete");
            Assert.IsNotNull(task.message);
        }
        [TestMethod()]
        public void KillInvalid()
        {
            SCTask task = new SCTask("kill", "1234567", "1");
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "error");
            Assert.IsNotNull(task.message);
        }
        [TestMethod()]
        public void PowerShellValid()
        {
            SCTask task = new SCTask("powershell", "Get-Process", "1");
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "complete");
            Assert.IsNotNull(task.message);
        }
        [TestMethod()]
        public void ProcValid()
        {
            SCTask task = new SCTask("run", "whoami", "1");
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "complete");
            Assert.IsNotNull(task.message);
        }
        [TestMethod()]
        public void ProcInvalid()
        {
            SCTask task = new SCTask("run", "asdf", "1");
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "error");
            Assert.IsNotNull(task.message);
        }
        [TestMethod()]
        public void ProcWithTokenValid()
        {
            SCTask task = new SCTask("steal_token", "", "1");
            task.DispatchTask(implant);

            task.command = "run";
            task.@params = "whoami /priv";
            task.status = "";
            task.message = "";
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "complete");
            Assert.IsNotNull(task.message);
            Assert.IsTrue(task.message.Contains("Privilege"));
            Tasks.Token.stolenHandle = IntPtr.Zero;
        }
        [TestMethod()]
        public void ProcWithTokenInvalid()
        {
            SCTask task = new SCTask("steal_token", "", "1");
            task.DispatchTask(implant);

            task.command = "run";
            task.@params = "asdf";
            task.status = "";
            task.message = "";
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "error");
            Assert.IsNotNull(task.message);
            Assert.IsTrue(task.message.Contains("2"));
            Tasks.Token.stolenHandle = IntPtr.Zero;
        }
        [TestMethod()]
        public void ProcessList()
        {
            SCTask task = new SCTask("ps", "", "1");
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "complete");
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
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "complete");
        }
        [TestMethod()]
        public void TokenInvalid()
        {
            SCTask task = new SCTask("steal_token", "12351", "1");
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "error");
        }
    }
}