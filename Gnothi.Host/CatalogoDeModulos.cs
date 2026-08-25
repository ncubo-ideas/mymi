namespace Gnothi.Host;

// DESCUBRIMIENTO: un directorio por modulo, cada uno con su manifiesto y su instalacion.
//
//   Modulos/
//     facturas/
//       manifiesto.json    <- de la institucion
//       instalacion.json   <- del Host
//
// Un modulo con manifiesto ilegible se SALTA con aviso por stderr, no tumba el Host. Un
// Host que no arranca porque una institucion publico un json torcido es un Host que le
// deja a un tercero la disponibilidad de todos los demas modulos.
internal sealed class CatalogoDeModulos : IAsyncDisposable
{
	private readonly List<Modulo> modulos = [];

	public IReadOnlyList<Modulo> Modulos => modulos;
	public string Raiz { get; }

	public CatalogoDeModulos(string raiz) => Raiz = raiz;

	public IReadOnlyList<string> Cargar()
	{
		modulos.Clear();
		List<string> avisos = [];

		if (!Directory.Exists(Raiz))
		{
			avisos.Add($"no existe el directorio de modulos: {Raiz}");
			return avisos;
		}

		foreach (string dir in Directory.EnumerateDirectories(Raiz).OrderBy(d => d))
		{
			string manifiesto = Path.Combine(dir, "manifiesto.json");
			string instalacion = Path.Combine(dir, "instalacion.json");

			if (!File.Exists(manifiesto)) { avisos.Add($"{Path.GetFileName(dir)}: sin manifiesto.json, se salta"); continue; }
			if (!File.Exists(instalacion)) { avisos.Add($"{Path.GetFileName(dir)}: sin instalacion.json, se salta"); continue; }

			try
			{
				Manifiesto m = Manifiesto.Leer(manifiesto);
				Instalacion i = Instalacion.Leer(instalacion);
				modulos.Add(new Modulo(m, i, dir));

				// El exceso de capacidades es la senal de alarma barata: un modulo de
				// facturas que pide microfono no necesita analisis, necesita un aviso.
				string[] excesos = [.. m.Capacidades?.Excesos() ?? []];
				if (excesos.Length > 0)
					avisos.Add($"{m.Modulo}: declara capacidades de mas ({string.Join(", ", excesos)})");
			}
			catch (Exception e)
			{
				avisos.Add($"{Path.GetFileName(dir)}: {e.GetType().Name}: {e.Message}");
			}
		}

		return avisos;
	}

	// RECARGA EN CALIENTE DEL CONSENTIMIENTO.
	//
	// Vuelve a leer SOLO los instalacion.json -el archivo de la persona- y devuelve qué
	// modulos cambiaron. No vuelve a leer manifiestos ni reconstruye modulos: recrear un
	// Modulo recrearia su cascara, y una cascara nueva le arranca el proceso hijo de abajo
	// a una llamada que podria estar en vuelo.
	//
	// Si el archivo no se puede leer, el modulo queda APAGADO. Es un archivo de
	// consentimiento: cuando no se puede saber qué autorizó la persona, la respuesta no es
	// suponer que autorizó. Cuesta un parpadeo si alguien lo pesca a medio escribir, y ese
	// es el precio correcto.
	public IReadOnlyList<string> RecargarConsentimiento()
	{
		List<string> cambios = [];

		foreach (Modulo m in modulos)
		{
			string ruta = Path.Combine(m.Directorio, "instalacion.json");
			bool encendido;
			IReadOnlyList<string> apagadas;

			try
			{
				Instalacion i = Instalacion.Leer(ruta);
				encendido = i.Encendido;
				apagadas = i.ToolsApagadas;

				// Lo que este metodo NO recarga, dicho en voz alta en vez de en silencio.
				bool otraCarpeta = i.CarpetaConcedida != m.Instalacion.CarpetaConcedida;
				bool otraCascara = i.Cascara.Comando != m.Instalacion.Cascara.Comando;
				if (otraCarpeta || otraCascara)
					cambios.Add($"{m.Nombre}: cambió la cascara o la carpeta, eso pide reiniciar el Host");
			}
			catch (Exception e)
			{
				encendido = false;
				apagadas = [];
				cambios.Add($"{m.Nombre}: no se pudo leer el consentimiento ({e.GetType().Name}), queda apagado");
			}

			if (m.Reconsentir(encendido, apagadas))
				cambios.Add($"{m.Nombre}: encendido={encendido}, apagadas=[{string.Join(", ", apagadas)}]");
		}

		return cambios;
	}

	public Modulo? Buscar(string nombre)
		=> modulos.FirstOrDefault(m => string.Equals(m.Nombre, nombre, StringComparison.OrdinalIgnoreCase));

	public async ValueTask DisposeAsync()
	{
		foreach (Modulo m in modulos) await m.DisposeAsync();
	}
}
