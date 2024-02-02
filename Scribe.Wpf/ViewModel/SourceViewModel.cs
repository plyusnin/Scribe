﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using DynamicData.PLinq;
using ReactiveUI;
using Scribe.EventsLayer;

namespace Scribe.Wpf.ViewModel
{
    public class SourceViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<HashSet<LogLevel>> _selectedLevels;
        private int _colorIndex;
        private bool _isSelected;

        private readonly ReadOnlyObservableCollection<LogLevelFilterViewModel> _displayLogLevels;

        public SourceViewModel(bool IsSelected, string Name, string ParentName, int ColorIndex, IList<LogLevel> DisabledLogLevels, string FullName)
        {
            _isSelected = IsSelected;
            _colorIndex = ColorIndex;
            this.FullName = FullName;
            this.ParentName = ParentName;
            this.Name   = Name;

            var displayLogLevelsSource = new SourceCache<LogLevelFilterViewModel, LogLevel>(x => x.Value);

            displayLogLevelsSource.AddOrUpdate(
                Enum.GetValues(typeof(LogLevel))
                    .OfType<LogLevel>()
                    .Select(level => new LogLevelFilterViewModel(level) { IsSelected = !DisabledLogLevels.Contains(level)}));

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

        public string ParentName { get; }
        public string FullName { get; }

        public override string ToString()
        {
            return FullName;
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