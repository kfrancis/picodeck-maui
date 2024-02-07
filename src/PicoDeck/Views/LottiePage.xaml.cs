namespace PicoDeck.Views;

public partial class LottiePage : ContentPage
{
	public LottiePage(LottieViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
