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
                }

                _started = false;
            }
        }

        // Implementation copied from https://github.com/rtumaykin/hyperv-dnsproxy/blob/master/DnsProxy.Service/ServiceManager.cs
        private static async Task ServerQueryReceived(object sender, QueryReceivedEventArgs eventArgs)
        {
            var message = eventArgs.Query as DnsMessage;
            var response = message?.CreateResponseInstance();

            if (message?.Questions.Count == 1)
            {
                // send query to upstream _servers
                var question = message.Questions[0];

                var upstreamResponse =
                    await DnsClient.Default.ResolveAsync(question.Name, question.RecordType, question.RecordClass);

                // if got an answer, copy it to the message sent to the client
                if (upstreamResponse != null)
                {
                    foreach (var record in (upstreamResponse.AnswerRecords))
                    {
                        response.AnswerRecords.Add(record);
                    }
                    foreach (var record in (upstreamResponse.AdditionalRecords))
                    {
                        response.AdditionalRecords.Add(record);
                    }

                    response.ReturnCode = ReturnCode.NoError;

                    // set the response
                    eventArgs.Response = response;
                }
            }
        }
    }
}
