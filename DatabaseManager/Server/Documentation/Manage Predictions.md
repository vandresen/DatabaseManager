## Manage Predictions
In Manage Predictions you can define prediction sets used for your DSM projects. A Prediction sets 
can consist of one or more rules. The system comes with a standard prediction set based on checking 
and prediction depth values. New prediction sets can be created and saved in Azure storage and you 
can retain these at a later time for use in your DSM project. The rule dimensions are as follows:
* Completness
* Consistency
* Entirety
* Uniqueness
* Validity
* Predictions

#### Completness
A Completeness rule checks to see if a data attribute of a data object has a value. If the data 
attribute is null, or has a string value of UNKNOWN, or a number value of -99999.0 then the data 
item fails the rule.


#### Consistency
A Consistency rule checks to see if the current data connector have the same value for a specific 
data attribute of a data item in another data connector specified in the parameter field. If the 
data attribute values are different, then the rule fails for the data item.

#### Entirety
An Entirety rule check to see if a child data item is missing. For a given data item, this 
rule will check to see if the data item has a specific child item based on the rule parameters. 
If the child object is missing, then the rule will fail.

#### Uniqueness
A Uniqueness rule will check for duplicates based on a set of attributes.
In the Rule_Parameters you will define what attributes you want to use to
determine a match. Use a ; to split the attibutes.
Example: SURFACE_LATITUDE; SURFACE_LONGITUDE; LEASE_NAME

Each attribute can also have a function applied to it. Every function has to start with *.

NORMALIZE
This function will make following changes to the attribute:
* Remove the following characters: _-#*.@~
* Remove spaces, tabs and enters
* Replace & with AND
* Make it Upper case

Example: *NORMALIZE(UWI)

#### Validity
Some validfity rules will rqeuire one or more parameters set in Rule Parameter. These must 
be expressed as Json.
The following validity methods are available:
* ValidityRange
* CurveSpikes
* IsNumber

##### ValidityRange
Checks that the data attribute is within a minimum and/or maximum value. The
[RangeMin] or [RangeMax] attribute can be set to a numeric value. If not defined then they 
will be set to -99999.0 and 99999.0 

#### Predictions
The following prediction methods comes with the system:
* DeleteDataObject
* PredictDepthUsingIDW
* PredictDominantLithology

##### DeleteDataObject
This method is used to delete data objects. 

##### PredictDepthUsingIDW
This method is using inverse distance weighted interpolation calculate depth values based on 
neighbour points.

##### PredictDominantLithology
This method will predict the Dominant Lithology attribute for a marker pick. It will use a GR 
log curve to predict whhat the lithology or rock will be. Using the pick depth it will use a 
smoothed curve value to determine the rock based on the values in table below:

|Rock      |  Min  | Max    |
|:-------- | :---- | :----- |
|Salt      |   0   |   10   |
|Limestone |  10   |   20   |
|Sandstone |  20   |   55   |
|Shale     |  55   |  150   |

Any other will be set to Unknown. 

If you want to update the PPDM database then you need to insert the above Lithologies into 
table R_LITHOLOGY.

