# DatabaseManager

Database Manager is a free, open source web based tool to manage 
PPDM database, Data Quality and Data Science projects. So far this version provides functionality to:
* Load the PPDM model
* Manage PPDM, CSV and LAS data connectors
* Transfer well data from one PPDM database, LAS or csv files to a PPDM database 
* Create a index for repository
* Manage data QC and prediction rules
* Dataops that includes data transfer, indexing data qc and predictions
* result viewer for data qc issues and predictions

The database is configurable so in practice any database model should work.
This tool is built for the cloud generation. It has two flavors:
* A blazor MVC version
* A serverless version using microservices

## Projects and folders
* Services - Contains micro services
* DatabaseManager.Common - Common dll's used by all projects 
* DatabaseManager.BlazorComponents - Common blazor components that can be used in custom User Interaces
* DatabaseManager.ServerLessClient - Serverless client using App functions to manage PPDM
* DatabaseManager.Appfunctions - App functions used for serverless DatabaseManager version
* DatabaseManager.LocalDataTransfer - A data transfer services for remote data access
* DatabaseManager - Standard Blazor MVC client server application

## License 
Database Manager is released under the GPLv3 or higher license.

## Contribution 
You can contribute to the enhancement of Database Manager either by providing 
bug fixes or enhancements to the Database Manager source code following the 
usual Github Fork-Pull Request process.

## Building the software
The software is based on .NET 6 using Blazor webassembly. You should use
Visual Studio 2022 or newer for building this. You can also use Azure Devops to build the software.

## Deployment
One way to publish this is to use the publish option in Visual Studio.

## Datamodel
The system does not ship with a data model. You must create a folder in your Azure File Storage called PPDM39. This is where you put the PPDM dll files that
you can download from the PPDM web site. In theory you can put any kind of ddl files here, but then you also need to update some of the other configuration
files. It is recommended that you use MS SQL Server 2019.

## Azure Storage Account Required
Database Manager requires an azure storage account. You define this with the key word AzureStorageConnection in your configuration or in the Setup option in the User Interface. This is where the system access data connectors, data models, rules, data access definitions and data files for loading such as LAS and csv files.

## API
The user interface is based on Blazor. An option for you is to build your own user interface but use our Web API. Here is a [Swagger Link](https://petrodataonline.azurewebsites.net/swagger) to the web api 

## Microservices
Some of the functionality is being converted into microservices. The following microservices are available:

### Data Sources
This is a CRUD for data sources used by Database Manager. The data sources are stored in a table in Azure storage. You need to define this in the header or a Connectionstring called AzurestorageConnection.

### Data configurations
This is a CRUD for data configuration files

### Indexer
This is a CRUD for the data index used by Data Manager.

### Rules
This is a CRUD for rules, functions and prediction sets

