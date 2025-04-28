using HelloWorld.ViewModels;
using Microsoft.Extensions.Logging;

namespace HelloWorld.Views;

public partial class StockTickerPage : ContentPage
{
    private readonly ILogger<StockTickerPage> _logger;
    private readonly StockTickerViewModel _viewModel;
    private readonly List<string> _symbols = new List<string> { "AAPL", "GOOGL", "AMZN", "META", "NFLX", "TSLA" };

    public StockTickerPage(StockTickerViewModel viewModel, ILogger<StockTickerPage> logger)
	{
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
        _logger = logger;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        try
        {
            base.OnNavigatedTo(args);
            _viewModel.StartStreaming();
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error navigating to StockTickerPage");
        }
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        try
        {
            base.OnNavigatedFrom(args);
            //await _viewModel.PauseStreamingAsync(_symbols);
            _viewModel.PauseStreaming();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating from StockTickerPage");
        }
    }
}