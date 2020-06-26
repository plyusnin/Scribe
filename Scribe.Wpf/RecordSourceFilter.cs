using System.Collections.Generic;
using System.Linq;
using LogList.Control.Manipulation;
using LogList.Control.Manipulation.Implementations.Filtering;
using Scribe.EventsLayer;
using Scribe.Wpf.ViewModel;

namespace Scribe.Wpf
{
    public class RecordSourceFilter : IFilter<LogRecordViewModel>
    {
        private readonly HashSet<(SourceViewModel source, LogLevel level)> _selection;

        public RecordSourceFilter(IEnumerable<SourceViewModel> Sources)
        {
            _selection = Sources.Where(s => s.IsSelected)
                                .SelectMany(s => s.SelectedLevels.Select(l => (s, l)))
                                .ToHashSet();
        }

        public bool Check(LogRecordViewModel Item)
        {
            return Item.Source.IsSelected && Item.Source.SelectedLevels.Contains(Item.Level);
        }

        public bool IsSubFilterFor(IFilter<LogRecordViewModel> Another)
        {
            if (Another == this)
                return true;

            switch (Another)
            {
                case EmptyFilter<LogRecordViewModel> empty:
                    return true;

                case RecordSourceFilter another:
                    return _selection.All(x => another._selection.Contains(x));

                default:
                    return false;
            }
        }
    }
}