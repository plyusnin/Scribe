using System;
using JetBrains.Annotations;

namespace Scribe.EventsLayer
{
    public interface IEvent
    {
        LogLevel Level { get; }
        string Logger { get; }
        string Message { get; }
        DateTime Time { get; }
        [CanBeNull] string Exception { get; }
    }
}
