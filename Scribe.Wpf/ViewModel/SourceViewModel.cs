﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Scribe.EventsLayer;

namespace Scribe.Wpf.ViewModel
{
    public class SourceViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<HashSet<LogLevel>> _selectedLevels;
        private int _colorIndex;
        private bool _isSelected;
        private readonly Subject<bool> _isSelectedSubject = new Subject<bool>();

        public SourceViewModel(bool IsSelected, string Name, int ColorIndex)
        {
            _isSelected = IsSelected;
            _colorIndex = ColorIndex;
            this.Name   = Name;

            DisplayLogLevels =
                new ObservableCollection<LogLevelFilterViewModel>(
                    Enum.GetValues(typeof(LogLevel))
                        .OfType<LogLevel>()
                        .Select(level => new LogLevelFilterViewModel(level)));

            DisplayLogLevels.ToObservableChangeSet()
                            .WhenPropertyChanged(x => x.IsSelected)
                            .Select(_ => Unit.Default)
                            .StartWith(Unit.Default)
                            .Select(_ => new HashSet<LogLevel>(
                                        DisplayLogLevels.Where(l => l.IsSelected).Select(l => l.Value)))
                            .ObserveOn(RxApp.MainThreadScheduler)
                            .ToProperty(this, x => x.SelectedLevels, out _selectedLevels);
        }

        public HashSet<LogLevel> SelectedLevels => _selectedLevels.Value;

        public ObservableCollection<LogLevelFilterViewModel> DisplayLogLevels { get; }

        public string Name { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                this.RaiseAndSetIfChanged(ref _isSelected, value);
                _isSelectedSubject.OnNext(value);
            }
        }

        public int ColorIndex
        {
            get => _colorIndex;
            set => this.RaiseAndSetIfChanged(ref _colorIndex, value);
        }

        public IObservable<bool> IsSelectedObservable => _isSelectedSubject.StartWith(_isSelected);
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