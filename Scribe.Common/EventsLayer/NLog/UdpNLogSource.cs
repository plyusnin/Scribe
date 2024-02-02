using System;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;

namespace Scribe.EventsLayer.NLog
{
    public class UdpNLogSource : NLogSourceBase, IDisposable
    {
        private readonly IDisposable _connection;
        private readonly Encoding _encoding = Encoding.UTF8;
        private readonly UdpClient _server;

        public UdpNLogSource()
        {
            _server = new UdpClient(48645);

            var source =
                Observable.FromAsync(_server.ReceiveAsync)
                          .Repeat()
                          .Select(msg => Deserialize(_encoding.GetString(msg.Buffer),
                                                     msg.RemoteEndPoint.Address.ToString()))
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