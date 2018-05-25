using System;

namespace Scribe.Gui.ViewModel
{
    public class LogRecordViewModel
    {
        public LogRecordViewModel(DateTime Time, SourceViewModel Source, string Message)
        {
            this.Time = Time;
            this.Source = Source;
            this.Message = Message;
        }

        public DateTime Time { get; }
        public SourceViewModel Source { get; }
        public string Message { get; }
    }
}
