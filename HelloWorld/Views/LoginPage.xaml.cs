using HelloWorld.ViewModels;
using Microsoft.Maui.Controls;
using Plugin.Maui.Biometric;

namespace HelloWorld.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
        

    }
}