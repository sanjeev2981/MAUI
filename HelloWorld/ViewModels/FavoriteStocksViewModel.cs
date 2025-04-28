using CommunityToolkit.Mvvm.ComponentModel;
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
                            Stocks.Add(new StockViewModel(symbol));
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

                        // Start the stock ticker service stream  
                        //_stockTickerService.StartStreaming(favoriteStocks);

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

        public async Task PauseStreamingAsync(IEnumerable<string> symbols)
        {
            try
            {
                _subscription?.Dispose();
                _subscription = null;

                if (symbols != null && _stockTickerService != null)
                {
                    await _stockTickerService.PauseAsync(symbols);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error pausing stream");
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
    }
}
