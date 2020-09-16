# DatabaseManager

Database Manager is a free, open source web based tool to manage 
PPDM database. So far this version provides functionality to:
* Load the PPDM model
* Manage PPDM data connectors
* Transfer well data from one PPDM database to another 
* Load data from LAS and csv files into PPDM
* Create a DSM index
* Manage DSM rules
* Execute QC rules and view result
* Execute prediction rules and view result
This tool is built for the cloud generation.

## License 
Database Manager is released under the GPLv3 or higher license.

## Contribution 
You can contribute to the enhancement of Database Manager either by providing 
bug fixes or enhancements to the Database Manager source code following the 
usual Github Fork-Pull Request process.

## Building the software
The software is based on ASP.NET Core 3.2 Blazor using webassembly. You should use
Visual Studio 2019.6 or newer for building this. From Visual Studio you can publish this to Azure or to a local web server.

## Datamodel
The system does not ship with a data model. You must create a folder in your Azure File Storage called PPDM39. This is where yo put the PPDM dll files that
you can download from the PPDM web site. In theory you can put any kind of ddl files here, but then you also need to update some of the other configuration
files

## Azure Storage Account Required
Database Manager requires an azure storage account. You define this with the key word AzureStorageConnection in your configuration. This is where the system access data connectors, data models, rules, data access definitions and data files for loading such as LAS and csv files.
