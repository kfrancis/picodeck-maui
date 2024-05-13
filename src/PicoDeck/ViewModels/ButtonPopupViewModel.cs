using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using Input;
using Input.Platforms.Windows;

namespace PicoDeck.ViewModels
{
    public partial class ButtonPopupViewModel : BaseViewModel
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedColor))]
        private int _selectedRed = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedColor))]
        private int _selectedGreen = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedColor))]
        private int _selectedBlue = 0;

        public Color SelectedColor => new Color(SelectedRed, SelectedGreen, SelectedBlue);

        [RelayCommand]
        private void DetectShortcut()
        {
            // Logic to start keyboard hook and detect shortcut keys
            var hook = Inputs.Use<IKeyboardHook>();
            var model = hook.KeyboardModel;

            model.KeyDown += (sender, key, state) =>
            {
                Debug.WriteLine($"KeyDown: {key} {state}");
                return true;
            };

            model.KeyUp += (sender, key, state) =>
            {
                Debug.WriteLine($"KeyDown: {key} {state}");
                return true;
            };

            hook.HookStart();

            if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
            {
                while (WindowsMessagePump.Pumping()) { }
            }
        }

        [RelayCommand]
        private async Task SaveConfigurationAsync(Popup popup)
        {
            await popup.CloseAsync();
        }
    }
}
