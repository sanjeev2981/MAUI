using CommunityToolkit.Mvvm.ComponentModel;
using HelloWorld.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloWorld.ViewModels
{
    public partial class StockTickerViewModel : ObservableObject
    {
        private readonly ILogger<StockTickerViewModel> _logger;
        private readonly StockTickerService _stockTickerService;
        private readonly UserService _userService;
        private IDisposable? _subscription;

        public ObservableCollection<StockViewModel> Stocks { get; } = new();

        public StockTickerViewModel(StockTickerService stockTickerService, ILogger<StockTickerViewModel> logger, UserService userService)
        {
            _logger = logger;
            _stockTickerService = stockTickerService;
            _userService = userService;
        }

        private void CreateStockModelCollection(IEnumerable<string> symbols)
        {
            try
            {
                if (symbols != null)
                {
                    if (Stocks.Count == 0)
                    {
                        foreach (var symbol in symbols)
                        {
                            Stocks.Add(new StockViewModel(symbol, NavigateToChart));
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
                    // Get the user's stocks  
                    var userName = Preferences.Get("Username", string.Empty);
                    IEnumerable<string>? userStocks = null; // Initialize the variable 

                    if (userName != null)
                    {
                        userStocks = _userService.GetUserStocks(userName);
                    }

                    if (userStocks != null)
                    {
                        //Create models for stocks that are shown on the page
                        CreateStockModelCollection(userStocks);

                        //Dispose existing subscription if any
                        _subscription?.Dispose();

                        // Start streaming service if not already started
                        if (!_stockTickerService.isConnected)
                            _stockTickerService.StartStreaming(userStocks);

                        // Subscribe to stock updates and update only the ones we show on the page
                        if (_stockTickerService.isConnected)
                        {
                            _subscription = _stockTickerService.StockUpdates?.Subscribe(update =>
                            {
                                var stock = Stocks.FirstOrDefault(s => s.Symbol == update.S);
                                stock?.Update(update.P, update.T);
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error starting stream for symbol");
            }
        }

        private void NavigateToChart(string symbol)
        {
            Shell.Current.GoToAsync($"//chartpage?symbol={symbol}");
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
