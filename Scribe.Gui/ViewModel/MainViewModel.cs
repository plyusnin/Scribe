using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using ReactiveUI;

namespace Scribe.Gui.ViewModel
{
    public class MainViewModel : ReactiveObject
    {
        private readonly IReactiveDerivedList<LogRecord> _allRecords;
        private readonly IRecordsSource _recordsSource;
        private HashSet<string> _enabledSources = new HashSet<string>();
        private readonly ObservableAsPropertyHelper<TimeSpan> _selectedInterval;

        private IList<LogRecord> _selectedRecords = new List<LogRecord>();

        public MainViewModel(IRecordsSource RecordsSource)
        {
            _recordsSource = RecordsSource;

            Sources = _recordsSource.Source
                                    .Select(s => s.Source)
                                    .Distinct()
                                    .Select(x => new SourceViewModel(true, x))
                                    .CreateCollection(DispatcherScheduler.Current);

            _allRecords = _recordsSource.Source
                                        .Buffer(TimeSpan.FromMilliseconds(100))
                                        .SelectMany(x => x)
                                        .CreateCollection(DispatcherScheduler.Current);

            Records = _allRecords.CreateDerivedCollection(x => x,
                                                          x => _enabledSources.Contains(x.Source));

            Sources.ChangeTrackingEnabled = true;
            Sources.ItemsAdded.Select(x => Unit.Default)
                   .Merge(Sources.ItemChanged.Select(x => Unit.Default))
                   .ObserveOnDispatcher()
                   .Subscribe(_ =>
                              {
                                  _enabledSources = new HashSet<string>(Sources.Where(s => s.IsSelected).Select(s => s.Name));
                                  Records.Reset();
                              });

            SelectedRecords = new ReactiveList<LogRecord>();

            this.WhenAnyObservable(x => x.SelectedRecords.Changed)
                .Select(_ => SelectedRecords)
                .Select(s => s.Count > 1 ? s.Max(r => r.Time) - s.Min(r => r.Time) : TimeSpan.Zero)
                .ToProperty(this, x => x.SelectedInterval, out _selectedInterval);
        }

        public TimeSpan SelectedInterval => _selectedInterval.Value;

        public IReactiveDerivedList<LogRecord> Records { get; }
        public IReactiveDerivedList<SourceViewModel> Sources { get; }

        public IReactiveList<LogRecord> SelectedRecords { get; }
    }
}
