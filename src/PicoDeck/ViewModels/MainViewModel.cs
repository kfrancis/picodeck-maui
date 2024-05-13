using CommunityToolkit.Maui.Views;

namespace PicoDeck.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    [RelayCommand]
    private async Task OpenButtonAsync(string buttonNum)
    {
        var configPopup = new ButtonPopup();
        await Application.Current.MainPage.ShowPopupAsync(configPopup);
    }
}
