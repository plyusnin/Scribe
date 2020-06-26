using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Threading;
using DynamicData;
using DynamicData.Binding;
using LogList.Control;
using Microsoft.Xaml.Behaviors;

namespace Scribe.Wpf.Behaviors
{
    public class AutoScrollBehavior : Behavior<LogListBox>
    {
        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
            "IsActive", typeof(bool), typeof(AutoScrollBehavior), new PropertyMetadata(true));

        private IDisposable _subscription;

        public bool IsActive
        {
            get => (bool) GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            Dispatcher.BeginInvoke(() =>
            {
                // _subscription = AssociatedObject.ItemsSource
                //                                 .ToObservableChangeSet()
                //                                 .ObserveOnDispatcher()
                //                                 .Where(_ => IsActive)
                //                                 .OnItemAdded(i => AssociatedObject.ScrollIntoView(i))
                //                                 .Subscribe();
            }, DispatcherPriority.Loaded);
        }

        protected override void OnDetaching()
        {
            _subscription?.Dispose();
        }
    }
}