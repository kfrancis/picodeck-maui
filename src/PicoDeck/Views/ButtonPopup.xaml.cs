using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace PicoDeck.Views
{
    public partial class ButtonPopup : Popup
    {
        public ButtonPopup() : this(Ioc.Default.GetRequiredService<ButtonPopupViewModel>())
        {
        }

        public ButtonPopup(ButtonPopupViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
