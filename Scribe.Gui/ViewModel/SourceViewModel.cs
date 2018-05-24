using ReactiveUI;

namespace Scribe.Gui.ViewModel
{
    public class SourceViewModel : ReactiveObject
    {
        private bool _isSelected;

        public SourceViewModel(bool IsSelected, string Name)
        {
            _isSelected = IsSelected;
            this.Name = Name;
        }

        public string Name { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }
    }
}
