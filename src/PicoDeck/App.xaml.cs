using Microsoft.Maui.Controls;

namespace PicoDeck;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();
    }
}
