using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;


namespace MyMauiApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new NavigationPage(new MainPage());
    }
}

