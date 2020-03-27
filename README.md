# DatabaseManager

Database Manager is a free, open source web based tool to manage 
PPDM database. So far this version provides functionality to:
* Load the PPDM model
* Manage PPDM data connectors
* Transfer well data from one PPDM database to another 

More functionality wil come later including 
ideally a serverless architecture.

## License 
Database Manager is released under the GPLv3 or higher license.

## Contribution 
You can contribute to the enhancement of Database Transfer either by providing 
bug fixes or enhancements to the Database Transfer source code following the 
usual Github Fork-Pull Request process.

## Building the software
The software is based on ASP.NET Core 3.2 Blazor Preview 2 using webassembly. It is 
recommended to use Visual Studio 2019 version 16.5.1 or newer for building this.

## Database Connectors
Database connector requires an azure storage account. You define this with AzureStorageConnection.
