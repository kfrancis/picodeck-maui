using CommunityToolkit.Maui.Views;

namespace PicoDeck.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<SysApplication> _applications;

    [ObservableProperty]
    private SysApplication _selectedApplication;

    public MainViewModel()
    {
        _applications = new ObservableCollection<SysApplication>();
        LoadApplications();
    }

    [RelayCommand]
    private async Task OpenButtonAsync(string buttonNum)
    {
        var configPopup = new ButtonPopup();
        await Application.Current.MainPage.ShowPopupAsync(configPopup);
    }

    private void LoadApplications()
    {
        // Sample data for demonstration purposes
        var applications = new List<SysApplication>
            {
                new SysApplication
                {
                    Name = "Application 1",
                    Key = 1,
                    OffColor = Colors.Red,
                    Shortcuts = new Dictionary<string, Shortcut>
                    {
                        { "Shortcut 1", new Shortcut { Keys = new List<string> { "Ctrl+Alt+A" }, Color = Colors.Green } }
                    }
                },
                new SysApplication
                {
                    Name = "Application 2",
                    Key = 2,
                    OffColor = Colors.Blue,
                    Shortcuts = new Dictionary<string, Shortcut>
                    {
                        { "Shortcut 1", new Shortcut { Keys = new List<string> { "Ctrl+Alt+B" }, Color = Colors.Yellow } }
                    }
                }
            };

        foreach (var app in applications.OrderBy(a => a.Key))
        {
            Applications.Add(app);
        }

        SelectedApplication = Applications[0];

        //foreach (var appShortcut in SelectedApplication.Shortcuts)
        //{
        //}
    }
}
