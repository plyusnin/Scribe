using System;
using System.Linq;
using System.Reactive.Linq;
using ReactiveUI;

namespace Scribe.Wpf.ViewModel
{
    public class SourceViewModelFactory
    {
        private readonly Settings _applicationSettings;
        private readonly Random _random = new Random();

        public SourceViewModelFactory(Settings ApplicationSettings)
        {
            _applicationSettings = ApplicationSettings;
        }

        public SourceViewModel CreateInstance(string SourceName)
        {
            var options = _applicationSettings.SourcesOptions?.FirstOrDefault(s => s.Name == SourceName);
            if (options == null)
            {
                options = new SourceOptions(SourceName) { IsEnabled = true, ColorIndex = _random.Next(5) };
                _applicationSettings.SourcesOptions.Add(options);
            }

            var viewModel = new SourceViewModel(options.IsEnabled, SourceName, options.ColorIndex, options.HideLogLevels);
            
            viewModel.WhenAnyValue(x => x.ColorIndex).BindTo(options, o => o.ColorIndex);
            viewModel.WhenAnyValue(x => x.IsSelected).BindTo(options, o => o.IsEnabled);
            viewModel.WhenAnyValue(x => x.SelectedLevels)
                     .Select(x => viewModel.DisplayLogLevels
                                           .Select(lvm => lvm.Value)
                                           .Where(l => !x.Contains(l))
                                           .ToList())
                     .BindTo(options, o => o.HideLogLevels);
            return viewModel;
        }
    }
}
