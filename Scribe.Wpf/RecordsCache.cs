using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using DynamicData.PLinq;
using Scribe.RecordsLayer;
using Scribe.Wpf.ViewModel;

namespace Scribe.Wpf
{
    public interface IRecordsCache
    {
        void                                      PutRecords(IEnumerable<LogRecord> Records);
        IObservableList<LogRecordViewModel>       VisibleRecords { get; }
        IObservableCache<SourceViewModel, string> Sources        { get; }

        void Filter(Func<LogRecordViewModel, bool> Filter);
        void Clear();
    }

    public class RecordsCache : IRecordsCache
    {
        private readonly Subject<Unit> _filterSubject = new Subject<Unit>();

        private readonly Dictionary<string, SourceViewModel> _sources;

        private readonly SourceCache<SourceViewModel, string> _sourcesCache;

        private readonly object _insertionLocker = new object();
        private readonly SourceViewModelFactory _sourceViewModelFactory;

        private readonly List<LogRecordViewModel> _records = new List<LogRecordViewModel>();

        public RecordsCache(SourceViewModelFactory SourceViewModelFactory)
        {
            _sourceViewModelFactory = SourceViewModelFactory;

            _sources      = new Dictionary<string, SourceViewModel>();
            _sourcesCache = new SourceCache<SourceViewModel, string>(x => x.Name);
            Sources = _sourcesCache.Connect()
                                   .AsObservableCache();

            _visibleRecords = new SourceList<LogRecordViewModel>();
            VisibleRecords = _visibleRecords.Connect()
                                            .AsObservableList();

            new[]
                {
                    _sourcesCache.Connect()
                                 .AutoRefresh(s => s.IsSelected)
                                 .AutoRefresh(s => s.SelectedLevels)
                                 .ToCollection()
                                 .Select(_ => Unit.Default),

                    _filterSubject
                }
               .Merge()
               .Throttle(TimeSpan.FromMilliseconds(50))
               .SelectMany((_, c) => GetFilteredList(c))
               .Subscribe(Refresh);
        }

        private long _lastAssignedItemId = 0;

        public void PutRecords(IEnumerable<LogRecord> Records)
        {
            _recordsLocker.Wait();
            try
            {
                var newItems = new List<LogRecordViewModel>();
                foreach (var record in Records)
                {
                    if (!_sources.TryGetValue(record.Source, out var source))
                    {
                        source = _sourceViewModelFactory.CreateInstance(record.Source);
                        _sources.Add(source.Name, source);
                        _sourcesCache.AddOrUpdate(source);
                    }

                    var recordViewModel = new LogRecordViewModel(Interlocked.Increment(ref _lastAssignedItemId),
                                                                 record.Time, source, record.Message, record.Level,
                                                                 record.Exception);

                    newItems.Add(recordViewModel);
                }

                _records.AddRange(newItems);
                _visibleRecords.AddRange(newItems.Where(_lastFilter));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                _recordsLocker.Release();
            }
        }

        public IObservableList<LogRecordViewModel>       VisibleRecords { get; }
        public IObservableCache<SourceViewModel, string> Sources        { get; }

        public void Filter(Func<LogRecordViewModel, bool> Filter)
        {
            _lastFilter = Filter;
            _filterSubject.OnNext(Unit.Default);
        }

        public void Clear()
        {
            _recordsLocker.Wait();
            try
            {
                _records.Clear();
                _visibleRecords.Clear();
            }
            finally
            {
                _recordsLocker.Release();
            }
        }

        private readonly SourceList<LogRecordViewModel> _visibleRecords;
        private Func<LogRecordViewModel, bool> _lastFilter = _ => true;

        private bool Filter(LogRecordViewModel Record)
        {
            if (!Record.Source.IsSelected) return false;
            if (!Record.Source.SelectedLevels.Contains(Record.Level)) return false;
            return _lastFilter(Record);
        }

        private void Refresh(IList<LogRecordViewModel> NewItems)
        {
            try
            {
                _visibleRecords.Edit(items =>
                {
                    items.Clear();
                    items.AddRange(NewItems);
                });
                Console.WriteLine($"Refreshed ({NewItems.Count} items)");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private readonly SemaphoreSlim _recordsLocker = new SemaphoreSlim(1);

        private async Task<List<LogRecordViewModel>> GetFilteredList(CancellationToken Cancellation)
        {
            await _recordsLocker.WaitAsync(Cancellation);
            try
            {
                Cancellation.ThrowIfCancellationRequested();
                var sw = Stopwatch.StartNew();
                var newItems = _records.AsParallel()
                                       .Where(Filter)
                                       .TakeWhile(_ => !Cancellation.IsCancellationRequested)
                                       .ToList();
                Console.WriteLine(
                    $"{newItems.Count} items waf filtered in {sw.Elapsed.TotalSeconds:F3} sec, ThreadID: {Thread.CurrentThread.ManagedThreadId}");
                Cancellation.ThrowIfCancellationRequested();
                return newItems;
            }
            finally
            {
                _recordsLocker.Release();
            }
        }
    }
}