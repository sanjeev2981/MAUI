using HelloWorld.ViewModels;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

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

        Debug.WriteLine($"BindingContext type: {BindingContext?.GetType().Name}");
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        try
        {
            _viewModel.StartStreaming();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating to FavoriteStocksPage");
        }
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        try
        {
            _viewModel.PauseStreaming();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error navigating from FavoriteStocksPage");
        }
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = e.CurrentSelection.FirstOrDefault() as StockViewModel;
        if (selected != null)
        {
            Debug.WriteLine($"?? SelectionChanged fired: {selected.Symbol}");
        }
    }
}