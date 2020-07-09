using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using DynamicData;
using LogList.Control.Manipulation;
using LogList.Control.Manipulation.Implementations;
using Scribe.RecordsLayer;
using Scribe.Wpf.ViewModel;

namespace Scribe.Wpf
{
    public interface IRecordsCache
    {
        ListViewModel<LogRecordViewModel>      VisibleRecords { get; }
        IObservableCache<SourceViewModel, int> Sources        { get; }
        bool                                   AutoScroll     { get; set; }
        void                                   PutRecords(IEnumerable<LogRecord> Records);

        void Filter(Func<LogRecordViewModel, bool> Filter);
        void Clear();
    }

    public class RecordsCache : IRecordsCache
    {
        private readonly Subject<Unit> _filterSubject = new Subject<Unit>();

        private readonly SemaphoreSlim _recordsLocker = new SemaphoreSlim(1);

        private readonly Dictionary<string, SourceViewModel> _sources;
        private readonly SourceCache<SourceViewModel, int> _sourcesCache;
        private readonly SourceViewModelFactory _sourceViewModelFactory;

        private Func<LogRecordViewModel, bool> _lastFilter = _ => true;


        public RecordsCache(SourceViewModelFactory SourceViewModelFactory, IObservable<string> TextSearchRequests)
        {
            _sourceViewModelFactory = SourceViewModelFactory;

            _sources      = new Dictionary<string, SourceViewModel>();
            _sourcesCache = new SourceCache<SourceViewModel, int>(x => x.Id);
            Sources = _sourcesCache.Connect()
                                   .AsObservableCache();

            VisibleRecords = new ListViewModel<LogRecordViewModel>();

            var sourcesFilter =
                _sourcesCache.Connect()
                             .AutoRefresh(s => s.IsSelected)
                             .AutoRefresh(s => s.SelectedLevels)
                             .ToCollection()
                             .Select(sources => new RecordSourceFilter(sources));

            var textFilter =
                TextSearchRequests.Select(r => Filters.ByStringRequest<LogRecordViewModel>(r));

            sourcesFilter.CombineLatest(textFilter,
                                        (s, t) => Filters.CompositeAll(s, t))
                         .Throttle(TimeSpan.FromMilliseconds(300))
                         .Subscribe(f => VisibleRecords.ApplyFilter(f));
        }

        public void PutRecords(IEnumerable<LogRecord> Records)
        {
            _recordsLocker.Wait();
            try
            {
                var newItems   = new List<LogRecordViewModel>();
                var newSources = new List<SourceViewModel>();
                foreach (var record in Records)
                {
                    var sourceFullName = record.Sender + '.' + record.Source;
                    if (!_sources.TryGetValue(sourceFullName, out var source))
                    {
                        var sourcePath = new List<string> { record.Sender };
                        sourcePath.AddRange(record.Source.Split('.'));

                        var directParent = -1;

                        for (var i = 1; i <= sourcePath.Count; i++)
                        {
                            var name = string.Join('.', sourcePath.Take(i));
                            if (_sources.TryGetValue(name, out source))
                            {
                                directParent = source.Id;
                                continue;
                            }

                            var nameWithoutSender = string.Join('.', sourcePath.Skip(1).Take(i - 1));
                            source = _sourceViewModelFactory.CreateInstance(
                                nameWithoutSender, record.Sender, directParent);
                            directParent = source.Id;

                            _sources.Add(name, source);
                            newSources.Add(source);
                        }

                        // source = _sourceViewModelFactory.CreateInstance(record.Source, record.Sender, directParent);
                        // _sources.Add(sourceFullName, source);
                        // newSources.Add(source);
                    }

                    var recordViewModel = new LogRecordViewModel(record.Time, record.OriginalTime, source, record.Message,
                                                                 record.Level, record.Exception);

                    newItems.Add(recordViewModel);
                }

                _sourcesCache.AddOrUpdate(newSources);
                VisibleRecords.Append(newItems, AutoScroll);
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

        public bool AutoScroll { get; set; }

        public ListViewModel<LogRecordViewModel>      VisibleRecords { get; }
        public IObservableCache<SourceViewModel, int> Sources        { get; }

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
                VisibleRecords.Clear();
            }
            finally
            {
                _recordsLocker.Release();
            }
        }
    }
}