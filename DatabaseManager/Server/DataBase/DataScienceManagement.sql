DROP TABLE IF EXISTS pdo_qc_index;
CREATE TABLE pdo_qc_index  
(  
   IndexNode hierarchyid PRIMARY KEY CLUSTERED,  
   IndexLevel AS IndexNode.GetLevel(),  
   IndexID int IDENTITY(1,1) UNIQUE, 
   DataName NVARCHAR(40) NOT NULL,  
   DataType NVARCHAR(40) NULL,
   DataKey NVARCHAR(400) NULL,
   QC_LOCATION sys.geography,
   Latitude NUMERIC(14,9),
   Longitude NUMERIC(14,9),
   UniqKey NVARCHAR(100),
   JsonDataObject NVARCHAR(max),
   QC_STRING NVARCHAR(400)  
);
DROP TABLE IF EXISTS pdo_qc_rules;
CREATE TABLE pdo_qc_rules
(
	Id INT IDENTITY(1,1) PRIMARY KEY, 
	Active NVARCHAR(1) NULL,
	DataType NVARCHAR(40) NOT NULL,
	DataAttribute NVARCHAR(255) NULL,
	RuleType NVARCHAR(40) NOT NULL,
	RuleName NVARCHAR(40) NOT NULL,
	RuleDescription NVARCHAR(255) NULL,
	RuleFunction NVARCHAR(255) NULL,
	RuleKey NVARCHAR(16) NULL,
	RuleParameters NVARCHAR(255) NULL,
	RuleFilter NVARCHAR(255) NULL,
	FailRule NVARCHAR(255) NULL,
	PredictionOrder int NULL,
	CreatedBy NVARCHAR(255) NULL,
	CreatedDate datetime NULL,
	ModifiedBy NVARCHAR(255) NULL,
	ModifiedDate datetime NULL,
	KeyNumber int NOT NULL
);
DROP TABLE IF EXISTS pdo_rule_functions;
CREATE TABLE pdo_rule_functions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FunctionName NVARCHAR(255) NOT NULL,
    FunctionUrl NVARCHAR(255) NOT NULL,
	FunctionType NVARCHAR(1),
    FunctionKey NVARCHAR(255)
);
DROP TABLE IF EXISTS pdo_rule_models;
CREATE TABLE pdo_rule_models (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RuleId INT NOT NULL,
    JsonData NVARCHAR(max)
);
DROP TABLE IF EXISTS pdo_indexes;
CREATE TABLE pdo_indexes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    IndexName NVARCHAR(255) NOT NULL UNIQUE,
    IndexBuilder NVARCHAR(max),
    ModifiedBy NVARCHAR(255) NULL,
    ModifiedDate datetime NULL
);
DROP TABLE IF EXISTS pdo_version_table;
CREATE TABLE pdo_version_table (
    VersionNumber INT NULL,
    ModifiedDate datetime NULL
);
CREATE UNIQUE INDEX QCINDEX ON pdo_qc_index(IndexLevel, IndexNode);