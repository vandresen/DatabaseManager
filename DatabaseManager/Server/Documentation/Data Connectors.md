## Data Connectors

This is where you can manage your data connectors.
You can create a new one, edit or delete connectors. All the connector definitions are stored in
your Azure storage account. If your implementation configuration has not been set up with a storage 
account then you can go to setting and define one. Under table storage the system will automatically
create a table called source that will contain all connectors.

#### New Connector
PPDM are the only connector type supported for the type being and we only support Microsoft SQL 
Server. Hit the New Connector button to create 
a new connector. You will need to enter the following information:

* Name: You can call you connector whatever you want, but it has to be unique.
* Database Name: Get the name of the SQL Server database from your database administrator
* Database Server: Get the database server name from your database administrator
* User Name: Enter you SQL Server user name. You can omitt this if using Windows login
* Password: Enter password unless you use Windows login as authentication

#### Edit
You can edit all the connector parameters except the name. If you don't like the name then you must
delete it first and create a new one.

#### Delete
This button will delete the connector.

