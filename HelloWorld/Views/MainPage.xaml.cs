using HelloWorld.ViewModels;
using Plugin.Maui.Biometric;
using System.Threading;

namespace HelloWorld.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _viewModel;

        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _viewModel = viewModel;
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_viewModel.Products.Count == 0) // Load products only if not already loaded
            {
                await _viewModel.LoadProductsAsync();
            }
        }

        private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.FilterCommand.Execute(e.NewTextValue);
        }

        private void OnPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = (Picker)sender;
            string sortBy = (string)picker.SelectedItem;
            _viewModel.SortCommand.Execute(sortBy);
        }
    }

}
