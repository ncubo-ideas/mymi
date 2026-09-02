using System;
using System.IO;
using System.Reflection;

namespace HolaMundo.App
{
	// CÓMO SE ENCUENTRA EL DOMINIO SIN NOMBRARLO.
	//
	// El modelo entero es `internal`, así que nadie de afuera nombra un tipo suyo en C#.
	// Eso deja una pregunta que parece trivial y no lo es: si nadie lo referencia,
	// ¿cómo lo encuentra el runtime?
	//
	// La trampa, y está MEDIDA: `Assembly.Load("HolaMundo.Dominio")` **falla** aunque el
	// DLL esté al lado del ejecutable, porque la carga por nombre simple se resuelve
	// contra el `deps.json` y no contra la carpeta. Hay que decirle DE DÓNDE cargarlo.
	//
	// De ahí `LoadFrom` con la ruta — y no es un parche: es más fiel a lo que va a pasar
	// de verdad, porque en la arquitectura objetivo el Host carga el módulo como un
	// ARCHIVO que la persona bajó, no como una dependencia de compilación.
	public static class ElDominio
	{
		private const string Nombre = "HolaMundo.Dominio";

		private static readonly Lazy<Assembly> cargado = new Lazy<Assembly>(Cargar);

		public static Assembly Assembly => cargado.Value;

		private static Assembly Cargar()
		{
			string ruta = Path.Combine(AppContext.BaseDirectory, Nombre + ".dll");

			if (!File.Exists(ruta))
				throw new FileNotFoundException(
					$"No está '{Nombre}.dll' junto al ejecutable ({AppContext.BaseDirectory}). "
					+ "El dominio no se referencia en compilación —es internal a propósito— así que "
					+ "cada host tiene que copiarlo.", ruta);

			return Assembly.LoadFrom(ruta);
		}
	}
}
