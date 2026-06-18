using System.Windows;
using MediWait.Wpf.Data;
using MediWait.Wpf.Sluzby;
using MediWait.Wpf.ViewModely;

namespace MediWait.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new HlavniViewModel(new DatabazeSluzba(), new SenzorovaSluzba());
    }
}
