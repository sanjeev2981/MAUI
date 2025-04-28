using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloWorld.ViewModels
{
    public partial class StockViewModel : ObservableObject
    {

        public string? Symbol { get; }

        [ObservableProperty]
        private decimal latestPrice;
        
        [ObservableProperty]
        private DateTime? lastUpdated;
        public StockViewModel(string symbol)
        {
            Symbol = symbol;
        }
        
        public void Update(decimal price, long timestamp)
        {
            LatestPrice = price;
            LastUpdated = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).LocalDateTime;
        }
    }
}
