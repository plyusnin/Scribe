using System;
using ReactiveUI;
using Scribe.EventsLayer;

namespace Scribe.Wpf.ViewModel
{
    public class LogRecordViewModel : ReactiveObject
    {
        private bool _isHighlighted;

        public LogRecordViewModel(long Id, DateTime Time, SourceViewModel Source, string Message, LogLevel Level,
                                  string Exception)
        {
            this.Id        = Id;
            this.Time      = Time;
            this.Source    = Source;
            this.Message   = Message;
            this.Level     = Level;
            this.Exception = Exception;
        }

        public long            Id        { get; }
        public DateTime        Time      { get; }
        public SourceViewModel Source    { get; }
        public string          Message   { get; }
        public LogLevel        Level     { get; }
        public string          Exception { get; }

        public bool IsHighlighted
        {
            get => _isHighlighted;
            set => this.RaiseAndSetIfChanged(ref _isHighlighted, value);
        }
    }
}