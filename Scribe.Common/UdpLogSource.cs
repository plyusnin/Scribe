using System;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Scribe
{
    public class UdpLogSource : ILogSource<NLogEvent>, IDisposable
    {
        private readonly IDisposable _connection;
        private readonly Encoding _encoding = Encoding.UTF8;
        private readonly UdpClient _server;

        public UdpLogSource()
        {
            _server = new UdpClient(48645);

            var source =
                Observable.FromAsync(_server.ReceiveAsync)
                          .Repeat()
                          .Select(msg => _encoding.GetString(msg.Buffer))
                          .Select(Deserialize)
                          .Publish();
            Source = source;
            _connection = source.Connect();
        }

        private NLogEvent Deserialize(string msg)
        {
            try
            {
                return JsonConvert.DeserializeObject<NLogEvent>(msg);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void Dispose()
        {
            _connection.Dispose();
            _server.Dispose();
        }

        public IObservable<NLogEvent> Source { get; }
    }
}
