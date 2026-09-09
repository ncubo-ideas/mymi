using System;
using System.Reflection;

namespace Examenes.App
{
	// COMO SE ENCUENTRA EL DOMINIO SIN NOMBRARLO.
	//
	// El ctor de PerformanceV2 recibe un Assembly, y `typeof(Expediente).Assembly` era la
	// linea que obligaba a los hosts a nombrar un tipo del dominio. El motor resuelve los
	// tipos por reflexion desde el TEXTO del script, y la reflexion no tiene frontera de
	// assembly: el dominio puede quedarse internal.
	//
	// POR QUE `Assembly.Load` Y NO `LoadFrom`: la carga por nombre simple se resuelve contra
	// el `deps.json`, no contra la carpeta. Este proyecto SIGUE teniendo el ProjectReference
	// al dominio -- lo que se quito es el derecho a NOMBRARLO desde los otros nodos, no la
	// referencia-- asi que el dominio figura en el deps.json de cada host y `Load` lo
	// encuentra. MEDIDO: con la referencia puesta, `Load` resuelve en consola y en el
	// servidor MCP. Si algun dia se quitara (ReferenceOutputAssembly=false), esto dejaria de
	// resolver con un FileNotFoundException aunque el dll este al lado del exe, y habria que
	// pasar a `LoadFrom` con la ruta -- que es lo que hace Presupuesto.App, que si la quito.
	//
	// PRECAUCION PARA WASM Y WASI: `Assembly.Load` no deja una referencia IL dura, asi que el
	// trimmer que Blazor activa en publish Release podria sacar el dominio del output. El
	// sintoma seria un `dotnet run` que anda y un publish que revienta. La respuesta es
	// `<TrimmerRootAssembly Include="Examenes.Dominio" />` en el csproj del host que publique
	// a wasm -- y vale saber que ese riesgo existe igual con `typeof`, que ancla el tipo pero
	// no los miembros que el DSL resuelve.
	public static class ElDominio
	{
		private const string Nombre = "Examenes.Dominio";

		private static readonly Lazy<Assembly> cargado =
			new Lazy<Assembly>(() => Assembly.Load(new AssemblyName(Nombre)));

		public static Assembly Assembly => cargado.Value;
	}
}
