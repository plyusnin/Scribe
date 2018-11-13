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
                                  .Select(e => new LogRecord(e.Time, e.Logger, e.Message, e.Level));
        }

        public IObservable<LogRecord> Source { get; }
    }
}
