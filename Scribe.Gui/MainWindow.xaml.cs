using System.Windows;
using Scribe.Gui.ViewModel;

namespace Scribe.Gui
{
    /// <summary>Логика взаимодействия для MainWindow.xaml</summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel(new NLogRecordsSource(new UdpLogSource()));
        }
    }
}
