-- OpenRIN_Services: idiomas y textos dinámicos (T05: internacionalización sin hojas de
-- recursos estáticos). Los textos VIVEN en la base; el sistema puede incorporar nuevos
-- idiomas y leyendas en caliente desde la administración.
IF OBJECT_ID('dbo.Idiomas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Idiomas
    (
        Codigo VARCHAR(10)   NOT NULL CONSTRAINT PK_Idiomas PRIMARY KEY,
        Nombre NVARCHAR(60)  NOT NULL,
        Activo BIT           NOT NULL CONSTRAINT DF_Idiomas_Activo DEFAULT 1
    );
END;
GO

IF OBJECT_ID('dbo.Textos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Textos
    (
        Id           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Textos PRIMARY KEY,
        CodigoIdioma VARCHAR(10)   NOT NULL CONSTRAINT FK_Textos_Idiomas REFERENCES dbo.Idiomas(Codigo),
        Clave        VARCHAR(200)  NOT NULL,
        Valor        NVARCHAR(1000) NOT NULL,
        CONSTRAINT UQ_Textos_Idioma_Clave UNIQUE (CodigoIdioma, Clave)
    );
END;
GO

-- Idiomas iniciales (la semilla de textos se genera desde los .resx del proyecto).
-- MERGE idempotente: corrige el nombre si el idioma ya existe.
MERGE dbo.Idiomas AS destino
USING (SELECT 'es' AS Codigo, N'Español (Latinoamérica)' AS Nombre) AS origen
ON destino.Codigo = origen.Codigo
WHEN MATCHED THEN UPDATE SET Nombre = origen.Nombre
WHEN NOT MATCHED THEN INSERT (Codigo, Nombre, Activo) VALUES (origen.Codigo, origen.Nombre, 1);
GO
MERGE dbo.Idiomas AS destino
USING (SELECT 'en' AS Codigo, N'English (US)' AS Nombre) AS origen
ON destino.Codigo = origen.Codigo
WHEN MATCHED THEN UPDATE SET Nombre = origen.Nombre
WHEN NOT MATCHED THEN INSERT (Codigo, Nombre, Activo) VALUES (origen.Codigo, origen.Nombre, 1);
GO
MERGE dbo.Idiomas AS destino
USING (SELECT 'zh-CN' AS Codigo, N'中文（简体）' AS Nombre) AS origen
ON destino.Codigo = origen.Codigo
WHEN MATCHED THEN UPDATE SET Nombre = origen.Nombre
WHEN NOT MATCHED THEN INSERT (Codigo, Nombre, Activo) VALUES (origen.Codigo, origen.Nombre, 1);
GO
