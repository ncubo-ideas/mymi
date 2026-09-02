using System;
using System.Reflection;

namespace DealershipCar.App
{
	/// <summary>
	/// El ensamblado del dominio, encontrado por NOMBRE y no nombrando un tipo suyo.
	/// </summary>
	// EL CTOR DE PerformanceV2 RECIBE UN Assembly, y `typeof(MaintenanceSchedule).Assembly` era
	// la unica linea que obligaba a este proyecto a nombrar un tipo del dominio. El motor
	// resuelve los tipos por reflexion desde el TEXTO del script, y la reflexion no tiene
	// frontera de assembly: el dominio se queda entero internal y esta Superficie no lo nombra.
	//
	// POR QUE `Assembly.Load` Y NO `LoadFrom`: la carga por nombre simple se resuelve contra el
	// `deps.json`, no contra la carpeta. Este proyecto SIGUE teniendo el ProjectReference al
	// dominio -lo que se quito es el derecho a NOMBRARLO, no la referencia- asi que el dominio
	// figura en el deps.json de cada host y `Load` lo encuentra. Referenciar sin poder nombrar
	// es exactamente la cerca: el atajo no es de mal gusto, es un error de compilacion.
	//
	// MEDIDO: resuelve en consola, en el servidor MCP y en las pruebas. Si algun dia se quitara
	// la referencia (ReferenceOutputAssembly=false), esto dejaria de resolver con un
	// FileNotFoundException aunque el dll este al lado del exe.
	//
	// PRECAUCION PARA WASM: `Assembly.Load` no deja una referencia IL dura, asi que el trimmer
	// que Blazor activa en publish Release podria sacar el dominio del output. La respuesta es
	// `<TrimmerRootAssembly Include="DealershipCar.Dominio" />` en el csproj del host que
	// publique a wasm -y ese riesgo existe igual con `typeof`, que ancla el tipo pero no los
	// miembros que el DSL resuelve-.
	public static class TheDomain
	{
		private const string Name = "DealershipCar.Dominio";

		private static readonly Lazy<Assembly> loaded =
			new Lazy<Assembly>(() => Assembly.Load(new AssemblyName(Name)));

		public static Assembly Assembly => loaded.Value;
	}
}
