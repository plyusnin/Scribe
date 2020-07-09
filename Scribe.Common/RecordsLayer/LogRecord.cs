using System;
using Scribe.EventsLayer;

namespace Scribe.RecordsLayer
{
    public class LogRecord
    {
        public LogRecord(
            DateTime Time, DateTime OriginalTime, string Source, string Sender, string Message, LogLevel Level,
            string Exception)
        {
            this.Time         = Time;
            this.Source       = Source;
            this.Message      = Message;
            this.Level        = Level;
            this.Exception    = Exception;
            this.OriginalTime = OriginalTime;
            this.Sender       = Sender;
        }

        public DateTime Time         { get; }
        public DateTime OriginalTime { get; }
        public string   Source       { get; }
        public string   Message      { get; }
        public LogLevel Level        { get; }
        public string   Exception    { get; }
        public string   Sender       { get; }
    }
}