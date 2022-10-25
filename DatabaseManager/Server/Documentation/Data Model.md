## Data Model
This is where you create the PPDM model and the model required for Data Science Management (DSM) work. There are 3 main options:

* PPDM Model
* PPDM Modifications
* DSM Model

#### PPDM Model
Any PPDM model can be used, but all the data access has been set up to use version 3.9. The PPDM data model files can be 
downloaded from the www.ppdm.org web site. You must be member in order to do this. 

The files must be copied to a folder in your azure storage account under File Shares called ppdm39. This must be manually created.
These files are very small so you basically don't pay anything for this storage. 

#### PPDM Modifications
Some minor modifications must be done to the PPDM model in order for any proper DSM work to function right.These are:

* Changing log curve values to NUMERIC(18,2)
* Allowing cascading delete on some table

#### DSM Model
The table below shows all the DSM tables that are being created.

| Table Name         | Table Description                         |
| :----------------- | :---------------------------------------- |
| pdo_qc_index       | Index containing all the data necessary to perform qc and predictions |
| pdo_qc_rules       | Contains all the qc and prediction rules  |
| pdo_rule_functions | Contains all the micro-service or app functions and internal metods used by the rules |

\
This option also created several stored procedures required by DSM. It also copies data definition files to the File Shares 
in folder connectdefinition.These files can be customized for your data connectivity needs. Finally it copies a standard rule set
into your data store for use by data qc and predictions.

An External Data Source for SQL Server is being created. The data source is called PDOAzureBlob. In order to create a data source
we need a Master Data Key. If one does not exist then the system will create one. You will need CONTROL permission on the database
for this. Application settings has to be defined for BlobStorage, BlobSecret and BlobCredential.

