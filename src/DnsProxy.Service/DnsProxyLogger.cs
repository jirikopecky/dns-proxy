using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace DnsProxy.Service
{
    [EventSource(Name = "JiriKopecky-DnsProxy-Logger")]
    internal sealed class DnsProxyLogger : EventSource
    {
        private const int ProxyStartedEvent = 1;
        private const int ProxyStoppedEvent = 2;

        private const int DnsQueryEvent = 10;
        private const int DnsQuestionEvent = 11;

        public static DnsProxyLogger Instance { get; } = new DnsProxyLogger();

        private DnsProxyLogger()
        {
            // A workaround for the problem where ETW activities do not get tracked until Tasks infrastructure is initialized.
            // This problem will be fixed in .NET Framework 4.6.2.
            Task.Run(() =>
            {
            });
        }

        [Event(ProxyStartedEvent, Level = EventLevel.Informational, Message = "DNS Proxy started, bound to address '{0}'.", Channel = EventChannel.Operational)]
        public void ProxyStarted(string address)
        {
            WriteEvent(ProxyStartedEvent, address);
        }

        [Event(ProxyStoppedEvent, Level = EventLevel.Informational, Message = "DNS Proxy for address '{0}' stopped.", Channel = EventChannel.Operational)]
        public void ProxyStopped(string address)
        {
            WriteEvent(ProxyStoppedEvent, address);
        }

        [Event(DnsQueryEvent, Level = EventLevel.Verbose, Message = "DNS Query ({0} questions)", Channel = EventChannel.Debug)]
        public void DnsQuery(int questionCount)
        {
            WriteEvent(DnsQueryEvent, questionCount);
        }

        [Event(DnsQuestionEvent, Level = EventLevel.Verbose, Message = "DNS question #{0}: {1}", Channel = EventChannel.Debug)]
        public void DnsQuestion(int questionIndex, string question)
        {
            WriteEvent(DnsQuestionEvent, questionIndex, question);
        }
    }
}
