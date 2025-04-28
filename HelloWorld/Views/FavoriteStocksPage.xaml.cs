using HelloWorld.ViewModels;
using Microsoft.Extensions.Logging;

namespace HelloWorld.Views;

public partial class FavoriteStocksPage : ContentPage
{
    private readonly ILogger<StockTickerPage> _logger;
    private readonly FavoriteStocksViewModel _viewModel;

    public FavoriteStocksPage(FavoriteStocksViewModel viewModel,ILogger<StockTickerPage> logger)
	{
		InitializeComponent();
        BindingContext = viewModel;
        _logger = logger;
        _viewModel = viewModel;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        try
        {
            base.OnNavigatedTo(args);
            _viewModel.StartStreaming();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to FavoriteStocksPage");
        }
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        try
        {
            base.OnNavigatedFrom(args);
            //await _viewModel.PauseStreamingAsync(_favoriteSymbols);
            _viewModel.PauseStreaming();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating from FavoriteStocksPage");
        }
    }
}