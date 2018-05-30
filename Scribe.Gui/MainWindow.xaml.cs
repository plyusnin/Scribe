using System;
using System.Linq;
using System.Reactive;
using System.Windows;
using System.Windows.Controls;
using Scribe.EventsLayer.NLog;
using Scribe.Gui.ViewModel;
using Scribe.RecordsLayer;  

namespace Scribe.Gui
{
    /// <summary>Логика взаимодействия для MainWindow.xaml</summary>
    public partial class MainWindow : Window
    {
        private DateTime _lastSelection = DateTime.Now;

        public MainWindow()
        {
            InitializeComponent();
            var settings = Settings.Load();
            var viewModel = new MainViewModel(new NLogRecordsSource(new UdpNLogSource()), new SourceViewModelFactory(settings));
            DataContext = viewModel;
            viewModel.Records.ShouldReset.Subscribe(OnLogReset);
        }

        private void OnLogReset(Unit Unit)
        {
            //var item = LogBox.Items
            //                 .OfType<LogRecordViewModel>()
            //                 .FirstOrDefault(l => l.Record == _lastSelection)
            //           ?? LogBox.Items
            //                    .OfType<LogRecordViewModel>()
            //                    .FirstOrDefault(l => l.TimeStamp >= _lastSelection.TimeStamp)
            //           ?? (LogBox.Items.Count > 0 ? LogBox.Items[LogBox.Items.Count - 1] : null);

            var item = LogBox.Items
                                .OfType<LogRecordViewModel>()
                                .FirstOrDefault(l => l.Time >= _lastSelection)
                       ?? (LogBox.Items.Count > 0 ? LogBox.Items[LogBox.Items.Count - 1] : null);

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
    }
}
