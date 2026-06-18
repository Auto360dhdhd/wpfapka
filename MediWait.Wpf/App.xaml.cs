namespace MediWait.Wpf;

public partial class App : Application
{
    public App(MainPage hlavniStranka)
    {
        InitializeComponent();
        MainPage = new NavigationPage(hlavniStranka);
    }
}
