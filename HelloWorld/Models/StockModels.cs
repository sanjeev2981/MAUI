using System.Collections.Generic;

namespace HelloWorld.Models
{
    public class StockPriceUpdate
    {
        public List<string> c { get; set; } = new List<string>();
        public string? S { get; set; } = string.Empty; // Symbol
        public decimal P { get; set; } = 0; // Price
        public long T { get; set; } = 0;   // Timestamp
        public int V { get; set; } = 0; //Volume
    }

    public class FinnhubResponse
    {
        public string? Type { get; set; }
        public StockPriceUpdate[]? Data { get; set; }
    }
}
