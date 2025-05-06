using CommunityToolkit.Mvvm.ComponentModel;
using HelloWorld.Services;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloWorld.ViewModels
{
    public partial class StockChartViewModel : ObservableObject
    {
        private readonly ILogger<StockChartViewModel> _logger;
        private readonly SingleStockService _singleStockService;
        private readonly ConcurrentQueue<PricePoint> _priceHistory = new();
        private IDisposable? _subscription;

        private const int MaxPoints = 100;
        
        private const double TimeWindowSeconds = 600;
       // public DateTime WindowEndTime { get; set; } = DateTime.Now;

        public event Action? CanvasInvalidated;

        [ObservableProperty]
        private string symbol = string.Empty;  

        public StockChartViewModel(SingleStockService singleStockService, ILogger<StockChartViewModel> logger)
        {
            _logger = logger;
            _singleStockService = singleStockService;
        }

        public void InvalidateChart() => CanvasInvalidated?.Invoke();

        public async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            try
            {
                if (query.TryGetValue("symbol", out var symbolValue))
                {
                    Symbol = symbolValue?.ToString() ?? string.Empty;
                   //WindowEndTime = DateTime.Now;

                    if (!string.IsNullOrEmpty(Symbol))
                    {
                        //Dispose existing subscription if any
                        _subscription?.Dispose();
                        _subscription = null;

                        // Start streaming service for the new symbol
                        await _singleStockService.StartStreaming(Symbol);

                        // Subscribe to stock updates
                        _subscription = _singleStockService.StockUpdates?.Subscribe(update =>
                            {
                                var point = new PricePoint
                                {
                                    Time = DateTimeOffset.FromUnixTimeMilliseconds(update.T).LocalDateTime,
                                    Price = update.P
                                };
                                //Add point to the history
                                _priceHistory.Enqueue(point);

                                while (_priceHistory.TryPeek(out var oldest) &&
                                    (DateTime.Now - oldest.Time).TotalSeconds > TimeWindowSeconds + 10)
                                {
                                    _priceHistory.TryDequeue(out _);
                                }

                                //Invalidate the canvas to trigger a redraw
                                CanvasInvalidated?.Invoke();
                            });
                            }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying query attributes & starting stream");
            }
        }

        public void PaintChart(SKCanvas canvas, int width, int height)
        {
            try
            {
                canvas.Clear(SKColors.Black);

                if (_priceHistory.IsEmpty)
                    return;

                var now = DateTime.Now;
                var minTime = now.AddSeconds(-TimeWindowSeconds);
                var maxTime = now;
                var timeRange = TimeWindowSeconds;

                var recentPoints = _priceHistory
                    .Where(p => p.Time >= minTime && p.Time <= maxTime)
                    .OrderBy(p => p.Time)
                    .ToArray();

                _logger.LogInformation($"Points in window: {recentPoints.Length}");

                if (recentPoints.Length < 2)
                    return;

                var minPrice = recentPoints.Min(p => p.Price);
                var maxPrice = recentPoints.Max(p => p.Price);

                // Y-axis padding
                var padding = (float)(maxPrice - minPrice) * 0.1f;
                if (padding == 0) padding = 1;
                minPrice -= (decimal)padding;
                maxPrice += (decimal)padding;
                var priceRange = maxPrice - minPrice;

                // Margins for axes
                float marginLeft = 60;
                float marginBottom = 30;
                float chartWidth = width - marginLeft;
                float chartHeight = height - marginBottom;

                //Fonts
                using var font32 = new SKFont
                {
                    Size = 32,
                    Edging = SKFontEdging.Antialias
                };
                using var font14 = new SKFont
                {
                    Size = 14,
                    Edging = SKFontEdging.Antialias
                };

                // Paints
                using var gridPaint = new SKPaint
                {
                    Color = SKColors.Gray,
                    StrokeWidth = 1,
                    Style = SKPaintStyle.Stroke,
                    PathEffect = SKPathEffect.CreateDash(new float[] { 4, 4 }, 0)
                };

                using var labelPaint = new SKPaint
                {
                    Color = SKColors.White,
                    IsAntialias = true,
                };

                using var priceLabelPaint = new SKPaint
                {
                    Color = SKColors.White,
                    IsAntialias = true,
                };

                using var linePaint = new SKPaint
                {
                    Color = SKColors.Lime,
                    StrokeWidth = 2,
                    Style = SKPaintStyle.Stroke,
                    IsAntialias = true
                };

                using var markerPaint = new SKPaint
                {
                    Color = SKColors.Red,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };

                // Draw horizontal gridlines and price labels
                const int ySteps = 5;
                float yStep = chartHeight / ySteps;
                float priceStep = (float)(priceRange / ySteps);

                for (int i = 0; i <= ySteps; i++)
                {
                    float y = i * yStep;
                    float price = (float)maxPrice - (i * priceStep);
                    canvas.DrawLine(marginLeft, y, width, y, gridPaint);
                    canvas.DrawText($"${price:F2}", marginLeft - 10, y + 5,font14, priceLabelPaint);
                }

                // Draw vertical time labels and gridlines
                int xLabelStepSeconds = TimeWindowSeconds > 600 ? 120 : 60;
                DateTime labelTime = minTime;

                while (labelTime <= maxTime)
                {
                    float secondsFromMin = (float)(labelTime - minTime).TotalSeconds;
                    float x = marginLeft + (secondsFromMin / (float)timeRange) * chartWidth;

                    canvas.DrawLine(x, 0, x, chartHeight, gridPaint);
                    canvas.DrawText(labelTime.ToString("HH:mm"), x, height - 5,SKTextAlign.Center ,font14, labelPaint);

                    labelTime = labelTime.AddSeconds(xLabelStepSeconds);
                }

                // Draw price line path
                var path = new SKPath();
                for (int i = 0; i < recentPoints.Length; i++)
                {
                    var pt = recentPoints[i];
                    float x = marginLeft + (float)((pt.Time - minTime).TotalSeconds / timeRange) * chartWidth;
                    float y = (float)(((maxPrice - pt.Price) / priceRange)) *chartHeight;

                    if (i == 0)
                        path.MoveTo(x, y);
                    else
                        path.LineTo(x, y);
                }

                canvas.DrawPath(path, linePaint);

                // Draw marker at last point
                var last = recentPoints.Last();
                float lastX = marginLeft + (float)((last.Time - minTime).TotalSeconds / timeRange) * chartWidth;
                float lastY = (float)((maxPrice - last.Price) / priceRange) * chartHeight;

                canvas.DrawCircle(lastX, lastY, 5, markerPaint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error painting chart");
            }
        }

        public async Task StopStreamingAsync()
        {
            try
            {
                if (_singleStockService != null)
                {
                    await _singleStockService.StopStreaming(Symbol);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping stock streaming");
            }
        }
    }
    public class PricePoint
    {
        public DateTime Time { get; set; }
        public decimal Price { get; set; }
    }
}
