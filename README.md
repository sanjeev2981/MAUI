# MAUI Stock Demo

This repository contains a cross-platform .NET MAUI application demonstrating several features:

- **Product browser** backed by `ProductService` which fetches products from [dummyjson](https://dummyjson.com) via `HttpClient`.
- **User authentication** handled by `UserService` with optional biometric login support via `Plugin.Maui.Biometric`.
- **Real‑time stock prices** from the Finnhub WebSocket API managed by `StockServiceBase` and its derived services `SingleStockService` and `StockTickerService`.
- **Interactive charting** implemented in `StockChartViewModel` using SkiaSharp.
- **MVVM architecture** using `CommunityToolkit.Mvvm` with view models located under `ViewModels/`.

The solution `HelloWorld.sln` targets Android, iOS and MacCatalyst (see `HelloWorld.csproj`). Pages reside in `Views/` and application startup is configured in `MauiProgram.cs`.

## Building

To build the application open the solution in Visual Studio 2022+ with the .NET MAUI workload installed. Alternatively run `dotnet build` if .NET SDK is available.

## Running

Choose a target platform in Visual Studio or specify one via the command line (e.g. `dotnet build -t:Run -f net8.0-android`).

## Project Structure

- `Services/` – application services including WebSocket stock services and HTTP API clients.
- `ViewModels/` – data binding and UI logic implemented with MVVM.
- `Views/` – XAML pages for login, product browsing, stock ticker, stock chart and web view.
- `Models/` – simple POCO models such as `Product`, `User` and `StockPriceUpdate`.

## Notes

Real‑time stock data requires a valid Finnhub token which is currently hard coded in `StockServiceBase`.
