using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Win32;
using ReactiveUI;
using Scribe.EventsLayer;
using Scribe.RecordsLayer;

namespace Scribe.Wpf.ViewModel
{
    public class MainViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<bool> _hasExceptionToShow;
        private readonly ICollection<ILogFileOpener> _logFileOpeners;
        private readonly ObservableAsPropertyHelper<TimeSpan> _selectedInterval;
        private readonly ObservableAsPropertyHelper<LogRecordViewModel> _selectedRecord;

        private readonly SourceCache<SourceViewModel, string> _sources =
            new SourceCache<SourceViewModel, string>(s => s.Name);

        private readonly ConcurrentDictionary<string, SourceViewModel> _sourcesDictionary =
            new ConcurrentDictionary<string, SourceViewModel>();

        private readonly SourceViewModelFactory _sourceViewModelFactory;

        private string _quickFilter;

        private SourceViewModel _selectedSource;

        public MainViewModel(IRecordsSource RecordsSource, SourceViewModelFactory SourceViewModelFactory,
                             ICollection<ILogFileOpener> LogFileOpeners)
        {
            _sourceViewModelFactory = SourceViewModelFactory;
            _logFileOpeners         = LogFileOpeners;

            _sources.Connect()
                    .Sort(SortExpressionComparer<SourceViewModel>.Ascending(s => s.Name))
                    .ObserveOnDispatcher()
                    .Bind(out _sourcesObservableCollection)
                    .Subscribe();

            OpenLogFile =
                ReactiveCommand.CreateFromTask(OpenLogFileRoutine, outputScheduler: DispatcherScheduler.Current);
            OpenLogFile.ThrownExceptions
                       .Subscribe(e => MessageBox.Show(e.Message, "Ой!"));

            var allRecords = new SourceList<ConnectedLogRecord>();

            RecordsSource.Source
                         .Buffer(TimeSpan.FromMilliseconds(100))
                         .Where(list => list.Any())
                         .Select(CreateConnectedLogRecord)
                         .ObserveOnDispatcher()
                         .Subscribe(x => allRecords.Edit(list => list.AddRange(x)));

            var quickFilter = this.WhenAnyValue(x => x.QuickFilter)
                                  .Throttle(TimeSpan.FromMilliseconds(200))
                                  .Select<string, Func<string, bool>>(
                                       qf => v => string.IsNullOrWhiteSpace(qf) ||
                                                  v.IndexOf(qf, StringComparison.CurrentCultureIgnoreCase) >= 0)
                                  .Publish();

            quickFilter.Connect();

            var records = allRecords.Connect()
                                    .Transform(x => new LogRecordViewModel(
                                                   x.Record.Time, x.Source, x.Record.Message, x.Record.Level,
                                                   x.Record.Exception))
                                    .AsObservableList();


            // TODO: Не, так не годится. Это что он, на каждый элемент будет по Observable открывать?? На каждый элемент лога??
            records.Connect()
                   .FilterOnObservable(x => new[]
                    {
                        x.Source.IsSelectedObservable,
                        quickFilter.Select(f => f(x.Message)),
                        //x.Source.SelectedLevels.Contains(x.Level)
                    }.CombineLatest(val => val.All(a => a)))
                   .ObserveOnDispatcher()
                   .Bind(out _recordsObservableCollection)
                   .Subscribe();

            HighlightRecord = ReactiveCommand.Create<LogRecordViewModel>(rec => rec.IsHighlighted = !rec.IsHighlighted);
            records.Connect()
                   .AutoRefresh(x => x.IsHighlighted)
                   .Filter(x => x.IsHighlighted)
                   .Sort(SortExpressionComparer<LogRecordViewModel>.Ascending(x => x.Time))
                   .ObserveOnDispatcher()
                   .Bind(out _highlightedRecords)
                   .Subscribe();

            SelectedRecords = new ObservableCollection<LogRecordViewModel>();

            Clear = ReactiveCommand.Create(() => allRecords.Clear(),
                                           allRecords.Connect().IsEmpty().Select(e => !e),
                                           DispatcherScheduler.Current);

            SelectedRecords.ObserveCollectionChanges()
                           .Select(_ => SelectedRecords)
                           .Select(s => s.Count > 1 ? s.Max(r => r.Time) - s.Min(r => r.Time) : TimeSpan.Zero)
                           .ObserveOnDispatcher()
                           .ToProperty(this, x => x.SelectedInterval, out _selectedInterval);

            SelectedRecords.ObserveCollectionChanges()
                           .Select(_ => SelectedRecords)
                           .Select(sel => sel.Count == 1 ? sel[0] : null)
                           .ObserveOnDispatcher()
                           .ToProperty(this, x => x.SelectedRecord, out _selectedRecord);

            this.WhenAnyValue(x => x.SelectedRecord)
                .Select(x => x?.Exception != null)
                .ToProperty(this, x => x.HasExceptionToShow, out _hasExceptionToShow);
        }

