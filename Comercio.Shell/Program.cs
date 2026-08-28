using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Comercio.Shell;

// EL SHELL DEL COMERCIO. Muestra el PWA del comercio dentro de un WebView2, servido desde un
// HOST VIRTUAL (https://<host>/) mapeado a la carpeta local 'web'. Ese mapeo es la clave: el
// PWA cree que esta en un origen https normal, no en file://, que le rompe fetch y las rutas
// absolutas. Esta ventana ES la contencion ("a raya"): el PWA corre dentro de ella.
internal static class Program
{
    private sealed class Config
    {
        public string Titulo { get; set; } = "Comercio";
        public string Host { get; set; } = "comercio.local";
        public string Entrada { get; set; } = "index.html";
        public string CarpetaWeb { get; set; } = "web";
        // Nombre de la carpeta de datos en %LocalAppData% donde va el cache de WebView2, fuera de
        // la carpeta de instalacion (asi el desinstalador la deja limpia).
        public string Datos { get; set; } = "Comercio";
    }

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        string baseDir = AppContext.BaseDirectory;
        Config cfg = LeerConfig(Path.Combine(baseDir, "comercio.json"));
        string webDir = Path.GetFullPath(Path.Combine(baseDir, cfg.CarpetaWeb));

        var form = new Form
        {
            Text = cfg.Titulo,
            Width = 1100,
            Height = 780,
            MinimumSize = new Size(800, 560),
            StartPosition = FormStartPosition.CenterScreen,
        };

        var web = new WebView2 { Dock = DockStyle.Fill };
        form.Controls.Add(web);

        form.Load += async (_, _) =>
        {
            try
            {
                // El user-data folder de WebView2 va en %LocalAppData%\<Datos>\WebView2, NO al lado
                // del exe (su default '<exe>.WebView2'). Asi el desinstalador deja LIMPIA la carpeta
                // de programa: el cache de runtime (EBWebView, Crashpad, GPUCache, ...) no queda
                // dentro de Programs\<comercio> impidiendo borrarla.
                string userData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    cfg.Datos, "WebView2");
                Directory.CreateDirectory(userData);
                var env = await CoreWebView2Environment.CreateAsync(userDataFolder: userData);
                await web.EnsureCoreWebView2Async(env);

                // https://<host>/X  ->  <webDir>\X. El PWA se sirve como si fuera un sitio https.
                web.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    cfg.Host, webDir, CoreWebView2HostResourceAccessKind.Allow);

                // Sin barra de herramientas de dev en release: nadie abre un inspector sobre los
                // datos de la persona.
#if !DEBUG
                web.CoreWebView2.Settings.AreDevToolsEnabled = false;
                web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
#endif

                web.CoreWebView2.Navigate($"https://{cfg.Host}/{cfg.Entrada}");
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    "No se pudo iniciar la vista del comercio.\n\n" + e.Message +
                    "\n\nProbablemente falta el WebView2 Runtime (viene con Microsoft Edge).",
                    cfg.Titulo, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        Application.Run(form);
    }

    private static Config LeerConfig(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                Config? c = JsonSerializer.Deserialize<Config>(
                    File.ReadAllText(path),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (c is not null) return c;
            }
        }
        catch { /* config invalida o ausente: defaults */ }
        return new Config();
    }
}
