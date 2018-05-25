using System.Windows;
using Scribe.EventsLayer.NLog;
using Scribe.Gui.ViewModel;
using Scribe.RecordsLayer;

namespace Scribe.Gui
{
    /// <summary>Логика взаимодействия для MainWindow.xaml</summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel(new NLogRecordsSource(new UdpNLogSource()));
        }
    }
}