        public ReactiveCommand<LogRecordViewModel, Unit> HighlightRecord { get; }

        private readonly ReadOnlyObservableCollection<LogRecordViewModel> _highlightedRecords;
        public ReadOnlyObservableCollection<LogRecordViewModel> HighlightedRecords => _highlightedRecords;

        public ReactiveCommand<Unit, Unit> OpenLogFile { get; }

        public TimeSpan           SelectedInterval   => _selectedInterval.Value;
        public bool               HasExceptionToShow => _hasExceptionToShow.Value;
        public LogRecordViewModel SelectedRecord     => _selectedRecord.Value;

        public ReactiveCommand<Unit, Unit> Clear { get; }

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

        private readonly ReadOnlyObservableCollection<LogRecordViewModel> _recordsObservableCollection;
        private readonly ReadOnlyObservableCollection<SourceViewModel> _sourcesObservableCollection;

        public ReadOnlyObservableCollection<LogRecordViewModel> Records => _recordsObservableCollection;
        public ReadOnlyObservableCollection<SourceViewModel>    Sources => _sourcesObservableCollection;

        public ObservableCollection<LogRecordViewModel> SelectedRecords { get; }

        private async Task OpenLogFileRoutine(CancellationToken Cancellation)
        {
            Cancellation.ThrowIfCancellationRequested();
            var openDialog = new OpenFileDialog
            {
                Title      = "Открыть лог-файл",
                DefaultExt = _logFileOpeners.First().Extension,
                Filter = string.Join(
                             "|", _logFileOpeners.Select(o => $"{o.Description} (*.{o.Extension})|*.{o.Extension}")) +
                         "|All files (*.*)|*.*"
            };
            if (openDialog.ShowDialog() == true)
            {
                var fileExt = Path.GetExtension(openDialog.FileName).ToLower();
                var opener = _logFileOpeners.FirstOrDefault(o => "." + o.Extension.ToLower() == fileExt)
                             ?? throw new ApplicationException($"Файлы формата {fileExt} не поддерживаются");

                await opener.OpenFileAsync(openDialog.FileName, Cancellation).ConfigureAwait(true);
            }
        }

        private long _recordId = 0;

        private IEnumerable<ConnectedLogRecord> CreateConnectedLogRecord(IList<LogRecord> NewRecords)
        {
            foreach (var record in NewRecords)
            {
                var source = _sourcesDictionary.GetOrAdd(
                    record.Source,
                    (key, facSource) =>
                    {
                        var s = _sourceViewModelFactory.CreateInstance(facSource);
                        _sources.AddOrUpdate(s);
                        return s;
                    },
                    record.Source);
                yield return new ConnectedLogRecord(Interlocked.Increment(ref _recordId), record, source);
            }
        }

        private class ConnectedLogRecord
        {
            public ConnectedLogRecord(long Id, LogRecord Record, SourceViewModel Source)
            {
                this.Id     = Id;
                this.Record = Record;
                this.Source = Source;
            }

            public long            Id     { get; }
            public LogRecord       Record { get; }
            public SourceViewModel Source { get; }
        }
    }
}