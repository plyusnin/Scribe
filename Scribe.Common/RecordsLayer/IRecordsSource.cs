using System;

namespace Scribe.RecordsLayer
{
    public interface IRecordsSource
    {
        IObservable<LogRecord> Source { get; }
    }
}
