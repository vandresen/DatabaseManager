## Data Transfer
Transfer data from files or databases into a PPDM database. The files can be in the form of CSV or
LAS.

You first select the source connector. If you are running in the cloud you have the option to start
a remote data transfer.

### Remote Data Transfer
Remote data transfer will use a azure data queue called datatransferqueue to communicate with a 
utility that has been installed on a server in your local environment. When transfer info is put 
on the queue then this service will start transfer. Make sure that you use a data connector 
that has a user and password defined.

### Logs
The system supports LAS files version 2. The files must be located in your Azure storage account under
Fileshares with name logs.

Well header attributes can be modified in the LASDataAccess.json file.

The system will not out the box load parameter info. In order to add this feature you must add the 
following to the PPDMAccessDef.json file:

"DataType": "LogParameter",
    "Keys": "UWI, WELL_LOG_ID, WELL_LOG_SOURCE, PARAMETER_SEQ_NO",
    "Select": "Select UWI, WELL_LOG_ID, WELL_LOG_SOURCE, PARAMETER_SEQ_NO, PARAMETER_TEXT_VALUE, 
                REPORTED_DESC, REPORTED_MNEMONIC, ROW_CHANGED_DATE, ROW_CHANGED_BY, ROW_CREATED_BY, 
                _CREATED_DATE from well_log_parm"

You must also delete the foreign key for the well_log_parm table.

ALTER TABLE well_log_parm DROP CONSTRAINT WLP_WL_FK;

### Other well data
Other well data in csv format must be put under a folder called wells. The names of the file must
be the data type name in plural with extension txt.
