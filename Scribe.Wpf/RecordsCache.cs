using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using DynamicData;
using DynamicData.Binding;
using Scribe.RecordsLayer;
using Scribe.Wpf.ViewModel;

namespace Scribe.Wpf
{
    public interface IRecordsCache
    {
        void                                      PutRecords(IEnumerable<LogRecord> Records);
        IObservableList<LogRecordViewModel>       VisibleRecords { get; }
        IObservableCache<SourceViewModel, string> Sources        { get; }

        void ScrollTo(ScrollingInformation Scrolling);
        void Filter(Func<LogRecordViewModel, bool> Filter);
    }

    public class RecordsCache : IRecordsCache
    {
        private Subject<ScrollingInformation> _scrollingSubject = new Subject<ScrollingInformation>();
        private Subject<Func<LogRecordViewModel, bool>> _filterSubject = new Subject<Func<LogRecordViewModel, bool>>();
        private Subject<Unit> _itemsArrived = new Subject<Unit>();

        private Dictionary<string, SourceViewModel> _sources;

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

            Observable.CombineLatest(_scrollingSubject.StartWith(new ScrollingInformation(1, 100)),
                                     _filterSubject.StartWith(_ => true),
                                     _itemsArrived,
                                     (scroll, filter, _) => new {scroll, filter})
                      .ObserveOn(TaskPoolScheduler.Default)
                      .Synchronize(_insertionLocker)
                      .Subscribe(x => Refresh(x.scroll, x.filter));
        }

        private long _lastAssignedItemId = 0;

        public void PutRecords(IEnumerable<LogRecord> Records)
        {
            lock (_insertionLocker)
            {
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

                    _records.Add(recordViewModel);
                }
                _itemsArrived.OnNext(Unit.Default);
            }
        }

        public IObservableList<LogRecordViewModel>       VisibleRecords { get; }
        public IObservableCache<SourceViewModel, string> Sources        { get; }

        public void ScrollTo(ScrollingInformation Scrolling)
        {
            _scrollingSubject.OnNext(Scrolling);
        }

        public void Filter(Func<LogRecordViewModel, bool> Filter)
        {
            _filterSubject.OnNext(Filter);
        }

        private readonly SourceList<LogRecordViewModel> _visibleRecords;

        private void Refresh(ScrollingInformation Scrolling, Func<LogRecordViewModel, bool> Filter)
        {
            var results = new List<LogRecordViewModel>(Scrolling.Count);
            var zeroId  = _records.FindIndex(r => r.Id == Scrolling.FirstElementId);
            if (zeroId == -1)
                zeroId = 0;

            for (var i = zeroId; i < _records.Count && results.Count < Scrolling.Count; i++)
                if (Filter(_records[i]))
                    results.Add(_records[i]);

            for (var i = zeroId - 1; i >= 0 && results.Count < Scrolling.Count; i--)
                if (Filter(_records[i]))
                    results.Add(_records[i]);

            _visibleRecords.EditDiff(results);
        }
    }

    public class ScrollingInformation
    {
        public ScrollingInformation(int FirstElementId, int Count)
        {
            this.FirstElementId = FirstElementId;
            this.Count          = Count;
        }

        public int FirstElementId { get; }
        public int Count          { get; }
    }
}