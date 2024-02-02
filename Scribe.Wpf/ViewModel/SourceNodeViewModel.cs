using System;
using System.Collections.ObjectModel;
using DynamicData;
using ReactiveUI;

namespace Scribe.Wpf.ViewModel
{
    public class SourceNodeViewModel : ReactiveObject
    {
        private readonly Node<SourceViewModel, string> _node;
        private readonly ReadOnlyObservableCollection<SourceNodeViewModel> _children;
        private bool _isSelected;

        public SourceNodeViewModel(Node<SourceViewModel, string> Node)
        {
            _node = Node;

            // this.WhenAnyValue(x => x.IsSelected)
            //     .Subscribe(selected => ApplySelection(_node, selected));

            _node.Children.Connect()
                 .Transform(n => new SourceNodeViewModel(n))
                 .Bind(out _children)
                 .Subscribe();
        }

        public ReadOnlyObservableCollection<SourceNodeViewModel> Children => _children;

        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }

        public SourceViewModel Source => _node.Item;

        private static void ApplySelection(Node<SourceViewModel, string> ToNode, bool Selected)
        {
            ToNode.Item.IsSelected = Selected;
            foreach (var child in ToNode.Children.Items) ApplySelection(child, Selected);
        }
    }
}