using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Scribe.EventsLayer.NLog
{
    public class FileNLogSource : NLogSourceBase, ILogFileOpener, IDisposable
    {
        private readonly Subject<NLogEvent> _emitter;

        public FileNLogSource()
        {
            _emitter = new Subject<NLogEvent>();
        }

        public override IObservable<NLogEvent> Source => _emitter;

        public void Dispose()
        {
            _emitter?.Dispose();
        }

        public string Description => "Scribe Log File";
        public string Extension => "scl";

        public async Task OpenFileAsync(string FileName, CancellationToken Cancellation)
        {
            var reader = new StreamReader(FileName);
            while (!reader.EndOfStream)
            {
                Cancellation.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                var nLogEvent = Deserialize(line);
                _emitter.OnNext(nLogEvent);
            }
        }
    }
}
