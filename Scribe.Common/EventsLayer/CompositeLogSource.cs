using System;
using System.Linq;
using System.Reactive.Linq;

namespace Scribe.EventsLayer
{
    public class CompositeLogSource<T> : ILogSource<T>, IDisposable
        where T : IEvent
    {
        private readonly ILogSource<T>[] _children;

        public CompositeLogSource(params ILogSource<T>[] Children)
        {
            _children = Children;
            Source = Children.Select(c => c.Source).Merge();
        }

        public void Dispose()
        {
            foreach (var disposableChild in _children.OfType<IDisposable>())
                disposableChild.Dispose();
        }

        public IObservable<T> Source { get; }
    }
}
