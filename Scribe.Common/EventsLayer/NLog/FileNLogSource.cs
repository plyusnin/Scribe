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

        public async Task OpenFileAsync(string FileName, CancellationToken Cancellation, IProgress<double> Progress)
        {
            var reader = new StreamReader(FileName);
            while (!reader.EndOfStream)
            {
                Cancellation.ThrowIfCancellationRequested();
                Progress.Report((double)reader.BaseStream.Position / reader.BaseStream.Length);
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                Progress.Report((double)reader.BaseStream.Position / reader.BaseStream.Length);
                var nLogEvent = Deserialize(line, Path.GetFileNameWithoutExtension(FileName));
                _emitter.OnNext(nLogEvent);
            }
        }
    }
}
