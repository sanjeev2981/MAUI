using HelloWorld.ViewModels;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui;

namespace HelloWorld.Views;

[QueryProperty(nameof(Symbol), "symbol")]
public partial class StockChartPage : ContentPage
{
    private readonly ILogger<StockChartPage> _logger;
    private readonly StockChartViewModel _viewModel;

    private string _symbol = string.Empty;
    public string Symbol
    {
        get => _symbol;
        set
        {
            _symbol = value;
            ApplyQueryToViewModel();
        }
    }
    public StockChartPage(StockChartViewModel viewModel, ILogger<StockChartPage> logger)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
        _logger = logger;
    }
    private void ApplyQueryToViewModel()
    {
        var query = new Dictionary<string, object> { { "symbol", _symbol } };
        _viewModel.ApplyQueryAttributes(query);
    }

    protected override void OnAppearing()
    {
        try
        {
            base.OnAppearing();

            _viewModel.CanvasInvalidated += () =>
                {
                    MainThread.BeginInvokeOnMainThread(() => canvasView.InvalidateSurface());
                };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error appearing on StockChartPage");
        }
    }


    private void OnCanvasViewPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        try
        {
            _viewModel.PaintChart(e.Surface.Canvas, e.Info.Width, e.Info.Height);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error painting surface");
        }
    }

    protected override async void OnDisappearing()
    {
        try
        {
            base.OnDisappearing();
            await _viewModel.StopStreamingAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disappearing from StockChartPage");
        }
        finally
        {
            canvasView.PaintSurface -= OnCanvasViewPaintSurface;
            base.OnDisappearing();
        }
    }
}