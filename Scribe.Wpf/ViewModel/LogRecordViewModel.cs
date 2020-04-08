using System;
using ReactiveUI;
using Scribe.EventsLayer;
using Splat;

namespace Scribe.Wpf.ViewModel
{
    public class LogRecordViewModel : ReactiveObject
    {
        private bool _isHighlighted;

        public LogRecordViewModel(DateTime Time, SourceViewModel Source, string Message, LogLevel Level, string Exception)
        {
            this.Time = Time;
            this.Source = Source;
            this.Message = Message;
            this.Level = Level;
            this.Exception = Exception;
        }

        public DateTime Time { get; }
        public SourceViewModel Source { get; }
        public string Message { get; }
        public LogLevel Level { get; }
        public string Exception { get; }

        public bool IsHighlighted
        {
            get => _isHighlighted;
            set => this.RaiseAndSetIfChanged(ref _isHighlighted, value);
        }
    }
}
