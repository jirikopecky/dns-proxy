using System;
using Topshelf;
using Topshelf.StartParameters;

namespace DnsProxy.Service
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            string listenConfig = null;

            var rc = HostFactory.Run(x =>
            {
                x.EnableStartParameters();
                x.WithStartParameter("listen", value => listenConfig = value);

                x.Service<DnsProxyService>(svc =>
                {
                    svc.ConstructUsing(name => new DnsProxyService(listenConfig));
                    svc.WhenStarted(dps => dps.Start());
                    svc.WhenStopped(dps => dps.Stop());
                });

                x.SetDescription("Simple DNS Proxy Service");
                x.SetDisplayName("Simple DNS Proxy");
                x.SetServiceName("simple-dns-proxy");

                x.RunAsNetworkService();
                x.StartAutomatically();
                x.EnableServiceRecovery(r =>
                {
                    r.OnCrashOnly();
                    r.RestartService(1);
                    r.RestartService(1);
                    r.RestartService(1);
                    r.SetResetPeriod(0);
                });
            });

            var exitCode = (int) rc;
            Environment.ExitCode = exitCode;
        }
    }
}
