using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using ReactiveUI;
using Scribe.EventsLayer;
using Scribe.RecordsLayer;

namespace Scribe.Gui.ViewModel
{
    public class MainViewModel : ReactiveObject
    {
        private readonly ReactiveList<ConnectedLogRecord> _allRecords;
        private readonly ObservableAsPropertyHelper<bool> _hasExceptionToShow;
        private readonly ICollection<ILogFileOpener> _logFileOpeners;
        private readonly IRecordsSource _recordsSource;
        private readonly ObservableAsPropertyHelper<TimeSpan> _selectedInterval;
        private readonly ObservableAsPropertyHelper<LogRecordViewModel> _selectedRecord;
        private readonly ReactiveList<SourceViewModel> _sources = new ReactiveList<SourceViewModel>();
        private readonly ConcurrentDictionary<string, SourceViewModel> _sourcesDictionary = new ConcurrentDictionary<string, SourceViewModel>();
        private readonly SourceViewModelFactory _sourceViewModelFactory;

        private string _quickFilter;

        private SourceViewModel _selectedSource;

        public MainViewModel(IRecordsSource RecordsSource, SourceViewModelFactory SourceViewModelFactory, ICollection<ILogFileOpener> LogFileOpeners)
        {
            _recordsSource = RecordsSource;
            _sourceViewModelFactory = SourceViewModelFactory;
            _logFileOpeners = LogFileOpeners;

            Sources = _sources.CreateDerivedCollection(x => x,
                                                       orderer: (a, b) => string.CompareOrdinal(a.Name, b.Name),
                                                       scheduler: DispatcherScheduler.Current);

            OpenLogFile = ReactiveCommand.CreateFromTask(OpenLogFileRoutine, outputScheduler: DispatcherScheduler.Current);
            OpenLogFile.ThrownExceptions
                       .Subscribe(e => MessageBox.Show(e.Message, "Ой!"));

            _allRecords = new ReactiveList<ConnectedLogRecord>();

            _recordsSource.Source
                          .Buffer(TimeSpan.FromMilliseconds(100))
                          .Where(list => list.Any())
                          .Select(l => CreateConnectedLogRecord(l).ToList())
                          .ObserveOnDispatcher()
                          .Subscribe(x => _allRecords.AddRange(x));

            var selectedRecords = _allRecords.CreateDerivedCollection(
                x => new LogRecordViewModel(x.Record.Time, x.Source, x.Record.Message, x.Record.Level, x.Record.Exception),
                x => x.Source.IsSelected && x.Source.SelectedLevels.Contains(x.Record.Level));

            Sources.ChangeTrackingEnabled = true;
            Sources.ItemsAdded.Select(x => Unit.Default)
                   .Merge(Sources.ItemChanged.Select(x => Unit.Default))
                   .ObserveOnDispatcher()
                   .Subscribe(_ => selectedRecords.Reset());

            Records = selectedRecords.CreateDerivedCollection(
                x => x,
                x => string.IsNullOrWhiteSpace(QuickFilter)
                     || x.Message.IndexOf(QuickFilter, StringComparison.CurrentCultureIgnoreCase) >= 0,
                signalReset: this.WhenAnyValue(x => x.QuickFilter)
                                 .Throttle(TimeSpan.FromMilliseconds(200))
                                 .ObserveOnDispatcher()
                                 .Select(x => Unit.Default));

            HighlightRecord = ReactiveCommand.Create<LogRecordViewModel>(rec => rec.IsHighlighted = !rec.IsHighlighted);
            HighlightedRecords = Records.CreateDerivedCollection(x => x, x => x.IsHighlighted,
                                                                 scheduler: DispatcherScheduler.Current,
                                                                 signalReset: HighlightRecord);

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

        public ReactiveCommand<LogRecordViewModel, Unit> HighlightRecord { get; }

        public IReactiveDerivedList<LogRecordViewModel> HighlightedRecords { get; }

        public ReactiveCommand<Unit, Unit> OpenLogFile { get; }

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

        private async Task OpenLogFileRoutine(CancellationToken Cancellation)
        {
            Cancellation.ThrowIfCancellationRequested();
            var openDialog = new OpenFileDialog
            {
                Title = "Открыть лог-файл",
                DefaultExt = _logFileOpeners.First().Extension,
                Filter = string.Join("|", _logFileOpeners.Select(o => $"{o.Description} (*.{o.Extension})|*.{o.Extension}")) + "|All files (*.*)|*.*"
            };
            if (openDialog.ShowDialog() == true)
            {
                var fileExt = Path.GetExtension(openDialog.FileName).ToLower();
                var opener = _logFileOpeners.FirstOrDefault(o => "." + o.Extension.ToLower() == fileExt)
                             ?? throw new ApplicationException($"Файлы формата {fileExt} не поддерживаются");

                using (_allRecords.SuppressChangeNotifications())
                //using (Records.SuppressChangeNotifications())
                //using (HighlightedRecords.SuppressChangeNotifications())
                {
                    await opener.OpenFileAsync(openDialog.FileName, Cancellation).ConfigureAwait(true);
                }
            }
        }

        private IEnumerable<ConnectedLogRecord> CreateConnectedLogRecord(IList<LogRecord> NewRecords)
        {
            foreach (var record in NewRecords)
            {
                var source = _sourcesDictionary.GetOrAdd(
                    record.Source,
                    key =>
                    {
                        var s = _sourceViewModelFactory.CreateInstance(record.Source);
                        _sources.Add(s);
                        return s;
                    });
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
