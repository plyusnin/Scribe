using System;

namespace Scribe.Gui.ViewModel
{
    public interface IRecordsSource
    {
        IObservable<LogRecord> Source { get; }
    }
}
