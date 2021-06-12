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

### Other well data
Other well data in csv format must be put under a folder called wells. The names of the file must
be the data type name in plural with extension txt.
