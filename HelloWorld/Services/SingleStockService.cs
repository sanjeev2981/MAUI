using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HelloWorld.Services
{
    //Manages connection & realtime + historical data for a single stock
    public class SingleStockService : IAsyncDisposable
    {
        private readonly ILogger<StockTickerService> _logger;

        private ClientWebSocket? _webSocket;
        private CancellationTokenSource? _cancellationTokenSource;
        private Subject<StockPriceUpdate>? _stockUpdates = new();

        public IObservable<StockPriceUpdate>? StockUpdates => _stockUpdates;

        private const string FinnhubToken = "d0afk3pr01qm3l9l4hbgd0afk3pr01qm3l9l4hc0";
        private const string BaseUrl = "wss://ws.finnhub.io?token=" + FinnhubToken;

        private bool _isSubscribed = false;
        public bool isSubscribed => _isSubscribed;
        public bool isConnected => _webSocket != null && _webSocket.State == WebSocketState.Open;

        public SingleStockService(ILogger<StockTickerService> logger)
        {
            _logger = logger;
        }

        private async Task SubscribeAsync(string symbol)
        {
            try
            {
                if (!string.IsNullOrEmpty(symbol))
                {
                    // Create a new websocket if not connected
                    if (!isConnected)
                    {
                        _webSocket = new ClientWebSocket();
                        _cancellationTokenSource = new CancellationTokenSource();
                        await _webSocket.ConnectAsync(new Uri(BaseUrl), _cancellationTokenSource.Token);
                    }
  
                    // Subscribe to the symbol
                    var message = JsonSerializer.Serialize(new { type = "subscribe", symbol });
                    var bytes = Encoding.UTF8.GetBytes(message);
                    await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
                    
                    _isSubscribed = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error subscribing to symbol: {symbol}");
            }
        }

        private async Task UnsubscribeAsync(string symbol)
        {
            try
            {
                if(isConnected)
                {
                    // Subscribe to the symbol
                    _logger.LogInformation($"Unsubscribing from symbol: {symbol}");
                    var message = JsonSerializer.Serialize(new { type = "unsubscribe", symbol });
                    var bytes = Encoding.UTF8.GetBytes(message);
                    await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);

                    _isSubscribed = false;
                }

            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error unsubscribing from symbol: {symbol}");
            }
        }

        public async Task StartStreaming(string symbol)
        {
            try
            {
                await SubscribeAsync(symbol);
                _ = ReceiveLoop(symbol);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,$"Error streaming for symbol: {symbol}");
            }
        }

        public async Task StopStreaming(string symbol)
        {
            try
            {
                await UnsubscribeAsync(symbol);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error stopping streaming for symbol: {symbol}");
            }
        }

        private async Task ReceiveLoop(string symbol)
        {
            var buffer = new byte[4096];

            try
            {
                if (_webSocket == null || _cancellationTokenSource == null)
                    return;

                _logger.LogInformation($"Starting Stream for symbol: {symbol}");
                while (_webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    // Skip ping messages
                    if (json.Contains("\"type\":\"ping\""))
                        continue;

                    var update = JsonSerializer.Deserialize<FinnhubResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (update?.Data != null)
                    {
                        foreach (var item in update.Data)
                            _stockUpdates?.OnNext(new StockPriceUpdate
                            {
                                S = item.S,
                                P = item.P,
                                T = item.T,
                                V = item.V
                            });
                    }
                }
                _logger.LogInformation($"Stopping Stream for symbol: {symbol}");
            }
            catch (OperationCanceledException)
            {
                // expected on StopAsync
            }
            catch (WebSocketException ex)
            {
                if (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                {
                    //Will reconnect automatically
                }
                else
                {
                    _logger.LogError($"WebSocket error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ReceiveLoop: {ex.Message}");
            }
        }

        public async Task CloseConnection()
        {
            try
            {
                if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "App closing", CancellationToken.None);
                }
                _webSocket?.Dispose();
                _cancellationTokenSource?.Cancel();
                _stockUpdates?.OnCompleted();

                _webSocket = null;
                _cancellationTokenSource = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping StockTickerService: {ex.Message}");
            }
            finally
            {
                _webSocket = null;
                _cancellationTokenSource = null;
                _stockUpdates?.OnCompleted();
                _stockUpdates = null;
            }
        }
        public async ValueTask DisposeAsync()
        {
            try
            {
                await CloseConnection();
                GC.SuppressFinalize(this);
            }
            catch(Exception ex)
            {
                _logger.LogError($"Error in DisposeAsync: {ex.Message}");
            }
            finally
            {
                _webSocket = null;
                _cancellationTokenSource = null;
                _stockUpdates?.OnCompleted();
                _stockUpdates = null;
            }
        }
    }
}
