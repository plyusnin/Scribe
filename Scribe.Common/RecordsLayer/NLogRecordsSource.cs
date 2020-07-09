using System;
using System.Reactive.Linq;
using Scribe.EventsLayer;
using Scribe.EventsLayer.NLog;

namespace Scribe.RecordsLayer
{
    public class NLogRecordsSource : IRecordsSource
    {
        private readonly ILogSource<NLogEvent> _eventsSource;

        public NLogRecordsSource(ILogSource<NLogEvent> EventsSource)
        {
            _eventsSource = EventsSource;
            Source = _eventsSource.Source
                                  .Select(e => new LogRecord(DateTime.Now, e.Time, e.Logger, e.Sender, e.Message, e.Level, e.Exception));
        }

        public IObservable<LogRecord> Source { get; }
    }
}
