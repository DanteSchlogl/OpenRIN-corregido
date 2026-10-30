-- OpenRIN_Services: sistema de permisos (T04) — catálogo en árbol (patrón composite) y
-- asignaciones por perfil. IMPORTANTE: importar con  sqlcmd -f 65001 -i permisos.sql
-- (los nombres llevan acentos). Las asignaciones se resetean a la línea base en cada corrida.
IF OBJECT_ID('dbo.Permisos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Permisos
    (
        Codigo      VARCHAR(30)   NOT NULL CONSTRAINT PK_Permisos PRIMARY KEY,
        Nombre      NVARCHAR(100) NOT NULL,
        Tipo        CHAR(1)       NOT NULL,   -- 'A' = atómico, 'C' = compuesto
        CodigoPadre VARCHAR(30)   NULL CONSTRAINT FK_Permisos_Padre REFERENCES dbo.Permisos(Codigo)
    );
END;
GO

-- Catálogo del árbol de permisos (códigos únicos estilo cátedra: letras + dígitos).
MERGE dbo.Permisos AS destino
USING (VALUES
    ('GE010', N'Gestión de pacientes',          'C', NULL),
    ('PA002', N'Gestionar pacientes e historias clínicas', 'A', 'GE010'),
    ('PA006', N'Consultar historia clínica',    'A', 'GE010'),
    ('GE020', N'Gestión de agenda',             'C', NULL),
    ('PA003', N'Gestionar agenda y turnos',     'A', 'GE020'),
    ('GE030', N'Triaje y RIN',                  'C', NULL),
    ('PA001', N'Ver panel de monitoreo',        'A', 'GE030'),
    ('PA004', N'Registrar recepción de RIN',    'A', 'GE030'),
    ('GE040', N'Seguimiento clínico',           'C', NULL),
    ('PA005', N'Gestionar eventos adversos',    'A', 'GE040'),
    ('PA007', N'Gestionar seguimiento clínico', 'A', 'GE040'),
    ('GE050', N'Reportes',                      'C', NULL),
    ('PA008', N'Ver reportes estadísticos',     'A', 'GE050'),
    ('GE060', N'Portal del paciente',           'C', NULL),
    ('PO001', N'Reportar RIN (portal)',         'A', 'GE060'),
    ('PO002', N'Ver historial propio (portal)', 'A', 'GE060'),
    ('GE070', N'Administración del sistema',    'C', NULL),
    ('SI001', N'Gestionar usuarios y bitácora', 'A', 'GE070'),
    ('SI002', N'Ver control de cambios',        'A', 'GE070'),
    ('SI003', N'Administrar idiomas y leyendas','A', 'GE070')
) AS origen(Codigo, Nombre, Tipo, CodigoPadre)
ON destino.Codigo = origen.Codigo
WHEN MATCHED THEN
    UPDATE SET Nombre = origen.Nombre, Tipo = origen.Tipo, CodigoPadre = origen.CodigoPadre
WHEN NOT MATCHED THEN
    INSERT (Codigo, Nombre, Tipo, CodigoPadre)
    VALUES (origen.Codigo, origen.Nombre, origen.Tipo, origen.CodigoPadre);
GO

-- Línea base de asignaciones por perfil (los códigos legacy se retiran).
DELETE FROM dbo.PerfilesPermisos
WHERE Permiso IN ('GESTION_AGENDA','GESTION_PACIENTES','PANEL_VER','RECEPCION_RIN','REPORTES_VER',
                  'PORTAL_REPORTAR_RIN','PORTAL_VER_HISTORIAL','EVENTOS_ADVERSOS','HISTORIA_CLINICA',
                  'SEGUIMIENTO_CLINICO','SISTEMA_ADMIN');
GO
DELETE FROM dbo.PerfilesPermisos
WHERE Perfil IN ('administrativo','medico','paciente','familiar_autorizado','sysadmin');
GO
INSERT INTO dbo.PerfilesPermisos (Perfil, Permiso) VALUES
    ('administrativo','PA001'), ('administrativo','PA002'), ('administrativo','PA003'),
    ('administrativo','PA004'), ('administrativo','PA008'),
    ('medico','PA001'), ('medico','PA005'), ('medico','PA006'), ('medico','PA007'), ('medico','PA008'),
    ('paciente','PO001'), ('paciente','PO002'),
    ('familiar_autorizado','PO001'), ('familiar_autorizado','PO002'),
    ('sysadmin','PA001'), ('sysadmin','PA002'), ('sysadmin','PA003'), ('sysadmin','PA004'),
    ('sysadmin','PA005'), ('sysadmin','PA006'), ('sysadmin','PA007'), ('sysadmin','PA008'),
    ('sysadmin','PO001'), ('sysadmin','PO002'), ('sysadmin','SI001'), ('sysadmin','SI002'), ('sysadmin','SI003');
GO
