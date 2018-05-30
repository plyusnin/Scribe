using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using Newtonsoft.Json;
using ReactiveUI;

namespace Scribe.Gui
{
    public class Settings : ReactiveObject, IDisposable
    {
        private static readonly string _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                                                    "Saut", "The Scribe", "Settings.json");

        private readonly IDisposable _savingConnection;

        [JsonConstructor]
        public Settings(IList<SourceOptions> Sources)
        {
            this.SourcesOptions = new ReactiveList<SourceOptions>(Sources)
            {
                ChangeTrackingEnabled = true
            };

            _savingConnection = this.WhenAnyObservable(x => x.SourcesOptions.ItemChanged,
                                                       x => x.SourcesOptions.Changed,
                                                       (a, b) => Unit.Default)
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
            catch (Exception) { }
        }

        public static Settings Load()
        {
            try
            {
                var json = File.ReadAllText(_settingsPath);
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
        private bool _isEnabled;

        public SourceOptions(string Name)
        {
            this.Name = Name;
        }

        [JsonProperty(PropertyName = "Name")]
        public string Name { get; }

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
    }
}
