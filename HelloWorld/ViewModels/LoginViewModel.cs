
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Plugin.Maui.Biometric;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelloWorld.Services;
using HelloWorld.Models;
using Microsoft.Extensions.Logging;

namespace HelloWorld.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly ILogger<LoginViewModel> _logger;

        private readonly IBiometric biometricService;
        private readonly UserService userService;
        private readonly StockTickerService stockTickerService;
        private User user;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private bool isRememberMe;

        public string Username
        {
            get => user?.Username ?? string.Empty;
            set
            {
                if (user != null)
                {
                    user.Username = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Password
        {
            get => user?.Password ?? string.Empty;
            set
            {
                if (user != null)
                {
                    user.Password = value;
                    OnPropertyChanged();
                }
            }
        }


        public ICommand LoginCommand { get; }
        public ICommand BiometricLoginCommand { get; }

        public LoginViewModel(IBiometric biometric, UserService userSvc, StockTickerService stockTickerSvc, ILogger<LoginViewModel> logger)
        {
            _logger = logger;

            biometricService = biometric;
            userService = userSvc;
            stockTickerService = stockTickerSvc;

            user = new User();

            LoginCommand = new RelayCommand(async () => await LoginAsync());
            BiometricLoginCommand = new RelayCommand(async () => await BiometricLogin());

            LoadRememberedCredentials();
            _logger.LogInformation("LoginViewModel initialized");
        }

        private async Task LoginAsync()
        {
            try
            {
                var user = await userService.AuthenticateAsync(Username, Password);

                if (user.IsAuthenticated)
                {
                    SaveCredentials();
                    if (IsRememberMe)
                    {
                        await SecureStorage.Default.SetAsync("usernameKey", user?.Username ?? String.Empty);
                        await SecureStorage.Default.SetAsync("auth_token", user?.AccessToken ?? String.Empty);
                    }
                    else
                    {
                        await SecureStorage.Default.SetAsync("usernameKey", string.Empty);
                        await SecureStorage.Default.SetAsync("auth_token", string.Empty);
                    }

                    // Successful login 
                    await HandleLoginSuccess();

                }
                else
                {
                    StatusMessage = "Invalid credentials. Please try again.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private void SaveCredentials()
        {
            try
            {
                _logger.BeginScope("Saving credentials");

                Preferences.Set("Username", user?.Username ?? String.Empty);
                Preferences.Set("IsRememberMe", IsRememberMe);
                _logger.LogInformation($"Credentials saved for {user?.Username} Remember Me: {IsRememberMe}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving credentials");
            }
        }

        private void LoadRememberedCredentials()
        {
            if (Preferences.Get("IsRememberMe", false))
            {
                user.Username = Preferences.Get("Username", string.Empty);
                IsRememberMe = true;
            }
        }

        private void ClearCredentials()
        {
            Preferences.Remove("Username");
            Preferences.Remove("IsRememberMe");
        }
        private async void LoadRememberedUser()
        {
            var rememberedUsername = await SecureStorage.Default.GetAsync("usernameKey");

            if (!string.IsNullOrEmpty(rememberedUsername))
            {
                user.Username = rememberedUsername;
                IsRememberMe = true; // Use the generated property instead of the field

                await BiometricLogin();
            }
        }

        private async Task BiometricLogin()
        {
            try
            {
                if (await biometricService.GetAuthenticationStatusAsync() == BiometricHwStatus.Success)
                {
                    var authenticationRequest = new AuthenticationRequest()
                    {
                        Title = "A good title",
                        Subtitle = "An equally good subtitle",
                        NegativeText = "Cancel",
                        Description = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua",
                        AllowPasswordAuth = false,
                    };

                    var result = await biometricService.AuthenticateAsync(authenticationRequest, CancellationToken.None);

                    if (result.Status == BiometricResponseStatus.Success)
                    {
                        var userName = await SecureStorage.Default.GetAsync("usernameKey");
                        var token = await SecureStorage.Default.GetAsync("auth_token");

                        if (!string.IsNullOrEmpty(userName) && await userService.IsValidToken(userName, token))
                        {
                            // Use the token for API requests or navigate to the main application page
                            await Shell.Current.GoToAsync("//mainpage");
                        }
                        else
                        {
                            StatusMessage = "Error: No token found, please login manually";
                        }
                    }
                    else if (result.Status == BiometricResponseStatus.Failure)
                    {
                        StatusMessage = "Error: Biometric authentication failed";
                    }
                }
                else
                {
                    StatusMessage = "Unavailable: Biometric authentication is not available on this device";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
            }
        }

        private async Task HandleLoginSuccess()
        {
            try
            {
                //Start the stock ticker service
                IEnumerable<string> symbols = userService.GetUserStocks(Username);
                if (symbols != null && stockTickerService != null)
                {
                    //Start subscription to symbols
                    await stockTickerService.SubscribeAsync(symbols);
                    // Start the stock ticker service stream
                    stockTickerService.StartStreaming(symbols);
                }

                //Navigate to the main page
                await Shell.Current.GoToAsync("//mainpage");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleLoginSuccess");
            }
        }
    }
}
