# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## Build & Run

```bash
# Build for Windows
dotnet build Proiect_Planificare_Buget.sln -f net10.0-windows10.0.19041.0

# Run on Windows
dotnet run -f net10.0-windows10.0.19041.0 --project Proiect_Planificare_Buget

# Build Release
dotnet build -c Release
```

There are no automated tests in this project.

## Architecture

**.NET 10 MAUI** cross-platform budget planning app targeting Windows, Android, iOS, and macOS Catalyst. UI labels and category names are in Romanian.

**MVVM pattern:**
- `Pages/` — XAML views with minimal code-behind; bind to ViewModels via `BindingContext`
- `ViewModels/` — inherits `ViewModelBase` (provides `INotifyPropertyChanged`, `SetProperty<T>`, and `RunOnMainThread`); all registered as Singletons in DI
- `Models/` — sealed data classes; `BudgetAppData` is the aggregate root containing all app data
- `Services/` — business logic, all Singleton-registered

**Navigation:** MAUI Shell (`AppShell.xaml`) with a flyout menu for 6 sections: Overview, Transactions, Budgets, Goals, Insights, Settings.

**Data persistence:** `BudgetDataService` serializes `BudgetAppData` to JSON at `FileSystem.AppDataDirectory/budget-planner-data.json`. Access is protected by a `SemaphoreSlim`. Raise `DataChanged` event after mutations so ViewModels can refresh.

**Currency conversion:** `ExchangeRateService` calls `frankfurter.app` API. Supported currencies: RON, EUR, USD.

**Report export:** `XmlReportService` generates XML snapshots of all app data.

**DI setup:** `MauiProgram.cs` registers `HttpClient`, all three services, and all six ViewModels.
