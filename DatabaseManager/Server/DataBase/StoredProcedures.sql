DROP PROCEDURE IF EXISTS spCreateIndex;
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

DROP PROCEDURE IF EXISTS spClearQCFlags;
GO
CREATE PROC spClearQCFlags   
AS   
BEGIN  
   BEGIN TRANSACTION  
      UPDATE pdo_qc_index
	  SET QC_STRING = ''
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

DROP PROCEDURE IF EXISTS spGetNumberOfDescendants;
GO
CREATE PROCEDURE spGetNumberOfDescendants(@indexnode varchar(400), @level int)
AS
BEGIN

DROP TABLE If EXISTS #MyTemp

SELECT INDEXID, DATATYPE, JSONDATAOBJECT, INDEXNODE
INTO #MyTemp
FROM pdo_qc_index 
WHERE IndexNode.IsDescendantOf(@indexnode) = 1 and INDEXLEVEL = @level

SELECT A.INDEXID, A.DATATYPE, A.JSONDATAOBJECT, ((select count(1) from pdo_qc_index B where B.IndexNode.IsDescendantOf(A.IndexNode) = 1)-1) AS NumberOfDataObjects 
FROM #MyTemp A

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
GO

DROP PROCEDURE IF EXISTS spFastDelete;
GO
CREATE PROCEDURE spFastDelete
@TableName NVARCHAR(128) 
AS 
BEGIN 
  SET NOCOUNT ON;
  DECLARE @Sql NVARCHAR(MAX);

  SET @sql = N' WHILE (1=1) '
           + N' BEGIN '
           + N' DELETE TOP(2000) FROM ' + QUOTENAME(@TableName)
           + N' IF @@ROWCOUNT < 1 BREAK '
           + N' END'

  EXECUTE sp_executesql @Sql

END
GO

DROP PROCEDURE IF EXISTS spGetMinMaxAllFormationPick;
GO
CREATE PROCEDURE spGetMinMaxAllFormationPick
AS
BEGIN
	drop table if exists #TempTable
	drop table if exists #TempTable2
	drop table if exists #TempTable3
	drop table if exists #MinMaxAllFormationPick
	select JSON_VALUE(JSONDATAOBJECT,'$.STRAT_UNIT_ID') AS STRAT_UNIT_ID, JSON_VALUE(JSONDATAOBJECT,'$.PICK_DEPTH') AS DEPTH INTO #TempTable from pdo_qc_index 
		where DATATYPE = 'MarkerWell' and JSONDATAOBJECT != ''
	select STRAT_UNIT_ID, TRY_CONVERT(float, DEPTH) As DEPTH into #TempTable2 from #TempTable

	delete from #TempTable2 where DEPTH is null

	select 
	STRAT_UNIT_ID, 
	min(DEPTH) as MIN, 
	max(DEPTH) as MAX  
	into #TempTable3 
	from #TempTable2 
	group by STRAT_UNIT_ID

	SELECT 
	ROW_NUMBER() OVER ( ORDER BY MIN ) AGE,
	STRAT_UNIT_ID, 
	MIN, 
	MAX
	INTO #MinMaxAllFormationPick
	FROM 
	#TempTable3

	select * from #MinMaxAllFormationPick
END
GO

DROP PROCEDURE IF EXISTS spInsertIndex;
DROP TYPE IF EXISTS UDIndexTable;
GO
CREATE TYPE UDIndexTable AS TABLE
(
      DataName NVARCHAR(40) NOT NULL,
      IndexNode NVARCHAR(255) NOT NULL,
      QcLocation NVARCHAR(255),
      DataType NVARCHAR(40) NULL,
	  DataKey NVARCHAR(400) NULL,
	  Latitude NUMERIC(14,9),
      Longitude NUMERIC(14,9),
	  JsonDataObject NVARCHAR(max)
)
GO

CREATE PROC spInsertIndex
(@TempTable AS UDIndexTable READONLY)
AS
BEGIN
      INSERT INTO pdo_qc_index(IndexNode, DataName, DataType, DataKey, JsonDataObject, Latitude, Longitude, QC_LOCATION)
      SELECT INDEXNODE, DATANAME, DATATYPE, DataKey, JsonDataObject, Latitude, Longitude, geography::STGeomFromText(QCLOCATION, 4326) FROM @TempTable
END