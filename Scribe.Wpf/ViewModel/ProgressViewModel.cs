using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveUI;

namespace Scribe.Wpf.ViewModel
{
    public class ProgressViewModel : ReactiveObject, IProgress<double>
    {
        private readonly Subject<double> _progress = new Subject<double>();
        private readonly ObservableAsPropertyHelper<double> _value;

        private bool _isActive;
        private string _text;

        public ProgressViewModel()
        {
            _progress.Select(v => Math.Round(v * 100) * 0.01)
                     .DistinctUntilChanged()
                     .ObserveOnDispatcher()
                     .ToProperty(this, x => x.Value, out _value);
        }

        public double Value => _value.Value;

        public bool IsActive
        {
            get => _isActive;
            set => this.RaiseAndSetIfChanged(ref _isActive, value);
        }

        public string Text
        {
            get => _text;
            set => this.RaiseAndSetIfChanged(ref _text, value);
        }

        public void Report(double value)
        {
            _progress.OnNext(value);
        }
    }
}