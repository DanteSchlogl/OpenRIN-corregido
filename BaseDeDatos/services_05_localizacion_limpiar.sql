-- OpenRIN_Services: los textos de es, en y zh-CN ahora viven en los .resx (ensamblados satélite por cultura).
-- Esta limpieza retira de la base la semilla de textos que usaban las versiones anteriores, para que no pise a los
-- recursos. Corre una sola vez: solo si todavía no existe la tabla Respaldos (primera instalación de esta versión).
-- Los idiomas agregados desde la administración y sus leyendas no se tocan.
IF OBJECT_ID('dbo.Respaldos', 'U') IS NULL
   AND OBJECT_ID('dbo.Textos', 'U') IS NOT NULL
BEGIN
    DELETE FROM dbo.Textos WHERE CodigoIdioma IN ('es', 'en', 'zh-CN');
END;
GO
