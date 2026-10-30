-- OpenRIN_Services: esquema de Seguridad (REQ-ARQ-006)
IF OBJECT_ID('dbo.Usuarios', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuarios
    (
        Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuarios PRIMARY KEY,
        NombreUsuario     VARCHAR(50)       NOT NULL CONSTRAINT UQ_Usuarios_NombreUsuario UNIQUE,
        NombreCompleto    NVARCHAR(150)     NOT NULL,
        HashPassword      NVARCHAR(400)     NOT NULL, -- formato "sal.iteraciones.hash" (PBKDF2-SHA256), nunca texto plano
        Perfil            VARCHAR(30)       NOT NULL,
        Email             NVARCHAR(200)     NULL,
        PreguntaSeguridad NVARCHAR(300)     NULL,     -- recuperación de contraseña
        RespuestaHash     NVARCHAR(400)     NULL,
        Idioma            VARCHAR(10)       NULL,     -- idioma preferido del usuario (REQ-ARQ-001)
        Activo            BIT               NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT (1),
        IntentosFallidos  INT               NOT NULL CONSTRAINT DF_Usuarios_Intentos DEFAULT (0),
        BloqueadoHasta    DATETIME2(0)      NULL,     -- anti fuerza bruta: 5 fallos -> 15 min
        FechaAlta         DATETIME2(0)      NOT NULL CONSTRAINT DF_Usuarios_FechaAlta DEFAULT SYSDATETIME()
    );
END;
GO

-- Migración idempotente: idioma preferido (bases creadas antes de REQ-ARQ-001)
IF COL_LENGTH('dbo.Usuarios', 'Idioma') IS NULL
    ALTER TABLE dbo.Usuarios ADD Idioma VARCHAR(10) NULL;
GO

IF OBJECT_ID('dbo.PerfilesPermisos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PerfilesPermisos
    (
        Perfil  VARCHAR(30)  NOT NULL,
        Permiso VARCHAR(100) NOT NULL,
        CONSTRAINT PK_PerfilesPermisos PRIMARY KEY (Perfil, Permiso)
    );
END;
GO
