namespace PicoDeck.Views;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
        BindingContext = viewModel;
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        if (Width < 600)
        {
            VisualStateManager.GoToState(this, "Narrow");
        }
        else
        {
            VisualStateManager.GoToState(this, "Wide");
        }
    }
}
