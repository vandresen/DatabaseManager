## Manage Predictions
In Manage Predictions you can define prediction sets used for your DSM projects. A Prediction sets 
can consist of one or more rules. The system comes with a standard prediction set based on checking 
and prediction depth values. New prediction sets can be created and saved in Azure storage and you 
can retain these at a later time for use in your DSM project. The rule dimensions are as follows:
* Completeness
* Consistency
* Entirety
* Uniqueness
* Validity
* Predictions

#### Completeness
A Completeness rule checks to see if a data attribute of a data object has a value. If the data 
attribute is null, or has a string value of UNKNOWN, or a number value of -99999.0 then the data 
item fails the rule.

| Field |Description  |
|--|--|
|Rule Name  |Enter a name that makes sense|
|Active	|Y or N if this rule will be processed. N means not processed|
|Data Type| Select the data type from the dropdown box|
|Data Attribute| Select attribute to be quality checked from the dropdown list|
|Rule Parameters | Not used for completeness|
|Rule Filter |Enter a filter to limit which data items is processed for this rule (attribute = value)|
|Description |Enter a description to clarify what the rule is checking for|

#### Consistency
A Consistency rule checks to see if the current data connector have the same value for a specific 
data attribute of a data item in another data connector specified in the parameter field. If the 
data attribute values are different, then the rule fails for the data item.

| Field |Description  |
|--|--|
|Rule Name  |Enter a name that makes sense|
|Active	|Y or N if this rule will be processed. N means not processed|
|Data Type| Select the data type from the dropdown box|
|Data Attribute| Select attribute to be quality checked from the dropdown list|
|Rule Parameters | Enter the data source name to be used for the consistency check|
|Rule Filter |Enter a filter to limit which data items is processed for this rule (attribute = value)|
|Description |Enter a description to clarify what the rule is checking for|

#### Entirety
An Entirety rule check to see if a child data item is missing. For a given data item, this 
rule will check to see if the data item has a specific child item based on the rule parameters. 
If the child object is missing, then the rule will fail.

| Field |Description  |
|--|--|
|Rule Name  |Enter a name that makes sense|
|Active	|Y or N if this rule will be processed. N means not processed|
|Data Type| Select the data type from the dropdown box|
|Rule Parameters | Enter the data type name to check for possible children|
|Rule Filter |Enter a filter to limit which data items is processed for this rule (attribute = value)|
|Description |Enter a description to clarify what the rule is checking for|

#### Uniqueness
A Uniqueness rule will check for duplicates based on a set of attributes.

| Field |Description  |
|--|--|
|Rule Name  |Enter a name that makes sense|
|Active	|Y or N if this rule will be processed. N means not processed|
|Data Type| Select the data type from the dropdown box|
|Rule Parameters | See more info below|
|Rule Filter |Enter a filter to limit which data items is processed for this rule (attribute = value)|
|Description |Enter a description to clarify what the rule is checking for|

In the Rule_Parameters you will define what attributes you want to use to
determine a match. Use a ; to split the attributes.
Example: SURFACE_LATITUDE; SURFACE_LONGITUDE; LEASE_NAME

Each attribute can also have a function applied to it. Every function has to start with *.

NORMALIZE
This function will make following changes to the attribute:
* Remove the following characters: _-#*.@~
* Remove spaces, tabs and enters
* Replace & with AND
* Make it Upper case

Example: *NORMALIZE(UWI)

An option is to add your own string of characters to remove.
Example: *NORMALIZE(UWI, "-,.")

NORMALIZE14
Same as above but it will pad 0 to create a string of 14 characters.

#### Validity
Some validity rules will require one or more parameters set in Rule Parameter. These must 
be expressed as Json.

| Field |Description  |
|--|--|
|Rule Name  |Enter a name that makes sense|
|Active	|Y or N if this rule will be processed. N means not processed|
|Data Type| Select the data type from the dropdown box|
|Data Attribute| Select attribute to be quality checked from the dropdown list|
|Rule Parameters | Additional parameters for the selected function |
|Rule Filter |Enter a filter to limit which data items is processed for this rule (attribute = value)|
|Rule Function | Select a function from the dropdown list |
|Description |Enter a description to clarify what the rule is checking for|

The following validity methods are available:
* ValidityRange
* CurveSpikes
* IsNumber
* StringLength

##### ValidityRange
Checks that the data attribute is within a minimum and/or maximum value. The
[RangeMin] or [RangeMax] attribute can be set to a numeric value. If not defined then they 
will be set to -99999.0 and 99999.0 

#### CurveSpikes
Checks for spikes in the log curve.
Arguments:
* WindowSize: This is how many points to include before and after where there is a potential spike. The default value is 2.
* SeveritySize: This is the severity or size of spike. The default value is 5. This value is a multiplier of the standard deviation. A value of 4 will multiply the standard deviation of the log curve value by 4.

#### IsNumber
Checks if the chosen attribute is a number.

#### StringLength
Checks that the chosen data attribute has a string length between a minimum and/or a 
maximum value. 
Arguments:
* Min: This sets the minimum number of characters for the specified attribute. Default is 20.
* Max: This sets the maximum number of characters for the specified attribute. Default is 20.


#### Predictions
Prediction rules use QC rules to make automated predictions for correcting 
the data. If a data item fails a QC rule, a prediction action will be taken.

| Field |Description  |
|--|--|
|Rule Name  |Enter a name that makes sense|
|Active	|Y or N if this rule will be processed. N means not processed|
|Data Type| Select the data type from the dropdown box|
|Data Attribute| Select attribute to be quality checked from the dropdown list|
|Rule Parameters | Additional parameters for the selected function |
|Rule Filter |Enter a filter to limit which data items is processed for this rule (attribute = value)|
|Fail Rule |Enter the rule key for the failed QC rule|
|Prediction Order | The order in which prediction rules are processed. Lower numbers processed first |
|Rule Function | Select a function from the dropdown list |
|Description |Enter a description to clarify what the rule is checking for|

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