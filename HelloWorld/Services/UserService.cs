using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HelloWorld.Models;
using System.Text.Json;
using System.Net.Security;
using Microsoft.Extensions.Logging;

namespace HelloWorld.Services
{
    public class UserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly HttpClient _httpClient;

        public UserService(HttpClient httpClient, ILogger<UserService> logger)
        {
            _logger = logger;
            var handler = new HttpClientHandler();
#if DEBUG
            handler.ServerCertificateCustomValidationCallback += (message, cert, chain, errors) =>
            {
                if (cert != null && cert.Issuer.Equals("CN=dummyjson.com"))
                    return true;
                else if (errors == SslPolicyErrors.RemoteCertificateChainErrors)
                    return true;

                return errors == System.Net.Security.SslPolicyErrors.None;
            };
#endif
            _httpClient = new HttpClient(handler);
        }

        public async Task<User> AuthenticateAsync(string username, string password)
        {
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                        new KeyValuePair<string, string>("username", username),
                        new KeyValuePair<string, string>("password", password)
                    });

                var response = await _httpClient.PostAsync("https://dummyjson.com/auth/login", content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var userResponse = JsonSerializer.Deserialize<User>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (userResponse != null)
                {
                    userResponse.IsAuthenticated = true;
                }

                _logger.LogInformation($"User {username} authentication response: {json}");
                return userResponse ?? new User();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error authenticating user {username}");
                return new User();
            }
        }

        public async Task<bool> IsValidToken(string username, string? token)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://dummyjson.com/user/me");
                request.Headers.Add("Authorization", $"Bearer {token}");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var userResponse = JsonSerializer.Deserialize<User>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation($"User {username} token validation response: {json}");
                return username == userResponse?.Username;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating token for user {username}");
                return false;
            }
        }

        public IEnumerable<string> GetUserStocks(string username)
        {
            try
            {
                switch (username)
                {
                    case "emilys":
                        return new List<string> { "AAPL", "GOOGL", "MSFT", "META", "AMZN", "NFLX" };
                    case "michaelw":
                        return new List<string> { "CVX", "XOM", "NOV", "OXY", "SLB", "COP", "HAL" };
                    default:
                        return new List<string>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting stocks for user {username}");
                return new List<string>();
            }
        }

        public IEnumerable<string> GetUserFavoriteStocks(string username)
        {
            try
            {
                switch (username)
                {
                    case "emilys":
                        return new List<string> { "AAPL", "GOOGL", "MSFT" };
                    case "michaelw":
                        return new List<string> { "CVX", "XOM", "NOV" };
                    default:
                        return new List<string>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting favorite stocks for user {username}");
                return new List<string>();
            }
        }
    }
}
