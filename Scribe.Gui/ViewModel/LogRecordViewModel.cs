using System;
using Scribe.EventsLayer;

namespace Scribe.Gui.ViewModel
{
    public class LogRecordViewModel
    {
        public LogRecordViewModel(DateTime Time, SourceViewModel Source, string Message, LogLevel Level)
        {
            this.Time = Time;
            this.Source = Source;
            this.Message = Message;
            this.Level = Level;
        }

        public DateTime Time { get; }
        public SourceViewModel Source { get; }
        public string Message { get; }
        public LogLevel Level { get; }
    }
}
