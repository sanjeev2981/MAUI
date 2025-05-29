using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HelloWorld.Models;


namespace HelloWorld.Services
{
    public class StockTickerService : StockServiceBase
    {
        private readonly ILogger<StockTickerService> _logger;

        public StockTickerService(ILogger<StockTickerService> logger) : base(logger)
        {
            _logger = logger;
        }

        public async void StartStreaming(IEnumerable<string> symbols)
        {
            try
            {
                await SubscribeAsync(symbols);
                _ = ReceiveLoop(symbols);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error starting StockTickerService: {ex.Message}");
            }
        }

        public async Task PauseAsync(IEnumerable<string> symbols)
        {
            try
            {
                if (isConnected)
                {
                    await UnsubscribeAsync(symbols);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error pausing StockTickerService: {ex.Message}");
            }
        }

        public async Task StopAsync()
        {
            await DisposeAsync();
        }

        public async ValueTask DisposeAsync() => await base.DisposeAsync();
    }
}
