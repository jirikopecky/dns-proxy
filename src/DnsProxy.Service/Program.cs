using System;
using Topshelf;

namespace DnsProxy.Service
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            string listenConfig = null;

            var rc = HostFactory.Run(x =>
            {
                x.AddCommandLineDefinition("listen", value => listenConfig = value);

                x.Service<DnsProxyService>(svc =>
                {
                    svc.ConstructUsing(name => new DnsProxyService(listenConfig));
                    svc.WhenStarted(dps => dps.Start());
                    svc.WhenStopped(dps => dps.Stop());
                });
                x.RunAsNetworkService();

                x.SetDescription("Simple DNS Proxy Service");
                x.SetDisplayName("Simple DNS Proxy");
                x.SetServiceName("simple-dns-proxy");
            });

            var exitCode = (int) rc;
            Environment.ExitCode = exitCode;
        }
    }
}
