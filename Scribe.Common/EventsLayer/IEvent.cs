using System;

namespace Scribe.EventsLayer
{
    public interface IEvent
    {
        string Logger { get; }
        string Message { get; }
        DateTime Time { get; }
    }
}
