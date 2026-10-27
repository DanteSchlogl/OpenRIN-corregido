using Microsoft.Web.WebView2.WinForms;
using System;
using System.IO;
using System.Windows.Forms;

namespace PROTOTIPO_Aero_Core
{
    public partial class Form1 : Form
    {
        private WebView2 webView;

        public Form1()
        {
            InitializeComponent(); // Este método lo genera el CLI automáticamente
            InitializeWebView();   // Nuestro método personalizado
        }

        private async void InitializeWebView()
        {
            // 1. Crear el control WebView2 y acoplarlo a todo el formulario
            webView = new WebView2();
            webView.Dock = DockStyle.Fill; // Ocupará todo el espacio de la ventana
            this.Controls.Add(webView);    // Lo agregamos al formulario

            // 2. Inicializar el entorno WebView2 (necesita ser asíncrono)
            await webView.EnsureCoreWebView2Async(null);

            // 3. Apuntar al archivo index.html dentro de la carpeta wwwroot
            string rutaHtml = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "index.html");

            // 4. Verificar si el archivo existe y cargarlo
            if (File.Exists(rutaHtml))
            {
                webView.Source = new Uri(rutaHtml);
            }
            else
            {
                MessageBox.Show($"Error crítico: No se encontró el archivo index.html en:\n{rutaHtml}", 
                                "Archivo no encontrado", 
                                MessageBoxButtons.OK, 
                                MessageBoxIcon.Error);
            }
        }
    }
}