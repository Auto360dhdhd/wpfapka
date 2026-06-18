using MediWait.Wpf.ViewModely;

namespace MediWait.Wpf;

public partial class MainPage : ContentPage
{
    public MainPage(HlavniViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
