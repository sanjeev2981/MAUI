using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelloWorld.Models;
using HelloWorld.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace HelloWorld.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private ILogger<MainViewModel> _logger;
        public ICommand LogoutCommand { get; }

        private readonly ProductService _productService;
        private ObservableCollection<Product> _allProducts;

        [ObservableProperty]
        private ObservableCollection<Product> products;

        public Command<string> SortCommand { get; }
        public Command<string> FilterCommand { get; }

        public MainViewModel(ProductService productService, ILogger<MainViewModel> logger)
        {
            _logger = logger;
            _productService = productService;

            Products = new ObservableCollection<Product>();
            _allProducts = new ObservableCollection<Product>();
            LogoutCommand = new RelayCommand(async () => await LogoutAsync());
            SortCommand = new Command<string>(SortProducts);
            FilterCommand = new Command<string>(FilterProducts);
        }

        public async Task LoadProductsAsync()
        {
            try
            {
                var productsCollection = await _productService.GetProductsAsync();
                if (productsCollection != null)
                {
                    _allProducts.Clear();
                    foreach (var product in productsCollection)
                    {
                        _allProducts.Add(product);
                    }
                    FilterProducts(string.Empty); // Initialize the filtered list
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products");
            }
        }

        private void SortProducts(string sortBy)
        {
            switch (sortBy)
            {
                case "Price":
                    Products = new ObservableCollection<Product>(Products.OrderBy(p => p.Price));
                    break;
                case "Name":
                    Products = new ObservableCollection<Product>(Products.OrderBy(p => p.Title));
                    break;
                default:
                    break;
            }
        }

        private void FilterProducts(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
            {
                Products = new ObservableCollection<Product>(_allProducts);
            }
            else
            {
                Products = new ObservableCollection<Product>(
                    _allProducts.Where(p => p.Title != null && p.Title.ToLower().Contains(filter.ToLower())));
            }
        }

        public async Task LogoutAsync()
        {
             SecureStorage.Default.Remove("authToken");
            
            //Navigate back to the login page
             await Shell.Current.GoToAsync("//login");
        }
    }
}
