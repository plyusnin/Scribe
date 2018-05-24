using System;
using System.Reactive.Concurrency;
using ReactiveUI;

namespace Scribe.Gui.ViewModel
{
    public class MainViewModel : ReactiveObject
    {
        private readonly IRecordsSource _recordsSource;

        public MainViewModel(IRecordsSource RecordsSource)
        {
            _recordsSource = RecordsSource;

            Records = _recordsSource.Source.CreateCollection(DispatcherScheduler.Current);
        }

        public IReactiveDerivedList<LogRecord> Records { get; }
    }

    public interface IRecordsSource
    {
        IObservable<LogRecord> Source { get; }
    }

    public class LogRecord
    {
        public LogRecord(DateTime Time, string Source, string Message)
        {
            this.Time = Time;
            this.Source = Source;
            this.Message = Message;
        }

        public DateTime Time { get; }
        public string Source { get; }
        public string Message { get; }
    }
}
