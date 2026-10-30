IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE TABLE [Diagnosticos] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(200) NOT NULL,
        [Descripcion] nvarchar(1000) NULL,
        [Activo] bit NOT NULL,
        CONSTRAINT [PK_Diagnosticos] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE TABLE [ObrasSociales] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(150) NOT NULL,
        [Activo] bit NOT NULL,
        CONSTRAINT [PK_ObrasSociales] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE TABLE [ReportesEstadisticos] (
        [Id] int NOT NULL IDENTITY,
        [Anio] int NOT NULL,
        [Mes] int NOT NULL,
        [FechaGeneracion] datetime2 NOT NULL,
        [IndicadorGlobalEficacia] decimal(5,2) NOT NULL,
        [CantidadOptimos] int NOT NULL,
        [CantidadSuboptimos] int NOT NULL,
        [CantidadDeficientes] int NOT NULL,
        [DetalleJson] nvarchar(max) NULL,
        CONSTRAINT [PK_ReportesEstadisticos] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE TABLE [Usuarios] (
        [Id] int NOT NULL IDENTITY,
        [NombreUsuario] nvarchar(50) NOT NULL,
        [NombreCompleto] nvarchar(300) NOT NULL,
        [Perfil] nvarchar(30) NOT NULL,
        [Email] nvarchar(400) NULL,
        [Telefono] nvarchar(50) NULL,
        [Activo] bit NOT NULL,
        [FechaAlta] datetime2 NOT NULL,
        CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE TABLE [Pacientes] (
        [Id] int NOT NULL IDENTITY,
        [NombreCompleto] nvarchar(300) NOT NULL,
        [DNI] nvarchar(20) NOT NULL,
        [Telefono] nvarchar(50) NOT NULL,
        [Email] nvarchar(400) NOT NULL,
        [IdObraSocial] int NULL,
        [NumeroAfiliado] nvarchar(50) NULL,
        [IdUsuarioPortal] int NULL,
        [Estado] nvarchar(20) NOT NULL,
        [MotivoBaja] nvarchar(500) NULL,
        [FechaAlta] datetime2 NOT NULL,
        [FechaBaja] datetime2 NULL,
        CONSTRAINT [PK_Pacientes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Pacientes_ObrasSociales_IdObraSocial] FOREIGN KEY ([IdObraSocial]) REFERENCES [ObrasSociales] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE TABLE [EventosAdversos] (
        [Id] int NOT NULL IDENTITY,
        [IdPaciente] int NOT NULL,
        [Fecha] datetime2 NOT NULL,
        [Tipo] nvarchar(30) NOT NULL,
        [Descripcion] nvarchar(max) NOT NULL,
        [Gravedad] nvarchar(30) NOT NULL,
        [AccionTomada] nvarchar(max) NOT NULL,
        [FechaRegistro] datetime2 NOT NULL,
        [IdUsuarioRegistro] int NULL,
        CONSTRAINT [PK_EventosAdversos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EventosAdversos_Pacientes_IdPaciente] FOREIGN KEY ([IdPaciente]) REFERENCES [Pacientes] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE TABLE [HistoriasClinicas] (
        [Id] int NOT NULL IDENTITY,
        [IdPaciente] int NOT NULL,
        [IdDiagnostico] int NULL,
        [LimiteInferiorRIN] decimal(5,2) NOT NULL,
        [LimiteSuperiorRIN] decimal(5,2) NOT NULL,
        [Medicamento] nvarchar(200) NOT NULL,
        [Dosis] nvarchar(200) NOT NULL,
        [PeriodicidadDias] int NOT NULL,
        [ProximaFechaControl] datetime2 NULL,
        [FechaConfiguracion] datetime2 NOT NULL,
        [UltimaActualizacion] datetime2 NOT NULL,
        CONSTRAINT [PK_HistoriasClinicas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HistoriasClinicas_Diagnosticos_IdDiagnostico] FOREIGN KEY ([IdDiagnostico]) REFERENCES [Diagnosticos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_HistoriasClinicas_Pacientes_IdPaciente] FOREIGN KEY ([IdPaciente]) REFERENCES [Pacientes] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE TABLE [MedicionesRIN] (
        [Id] int NOT NULL IDENTITY,
        [IdPaciente] int NOT NULL,
        [ValorRIN] decimal(5,2) NOT NULL,
        [FechaMedicion] datetime2 NOT NULL,
        [Canal] nvarchar(30) NOT NULL,
        [IdUsuarioRegistro] int NULL,
        [FechaRegistro] datetime2 NOT NULL,
        [NivelCriticidad] nvarchar(30) NULL,
        [PorTendenciaPeligrosa] bit NOT NULL,
        CONSTRAINT [PK_MedicionesRIN] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MedicionesRIN_Pacientes_IdPaciente] FOREIGN KEY ([IdPaciente]) REFERENCES [Pacientes] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE TABLE [Seguimientos] (
        [Id] int NOT NULL IDENTITY,
        [IdPaciente] int NOT NULL,
        [FechaRegistro] datetime2 NOT NULL,
        [TipoContacto] nvarchar(30) NOT NULL,
        [Resultado] nvarchar(30) NOT NULL,
        [Observaciones] nvarchar(1000) NULL,
        [IdUsuarioRegistro] int NULL,
        [DecisionClinica] nvarchar(30) NOT NULL,
        [DetalleDecision] nvarchar(1000) NULL,
        [IdUsuarioDecision] int NULL,
        [FechaDecision] datetime2 NULL,
        CONSTRAINT [PK_Seguimientos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Seguimientos_Pacientes_IdPaciente] FOREIGN KEY ([IdPaciente]) REFERENCES [Pacientes] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE TABLE [Turnos] (
        [Id] int NOT NULL IDENTITY,
        [IdPaciente] int NOT NULL,
        [IdUsuarioMedico] int NOT NULL,
        [FechaHora] datetime2 NOT NULL,
        [Estado] nvarchar(30) NOT NULL,
        [Observaciones] nvarchar(1000) NULL,
        [FechaCreacion] datetime2 NOT NULL,
        CONSTRAINT [PK_Turnos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Turnos_Pacientes_IdPaciente] FOREIGN KEY ([IdPaciente]) REFERENCES [Pacientes] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE TABLE [Alertas] (
        [Id] int NOT NULL IDENTITY,
        [IdPaciente] int NOT NULL,
        [IdMedicion] int NULL,
        [Tipo] nvarchar(30) NOT NULL,
        [NivelCriticidad] nvarchar(30) NULL,
        [Descripcion] nvarchar(max) NOT NULL,
        [FechaGeneracion] datetime2 NOT NULL,
        [Estado] nvarchar(30) NOT NULL,
        [NotificadaMedico] bit NOT NULL,
        [NotificadaAdministrativo] bit NOT NULL,
        [EscaladaAMedico] bit NOT NULL,
        [FechaEscalada] datetime2 NULL,
        [FechaResolucion] datetime2 NULL,
        [IdUsuarioResolucion] int NULL,
        [AccionResolucion] nvarchar(1000) NULL,
        CONSTRAINT [PK_Alertas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Alertas_MedicionesRIN_IdMedicion] FOREIGN KEY ([IdMedicion]) REFERENCES [MedicionesRIN] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Alertas_Pacientes_IdPaciente] FOREIGN KEY ([IdPaciente]) REFERENCES [Pacientes] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Descripcion', N'Nombre') AND [object_id] = OBJECT_ID(N'[Diagnosticos]'))
        SET IDENTITY_INSERT [Diagnosticos] ON;
    EXEC(N'INSERT INTO [Diagnosticos] ([Id], [Activo], [Descripcion], [Nombre])
    VALUES (1, CAST(1 AS bit), N''Arritmia supraventricular; anticoagulación para prevención de ACV.'', N''Fibrilación auricular''),
    (2, CAST(1 AS bit), N''Anticoagulación para tratamiento y prevención de recurrencias.'', N''Trombosis venosa profunda''),
    (3, CAST(1 AS bit), N''Anticoagulación tras evento embólico pulmonar.'', N''Tromboembolismo pulmonar''),
    (4, CAST(1 AS bit), N''Anticoagulación permanente por válvula mecánica.'', N''Prótesis valvular mecánica''),
    (5, CAST(1 AS bit), N''Trombofilia autoinmune con indicación de anticoagulación.'', N''Síndrome antifosfolípido'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Descripcion', N'Nombre') AND [object_id] = OBJECT_ID(N'[Diagnosticos]'))
        SET IDENTITY_INSERT [Diagnosticos] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Nombre') AND [object_id] = OBJECT_ID(N'[ObrasSociales]'))
        SET IDENTITY_INSERT [ObrasSociales] ON;
    EXEC(N'INSERT INTO [ObrasSociales] ([Id], [Activo], [Nombre])
    VALUES (1, CAST(1 AS bit), N''OSDE''),
    (2, CAST(1 AS bit), N''Swiss Medical''),
    (3, CAST(1 AS bit), N''PAMI''),
    (4, CAST(1 AS bit), N''IOMA''),
    (5, CAST(1 AS bit), N''Sin obra social (particular)'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Nombre') AND [object_id] = OBJECT_ID(N'[ObrasSociales]'))
        SET IDENTITY_INSERT [ObrasSociales] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Email', N'FechaAlta', N'NombreCompleto', N'NombreUsuario', N'Perfil', N'Telefono') AND [object_id] = OBJECT_ID(N'[Usuarios]'))
        SET IDENTITY_INSERT [Usuarios] ON;
    EXEC(N'INSERT INTO [Usuarios] ([Id], [Activo], [Email], [FechaAlta], [NombreCompleto], [NombreUsuario], [Perfil], [Telefono])
    VALUES (1, CAST(1 AS bit), NULL, ''2026-09-15T00:00:00.0000000'', N''Administrador del sistema'', N''sysadmin'', N''sysadmin'', NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Email', N'FechaAlta', N'NombreCompleto', N'NombreUsuario', N'Perfil', N'Telefono') AND [object_id] = OBJECT_ID(N'[Usuarios]'))
        SET IDENTITY_INSERT [Usuarios] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE INDEX [IX_Alertas_IdMedicion] ON [Alertas] ([IdMedicion]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE INDEX [IX_Alertas_IdPaciente] ON [Alertas] ([IdPaciente]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Diagnosticos_Nombre] ON [Diagnosticos] ([Nombre]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE INDEX [IX_EventosAdversos_IdPaciente] ON [EventosAdversos] ([IdPaciente]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE INDEX [IX_HistoriasClinicas_IdDiagnostico] ON [HistoriasClinicas] ([IdDiagnostico]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_HistoriasClinicas_IdPaciente] ON [HistoriasClinicas] ([IdPaciente]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE INDEX [IX_MedicionesRIN_IdPaciente] ON [MedicionesRIN] ([IdPaciente]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ObrasSociales_Nombre] ON [ObrasSociales] ([Nombre]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Pacientes_DNI] ON [Pacientes] ([DNI]) WHERE [Estado] = N''Activo''');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pacientes_IdObraSocial] ON [Pacientes] ([IdObraSocial]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE INDEX [IX_Seguimientos_IdPaciente] ON [Seguimientos] ([IdPaciente]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE INDEX [IX_Turnos_IdPaciente] ON [Turnos] ([IdPaciente]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Usuarios_NombreUsuario] ON [Usuarios] ([NombreUsuario]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915171448_Inicial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260915171448_Inicial', N'8.0.31');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    ALTER TABLE [Usuarios] ADD [DVH] nvarchar(128) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    ALTER TABLE [Turnos] ADD [DVH] nvarchar(128) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    ALTER TABLE [Seguimientos] ADD [DVH] nvarchar(128) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    ALTER TABLE [ReportesEstadisticos] ADD [DVH] nvarchar(128) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    ALTER TABLE [Pacientes] ADD [DVH] nvarchar(128) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    ALTER TABLE [ObrasSociales] ADD [DVH] nvarchar(128) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    ALTER TABLE [MedicionesRIN] ADD [DVH] nvarchar(128) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    ALTER TABLE [HistoriasClinicas] ADD [DVH] nvarchar(128) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    ALTER TABLE [EventosAdversos] ADD [DVH] nvarchar(128) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    ALTER TABLE [Diagnosticos] ADD [DVH] nvarchar(128) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    ALTER TABLE [Alertas] ADD [DVH] nvarchar(128) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    CREATE TABLE [DigitosVerificadores] (
        [Id] int NOT NULL IDENTITY,
        [NombreTabla] nvarchar(100) NOT NULL,
        [DVV] nvarchar(128) NOT NULL,
        [FechaCalculo] datetime2 NOT NULL,
        CONSTRAINT [PK_DigitosVerificadores] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    EXEC(N'UPDATE [Diagnosticos] SET [DVH] = NULL
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    EXEC(N'UPDATE [Diagnosticos] SET [DVH] = NULL
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    EXEC(N'UPDATE [Diagnosticos] SET [DVH] = NULL
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    EXEC(N'UPDATE [Diagnosticos] SET [DVH] = NULL
    WHERE [Id] = 4;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    EXEC(N'UPDATE [Diagnosticos] SET [DVH] = NULL
    WHERE [Id] = 5;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    EXEC(N'UPDATE [ObrasSociales] SET [DVH] = NULL
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    EXEC(N'UPDATE [ObrasSociales] SET [DVH] = NULL
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    EXEC(N'UPDATE [ObrasSociales] SET [DVH] = NULL
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    EXEC(N'UPDATE [ObrasSociales] SET [DVH] = NULL
    WHERE [Id] = 4;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    EXEC(N'UPDATE [ObrasSociales] SET [DVH] = NULL
    WHERE [Id] = 5;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    EXEC(N'UPDATE [Usuarios] SET [DVH] = NULL
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DigitosVerificadores_NombreTabla] ON [DigitosVerificadores] ([NombreTabla]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915200148_IntegridadDVH'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260915200148_IntegridadDVH', N'8.0.31');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919104547_ControlDeCambios'
)
BEGIN
    CREATE TABLE [AuditoriaCambios] (
        [Id] int NOT NULL IDENTITY,
        [Entidad] nvarchar(100) NOT NULL,
        [IdRegistro] int NOT NULL,
        [FechaCambio] datetime2 NOT NULL,
        [Usuario] nvarchar(100) NOT NULL,
        [TipoCambio] nvarchar(30) NOT NULL,
        [DatosAnteriores] nvarchar(max) NULL,
        [DatosNuevos] nvarchar(max) NULL,
        CONSTRAINT [PK_AuditoriaCambios] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919104547_ControlDeCambios'
)
BEGIN
    CREATE INDEX [IX_AuditoriaCambios_Entidad_IdRegistro] ON [AuditoriaCambios] ([Entidad], [IdRegistro]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260919104547_ControlDeCambios'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260919104547_ControlDeCambios', N'8.0.31');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030133110_ReferenciasAUsuarios'
)
BEGIN
    CREATE INDEX [IX_Turnos_IdUsuarioMedico] ON [Turnos] ([IdUsuarioMedico]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030133110_ReferenciasAUsuarios'
)
BEGIN
    CREATE INDEX [IX_Seguimientos_IdUsuarioDecision] ON [Seguimientos] ([IdUsuarioDecision]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030133110_ReferenciasAUsuarios'
)
BEGIN
    CREATE INDEX [IX_Seguimientos_IdUsuarioRegistro] ON [Seguimientos] ([IdUsuarioRegistro]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030133110_ReferenciasAUsuarios'
)
BEGIN
    CREATE INDEX [IX_Pacientes_IdUsuarioPortal] ON [Pacientes] ([IdUsuarioPortal]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030133110_ReferenciasAUsuarios'
)
BEGIN
    CREATE INDEX [IX_MedicionesRIN_IdUsuarioRegistro] ON [MedicionesRIN] ([IdUsuarioRegistro]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030133110_ReferenciasAUsuarios'
)
BEGIN
    CREATE INDEX [IX_EventosAdversos_IdUsuarioRegistro] ON [EventosAdversos] ([IdUsuarioRegistro]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030133110_ReferenciasAUsuarios'
)
BEGIN
    CREATE INDEX [IX_Alertas_IdUsuarioResolucion] ON [Alertas] ([IdUsuarioResolucion]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030133110_ReferenciasAUsuarios'
)
BEGIN
    ALTER TABLE [Alertas] ADD CONSTRAINT [FK_Alertas_Usuarios_IdUsuarioResolucion] FOREIGN KEY ([IdUsuarioResolucion]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030133110_ReferenciasAUsuarios'
)
BEGIN
    ALTER TABLE [EventosAdversos] ADD CONSTRAINT [FK_EventosAdversos_Usuarios_IdUsuarioRegistro] FOREIGN KEY ([IdUsuarioRegistro]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030133110_ReferenciasAUsuarios'
)
BEGIN
    ALTER TABLE [MedicionesRIN] ADD CONSTRAINT [FK_MedicionesRIN_Usuarios_IdUsuarioRegistro] FOREIGN KEY ([IdUsuarioRegistro]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030133110_ReferenciasAUsuarios'
)
BEGIN
    ALTER TABLE [Pacientes] ADD CONSTRAINT [FK_Pacientes_Usuarios_IdUsuarioPortal] FOREIGN KEY ([IdUsuarioPortal]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030133110_ReferenciasAUsuarios'
)
BEGIN
    ALTER TABLE [Seguimientos] ADD CONSTRAINT [FK_Seguimientos_Usuarios_IdUsuarioDecision] FOREIGN KEY ([IdUsuarioDecision]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030133110_ReferenciasAUsuarios'
)
BEGIN
    ALTER TABLE [Seguimientos] ADD CONSTRAINT [FK_Seguimientos_Usuarios_IdUsuarioRegistro] FOREIGN KEY ([IdUsuarioRegistro]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030133110_ReferenciasAUsuarios'
)
BEGIN
    ALTER TABLE [Turnos] ADD CONSTRAINT [FK_Turnos_Usuarios_IdUsuarioMedico] FOREIGN KEY ([IdUsuarioMedico]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030133110_ReferenciasAUsuarios'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030133110_ReferenciasAUsuarios', N'8.0.31');
END;
GO

COMMIT;
GO

