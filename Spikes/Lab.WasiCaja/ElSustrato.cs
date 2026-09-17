using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Lab.WasiCaja
{
	// EL SUSTRATO CARGA EL MODULO DESDE AFUERA DEL WASM. Paso 3 · 1 de C10.
	//
	// LA PREGUNTA QUE ESTE ARCHIVO CONTESTA, y de ella depende todo lo demas: ¿puede el invitado
	// cargar un ensamblado que NO venia adentro del .wasm? Si no puede, el sustrato no se puede
	// separar del modulo con este toolchain y hay que decirlo con esa palabra.
	//
	// POR QUE HACE FALTA SEPARARLOS. Hoy el .wasm lleva examenes adentro Y el .gmod trae sus tres
	// dll, que en modo caja no se usan nunca. Dos copias del mismo codigo, y la que corre es la del
	// wasm: actualizar el modulo NO actualiza lo que corre en la caja. Se midio el 2026-09-16 --
	// examenes paso de 3.9.0 a 3.10.0 y la caja siguio ejecutando la copia vieja.
	//
	// EL REPARTO QUE EL BUNDLE YA DECLARA, y no hay que inventarlo: el `dominio/` de un .gmod trae
	// TRES dll -- App, Dominio, Eventos -- y NO trae ni Puppeteer ni el Portador. El motor lo presta
	// el Host. Asi que el sustrato lleva el motor y el modulo entra por esos tres.
	//
	// ES LO MISMO QUE HACE InProcessShell, ADENTRO DE LA CAJA. Alla el Host abre el dll en su propio
	// proceso; aca lo abre codigo que corre encerrado, sobre una carpeta que el anfitrion monto de
	// solo lectura. El reparto de autoridad no cambia: el modulo sigue sin elegir nada.
	//
	// SE INFORMA PASO POR PASO, como la sonda de iOS y por la misma razon: si esto falla, la
	// respuesta a EN CUAL DE LOS CUATRO PASOS tiene que estar escrita antes de que alguien la
	// busque. Un «no cargo» sin el paso cuesta una tarde.
	internal static class ElSustrato
	{
		// LAS MARCAS SE BUSCAN POR NOMBRE, igual que en ElBorde y por la misma razon: si se usara
		// `typeof(ModuleSurfaceAttribute)`, el modulo tendria que cargar EL MISMO ensamblado del
		// contrato que el sustrato, y eso es justo el acoplamiento que el prestamo evita.
		private static readonly string[] Marcas = ["ModuleSurfaceAttribute", "McpServerToolTypeAttribute"];

		// LA VECINDAD, que es como resuelve InProcessShell en movil y aca no hay alternativa: el
		// `.deps.json` del modulo no viaja en el bundle, y AssemblyDependencyResolver depende de
		// hostpolicy, que bajo WASI no existe. Todo lo del modulo esta en su carpeta; todo lo demas
		// -- el motor, el BCL -- ya esta adentro del wasm y lo resuelve el contexto por omision.
		private static string vecindad = "";
		private static bool enganchado;

		internal static object Cargar(string carpetaDelModulo, string entrada, string concedida, Action<string> decir)
		{
			vecindad = carpetaDelModulo;

			if (!enganchado)
			{
				AssemblyLoadContext.Default.Resolving += PorVecindad;
				enganchado = true;
			}

			// 1 · QUE LA CARPETA EXISTA DEL LADO DE ADENTRO. Si el anfitrion no la monto, aca no
			//     hay nada, y el error tiene que decir eso y no «no encontre el tipo».
			if (!Directory.Exists(carpetaDelModulo))
				throw new InvalidOperationException(
					$"1) la carpeta del modulo no existe adentro de la caja: '{carpetaDelModulo}'. "
					+ "El anfitrion no la monto.");
			decir("1) la carpeta del modulo esta montada .......... OK ("
				+ Directory.GetFiles(carpetaDelModulo, "*.dll").Length + " dll)");

			// 2 · CARGAR EL ENSAMBLADO DE ENTRADA. Este es EL paso: es lo que el AOT obligatorio
			//     prohibe en iOS y lo que nadie habia probado bajo WASI.
			Assembly asm;
			try { asm = Assembly.LoadFrom(entrada); }
			catch (Exception e)
			{
				throw new InvalidOperationException(
					$"2) LoadFrom('{entrada}') fallo: {e.GetType().Name}: {e.Message}", e);
			}
			decir("2) Assembly.LoadFrom ........................... OK (" + asm.GetName().Name + ")");

			// 3 · LEER LOS TIPOS. Aca es donde Android se caia cuando el paso 2 no resolvia: sin
			//     dependencias, GetTypes tira por el primero que falta.
			Type[] tipos;
			try { tipos = asm.GetTypes(); }
			catch (ReflectionTypeLoadException e)
			{
				string faltan = string.Join(", ", e.LoaderExceptions
					.Where(x => x is not null).Select(x => x.Message).Take(3));
				throw new InvalidOperationException($"3) GetTypes fallo; falta: {faltan}", e);
			}
			decir("3) GetTypes .................................... OK (" + tipos.Length + " tipos)");

			// 4 · LA CLASE MARCADA. Si el ensamblado carga y no hay marca, el paquete esta mal
			//     armado y eso es distinto de que el toolchain no sirva.
			Type marcada = tipos.FirstOrDefault(t => t.GetCustomAttributes(false)
				.Any(a => Marcas.Contains(a.GetType().Name)))
				?? throw new InvalidOperationException(
					"4) ningun tipo lleva [ModuleSurface]. El paquete no declara superficie.");
			decir("4) la clase marcada ............................ OK (" + marcada.Name + ")");

			// 5 · CONSTRUIRLA CON LA CARPETA CONCEDIDA. Misma convencion que InProcessShell.Build:
			//     todo string que pida el constructor recibe la carpeta que el anfitrion otorgo. La
			//     superficie arranca su propio actor ahi adentro, que es lo que tarda.
			object superficie;
			try { superficie = Construir(marcada, concedida, 0); }
			catch (TargetInvocationException e) when (e.InnerException is not null)
			{
				throw new InvalidOperationException(
					$"5) construir {marcada.Name} fallo: {e.InnerException.GetType().Name}: {e.InnerException.Message}",
					e.InnerException);
			}
			decir("5) construida sobre la carpeta concedida ....... OK");

			return superficie;
		}

		// Un ensamblado que el modulo trae y el sustrato no tiene: se busca AL LADO del de entrada.
		// Devolver null deja que el contexto por omision siga buscando, que es lo que resuelve el
		// motor y el BCL -- esos SI estan adentro del wasm.
		private static Assembly PorVecindad(AssemblyLoadContext ctx, AssemblyName nombre)
		{
			string vecino = Path.Combine(vecindad, (nombre.Name ?? "") + ".dll");
			return File.Exists(vecino) ? ctx.LoadFromAssemblyPath(vecino) : null;
		}

		// La misma convencion que InProcessShell, y con la misma incomodidad anotada alla: un string
		// que pide un constructor recibe la carpeta concedida. Funciona en los cinco modulos del POC
		// y NO esta declarado en ningun marcador -- el anfitrion esta adivinando.
		private static object Construir(Type t, string concedida, int hondura)
		{
			if (hondura > 4) throw new InvalidOperationException($"cadena de constructores muy honda en {t.Name}");

			ConstructorInfo[] ctores = t
				.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				.OrderBy(c => c.GetParameters().Length).ToArray();

			if (ctores.Length == 0) throw new InvalidOperationException($"{t.Name} no tiene constructor accesible");

			ConstructorInfo c = ctores[0];
			object[] args = c.GetParameters()
				.Select(p => p.ParameterType == typeof(string) ? concedida : Construir(p.ParameterType, concedida, hondura + 1))
				.ToArray();

			return c.Invoke(args);
		}
	}
}
