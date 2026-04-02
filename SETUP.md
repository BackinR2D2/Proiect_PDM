# Ghid setup local - Budget Planner

## Ce contine proiectul MAUI

Aplicatia respecta cerintele oficiale pentru proiectul .NET MAUI:

- 8 pagini: Overview, Tranzactii, Bugete, Categorii, Obiective, Rapoarte, Insight-uri, Setari
- controale variate pentru afisare, editare, selectie si colectii
- stocare persistenta locala in SQLite
- data binding dinamic pe baza de ViewModels
- acces la retea pentru curs valutar JSON si export XML pentru raport

## Ce ai nevoie

- Visual Studio 2022 cu workload-ul `.NET Multi-platform App UI development`
- Android SDK si emulator configurat in Visual Studio daca vrei test pe Android

## Persistenta datelor

Aplicatia nu mai depinde de SQL Server local.

Datele sunt salvate automat intr-o baza SQLite locala, in directorul de date al aplicatiei:

- Windows: in `FileSystem.AppDataDirectory`
- Android: in spatiul local al aplicatiei

La prima pornire, aplicatia:

- creeaza baza SQLite daca nu exista
- creeaza tabela de stare a aplicatiei
- insereaza automat date demo

## Rulare pe Windows

Din Visual Studio:

1. Deschide `Proiect_Planificare_Buget.sln`
2. Selecteaza profilul `Windows Machine`
3. Apasa `F5`

Din terminal:

```bash
dotnet build Proiect_Planificare_Buget\Proiect_Planificare_Buget.csproj -f net10.0-windows10.0.19041.0
dotnet run -f net10.0-windows10.0.19041.0 --project Proiect_Planificare_Buget
```

## Build pe Android

Din Visual Studio:

1. Selecteaza emulatorul sau dispozitivul Android
2. Ruleaza proiectul MAUI pe target Android

Din terminal:

```bash
dotnet build Proiect_Planificare_Buget\Proiect_Planificare_Buget.csproj -f net10.0-android -p:RuntimeIdentifier=android-x64
```

Pentru dispozitiv ARM, poti folosi un alt runtime identifier Android potrivit.

## Verificare rapida

Dupa primul start ar trebui sa vezi:

- categorii predefinite pentru cheltuieli si venituri
- 5 bugete demo
- 2 obiective demo
- 8 tranzactii demo

In build-urile `DEBUG`, aplicatia scrie si un fisier de diagnostic numit `budget-db-status.txt` in directorul de date local al aplicatiei.

## Fisiere importante

- `Proiect_Planificare_Buget/Services/BudgetDataService.cs` - logica de persistenta si business
- `Proiect_Planificare_Buget/Services/BudgetDataService.Sqlite.Windows.cs` - backend SQLite pentru Windows
- `Proiect_Planificare_Buget/Services/BudgetDataService.Sqlite.Android.cs` - backend SQLite pentru Android
- `Proiect_Planificare_Buget/Services/ExchangeRateService.cs` - acces la retea si procesare JSON
- `Proiect_Planificare_Buget/Services/XmlReportService.cs` - generare raport XML
