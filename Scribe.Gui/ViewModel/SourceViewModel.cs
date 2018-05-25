using ReactiveUI;

namespace Scribe.Gui.ViewModel
{
    public class SourceViewModel : ReactiveObject
    {
        private int _colorIndex;
        private bool _isSelected;

        public SourceViewModel(bool IsSelected, string Name, int ColorIndex)
        {
            _isSelected = IsSelected;
            _colorIndex = ColorIndex;
            this.Name = Name;
        }

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
    }
}
