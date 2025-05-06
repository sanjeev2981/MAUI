using HelloWorld.Views;

namespace HelloWorld
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register routes
            Routing.RegisterRoute("chartpage", typeof(StockChartPage));
        }
    }
}
