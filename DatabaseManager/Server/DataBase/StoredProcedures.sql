﻿DROP PROCEDURE IF EXISTS spCreateIndex;
GO
CREATE PROC spCreateIndex   
AS   
BEGIN  
   BEGIN TRANSACTION  
      INSERT pdo_qc_index (IndexNode, DataName, DataType)  
	  OUTPUT inserted.IndexID 
      VALUES(hierarchyid::GetRoot(), 'QCProject', 'QCProject')  
   COMMIT  
END;
GO

DROP PROCEDURE IF EXISTS spAddIndex;
GO
CREATE PROC spAddIndex(@parentid int, @d_name varchar(40), @type varchar(40), @datakey varchar(400), @jsondataobject varchar(max))   
AS   
BEGIN  
   DECLARE @mIndexNode hierarchyid, @lc hierarchyid  
   SELECT @mIndexNode = IndexNode   
   FROM pdo_qc_index   
   WHERE INDEXID = @parentid  
   SET TRANSACTION ISOLATION LEVEL SERIALIZABLE  
   BEGIN TRANSACTION  
      SELECT @lc = max(IndexNode)   
      FROM pdo_qc_index   
      WHERE IndexNode.GetAncestor(1) =@mIndexNode ;  

      INSERT pdo_qc_index (IndexNode, DataName, DataType, DataKey, JsonDataObject)  
	  OUTPUT inserted.IndexID 
      VALUES(@mIndexNode.GetDescendant(@lc, NULL), @d_name, @type, @datakey, @jsondataobject)  
   COMMIT  
END;
GO

DROP PROCEDURE IF EXISTS spAddIndexWithLocation;
GO
CREATE PROC spAddIndexWithLocation(@parentid int, @d_name varchar(40), 
                       @type varchar(40), @datakey varchar(400), 
					   @jsondataobject varchar(max), @latitude numeric(14,9), @longitude numeric(14,9))   
AS   
BEGIN 
   DECLARE @location geography
   SET @location = geography::Point(@latitude, @longitude, 4326)
   DECLARE @mIndexNode hierarchyid, @lc hierarchyid  
   SELECT @mIndexNode = IndexNode   
   FROM pdo_qc_index   
   WHERE INDEXID = @parentid  
   SET TRANSACTION ISOLATION LEVEL SERIALIZABLE  
   BEGIN TRANSACTION  
      SELECT @lc = max(IndexNode)   
      FROM pdo_qc_index   
      WHERE IndexNode.GetAncestor(1) =@mIndexNode ;  

      INSERT pdo_qc_index (IndexNode, DataName, DataType, DataKey, JsonDataObject, Latitude, Longitude, QC_LOCATION)  
	  OUTPUT inserted.IndexID 
      VALUES(@mIndexNode.GetDescendant(@lc, NULL), @d_name, @type, @datakey, @jsondataobject, @latitude, @longitude, @location)  
   COMMIT  
END;
GO

DROP PROCEDURE IF EXISTS spGetDescendants;
GO
CREATE PROCEDURE spGetDescendants(@indexnode varchar(400))
AS
BEGIN
    Select 
	INDEXID, IndexNode.ToString() AS Text_IndexNode, INDEXLEVEL, 
	DATANAME, DATATYPE, DATAKEY, QC_STRING, JSONDATAOBJECT 
	from pdo_qc_index
	WHERE IndexNode.IsDescendantOf(@indexnode) = 1
END
GO

DROP PROCEDURE IF EXISTS spGetNeighborsNoFailures;
GO
CREATE PROC spGetNeighborsNoFailures(@indexId int, @failRule nvarchar(255))

AS
BEGIN
   DECLARE @point geography;
   DECLARE @dataType varchar(40)
   DECLARE @dataName varchar(40)
   DECLARE @level smallint

   select @point = QC_LOCATION, @dataType = DATATYPE, 
   @level = INDEXLEVEL, @dataName = DATANAME
   from pdo_qc_index
   where INDEXID = @indexId

   IF (@level = 2)
   BEGIN
     SET @dataName = '%'
   END

   Select 
	TOP(24) INDEXID, DATANAME, LATITUDE, LONGITUDE, DATAKEY,
	qc_location.STDistance(@point) as DISTANCE, JSONDATAOBJECT 
	from pdo_qc_index
    Where 
        qc_location.STDistance(@point) IS NOT NULL and 
        DATATYPE = @dataType and 
        DATANAME like @dataName and 
        INDEXID != @indexId and
        QC_STRING not like @failRule
	ORDER By DISTANCE
END
GO

DROP PROCEDURE IF EXISTS spGetNeighborsNoFailuresDepth;
GO
CREATE PROC spGetNeighborsNoFailuresDepth(@indexId int, @failRule nvarchar(255), @path nvarchar(40))

AS
BEGIN
   DECLARE @point geography;
   DECLARE @dataType varchar(40)
   DECLARE @dataName varchar(40)
   DECLARE @level smallint

   select @point = QC_LOCATION, @dataType = DATATYPE, 
   @level = INDEXLEVEL, @dataName = DATANAME 
   from pdo_qc_index
   where INDEXID = @indexId

   IF (@level = 2)
   BEGIN
     SET @dataName = '%'
   END

   Select 
	TOP(24) INDEXID, DATANAME, LATITUDE, LONGITUDE, DATAKEY,
	qc_location.STDistance(@point) as DISTANCE, JSONDATAOBJECT, 
	JSON_VALUE(JSONDATAOBJECT, @path) as DEPTH
	from pdo_qc_index
    Where 
        qc_location.STDistance(@point) IS NOT NULL and 
        DATATYPE = @dataType and 
        DATANAME like @dataName and 
        INDEXID != @indexId and
        QC_STRING not like @failRule and
		ISJSON(JSONDATAOBJECT) > 0
	ORDER By DISTANCE
END