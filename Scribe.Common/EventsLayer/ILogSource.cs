using System;

namespace Scribe.EventsLayer
{
    public interface ILogSource<out TLogElement> where TLogElement : IEvent
    {
        IObservable<TLogElement> Source { get; }
    }
}
