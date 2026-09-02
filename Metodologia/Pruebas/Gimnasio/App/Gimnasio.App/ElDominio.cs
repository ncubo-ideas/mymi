using System;
using System.IO;
using System.Reflection;

namespace Gimnasio.App
{
	// El dominio es internal, asi que nadie de afuera lo nombra en C#. Se carga POR RUTA
	// con LoadFrom: Assembly.Load por nombre simple falla aunque el DLL este al lado del
	// ejecutable, porque resuelve contra el deps.json. Regla 3 del substrato.
	public static class ElDominio
	{
		private const string Nombre = "Gimnasio.Dominio";

		private static readonly Lazy<Assembly> cargado = new Lazy<Assembly>(Cargar);

		public static Assembly Assembly { get { return cargado.Value; } }

		private static Assembly Cargar()
		{
			string ruta = Path.Combine(AppContext.BaseDirectory, Nombre + ".dll");
			if (!File.Exists(ruta))
				throw new FileNotFoundException(
					"No esta " + Nombre + ".dll junto al ejecutable (" + AppContext.BaseDirectory
					+ "). El dominio no se referencia en compilacion -es internal a proposito-.", ruta);
			return Assembly.LoadFrom(ruta);
		}
	}
}
