using System;
using System.Net;
using System.Threading.Tasks;
using ARSoft.Tools.Net.Dns;

namespace DnsProxy.Service
{
    internal class DnsProxyService
    {
        private readonly object _syncRoot = new object();
        private readonly string[] _listenAddresses;
        private readonly DnsServer[] _servers;
        private bool _started;

        public DnsProxyService(string listenConfig)
        {
            if (string.IsNullOrEmpty(listenConfig))
            {
                throw new ArgumentNullException(nameof(listenConfig));
            }

            _listenAddresses = listenConfig.Split(';');
            _servers = new DnsServer[_listenAddresses.Length];
        }

        public void Start()
        {
            if (_started)
            {
                return;
            }

            lock (_syncRoot)
            {

                for (int i = 0; i < _listenAddresses.Length; i++)
                {
                    var address = IPAddress.Parse(_listenAddresses[i]);
                    var server = new DnsServer(address, 10, 10);

                    server.QueryReceived += ServerQueryReceived;
                    server.Start();
                    _servers[i] = server;

                    DnsProxyLogger.Instance.ProxyStarted(_listenAddresses[i]);
                }

                _started = true;
            }
        }

        public void Stop()
        {
            if (!_started)
            {
                return;
            }

            lock (_syncRoot)
            {
                for (int i = 0; i < _servers.Length; i++)
                {
                    _servers[i].Stop();
                    _servers[i].QueryReceived -= ServerQueryReceived;
                    _servers[i] = null;

                    DnsProxyLogger.Instance.ProxyStopped(_listenAddresses[i]);
                }

                _started = false;
            }
        }

        private static async Task ServerQueryReceived(object sender, QueryReceivedEventArgs eventArgs)
        {
            if (!(eventArgs.Query is DnsMessage message))
            {
                return;
            }

            if (DnsProxyLogger.Instance.IsEnabled())
            {
                DnsProxyLogger.Instance.DnsQuery(message.Questions.Count);
                // log the questions
                for (int i = 0; i < message.Questions.Count; i++)
                {
                    DnsProxyLogger.Instance.DnsQuestion(i, message.Questions[i].ToString());
                }
            }

            // relay the message to upstream servers
            var response = await DnsClient.Default.SendMessageAsync(message);

            eventArgs.Response = response;
        }
    }
}
