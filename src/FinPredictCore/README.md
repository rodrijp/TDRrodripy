# FinPredictCore

## 📋 Descripció del Projecte

**FinPredictCore** és la capa central del sistema FinPredict. Conté la lògica principal per a:

- 📊 **Extracció de dades financeres**: Descàrrega automàtica de dades de fonts externes (com Macrotrends)
- 🔄 **Transformació de dades**: Neteja i formatatge de fitxers CSV
- 💼 **Gestió de serveis**: Accés als serveis de dades històriques
- 🔌 **Inyecció de dependències**: Configuració centralitzada de serveis

## 🛠️ Tecnologies Utilitzades

- **.NET 10.0**: Framework de desenvolupament
- **Microsoft.Playwright**: Automatització de navegador per a descàrrega de fitxers
- **Microsoft.Extensions.DependencyInjection**: Contenidor d'inyecció de dependències
- **Entity Framework Core**: ORM per a accés a dades

## 📦 Estructura del Projecte

```
FinPredictCore/
├── DependencyInjection/
│   └── ServiceCollectionExtensions.cs    # Configuració de serveis
├── Fuentes/
│   └── Macrotrends.cs                    # Integració amb Macrotrends
├── Jobs/
│   └── IImportFuentesToDB.cs             # Interfície de jobs
├── Service/
│   └── HistoricalData/                   # Serveis de dades històriques
└── FinPredictCore.csproj
```

## ⚙️ Instal·lació i Configuració

### 1. Requisits Previs

- .NET 10.0 SDK instal·lat
- PowerShell (per a Windows)

### 2. Instal·lació de Playwright

FinPredictCore utilitza **Microsoft.Playwright** per a automatitzar la descàrrega de fitxers CSV desde navegadors web.

Per instal·lar els navegadors necessaris, executa per versions:

```powershell
#pwsh bin/Debug/netx/playwright.ps1 install  No funciona encara a NET 10.0
instal·lar nodejs
npx playwright install
```

> **Nota**: Substitueix `net10.0` per la versió de framework que estiguis utilitzant si és diferent.

### 3. Restauració de Dependències

```bash
dotnet restore
```

### 4. Compilació

```bash
dotnet build
```

## 🚀 Ús

### Descàrrega de Dades amb Macrotrends

```csharp
var macrotrends = new Macrotrends();

// Descarregar CSV des de Macrotrends
await macrotrends.DownloadCSVAsync(
    "https://www.macrotrends.net/...", 
    @"C:\ruta\destino\datos.csv"
);

// Netejar i formatar CSV
macrotrends.LimpiarCSV(
    @"C:\ruta\origen\datos.csv",
    @"C:\ruta\destino\datos_limpios.csv"
);
```

### Configuració de Dependències

```csharp
var services = new ServiceCollection();
services.AddFinPredictServices();
var serviceProvider = services.BuildServiceProvider();

var historicalDataService = serviceProvider.GetRequiredService<IHistoricalDataService>();
```

## 🔗 Funcionalitats Principals

### Classe `Macrotrends`

#### `DownloadCSVAsync(url, rutaDestino)`
Descarrega un fitxer CSV desde una URL utilitzant Playwright:
- Obre un navegador Chromium
- Navega a la URL especificada
- Fa clic al botó "Download Data"
- Guarda el fitxer en la ruta destino

#### `LimpiarCSV(rutaOrigen, rutaDestino)`
Neteja i normalitza dades CSV:
- Processa dates i valors decimals
- Agrupa dades per mes (primer valor de cada mes)
- Ordena cronològicament
- Exporta en format CSV

## 📝 Configuració d'Entorn

Les dependències i versions estan especificades a `FinPredictCore.csproj`:

- `Microsoft.Extensions.DependencyInjection`: v10.0.9
- `Microsoft.Extensions.Hosting`: v10.0.9
- `Microsoft.Extensions.Logging`: v10.0.9
- `Microsoft.Playwright`: v1.48.2

## ⚠️ Notas Importantes

- Playwright requeriex una connexió a Internet per a descarregar els navegadors
- Els fitxers CSV es guarden en la ruta especificada (assegurant que la carpeta existeix)
- Usa `async/await` per a les operacions de descàrrega

## 📞 Suport

Per a més informació, consulta la documentació principal del projecte FinPredict.
