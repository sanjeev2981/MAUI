using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HelloWorld.Models;

namespace HelloWorld.Services
{
    //Manages connection & realtime + historical data for a single stock
    public class SingleStockService : StockServiceBase
    {
        private new readonly ILogger<SingleStockService> _logger;

        public SingleStockService(ILogger<SingleStockService> logger) : base(logger)
        {
            _logger = logger;
        }

        public async Task StartStreaming(string symbol)
        {
            try
            {
                await SubscribeAsync(new[] { symbol });
                _ = ReceiveLoop(new[] { symbol });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error streaming for symbol: {symbol}");
            }
        }

        public async Task StopStreaming(string symbol)
        {
            try
            {
                await UnsubscribeAsync(new[] { symbol });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error stopping streaming for symbol: {symbol}");
            }
        }

        public override async ValueTask DisposeAsync() => await base.DisposeAsync();
    }
}
