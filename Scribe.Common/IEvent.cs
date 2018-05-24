using System;

namespace Scribe
{
    public interface IEvent
    {
        string Logger { get; }
        string Message { get; }
        DateTime Time { get; }
    }
}
