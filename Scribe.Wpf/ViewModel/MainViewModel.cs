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
using DynamicData;
using DynamicData.Alias;
using DynamicData.Binding;
using LogList.Control.Manipulation.Implementations;
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
        private readonly ReadOnlyObservableCollection<SourceNodeViewModel> _sourcesObservableCollection;

        private bool _autoScroll = true;
        private string _quickFilter;

        public MainViewModel(
            IRecordsSource RecordsSource, SourceViewModelFactory SourceViewModelFactory,
            ICollection<ILogFileOpener> LogFileOpeners)
        {
            _logFileOpeners = LogFileOpeners;

            IRecordsCache recordsCache =
                new RecordsCache(SourceViewModelFactory, this.WhenAnyValue(x => x.QuickFilter));

            this.WhenAnyValue(x => x.AutoScroll)
                .Subscribe(v => recordsCache.AutoScroll = v);

            RecordsSource.Source
                         .Buffer(TimeSpan.FromMilliseconds(100))
                         .Where(list => list.Any())
                         .Subscribe(recordsCache.PutRecords);

            // Binding Sources
            recordsCache.Sources.Connect()
                        .Sort(SortExpressionComparer<SourceViewModel>.Ascending(s => s.Name))
                        .ObserveOnDispatcher()
                        .TransformToTree(x => x.ParentId)
                        .Transform(node => new SourceNodeViewModel(node))
                        .Bind(out _sourcesObservableCollection)
                        .Subscribe();

            Records = recordsCache.VisibleRecords;

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


            HighlightRecord = ReactiveCommand.Create<LogRecordViewModel>(rec => rec.IsHighlighted = !rec.IsHighlighted);
            // recordsCache.VisibleRecords.Connect()
            //             .AutoRefresh(x => x.IsHighlighted)
            //             .Filter(x => x.IsHighlighted)
            //             .Sort(SortExpressionComparer<LogRecordViewModel>.Ascending(x => x.Time))
            //             .ObserveOnDispatcher()
            //             .Bind(out _highlightedRecords)
            //             .Subscribe();
            HighlightedRecords =
                new ReadOnlyObservableCollection<LogRecordViewModel>(new ObservableCollection<LogRecordViewModel>());

            SelectedRecords = new ObservableCollection<LogRecordViewModel>();

            Clear = ReactiveCommand.Create(() => recordsCache.Clear(),
                                           // recordsCache
                                           //    .VisibleRecords.Connect().IsEmpty().Select(e => !e).ObserveOnDispatcher(),
                                           Observable.Return(true),
                                           DispatcherScheduler.Current);

            SelectedRecords.ObserveCollectionChanges()
                           .Select(_ => SelectedRecords)
                           .Select(s => s.Count > 1 ? s.Max(r => r.OriginalTime) - s.Min(r => r.OriginalTime) : TimeSpan.Zero)
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
            
            this.Progress = new ProgressViewModel();
        }

        public ProgressViewModel Progress { get; }

        public ReactiveCommand<LogRecordViewModel, Unit>        HighlightRecord    { get; }
        public ReadOnlyObservableCollection<LogRecordViewModel> HighlightedRecords { get; }

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

        public bool AutoScroll
        {
            get => _autoScroll;
            set => this.RaiseAndSetIfChanged(ref _autoScroll, value);
        }

        public ListViewModel<LogRecordViewModel>             Records { get; }
        public ReadOnlyObservableCollection<SourceNodeViewModel> Sources => _sourcesObservableCollection;

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

                Progress.IsActive = true;
                Progress.Text     = "Загрузка";
                await opener.OpenFileAsync(openDialog.FileName, Cancellation, Progress).ConfigureAwait(true);
                Progress.IsActive = false;
            }
        }
    }
}