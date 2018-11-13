using System.Windows;
using System.Windows.Controls;
using Scribe.EventsLayer;
using Scribe.Gui.ViewModel;

namespace Scribe.Gui
{
    public class ItemLevelStyleSelector : StyleSelector
    {
        public Style DefaultStyle { get; set; }

        public Style TraceStyle { get; set; }
        public Style DebugStyle { get; set; }
        public Style InfoStyle { get; set; }
        public Style WarnStyle { get; set; }
        public Style ErrorStyle { get; set; }
        public Style FatalStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            var record = (LogRecordViewModel)item;
            switch (record.Level)
            {
                case LogLevel.Trace: return TraceStyle ?? DefaultStyle;
                case LogLevel.Debug: return DebugStyle ?? DefaultStyle;
                case LogLevel.Info: return InfoStyle ?? DefaultStyle;
                case LogLevel.Warn: return WarnStyle ?? DefaultStyle;
                case LogLevel.Error: return ErrorStyle ?? DefaultStyle;
                case LogLevel.Fatal: return FatalStyle ?? DefaultStyle;
                default: return DefaultStyle;
            }
        }
    }
}
