-- OpenRIN_Services: tabla de bitácora (REQ-ARQ-003)
IF OBJECT_ID('dbo.Logs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Logs
    (
        Id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Logs PRIMARY KEY,
        Fecha     DATETIME2(0)      NOT NULL CONSTRAINT DF_Logs_Fecha DEFAULT SYSDATETIME(),
        Nivel     VARCHAR(10)       NOT NULL,
        Mensaje   NVARCHAR(2000)    NOT NULL,
        Excepcion NVARCHAR(MAX)     NULL,
        Usuario   NVARCHAR(150)     NULL,
        Capa      NVARCHAR(150)     NULL
    );
END;
GO
