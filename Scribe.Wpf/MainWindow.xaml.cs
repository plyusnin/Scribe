using System;
using System.Linq;
using System.Reactive;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Scribe.EventsLayer;
using Scribe.EventsLayer.NLog;
using Scribe.RecordsLayer;
using Scribe.Wpf.ViewModel;

namespace Scribe.Wpf
{
    /// <summary>Логика взаимодействия для MainWindow.xaml</summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private DateTime _lastSelection = DateTime.Now;

        public MainWindow()
        {
            InitializeComponent();
            var settings       = Settings.Load();
            var fileNLogSource = new FileNLogSource();
            _viewModel = new MainViewModel(
                new NLogRecordsSource(new CompositeLogSource<NLogEvent>(new UdpNLogSource(), fileNLogSource)),
                new SourceViewModelFactory(settings),
                new ILogFileOpener[] {fileNLogSource});
            DataContext = _viewModel;
            _viewModel.RecordsOnReset.Subscribe(OnLogReset);
        }

        private void OnLogReset(Unit Unit)
        {
            var item = LogBox.Items
                             .OfType<LogRecordViewModel>()
                             .FirstOrDefault(l => l.Time >= _lastSelection)
                       ?? (LogBox.Items.Count > 0 ? LogBox.Items[^1] : null);

            if (item != null)
            {
                LogBox.SelectedItem = item;
                LogBox.ScrollIntoView(item);
            }
        }

        private void LogBox_OnSelectionChanged(object Sender, SelectionChangedEventArgs E)
        {
            if (LogBox.SelectedItems.Count > 0)
            {
                var sel = LogBox.SelectedItems
                                .OfType<LogRecordViewModel>()
                                .First();

                _lastSelection = sel.Time;
            }
        }

        private void OnRecordItemDoubleClick(object Sender, MouseButtonEventArgs E)
        {
            try
            {
                var item   = (ListViewItem) Sender;
                var record = (LogRecordViewModel) item.DataContext;

                _viewModel.HighlightRecord.Execute(record).Subscribe();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void CommandBinding_OnExecuted(object Sender, ExecutedRoutedEventArgs E)
        {
            var item = E.Parameter;
            if (item != null)
            {
                LogBox.SelectedItem = item;
                LogBox.ScrollIntoView(item);
            }

            BookmarksButton.IsChecked = false;
        }
    }
}