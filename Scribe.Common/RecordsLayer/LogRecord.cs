﻿using System;
using Scribe.EventsLayer;

namespace Scribe.RecordsLayer
{
    public class LogRecord
    {
        public LogRecord(DateTime Time, string Source, string Message, LogLevel Level)
        {
            this.Time = Time;
            this.Source = Source;
            this.Message = Message;
            this.Level = Level;
        }

        public DateTime Time { get; }
        public string Source { get; }
        public string Message { get; }
        public LogLevel Level { get; }
    }
}
