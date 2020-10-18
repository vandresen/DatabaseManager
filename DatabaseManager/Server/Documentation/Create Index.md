## Create Index
Select a taxonomy file that you want to use for calculating the index. The taxonomy 
files will be located in your Azure storage account under a Fileshare folder called 
taxonomy. When you create the data model the a default taxonomy called WellBore.json 
will be inserted here. Be aware that every time you create the datamodel then this file
will be inserted. It is not advised to customize this file. Instead copy it and make the
necessary changes.

This file must be in json format. You may have multiple top level data types. The example
file only has one, WellBore. Under each data type you make have children. The levels of 
childrens are unlimited. The main keywords are:

* DataName
* NameAttribute
* LatitudeAttribute
* LongitudeAttribute
* Keys
* UseParentLocation
* DataObjects
