using System;
using LogList.Control;
using LogList.Control.Manipulation.Implementations.Filtering;
using ReactiveUI;
using Scribe.EventsLayer;

namespace Scribe.Wpf.ViewModel
{
    public class LogRecordViewModel : ReactiveObject, ILogItem, IFilterableByString
    {
        private bool _isHighlighted;

        public LogRecordViewModel(
            DateTime Time, SourceViewModel Source, string Message, LogLevel Level,
            string Exception)
        {
            this.Time      = Time;
            this.Source    = Source;
            this.Message   = Message;
            this.Level     = Level;
            this.Exception = Exception;

            FilterString = Message;
        }

        public SourceViewModel Source    { get; }
        public string          Message   { get; }
        public LogLevel        Level     { get; }
        public string          Exception { get; }

        public bool IsHighlighted
        {
            get => _isHighlighted;
            set => this.RaiseAndSetIfChanged(ref _isHighlighted, value);
        }

        public string FilterString { get; }

        public DateTime Time { get; }
    }
}