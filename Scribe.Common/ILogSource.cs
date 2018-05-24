using System;

namespace Scribe
{
    public interface ILogSource<out TLogElement> where TLogElement : IEvent
    {
        IObservable<TLogElement> Source { get; }
    }
}
