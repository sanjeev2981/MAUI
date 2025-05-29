using HelloWorld.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HelloWorld.Services
{
    public abstract class StockServiceBase : IAsyncDisposable
    {
        protected readonly ILogger _logger;

        protected ClientWebSocket? _webSocket;
        protected CancellationTokenSource? _cancellationTokenSource;
        protected Subject<StockPriceUpdate>? _stockUpdates = new();

        public IObservable<StockPriceUpdate>? StockUpdates => _stockUpdates;

        protected const string FinnhubToken = "d0afk3pr01qm3l9l4hbgd0afk3pr01qm3l9l4hc0";
        protected const string BaseUrl = "wss://ws.finnhub.io?token=" + FinnhubToken;

        protected bool _isSubscribed = false;
        public bool isSubscribed => _isSubscribed;
        public bool isConnected => _webSocket != null && _webSocket.State == WebSocketState.Open;

        protected StockServiceBase(ILogger logger)
        {
            _logger = logger;
        }

        protected async Task ConnectAsync()
        {
            if (isConnected)
                return;

            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();
            await _webSocket.ConnectAsync(new Uri(BaseUrl), _cancellationTokenSource.Token);
        }

        protected async Task SubscribeAsync(IEnumerable<string> symbols)
        {
            if (symbols == null)
                return;

            await ConnectAsync();

            foreach (var symbol in symbols)
            {
                if (string.IsNullOrWhiteSpace(symbol))
                    continue;

                var message = JsonSerializer.Serialize(new { type = "subscribe", symbol });
                var bytes = Encoding.UTF8.GetBytes(message);
                await _webSocket!.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cancellationTokenSource!.Token);
            }

            _isSubscribed = true;
        }

        protected async Task UnsubscribeAsync(IEnumerable<string> symbols)
        {
            if (!isConnected || _cancellationTokenSource == null || symbols == null)
                return;

            foreach (var symbol in symbols)
            {
                if (string.IsNullOrWhiteSpace(symbol))
                    continue;

                var message = JsonSerializer.Serialize(new { type = "unsubscribe", symbol });
                var bytes = Encoding.UTF8.GetBytes(message);
                await _webSocket!.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
            }

            _isSubscribed = false;
        }

        protected async Task ReceiveLoop(IEnumerable<string> symbols)
        {
            var buffer = new byte[4096];

            try
            {
                if (_webSocket == null || _cancellationTokenSource == null)
                    return;

                _logger.LogInformation($"Starting Stream for symbols: {string.Join(", ", symbols)}");
                while (_webSocket.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);

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

        public virtual async ValueTask DisposeAsync()
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
                _logger.LogError($"Error disposing StockService: {ex.Message}");
            }
            finally
            {
                _webSocket = null;
                _cancellationTokenSource = null;
                _stockUpdates = null;
            }
        }
    }
}
