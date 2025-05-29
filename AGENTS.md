# Contribution Guide

This project contains a .NET MAUI sample located under `HelloWorld/` and a solution file `HelloWorld.sln`. The app follows an MVVM approach with view models under `ViewModels/`, UI pages under `Views/`, domain models under `Models/` and application logic under `Services/`.

## MAUI and MVVM
- **Dependency injection** is configured in `MauiProgram.cs` where services and view models are registered with the DI container.
- **CommunityToolkit.Mvvm** is used for state management. View models derive from `ObservableObject` and expose properties using `[ObservableProperty]` attributes.
- **Logging** uses `ILogger` implementations injected via DI. Prefer asynchronous methods for service APIs and log errors or important state changes.
- **Credential storage** should use `SecureStorage` for secrets or `Preferences` for non‑sensitive values.

## Coding Conventions
- Register all new services and view models in `MauiProgram.cs`.
- Use `ObservableCollection` for any collection bound to the UI.
- Name classes and members consistently with existing code.
- Wrap network calls and WebSocket interactions in `try/catch` blocks and log exceptions.
- Base service methods such as `ConnectAsync`, `SubscribeAsync` and `UnsubscribeAsync` in `StockServiceBase` currently lack comprehensive error handling. Future changes should add `try/catch` blocks and log failures so services fail gracefully.

## Programmatic Checks
- Run `dotnet build HelloWorld.sln` before committing to ensure the application compiles.

This document summarizes patterns already present and encourages improved exception handling for future contributions.
