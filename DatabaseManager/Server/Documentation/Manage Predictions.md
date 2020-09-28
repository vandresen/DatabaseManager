## Manage Predictions
In Manage Predictions you can define prediction sets used for your DSM projects. A Prediction sets 
can consist of one or more rules. The system comes with a standard prediction set based depth values.
New prediction sets can be created and saved in Azure storage and you can retain these at a later 
time for use in your DSM project. Thye rule dimensions are as follows:
* Completness
* Entirety
* Validity
* Predictions

#### Uniqueness
A Uniqueness rule will check for duplicates based on a set of attributes.
In the Rule_Parameters you will define what attributes you want to use to
determine a match. Use a ; to split the attibutes.
Example: SURFACE_LATITUDE; SURFACE_LONGITUDE; LEASE_NAME

Each attribute can also have a function applied to it. Every function has to start with *.

NORMALIZE
this function will remove the following characters: _-#*.@~

#### Validity
Some validfity rules will reuire one or more parameters set in Rule Parameter. These must 
be treated as JSon.
The following validity methods are available:

##### ValidityRange
Checks that the data attribute is within a minimum and/or maximum value. The
[RangeMin] or [RangeMax] attribute can be set to a numeric value. If not defined then they 
will be set to -99999.0 and 99999.0 