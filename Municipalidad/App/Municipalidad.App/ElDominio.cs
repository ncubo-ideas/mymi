using System;
using System.Reflection;

namespace Municipalidad.App
{
	// El dominio es internal, asi que nadie de afuera lo nombra en C#. Se carga POR NOMBRE
	// SIMPLE, que resuelve contra el deps.json del anfitrion y contra el manifiesto de
	// arranque de wasm; una ruta de disco no existe adentro de un navegador.
	public static class ElDominio
	{
		private const string Nombre = "Municipalidad.Dominio";

		private static readonly Lazy<Assembly> cargado =
			new Lazy<Assembly>(() => Assembly.Load(new AssemblyName(Nombre)));

		public static Assembly Assembly { get { return cargado.Value; } }
	}
}
