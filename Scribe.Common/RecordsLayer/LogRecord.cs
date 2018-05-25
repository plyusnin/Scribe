using System;

namespace Scribe.RecordsLayer
{
    public class LogRecord
    {
        public LogRecord(DateTime Time, string Source, string Message)
        {
            this.Time = Time;
            this.Source = Source;
            this.Message = Message;
        }

        public DateTime Time { get; }
        public string Source { get; }
        public string Message { get; }
    }
}
