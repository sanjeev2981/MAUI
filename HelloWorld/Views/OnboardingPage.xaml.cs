namespace HelloWorld.Views;

public partial class OnboardingPage : ContentPage
{
	public OnboardingPage()
	{
		InitializeComponent();
        BindingContext = this;
    }
    private async void OnCloseButtonClicked(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PopModalAsync();
    }

    public List<string> ImageSources { get; set; } = new List<string>
    {
        "image_1.png",  // Replace with your actual image file names
        "image_2.png",
        "image_3.png"
    };
}