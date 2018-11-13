using System;

namespace Scribe.EventsLayer.NLog
{
    public class NLogEvent : IEvent
    {
        public NLogEvent(string Logger, LogLevel Level, string Message, DateTime Time, string Exception)
        {
            this.Logger = Logger;
            this.Level = Level;
            this.Message = Message;
            this.Time = Time;
            this.Exception = Exception;
        }

        public LogLevel Level { get; }
        public string Exception { get; }

        public string Logger { get; }
        public string Message { get; }
        public DateTime Time { get; }

        public override string ToString()
        {
            return $"{nameof(Logger)}: {Logger}, {nameof(Message)}: {Message}, {nameof(Time)}: {Time}";
        }
    }
}
