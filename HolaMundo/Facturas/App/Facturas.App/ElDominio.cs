using System;
using System.Reflection;

namespace Facturas.App
{
	// COMO SE ENCUENTRA EL DOMINIO SIN NOMBRARLO.
	//
	// El modelo entero es `internal`, asi que ningun nodo de afuera nombra un tipo suyo en
	// C#. Eso deja una pregunta que parece trivial y no lo es: si nadie lo nombra, como lo
	// encuentra el runtime?
	//
	// Por NOMBRE. El ctor de PerformanceV2 recibe un Assembly, y `typeof(Facturario).Assembly`
	// era la unica linea que obligaba a este proyecto a nombrar un tipo del dominio. El motor
	// resuelve los tipos por reflexion desde el TEXTO del script, y la reflexion no tiene
	// frontera de assembly: el dominio entero puede quedarse internal.
	//
	// POR QUE `Assembly.Load` Y NO `LoadFrom`: la carga por nombre simple se resuelve contra
	// el `deps.json`, no contra la carpeta. Este proyecto SIGUE teniendo el ProjectReference
	// al dominio -- lo que se quito es el derecho a nombrarlo, no la referencia-- asi que el
	// dominio figura en el deps.json de cada host y `Load` lo encuentra. Si algun dia se
	// quitara la referencia (ReferenceOutputAssembly=false), esto dejaria de resolver y
	// habria que pasar a `LoadFrom` con la ruta.
	//
	// PRECAUCION PARA WASM Y WASI: `Assembly.Load` no deja una referencia IL dura, asi que el
	// trimmer que Blazor activa en publish Release podria sacar el dominio del output. El
	// sintoma seria un `dotnet run` que anda y un publish que revienta. La respuesta es
	// `<TrimmerRootAssembly Include="Facturas.Dominio" />` en el csproj del host que publique
	// a wasm -- y vale saber que ese riesgo existe igual con `typeof`, que ancla el tipo pero
	// no los miembros que el DSL resuelve.
	public static class ElDominio
	{
		private const string Nombre = "Facturas.Dominio";

		private static readonly Lazy<Assembly> cargado =
			new Lazy<Assembly>(() => Assembly.Load(new AssemblyName(Nombre)));

		public static Assembly Assembly => cargado.Value;
	}
}
