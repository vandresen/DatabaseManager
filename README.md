# DatabaseManager

![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)
![.NET](https://img.shields.io/badge/.NET-6.0-purple.svg)
![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4.svg)

**DatabaseManager** is a free, open-source web-based tool for managing [PPDM](https://ppdm.org/) databases, Data Quality, and Data Science projects. It enables well data transfer, data indexing, QC rule management, and predictive analytics — all from a configurable, cloud-ready interface.

---

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Project Structure](#project-structure)
- [Deployment](#deployment)
- [Data Model](#data-model)
- [API & Microservices](#api--microservices)
- [Contributing](#contributing)
- [License](#license)

---

## Features

- Load and manage the PPDM data model
- Manage PPDM, CSV, and LAS data connectors
- Transfer well data between PPDM databases, LAS files, and CSV files
- Create and manage a data repository index
- Define and run Data QC and prediction rules
- End-to-end DataOps pipeline: transfer → index → QC → predict
- View data QC issues and prediction results

---

## Architecture

DatabaseManager ships in two flavors:

| Flavor | Description |
|---|---|
| **Blazor MVC** | Traditional client-server Blazor application |
| **Serverless Client** | Blazor WebAssembly frontend backed by Azure microservices |

---

## Prerequisites

Before building or running DatabaseManager, ensure you have the following:

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or newer (or Azure DevOps for CI builds)
- **Microsoft SQL Server 2019** (recommended)
- An **Azure Storage Account** (required for connectors, models, rules, and data files)
- An **Esri ArcGIS API Key** (required for the basemap tab)
- The **PPDM DDL files**, downloadable from the [PPDM website](https://ppdm.org/)

---

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/your-org/DatabaseManager.git
cd DatabaseManager
```

### 2. Set up Azure Storage

DatabaseManager requires an Azure Storage Account. Create one in the Azure Portal, then:

- Create a file share folder named **`PPDM39`** and upload your PPDM DDL files there.
- Note your storage connection string — you will need it in the next step.

### 3. Configure the application

For the **Serverless Client**, create or update `appsettings.json` (see [Configuration](#configuration) below).

For the **MVC version**, set the `AzureStorageConnection` key in your `appsettings.json` or via the Setup option in the UI.

### 4. Build and run

Open the solution in Visual Studio 2022 and press **F5**, or build from the CLI:

```bash
dotnet build
dotnet run --project DatabaseManager
```

---

## Configuration

### Serverless Client — `appsettings.json`

```json
{
  "Sqlite": false,
  "ArcGISApiKey": "<your-esri-arcgis-api-key>",
  "ServiceUrls": {
    "DataSourceAPI": "<url>",
    "DataSourceKey": "<key>",
    "IndexAPI": "<url>",
    "IndexKey": "<key>",
    "DataConfigurationAPI": "<url>",
    "DataConfigurationKey": "<key>",
    "DataModelAPI": "<url>",
    "DataModelKey": "<key>",
    "DataRuleAPI": "<url>",
    "DataRuleKey": "<key>",
    "DataTransferAPI": "<url>",
    "DataTransferKey": "<key>"
  }
}
```

| Key | Type | Description |
|---|---|---|
| `Sqlite` | `bool` | Set to `true` to use SQLite-backed microservices instead of SQL Server |
| `ArcGISApiKey` | `string` | API key from Esri — required for the basemap tab |
| `ServiceUrls.*API` | `string` | Base URL for the corresponding microservice endpoint |
| `ServiceUrls.*Key` | `string` | API key or access key for the corresponding microservice |

### Azure Storage Connection

Set the `AzureStorageConnection` key in your configuration or via the **Setup** option in the UI. This connection string grants access to data connectors, models, rules, and LAS/CSV files.

---

## Project Structure

```
/
├── Services/                          # Microservices
├── DatabaseManager.Common/            # Shared libraries used across all projects
├── DatabaseManager.BlazorComponents/  # Reusable Blazor UI components
├── DatabaseManager.ServerLessClient/  # Serverless Blazor WebAssembly client
├── DatabaseManager.Appfunctions/      # Azure Functions for the serverless version
├── DatabaseManager.LocalDataTransfer/ # Data transfer service for remote data access
└── DatabaseManager/                   # Standard Blazor MVC client-server application
```

---

## Deployment

### Option 1: Visual Studio Publish

Use the **Publish** option in Visual Studio to deploy to your target environment.

### Option 2: Azure App Service (zip deploy)

1. Build and publish the MVC version to a local folder.
2. Zip the output folder.
3. Deploy via [Kudu](https://github.com/projectkudu/kudu) in your Azure App Service:
   - Navigate to `https://<your-app>.scm.azurewebsites.net`
   - Use the **Zip Deploy** tool to upload your zip file.

### Option 3: Self-hosted Web Server

Download the pre-built zip from the [Releases](../../releases) page and extract it to your web server's root directory.

---

## Data Model

DatabaseManager does not ship with a data model. To set one up:

1. Download the PPDM DDL files from the [PPDM website](https://ppdm.org/).
2. In your Azure File Storage account, create a folder named **`PPDM39`**.
3. Upload the DDL files to that folder.

> **Note:** In principle, any DDL files can be used, but additional configuration files may also need to be updated. SQL Server 2019 is the recommended database engine.

---

## API & Microservices

### Swagger / Web API

The UI is built on Blazor, but you can build your own frontend against the Web API.  
📄 [View the Swagger documentation](https://petrodataonline.azurewebsites.net/swagger)

### Available Microservices

| Service | Description |
|---|---|
| **Data Sources** | CRUD operations for data source connectors (stored in Azure Storage) |
| **Data Configurations** | CRUD operations for data configuration files |
| **Indexer** | CRUD operations for the data repository index |
| **Rules** | CRUD operations for rules, functions, and prediction sets |

All microservices require the `AzureStorageConnection` connection string, either in the request header or in the application configuration.

---

## Contributing

Contributions are welcome! To get started:

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-improvement`
3. Commit your changes: `git commit -m 'Add my improvement'`
4. Push to the branch: `git push origin feature/my-improvement`
5. Open a Pull Request

Please open an issue first for major changes so we can discuss the approach.

---

## License

DatabaseManager is released under the [GNU General Public License v3.0 or later](LICENSE).
