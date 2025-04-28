using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HelloWorld.Models;
using Microsoft.Extensions.Logging;

namespace HelloWorld.Services
{
    public class ProductService
    {
        private readonly ILogger<ProductService> _logger;

        private readonly HttpClient _httpClient;

        public ProductService(HttpClient httpClient, ILogger<ProductService> logger)
        {
            _logger = logger;
            var handler = new HttpClientHandler();

#if DEBUG
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
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

        public async Task<IEnumerable<Product>> GetProductsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://dummyjson.com/products");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var productsResponse = JsonSerializer.Deserialize<ProductResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return productsResponse?.Products ?? new List<Product>();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error fetching products");
                return new List<Product>();
            }
        }
    }
    public class ProductResponse
    {
        public List<Product>? Products { get; set; }
    }
}
