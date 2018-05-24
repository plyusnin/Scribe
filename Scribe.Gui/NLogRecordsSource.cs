using System;
using System.Reactive.Linq;
using Scribe.Gui.ViewModel;

namespace Scribe.Gui
{
    public class NLogRecordsSource : IRecordsSource
    {
        private readonly ILogSource<NLogEvent> _eventsSource;

        public NLogRecordsSource(ILogSource<NLogEvent> EventsSource)
        {
            _eventsSource = EventsSource;
            Source = _eventsSource.Source
                                  .Select(e => new LogRecord(e.Time, e.Logger, e.Message));
        }

        public IObservable<LogRecord> Source { get; }
    }
}
