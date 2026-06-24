# FinPredictData — Capa d'Accés a Dades

Projecte de capa d'accés a dades (DAL) basat en **Entity Framework Core** i **PostgreSQL (Npgsql)**. Generat mitjançant *scaffolding* invers (`dotnet ef dbcontext scaffold`) a partir de la base de dades `tdrmercatdb`.

## Models (Entitats)

| Model | Descripció |
|---|---|
| `Source` | Fonts de dades (ex.: Macrotrends, Yahoo Finance) |
| `Datum` | Series de dades financeres, vinculades a una font |
| `HistoricalDatum` | Valors historics serie temporal (data, valor) |

**Relacions:**
- `Source` 1:N `Datum` → `Source.Data`
- `Datum` 1:N `HistoricalDatum` → `Datum.HistoricalData`
- `HistoricalDatum` N:1 `Datum` → `HistoricalDatum.Data` (FK `DataId`)
- `Datum` N:1 `Source` → `Datum.Source` (FK `SourceId`)

## Connexio a la Base de Dades

La cadena de connexió es llegeix des de `appsettings.json` del projecte `FinPredictConsola`:

```json
{
  "ConnectionStrings": {
    "TDRMercatDB": "Host=localhost;Port=5432;Database=tdrmercatdb;Username=tdrmercat;Password=xxx"
  }
}
```

El `Program.cs` carrega la configuracio amb `Microsoft.Extensions.Configuration.Json` i la posa a disposicio del `TDRMercatContext` via `DbContextOptions`.

> **Nota:** `Context/TDRMercatContext.cs` conserva una cadena hardcodejada com a fallback per a fer scaffolding o usos puntuals, pero el flux normal llig des de `appsettings.json`.

## Requisits

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 15+ (o compatible)
- Paquets NuGet (ja al `.csproj`):
  - `Microsoft.EntityFrameworkCore.Design` (10.0.9)
  - `Npgsql.EntityFrameworkCore.PostgreSQL` (10.0.2)

## Com Rescaffoldjar la Base de Dades

Per regenerar les entitats i el context des de la base de dades PostgreSQL:

```bash
cd C:\TDR\TDRrodripy\src\FinPredictData
dotnet ef dbcontext scaffold 
  "Host=localhost;Port=5432;Database=tdrmercatdb;Username=tdrmercat;Password=xxxx" \
  Npgsql.EntityFrameworkCore.PostgreSQL \
  --context "Context/TDRMercatContext" \
  --context-dir Context \
  --output-dir Models \
  --namespace FinPredictData.Models \
  --context-namespace FinPredictData.Context \
  --force
```

| Flag | Significat |
|---|---|
| `--force` | Sobreescriu fitxers existents |
| `--context-dir` | Directori on es genera el DbContext |
| `--output-dir` | Directori on es generen les entitats |
| `--namespace` | Namespace de les entitats |
| `--context-namespace` | Namespace del DbContext |


## Compilacio

Des de l'arrel de la solucio:

```bash
dotnet restore
dotnet build
```
