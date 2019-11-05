using Microsoft.VisualStudio.TestTools.UnitTesting;
using SaltedCaramel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaltedCaramel.Tests
{
    [TestClass()]
    public class DispatchTaskTests
    {
        SCImplant implant = new SCImplant();
        [TestMethod()]
        public void TaskChangeDirValid()
        {
            SCTaskObject task = new SCTaskObject("cd", "C:\\Temp", "1");
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "complete");
        }
        [TestMethod()]
        public void TaskChangeDirInvalid()
        {
            SCTaskObject task = new SCTaskObject("cd", "C:\\asdf", "1");
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "error");
        }
        [TestMethod()]
        public void DirectoryListValid()
        {
            SCTaskObject task = new SCTaskObject("ls", "C:\\Temp", "1");
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "complete");
            Assert.IsNotNull(task.message);
        }
        [TestMethod()]
        public void DirectoryListInvalid()
        {
            SCTaskObject task = new SCTaskObject("ls", "C:\\asdf", "1");
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
            SCTaskObject task = new SCTaskObject("kill", proc.Id.ToString(), "1");
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "complete");
            Assert.IsNotNull(task.message);
        }
        [TestMethod()]
        public void KillInvalid()
        {
            SCTaskObject task = new SCTaskObject("kill", "1234567", "1");
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "error");
            Assert.IsNotNull(task.message);
        }
        [TestMethod()]
        public void PowerShellValid()
        {
            SCTaskObject task = new SCTaskObject("powershell", "Get-Process", "1");
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "complete");
            Assert.IsNotNull(task.message);
        }
        [TestMethod()]
        public void ProcValid()
        {
            SCTaskObject task = new SCTaskObject("run", "whoami", "1");
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "complete");
            Assert.IsNotNull(task.message);
        }
        [TestMethod()]
        public void ProcInvalid()
        {
            SCTaskObject task = new SCTaskObject("run", "asdf", "1");
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "error");
            Assert.IsNotNull(task.message);
        }
        [TestMethod()]
        public void ProcWithTokenValid()
        {
            SCTaskObject task = new SCTaskObject("steal_token", "", "1");
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
            SCTaskObject task = new SCTaskObject("steal_token", "", "1");
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
            SCTaskObject task = new SCTaskObject("ps", "", "1");
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "complete");
            Assert.IsNotNull(task.message);
            Assert.IsTrue(task.message.Contains("explorer"));
        }
        // Relies on being able to communicate with Apfell server
        //[TestMethod()]
        //public void ScreenCapture()
        //{
        //    SCTaskObject task = new SCTaskObject("screencapture", "", "1");
        //    task.DispatchTask(implant);
        //    Assert.AreEqual(task.status, "complete");
        //}
        [TestMethod()]
        public void TokenWinlogon()
        {
            SCTaskObject task = new SCTaskObject("steal_token", "", "1");
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "complete");
        }
        [TestMethod()]
        public void TokenInvalid()
        {
            SCTaskObject task = new SCTaskObject("steal_token", "12351", "1");
            task.DispatchTask(implant);
            Assert.AreEqual(task.status, "error");
        }
    }
}