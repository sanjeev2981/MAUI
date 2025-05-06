using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Reactive.Linq;
using System.Net;


namespace HelloWorld.Services
{
    public class StockTickerService : IAsyncDisposable
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

        public StockTickerService(ILogger<StockTickerService> logger)
        {
            _logger = logger;  
        }

        public async Task SubscribeAsync(IEnumerable<string> symbols)
        {
            try
            {
                if(symbols != null)
                {
                    // Connect to the WebSocket server
                    _webSocket = new ClientWebSocket();
                    _cancellationTokenSource = new CancellationTokenSource();
                    await _webSocket.ConnectAsync(new Uri(BaseUrl), _cancellationTokenSource.Token);

                    // Subscribe to each symbol
                    foreach (var symbol in symbols)
                    {
                        var message = JsonSerializer.Serialize(new { type = "subscribe", symbol });
                        var bytes = Encoding.UTF8.GetBytes(message);
                        await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
                    }

                    _isSubscribed = true;
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message,"Error subscribing to symbols");
            }
        }

        public async Task UnsubscribeFromSymbols(IEnumerable<string> symbols)
        {
            try
            {
                if (symbols != null && _webSocket != null && _cancellationTokenSource != null)
                {
                    foreach (var symbol in symbols)
                    {
                        var message = JsonSerializer.Serialize(new { type = "unsubscribe", symbol });
                        var bytes = Encoding.UTF8.GetBytes(message);
                        await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error unsubscribing from symbols: {ex.Message}");
            }
        }
        public async void StartStreaming(IEnumerable<string> symbols)
        {
            try
            {   
                if(!isConnected)
                {
                    await SubscribeAsync(symbols);
                }
                else
                {
                    _logger.LogInformation($"Already connected to WebSocket. No need to reconnect.");
                }
                _ = ReceiveLoop(symbols);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error starting StockTickerService: {ex.Message}");
            }
        }
        private async Task ReceiveLoop(IEnumerable<string> symbols)
        {
            var buffer = new byte[4096];

            try
            {
                if(_webSocket == null || _cancellationTokenSource == null)
                    return;

                _logger.LogInformation($"Starting Stream for symbols: {string.Join(", ", symbols)}");
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
                _logger.LogInformation($"Stopping Stream for symbols: {string.Join(", ", symbols)}");
            }
            catch (OperationCanceledException)
            {
                // expected on StopAsync
            }
            catch (WebSocketException ex)
            {
                if(ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
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

        public async Task PauseAsync(IEnumerable<string> symbols)
        {
            try
            {
                if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    await UnsubscribeFromSymbols(symbols);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error pausing StockTickerService: {ex.Message}");
            }
        }
        public async Task StopAsync()
        {
            try
            {
                if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stopping", CancellationToken.None);
                }

                _cancellationTokenSource?.Cancel();
                _webSocket?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping StockTickerService: {ex.Message}");
            }
            finally
            {
               _webSocket = null;
               _cancellationTokenSource = null;
            }
        }

        public async ValueTask DisposeAsync()
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

                GC.SuppressFinalize(this);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error disposing StockTickerService: {ex.Message}");
            }
            finally
            {
                _webSocket = null;
                _cancellationTokenSource = null;
                _stockUpdates = null;
            }
        }
    }
    public class StockPriceUpdate
    {
        public List<string> c { get; set; } = new List<string>();
        public string? S { get; set; } = string.Empty; // Symbol
        public decimal P { get; set; } = 0;// Price
        public long T { get; set; } = 0;   // Timestamp
        public int V { get; set; } = 0; //Volume
    }
    public class FinnhubResponse
    {
        public string? Type { get; set; }
        public StockPriceUpdate[]? Data { get; set; }
    }
}
