# Release 1.30
* Added support for DQM projects in a sqlite database
* Added functionality to delete when editing Taxonomy
* Added a new tab for basemaps using Arcgis. This only work for the serverless client for the time being

# Release 1.29
* Under data transfer there is a new option to transfer the index into a database. It will insert new objects and update existing objects
* Implemented support for using Sqlite when indexing with the serverless user interface

# Release 1.28
* Added support for the Index sqlite microservice in Serverless GUI. A Sqlite flag in application settings will determine if sqlite is used or not

# Release 1.27
* The Blazor MVC version has been changed do not support Dataops, but instead we have implemented an Execute menu thet will do Data Qc and Prediction
* Implemented an example of external prediction rule. This will use Bing to predict the ground elevation. In order to use this you need a Bing key
