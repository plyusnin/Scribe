using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using ReactiveUI;

namespace Scribe.Wpf.ViewModel
{
    public class SourceViewModelFactory
    {
        private readonly Settings _applicationSettings;
        private readonly Random _random = new Random();

        private int _lastModelId = -1;

        public SourceViewModelFactory(Settings ApplicationSettings)
        {
            _applicationSettings = ApplicationSettings;
        }

        public SourceViewModel CreateInstance(string SourceName, string Sender, int DirectParentId)
        {
            var shortName = string.IsNullOrWhiteSpace(SourceName) ? Sender : SourceName.Substring(SourceName.LastIndexOf('.') + 1);
            var fullName = Sender + (string.IsNullOrWhiteSpace(SourceName)
                ? string.Empty
                : '.' + SourceName);
            
            var options = _applicationSettings.SourcesOptions?.FirstOrDefault(s => s.Name == fullName);
            if (options == null)
            {
                options = new SourceOptions(fullName) { IsEnabled = true, ColorIndex = _random.Next(5) };
                _applicationSettings.SourcesOptions.Add(options);
            }
            
            var viewModel = new SourceViewModel(Interlocked.Increment(ref _lastModelId),
                                                options.IsEnabled, SourceName, options.ColorIndex,
                                                options.HideLogLevels, fullName, DirectParentId, shortName);

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