using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Xml;
using Newtonsoft.Json;
using ReactiveUI;
using Scribe.EventsLayer;

namespace Scribe.Wpf
{
    public class Settings : ReactiveObject, IDisposable
    {
        private static readonly string _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Saut", "The Scribe", "Settings.json");

        private readonly IDisposable _savingConnection;

        [JsonConstructor]
        public Settings(IList<SourceOptions> Sources)
        {
            SourcesOptions = new ReactiveList<SourceOptions>(Sources)
            {
                ChangeTrackingEnabled = true
            };

            _savingConnection = SourcesOptions.ItemChanged.Select(x => Unit.Default)
                                              .Merge(SourcesOptions.Changed.Select(x => Unit.Default))
                                              .Subscribe(_ => Save());
        }

        [JsonProperty(PropertyName = "Sources")]
        public IReactiveList<SourceOptions> SourcesOptions { get; }

        public static Settings Default => new Settings(new List<SourceOptions>());

        public void Dispose()
        {
            _savingConnection?.Dispose();
        }

        private void Save()
        {
            try
            {
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath));
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception)
            {
            }
        }

        public static Settings Load()
        {
            try
            {
                var json     = File.ReadAllText(_settingsPath);
                var settings = JsonConvert.DeserializeObject<Settings>(json);
                return settings;
            }
            catch (Exception)
            {
                return Default;
            }
        }
    }

    public class SourceOptions : ReactiveObject
    {
        private int _colorIndex;
        private List<LogLevel> _hideLogLevels;
        private bool _isEnabled;

        public SourceOptions(string Name)
        {
            this.Name      = Name;
            _hideLogLevels = new List<LogLevel>();
        }

        [JsonProperty(PropertyName = "Name")] public string Name { get; }

        [JsonProperty(PropertyName = "IsEnabled")]
        public bool IsEnabled
        {
            get => _isEnabled;
            set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
        }

        [JsonProperty(PropertyName = "ColorIndex")]
        public int ColorIndex
        {
            get => _colorIndex;
            set => this.RaiseAndSetIfChanged(ref _colorIndex, value);
        }

        [JsonProperty(PropertyName = "HideLogLevels")]
        public List<LogLevel> HideLogLevels
        {
            get => _hideLogLevels;
            set => this.RaiseAndSetIfChanged(ref _hideLogLevels, value);
        }
    }
}