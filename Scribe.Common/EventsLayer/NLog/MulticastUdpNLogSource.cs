using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;

namespace Scribe.EventsLayer.NLog
{
    public class MulticastUdpNLogSource : NLogSourceBase, IDisposable
    {
        private readonly IDisposable _connection;
        private readonly Encoding _encoding = Encoding.UTF8;
        private readonly UdpClient _server;

        public MulticastUdpNLogSource()
        {
            _server = new UdpClient(48644);
            _server.JoinMulticastGroup(IPAddress.Parse("224.66.15.229"));

            var source =
                Observable.FromAsync(_server.ReceiveAsync)
                          .Repeat()
                          .Select(msg => new { sender = msg.RemoteEndPoint.Address, message = _encoding.GetString(msg.Buffer)})
                          .Select(x => Deserialize(x.message, x.sender.ToString()))
                          .Publish();
            Source      = source;
            _connection = source.Connect();
        }

        public override IObservable<NLogEvent> Source { get; }

        public void Dispose()
        {
            _connection.Dispose();
            _server.Dispose();
        }
    }
}