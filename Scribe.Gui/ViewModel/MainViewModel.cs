using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using ReactiveUI;
using Scribe.RecordsLayer;

namespace Scribe.Gui.ViewModel
{
    public class MainViewModel : ReactiveObject
    {
        private readonly ReactiveList<ConnectedLogRecord> _allRecords;
        private readonly IRecordsSource _recordsSource;
        private readonly ObservableAsPropertyHelper<TimeSpan> _selectedInterval;
        private readonly ReactiveList<SourceViewModel> _sources = new ReactiveList<SourceViewModel>();
        private readonly Dictionary<string, SourceViewModel> _sourcesDictionary = new Dictionary<string, SourceViewModel>();
        private readonly SourceViewModelFactory _sourceViewModelFactory;

        private SourceViewModel _selectedSource;

        public MainViewModel(IRecordsSource RecordsSource, SourceViewModelFactory SourceViewModelFactory)
        {
            _recordsSource = RecordsSource;
            _sourceViewModelFactory = SourceViewModelFactory;

            Sources = _sources.CreateDerivedCollection(x => x,
                                                       orderer: (a, b) => string.CompareOrdinal(a.Name, b.Name),
                                                       scheduler: DispatcherScheduler.Current);

            _allRecords = new ReactiveList<ConnectedLogRecord>();

            _recordsSource.Source
                          .Buffer(TimeSpan.FromMilliseconds(100))
                          .ObserveOnDispatcher()
                          .Select(CreateConnectedLogRecord)
                          .ObserveOn(DispatcherScheduler.Current)
                          .Subscribe(x => _allRecords.AddRange(x));

            Sources.ChangeTrackingEnabled = true;
            Sources.ItemsAdded.Select(x => Unit.Default)
                   .Merge(Sources.ItemChanged.Select(x => Unit.Default))
                   .ObserveOnDispatcher()
                   .Subscribe(_ => Records.Reset());

            Records = _allRecords.CreateDerivedCollection(x => new LogRecordViewModel(x.Record.Time, x.Source, x.Record.Message, x.Record.Level),
                                                          x => x.Source.IsSelected);

            SelectedRecords = new ReactiveList<LogRecordViewModel>();

            Clear = ReactiveCommand.Create(() => _allRecords.Clear(),
                                           _allRecords.IsEmptyChanged.Select(x => !x));

            this.WhenAnyObservable(x => x.SelectedRecords.Changed)
                .Select(_ => SelectedRecords)
                .Select(s => s.Count > 1 ? s.Max(r => r.Time) - s.Min(r => r.Time) : TimeSpan.Zero)
                .ToProperty(this, x => x.SelectedInterval, out _selectedInterval);
        }

        public ReactiveCommand Clear { get; }

        public TimeSpan SelectedInterval => _selectedInterval.Value;

        public SourceViewModel SelectedSource
        {
            get => _selectedSource;
            set => this.RaiseAndSetIfChanged(ref _selectedSource, value);
        }

        public IReactiveDerivedList<LogRecordViewModel> Records { get; }
        public IReactiveDerivedList<SourceViewModel> Sources { get; }

        public IReactiveList<LogRecordViewModel> SelectedRecords { get; }

        private IEnumerable<ConnectedLogRecord> CreateConnectedLogRecord(IList<LogRecord> NewRecords)
        {
            foreach (var record in NewRecords)
            {
                SourceViewModel source;
                if (!_sourcesDictionary.TryGetValue(record.Source, out source))
                {
                    source = _sourceViewModelFactory.CreateInstance(record.Source);

                    _sourcesDictionary.Add(record.Source, source);
                    _sources.Add(source);
                }
                yield return new ConnectedLogRecord(record, source);
            }
        }

        private class ConnectedLogRecord
        {
            public ConnectedLogRecord(LogRecord Record, SourceViewModel Source)
            {
                this.Record = Record;
                this.Source = Source;
            }

            public LogRecord Record { get; }
            public SourceViewModel Source { get; }
        }
    }
}
