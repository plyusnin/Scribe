using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using Scribe.EventsLayer;

namespace Scribe.Wpf.ViewModel
{
    public class SourceViewModel : ReactiveObject
    {
        private readonly ReadOnlyObservableCollection<LogLevelFilterViewModel> _displayLogLevels;
        private readonly ObservableAsPropertyHelper<HashSet<LogLevel>> _selectedLevels;
        private int _colorIndex;
        private bool _isSelected;

        public SourceViewModel(
            int Id, bool IsSelected, string Name, int ColorIndex, IList<LogLevel> DisabledLogLevels,
            string FullName, int ParentId, string ShortName)
        {
            _isSelected    = IsSelected;
            _colorIndex    = ColorIndex;
            this.FullName  = FullName;
            this.ParentId  = ParentId;
            this.ShortName = ShortName;
            this.Id        = Id;
            this.Name      = Name;

            var displayLogLevelsSource = new SourceCache<LogLevelFilterViewModel, LogLevel>(x => x.Value);

            displayLogLevelsSource.AddOrUpdate(
                Enum.GetValues(typeof(LogLevel))
                    .OfType<LogLevel>()
                    .Select(level => new LogLevelFilterViewModel(level)
                                { IsSelected = !DisabledLogLevels.Contains(level) }));

            displayLogLevelsSource.Connect()
                                  .ObserveOn(RxApp.MainThreadScheduler)
                                  .Bind(out _displayLogLevels)
                                  .Subscribe();

            displayLogLevelsSource.Connect()
                                  .AutoRefresh(x => x.IsSelected)
                                  .Filter(x => x.IsSelected)
                                  .ToCollection()
                                  .Select(ll => new HashSet<LogLevel>(ll.Select(l => l.Value)))
                                  .ToProperty(this, x => x.SelectedLevels, out _selectedLevels);
        }

        public HashSet<LogLevel> SelectedLevels => _selectedLevels.Value;

        public ReadOnlyObservableCollection<LogLevelFilterViewModel> DisplayLogLevels => _displayLogLevels;

        public int Id { get; }

        public string Name { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }

        public int ColorIndex
        {
            get => _colorIndex;
            set => this.RaiseAndSetIfChanged(ref _colorIndex, value);
        }

        public string FullName  { get; }
        public int    ParentId  { get; }
        public string ShortName { get; }

        public override string ToString()
        {
            return $"{Id}: {FullName}   (under {ParentId})";
        }
    }

    public class LogLevelFilterViewModel : ReactiveObject
    {
        private bool _isSelected;

        public LogLevelFilterViewModel(LogLevel Value)
        {
            this.Value  = Value;
            Name        = Value.ToString();
            _isSelected = true;
        }

        public LogLevel Value { get; }

        public string Name { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }
    }
}