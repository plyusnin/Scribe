using System;
using System.Collections.ObjectModel;
using DynamicData;
using ReactiveUI;

namespace Scribe.Wpf.ViewModel
{
    public class SourceNodeViewModel : ReactiveObject
    {
        private readonly ReadOnlyObservableCollection<SourceNodeViewModel> _children;
        private readonly Node<SourceViewModel, int> _node;
        private bool _isSelected;

        public SourceNodeViewModel(Node<SourceViewModel, int> Node)
        {
            _node = Node;

            Source.WhenAnyValue(x => x.IsSelected)
                  .BindTo(this, x => x.IsSelected);

            this.WhenAnyValue(x => x.IsSelected)
                .BindTo(Source, x => x.IsSelected);

            _node.Children.Connect()
                 .Transform(n => new SourceNodeViewModel(n))
                 .Bind(out _children)
                 .Subscribe();

            // this.WhenAnyValue(x => x.IsSelected)
            //     .Subscribe(sel =>
            //      {
            //          foreach (var child in Children) child.IsSelected = sel;
            //      });
        }

        public ReadOnlyObservableCollection<SourceNodeViewModel> Children => _children;

        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }

        public SourceViewModel Source => _node.Item;
    }
}