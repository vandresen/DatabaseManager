DROP FUNCTION IF EXISTS dbo.fnGetNumberOfChildren
GO
CREATE FUNCTION fnGetNumberOfChildren
	(
		@IndexNode hierarchyid
	)
RETURNS INT
AS
BEGIN
	DECLARE @ObjectCount int;
    SELECT @ObjectCount = COUNT(1)
    FROM pdo_qc_index
	WHERE IndexNode.IsDescendantOf(@IndexNode) = 1; 
	RETURN @ObjectCount-1;
END