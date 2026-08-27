using System;
using System.IO;
using System.Reflection;

namespace Presupuesto.App
{
	// CÓMO SE ENCUENTRA EL DOMINIO SIN NOMBRARLO.
	//
	// El modelo entero es `internal`, así que ningún nodo de afuera nombra un tipo suyo en
	// C#. Eso deja una pregunta que parece trivial y no lo es: **si nadie lo referencia,
	// ¿cómo lo encuentra el runtime?**
	//
	// MEDIDO, y es la trampa: `Assembly.Load(new AssemblyName("Presupuesto.Dominio"))`
	// **falla** aunque el DLL esté al lado del ejecutable —
	//
	//     System.IO.FileNotFoundException: Could not load file or assembly
	//     'Presupuesto.Dominio, Culture=neutral, PublicKeyToken=null'
	//
	// porque la carga por nombre simple se resuelve contra el **`deps.json`**, no contra la
	// carpeta. Y el dominio no figura en el `deps.json` de un proyecto que no lo referencia
	// en compilación, que es justamente el caso que la cerca produce. Copiar el archivo no
	// alcanza: hay que decirle al runtime **de dónde** cargarlo.
	//
	// De ahí `LoadFrom` con la ruta. Y no es un parche: es más fiel a lo que va a pasar de
	// verdad, porque en la arquitectura objetivo **el Host carga el módulo como un archivo**
	// que la persona bajó de la institución, no como una dependencia de compilación.
	//
	// Se carga UNA sola vez: `LoadFrom` sobre la misma ruta devuelve el mismo assembly, pero
	// dos rutas distintas al mismo archivo darían dos assemblies y el motor no reconocería
	// los tipos de uno en el otro.
	public static class ElDominio
	{
		private const string Nombre = "Presupuesto.Dominio";

		private static readonly Lazy<Assembly> cargado = new Lazy<Assembly>(Cargar);

		public static Assembly Assembly => cargado.Value;

		private static Assembly Cargar()
		{
			string ruta = Path.Combine(AppContext.BaseDirectory, Nombre + ".dll");

			if (!File.Exists(ruta))
				throw new FileNotFoundException(
					$"No está '{Nombre}.dll' junto al ejecutable ({AppContext.BaseDirectory}). "
					+ "El dominio no se referencia en compilación —es internal a propósito— así que "
					+ "cada host tiene que copiarlo. Revisá el ProjectReference con "
					+ "ReferenceOutputAssembly=false y CopyToOutputDirectory en el csproj del host.",
					ruta);

			return Assembly.LoadFrom(ruta);
		}
	}
}
