﻿using System;
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

        private string _quickFilter;
        private readonly ObservableAsPropertyHelper<LogRecordViewModel> _selectedRecord;

        private SourceViewModel _selectedSource;
        private readonly ObservableAsPropertyHelper<bool> _hasExceptionToShow;

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
                          .Subscribe(x => _allRecords.AddRange(x));

            var selectedRecords = _allRecords.CreateDerivedCollection(
                x => new LogRecordViewModel(x.Record.Time, x.Source, x.Record.Message, x.Record.Level, x.Record.Exception),
                x => x.Source.IsSelected && x.Source.SelectedLevels.Contains(x.Record.Level),
                scheduler: DispatcherScheduler.Current);

            Sources.ChangeTrackingEnabled = true;
            Sources.ItemsAdded.Select(x => Unit.Default)
                   .Merge(Sources.ItemChanged.Select(x => Unit.Default))
                   .ObserveOnDispatcher()
                   .Subscribe(_ => selectedRecords.Reset());

            Records = selectedRecords.CreateDerivedCollection(
                x => x,
                x => string.IsNullOrWhiteSpace(QuickFilter)
                     || x.Message.ToLower().Contains(QuickFilter.ToLower()),
                signalReset: this.WhenAnyValue(x => x.QuickFilter).Select(x => Unit.Default),
                scheduler: DispatcherScheduler.Current);

            SelectedRecords = new ReactiveList<LogRecordViewModel>();

            Clear = ReactiveCommand.Create(() => _allRecords.Clear(),
                                           _allRecords.IsEmptyChanged.Select(x => !x),
                                           DispatcherScheduler.Current);

            this.WhenAnyObservable(x => x.SelectedRecords.Changed)
                .Select(_ => SelectedRecords)
                .Select(s => s.Count > 1 ? s.Max(r => r.Time) - s.Min(r => r.Time) : TimeSpan.Zero)
                .ToProperty(this, x => x.SelectedInterval, out _selectedInterval);

            this.WhenAnyObservable(x => x.SelectedRecords.Changed)
                .Select(_ => SelectedRecords)
                .Select(sel => sel.Count == 1 ? sel[0] : null)
                .ToProperty(this, x => x.SelectedRecord, out _selectedRecord);

            this.WhenAnyValue(x => x.SelectedRecord)
                .Select(x => x?.Exception != null)
                .ToProperty(this, x => x.HasExceptionToShow, out _hasExceptionToShow);
        }

        public TimeSpan SelectedInterval => _selectedInterval.Value;
        public bool HasExceptionToShow => _hasExceptionToShow.Value;
        public LogRecordViewModel SelectedRecord => _selectedRecord.Value;

        public ReactiveCommand Clear { get; }

        public SourceViewModel SelectedSource
        {
            get => _selectedSource;
            set => this.RaiseAndSetIfChanged(ref _selectedSource, value);
        }

        public string QuickFilter
        {
            get => _quickFilter;
            set => this.RaiseAndSetIfChanged(ref _quickFilter, value);
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
