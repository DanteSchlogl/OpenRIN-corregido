using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Negocio.BLL;
using Negocio.DomainModel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Negocio.UI
{
    /// <summary>
    /// Renderizadores de exportación de la capa de presentación (REQ-FUNC-013): generan el
    /// archivo real del historial clínico unificado en PDF (con la librería de terceros
    /// QuestPDF, requisito A02), en Excel (.xlsx OOXML generado como paquete ZIP) o en
    /// JSON serializado (A03: archivo serializado con información relevante).
    /// </summary>
    public static class Exportadores
    {
        static Exportadores()
        {
            // Licencia comunitaria de QuestPDF: válida para proyectos sin fines comerciales.
            QuestPDF.Settings.License = LicenseType.Community;
            // La aplicación es de escritorio Windows: se usan las fuentes instaladas del sistema
            // (por ejemplo, Arial). Sin esto, QuestPDF solo dispone de su fuente embebida (Lato).
            QuestPDF.Settings.UseSystemFonts = true;
        }

        /// <summary>Carpeta de exportaciones del usuario (Documentos\OpenRIN\Exportaciones).</summary>
        public static string CarpetaExportacion()
        {
            string documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string carpeta = Path.Combine(documentos, "OpenRIN", "Exportaciones");
            Directory.CreateDirectory(carpeta);
            return carpeta;
        }

        /// <summary>Escribe el historial exportado con el nombre sugerido por la BLL. Devuelve la ruta absoluta.</summary>
        public static string EscribirHistorial(ExportacionHistorial exportacion, string paciente, string formato)
        {
            string ruta = Path.Combine(CarpetaExportacion(), exportacion.NombreArchivoSugerido);
            if (string.Equals(formato, "Excel", StringComparison.OrdinalIgnoreCase))
            {
                EscribirXlsx(ruta, paciente, exportacion.Items);
            }
            else if (string.Equals(formato, "JSON", StringComparison.OrdinalIgnoreCase))
            {
                EscribirJson(ruta, paciente, exportacion);
            }
            else
            {
                EscribirPdf(ruta, paciente, exportacion.Items);
            }
            return ruta;
        }

        /// <summary>
        /// Escribe el historial como archivo JSON serializado (A03): un documento con metadatos
        /// y la lista de registros. Se genera con UTF-8 sin BOM y sin escapes innecesarios,
        /// de modo que el contenido sea compacto y directamente legible.
        /// </summary>
        private static void EscribirJson(string ruta, string paciente, ExportacionHistorial exportacion)
        {
            var documento = new
            {
                sistema = "OpenRIN",
                documento = "Historial clínico unificado",
                paciente,
                generado = DateTime.Now.ToString("s"),
                totalRegistros = exportacion.Items.Count,
                registros = exportacion.Items.Select(item => new
                {
                    fecha = item.Fecha.ToString("yyyy-MM-dd"),
                    tipo = item.Tipo,
                    descripcion = item.Descripcion
                }).ToList()
            };

            string json = JsonSerializer.Serialize(documento, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText(ruta, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        // ------------------------------------------------------------------ PDF

        /// <summary>
        /// Escribe el historial clínico en PDF utilizando QuestPDF (librería de terceros,
        /// requisito A02): encabezado con los datos del paciente, listado de registros y pie
        /// con numeración de páginas. La librería gestiona la paginación y el tamaño A4.
        /// </summary>
        private static void EscribirPdf(string ruta, string paciente, List<ItemHistorial> items)
        {
            Document.Create(contenedor =>
            {
                contenedor.Page(pagina =>
                {
                    pagina.Size(PageSizes.A4);
                    pagina.Margin(2, Unit.Centimetre);
                    pagina.DefaultTextStyle(estilo => estilo.FontSize(10).FontFamily("Arial"));

                    pagina.Header().Column(encabezado =>
                    {
                        encabezado.Item().Text("OpenRIN - Historial clínico unificado")
                            .FontSize(16).Bold().FontColor("#1F3864");
                        encabezado.Item().Text($"Paciente: {paciente}").FontSize(11).Bold();
                        encabezado.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontSize(9).FontColor("#666666");
                        encabezado.Item().PaddingTop(6).LineHorizontal(1).LineColor("#2E5395");
                    });

                    pagina.Content().PaddingVertical(10).Column(cuerpo =>
                    {
                        cuerpo.Spacing(5);
                        foreach (ItemHistorial item in items)
                        {
                            cuerpo.Item().Row(fila =>
                            {
                                fila.ConstantItem(80).Text(item.Fecha.ToString("dd/MM/yyyy"))
                                    .FontSize(9).FontColor("#444444");
                                fila.ConstantItem(130).Text(item.Tipo).FontSize(9).Bold();
                                fila.RelativeItem().Text(item.Descripcion).FontSize(9);
                            });
                        }
                    });

                    pagina.Footer().AlignRight().Text(texto =>
                    {
                        texto.DefaultTextStyle(estilo => estilo.FontSize(8).FontColor("#666666"));
                        texto.Span("Página ");
                        texto.CurrentPageNumber();
                        texto.Span(" de ");
                        texto.TotalPages();
                    });
                });
            }).GeneratePdf(ruta);
        }

        // ------------------------------------------------------------------ XLSX

        /// <summary>
        /// Escribe un archivo .xlsx mínimo válido (paquete OOXML compuesto por ZIP + XML) con el
        /// historial en una hoja llamada "Historial", usando cadenas en línea.
        /// </summary>
        private static void EscribirXlsx(string ruta, string paciente, List<ItemHistorial> items)
        {
            using FileStream fs = File.Create(ruta);
            using var zip = new ZipArchive(fs, ZipArchiveMode.Create);

            Agregar(zip, "[Content_Types].xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                "</Types>");

            Agregar(zip, "_rels/.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "</Relationships>");

            Agregar(zip, "xl/workbook.xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                "<sheets><sheet name=\"Historial\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");

            Agregar(zip, "xl/_rels/workbook.xml.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                "</Relationships>");

            var hoja = new StringBuilder();
            hoja.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            hoja.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            int fila = 1;
            AgregarFila(hoja, fila++, new[] { "Paciente", paciente, "Generado", DateTime.Now.ToString("dd/MM/yyyy HH:mm") });
            AgregarFila(hoja, fila++, new[] { "Fecha", "Tipo", "Descripcion" });
            foreach (ItemHistorial item in items)
            {
                AgregarFila(hoja, fila++, new[] { item.Fecha.ToString("dd/MM/yyyy"), item.Tipo, item.Descripcion });
            }
            hoja.Append("</sheetData></worksheet>");
            Agregar(zip, "xl/worksheets/sheet1.xml", hoja.ToString());
        }

        private static void AgregarFila(StringBuilder hoja, int numeroFila, string[] celdas)
        {
            hoja.Append("<row r=\"").Append(numeroFila).Append("\">");
            string[] columnas = { "A", "B", "C", "D" };
            for (int i = 0; i < celdas.Length && i < columnas.Length; i++)
            {
                hoja.Append("<c r=\"").Append(columnas[i]).Append(numeroFila).Append("\" t=\"inlineStr\"><is><t>")
                    .Append(EscaparXml(celdas[i]))
                    .Append("</t></is></c>");
            }
            hoja.Append("</row>");
        }

        private static string EscaparXml(string texto)
            => texto.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

        private static void Agregar(ZipArchive zip, string nombre, string contenido)
        {
            ZipArchiveEntry entrada = zip.CreateEntry(nombre);
            using Stream stream = entrada.Open();
            byte[] bytes = Encoding.UTF8.GetBytes(contenido);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
