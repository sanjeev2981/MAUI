using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloWorld.ViewModels
{
    public partial class StockViewModel : ObservableObject
    {
        private readonly Action<string> _navigateAction;
        public IRelayCommand ViewChartCommand { get; }

        public string? Symbol { get; }

        [ObservableProperty]
        private decimal latestPrice;
        
        [ObservableProperty]
        private DateTime? lastUpdated;
        public StockViewModel(string symbol, Action<string> navigateAction)
        {
            Symbol = symbol;
            _navigateAction = navigateAction;
            ViewChartCommand = new RelayCommand(OnViewChart);
        }

        public StockViewModel()
        {
        }

        public void Update(decimal price, long timestamp)
        {
            LatestPrice = price;
            LastUpdated = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).LocalDateTime;
        }
        private void OnViewChart()
        {
            _navigateAction?.Invoke(Symbol);
        }
    }
}
