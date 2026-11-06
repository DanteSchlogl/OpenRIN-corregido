-- OpenRIN_Services: registro de respaldos (origen, estado sano o no, tamaño y usuario)
IF OBJECT_ID('dbo.Respaldos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Respaldos
    (
        Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Respaldos PRIMARY KEY,
        Fecha         DATETIME2(3)      NOT NULL CONSTRAINT DF_Respaldos_Fecha DEFAULT SYSDATETIME(),
        BaseDatos     NVARCHAR(128)     NOT NULL,
        Archivo       NVARCHAR(500)     NOT NULL,
        TamanoBytes   BIGINT            NOT NULL CONSTRAINT DF_Respaldos_Tam DEFAULT 0,
        Origen        VARCHAR(12)       NOT NULL,   -- Manual | Automatico | Previo | Importado
        Sano          BIT               NOT NULL,   -- pasó RESTORE VERIFYONLY con CHECKSUM
        Usuario       NVARCHAR(150)     NULL,
        Observaciones NVARCHAR(500)     NULL,
        CONSTRAINT CK_Respaldos_Origen CHECK (Origen IN ('Manual','Automatico','Previo','Importado'))
    );
    CREATE INDEX IX_Respaldos_Fecha ON dbo.Respaldos (Fecha DESC);
END;
GO
