using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelloWorld.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace HelloWorld.ViewModels
{
    public partial class FavoriteStocksViewModel : ObservableObject
    {
        private readonly ILogger<StockTickerViewModel> _logger;
        private readonly StockTickerService _stockTickerService;
        private readonly UserService _userService;
        private IDisposable? _subscription;

        public ObservableCollection<StockViewModel> Stocks { get; } = new();

        [ObservableProperty]
        private StockViewModel? selectedStock;

        public FavoriteStocksViewModel(StockTickerService stockTickerService, ILogger<StockTickerViewModel> logger, UserService userService)
        {
            _logger = logger;
            _stockTickerService = stockTickerService;
            _userService = userService;
        }

        private void StartSubscription(IEnumerable<string> symbols)
        {
            try
            {
                if (symbols != null)
                {
                    if (Stocks.Count == 0)
                    {
                        foreach (var symbol in symbols)
                        {
                            Stocks.Add(new StockViewModel(symbol,NavigateToChart));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error subscribing to symbols {symbols}");
            }
        }

        public void StartStreaming()
        {
            try
            {
                if (_stockTickerService != null && _userService != null)
                {
                    // Get the user's favorite stocks  
                    var userName = Preferences.Get("Username", string.Empty);
                    IEnumerable<string>? favoriteStocks = null; // Initialize the variable  

                    if (userName != null)
                    {
                        favoriteStocks = _userService.GetUserFavoriteStocks(userName);
                    }

                    if (favoriteStocks != null)
                    {
                        // Start subscription to symbols  
                        StartSubscription(favoriteStocks);

                        // Dispose existing subscription if any  
                        _subscription?.Dispose();

                        // Subscribe to stock updates  
                        _subscription = _stockTickerService.StockUpdates?.Subscribe(update =>
                        {
                            var stock = Stocks.FirstOrDefault(s => s.Symbol == update.S);
                            stock?.Update(update.P, update.T);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error starting stream for symbols");
            }
        }

        public void PauseStreaming()
        {
            try
            {
                _subscription?.Dispose();
                _subscription = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error pausing stream");
            }
        }

        [RelayCommand]
        //private async Task NavigateToChart(string symbol)
        //{
        //    await Shell.Current.GoToAsync($"//chartpage?symbol={symbol}");
        //}

        private void NavigateToChart(string symbol)
        {
            Shell.Current.GoToAsync($"chartpage?symbol={Uri.EscapeDataString(symbol)}");
        }
        //partial void OnSelectedStockChanged(StockViewModel? value)
        //{
        //    if (value is not null)
        //    {
        //        NavigateToChartCommand.Execute(value.Symbol);
        //        SelectedStock = null;
        //    }
        //} 
    }
}
