using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
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

        private readonly ReadOnlyObservableCollection<LogRecordViewModel> _highlightedRecords;
        private readonly ICollection<ILogFileOpener> _logFileOpeners;

        private readonly ReadOnlyObservableCollection<LogRecordViewModel> _recordsObservableCollection;
        private readonly ObservableAsPropertyHelper<TimeSpan> _selectedInterval;
        private readonly ObservableAsPropertyHelper<LogRecordViewModel> _selectedRecord;
        private readonly ReadOnlyObservableCollection<SourceViewModel> _sourcesObservableCollection;
        private string _quickFilter;

        public MainViewModel(IRecordsSource RecordsSource, SourceViewModelFactory SourceViewModelFactory,
                             ICollection<ILogFileOpener> LogFileOpeners)
        {
            _logFileOpeners = LogFileOpeners;

            IRecordsCache recordsCache = new RecordsCache(SourceViewModelFactory);

            RecordsSource.Source
                         .Buffer(TimeSpan.FromMilliseconds(100))
                         .Where(list => list.Any())
                         .ObserveOn(TaskPoolScheduler.Default)
                         .Subscribe(recordsCache.PutRecords);

            // Binding Sources
            recordsCache.Sources.Connect()
                        .Sort(SortExpressionComparer<SourceViewModel>.Ascending(s => s.Name))
                        .ObserveOnDispatcher()
                        .Bind(out _sourcesObservableCollection)
                        .Subscribe();

            // Binding Records
            recordsCache.VisibleRecords.Connect()
                        .ObserveOnDispatcher()
                        .Bind(out _recordsObservableCollection)
                        .Subscribe();

            OpenLogFile = ReactiveCommand.CreateFromTask(OpenLogFileRoutine,
                                                         outputScheduler: DispatcherScheduler.Current);
            OpenLogFile.ThrownExceptions
                       .Subscribe(e => MessageBox.Show(e.Message, "Ой!"));


            this.WhenAnyValue(x => x.QuickFilter)
                .Throttle(TimeSpan.FromMilliseconds(100))
                .Select<string, Func<string, bool>>(
                     qf => v => string.IsNullOrWhiteSpace(qf) ||
                                v.IndexOf(qf, StringComparison.CurrentCultureIgnoreCase) >= 0)
                .Subscribe(f => recordsCache.Filter(r => f(r.Message)));

            RecordsOnReset = recordsCache.OnResetObservable.ObserveOnDispatcher(DispatcherPriority.DataBind);

            HighlightRecord = ReactiveCommand.Create<LogRecordViewModel>(rec => rec.IsHighlighted = !rec.IsHighlighted);
            recordsCache.VisibleRecords.Connect()
                        .AutoRefresh(x => x.IsHighlighted)
                        .Filter(x => x.IsHighlighted)
                        .Sort(SortExpressionComparer<LogRecordViewModel>.Ascending(x => x.Time))
                        .ObserveOnDispatcher()
                        .Bind(out _highlightedRecords)
                        .Subscribe();

            SelectedRecords = new ObservableCollection<LogRecordViewModel>();

            Clear = ReactiveCommand.Create(() => recordsCache.Clear(),
                                           recordsCache
                                              .VisibleRecords.Connect().IsEmpty().Select(e => !e).ObserveOnDispatcher(),
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

        public IObservable<Unit> RecordsOnReset { get; }

        public ReactiveCommand<LogRecordViewModel, Unit>        HighlightRecord    { get; }
        public ReadOnlyObservableCollection<LogRecordViewModel> HighlightedRecords => _highlightedRecords;

        public ReactiveCommand<Unit, Unit> OpenLogFile { get; }

        public TimeSpan           SelectedInterval   => _selectedInterval.Value;
        public bool               HasExceptionToShow => _hasExceptionToShow.Value;
        public LogRecordViewModel SelectedRecord     => _selectedRecord.Value;

        public ReactiveCommand<Unit, Unit> Clear { get; }

        public string QuickFilter
        {
            get => _quickFilter;
            set => this.RaiseAndSetIfChanged(ref _quickFilter, value);
        }

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
    }
}