using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Municipalidad.App;
using Municipalidad.Mcp.Tools;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// EL MANIFIESTO DICE LO QUE EL ENSAMBLADO OFRECE, Y ESTA PRUEBA EXISTE POR UNA MEDICION.
	//
	// Al escribir el manifiesto de este modulo se leyo el del modulo de referencia: declara OCHO tools
	// y su codigo declara TRECE -- how_much_my_car_is_driven, what_has_been_done_to_my_car,
	// what_i_have_spent_on_my_car, what_is_left_before_each_service y what_plate_my_car_has no estan --.
	// Su propia nota //idioma cuenta que ya se le habia quedado atras una vez, con siete descripciones
	// en ingles que ninguna tool decia. Es decir: SE SABIA, ESTABA ESCRITO, Y VOLVIO A PASAR.
	//
	// Un manifiesto no se mantiene al dia con disciplina, porque nada se pone rojo cuando se atrasa. La
	// descripcion no es documentacion de un archivo: es EL MISMO TEXTO que viaja al asistente y lo unico
	// que tiene para decidir si una tool contesta la pregunta de la persona. Un manifiesto viejo le
	// ofrece a Gnothi un catalogo que el modulo no cumple.
	//
	// SE LEE COMO DATO Y NO COMO RECURSO EMBEBIDO: el artifacts-path reubica el output entero y ninguna
	// cantidad fija de '..' llega de vuelta al modulo. Es la misma mecanica que DEC-18 dejo establecida
	// para los csproj de las caratulas.
	//
	// LA COMPARACION ES POR NOMBRE Y NUNCA POR POSICION. El manifiesto los lista en el orden en que
	// Program.cs registra las seis clases; el SDK devuelve su tools/list en otro orden, estable pero
	// suyo. Ordenar los dos lados antes de comparar es lo que hace que esta prueba mida el contenido y
	// no una coincidencia de orden.
	[TestFixture]
	public sealed class ManifestSaysExactlyWhatTheAssemblyOffers
	{
		private JsonElement elManifiesto;

		[SetUp]
		public void Leer()
		{
			string ruta = Path.Combine(
				TestContext.CurrentContext.TestDirectory, "manifiesto.json.dato");

			Assert.That(File.Exists(ruta), Is.True,
				"El manifiesto no llego al output como dato. Sin el, esta prueba no puede fallar, y una "
				+ "prueba que no puede fallar no sirve de nada: ver el ItemGroup del csproj de este "
				+ "proyecto.");

			elManifiesto = JsonDocument.Parse(File.ReadAllText(ruta)).RootElement.Clone();
		}

		// EL JUEGO DE NOMBRES, COMO IGUALDAD DE CONJUNTOS. Se pone roja tambien si alguien BORRA una
		// tool del codigo sin sacarla de aca, y el rojo dice cual.
		[Test]
		public void TheManifestOffersExactlyTheToolsTheAssemblyDeclares()
		{
			List<string> enElManifiesto = new List<string>();

			foreach (JsonElement cada in elManifiesto.GetProperty("tools").EnumerateArray())
				enElManifiesto.Add(cada.GetProperty("nombre").GetString());

			List<string> enElEnsamblado = new List<string>(LasToolsDelEnsamblado().Keys);

			enElManifiesto.Sort(StringComparer.Ordinal);
			enElEnsamblado.Sort(StringComparer.Ordinal);

			Assert.That(enElManifiesto, Is.EqualTo(enElEnsamblado),
				"El manifiesto y el ensamblado no ofrecen las mismas tools. Al modulo de referencia le "
				+ "paso esto con cinco, en silencio: Gnothi arma su catalogo leyendo este archivo, sin "
				+ "arrancar el modulo, asi que una tool que sobra la ofrece y no existe, y una que falta "
				+ "existe y nadie la ofrece.");
		}

		// Y CADA DESCRIPCION, CARACTER POR CARACTER. Es la mitad que importa: los nombres se notan
		// enseguida y el texto no. Cambiar un [Description] sin tocar el manifiesto deja al asistente
		// eligiendo con una voz que el modulo ya no tiene.
		[Test]
		public void EveryDescriptionInTheManifestIsTheOneThatTravels()
		{
			Dictionary<string, MethodInfo> delEnsamblado = LasToolsDelEnsamblado();

			Assert.Multiple(() =>
			{
				foreach (JsonElement cada in elManifiesto.GetProperty("tools").EnumerateArray())
				{
					string nombre = cada.GetProperty("nombre").GetString();

					if (!delEnsamblado.ContainsKey(nombre)) continue;

					Assert.That(cada.GetProperty("descripcion").GetString(),
						Is.EqualTo(LaDescripcionDe(delEnsamblado[nombre])),
						"La descripcion de " + nombre + " no es la que viaja. No es documentacion de "
						+ "este archivo: es el mismo texto, y se copia tal cual.");
				}
			});
		}

		// Y CADA PARAMETRO, CON SU NOMBRE, SU TIPO Y SU DESCRIPCION. El formato de 'entrada' no sabe
		// expresar un opcional ni un rango, asi que lo que se afirma es lo que si sabe decir.
		[Test]
		public void EveryParameterInTheManifestIsTheOneTheToolTakes()
		{
			Dictionary<string, MethodInfo> delEnsamblado = LasToolsDelEnsamblado();

			Assert.Multiple(() =>
			{
				foreach (JsonElement cada in elManifiesto.GetProperty("tools").EnumerateArray())
				{
					string nombre = cada.GetProperty("nombre").GetString();

					if (!delEnsamblado.ContainsKey(nombre)) continue;

					List<string> declarados = new List<string>();

					foreach (JsonElement p in cada.GetProperty("entrada").EnumerateArray())
						declarados.Add(p.GetProperty("nombre").GetString()
							+ " : " + p.GetProperty("tipo").GetString()
							+ " : " + p.GetProperty("descripcion").GetString());

					List<string> reales = new List<string>();

					foreach (ParameterInfo p in delEnsamblado[nombre].GetParameters())
						reales.Add(p.Name + " : " + ElTipoDeManifiestoDe(p) + " : " + LaDescripcionDe(p));

					Assert.That(declarados, Is.EqualTo(reales),
						"Los parametros que el manifiesto declara para " + nombre + " no son los que la "
						+ "tool toma.");
				}
			});
		}

		// EL SUSTRATO ES UN RANGO Y NO UNA VERSION PUNTUAL. Si un modulo pinea una version exacta, cada
		// actualizacion del Host huerfana a todos los modulos instalados.
		[Test]
		public void TheSubstrateIsARangeAndNeverAPointVersion()
		{
			string rango = elManifiesto.GetProperty("sustrato").GetProperty("rango").GetString();

			Assert.Multiple(() =>
			{
				Assert.That(rango, Does.StartWith(">="),
					"El rango tiene que abrir con un minimo inclusivo.");
				Assert.That(rango, Does.Contain("<"),
					"Y cerrar con un maximo excluyente. Sin el, el modulo dice que soporta cualquier "
					+ "version futura del Host, incluida la que le cambie el sustrato debajo.");
				Assert.That(rango, Does.Not.Match(@"^\d+\.\d+\.\d+$"),
					"Una version puntual aca huerfana al modulo en la siguiente actualizacion del Host.");
			});
		}

		// LAS DOS CAPACIDADES CONTRASTABLES, que es lo que las hace valer. La cascara MCP abre una
		// carpeta y ningun socket, asi que 'red' en false y 'carpeta' en 'propia' son verificables --
		// y lo que las verifica de verdad no es este renglon, es que ninguna tool tenga por donde
		// escribir ni a quien preguntarle.
		[Test]
		public void TheDeclaredCapabilitiesAreTheOnesTheShellCanBeHeldTo()
		{
			JsonElement capacidades = elManifiesto.GetProperty("capacidades");

			Assert.Multiple(() =>
			{
				Assert.That(capacidades.GetProperty("red").GetBoolean(), Is.False,
					"La cascara no le pregunta nada a la municipalidad: quien trae hechos nuevos es la "
					+ "app de la persona, y eso no corre bajo Gnothi.");
				Assert.That(capacidades.GetProperty("carpeta").GetString(), Is.EqualTo("propia"),
					"Lee la carpeta de la persona y ninguna otra.");
				Assert.That(capacidades.GetProperty("parlante").GetBoolean(), Is.False, "parlante");
				Assert.That(capacidades.GetProperty("microfono").GetBoolean(), Is.False, "microfono");

				foreach (JsonElement cada in elManifiesto.GetProperty("tools").EnumerateArray())
					Assert.That(cada.GetProperty("soloLectura").GetBoolean(), Is.True,
						cada.GetProperty("nombre").GetString() + " se declara de solo lectura. Lo que "
						+ "sostiene esa declaracion no es este renglon: es que leer no deje entrada, y "
						+ "eso lo mide TheReadOnlyHostNeverWritesToThePersonsJournal.");
			});
		}

		// Y EL MODULO SE LLAMA COMO EL ACTOR, que es la misma cosa dicha en dos archivos: Gnothi arma su
		// catalogo con este nombre y el journal de la persona vive en una carpeta con ese nombre
		// adentro. Que se separen seria un catalogo apuntando a un registro que no existe.
		[Test]
		public void TheModuleIsNamedAfterTheActorWhoseJournalItReads()
		{
			Assert.Multiple(() =>
			{
				Assert.That(elManifiesto.GetProperty("modulo").GetString(),
					Is.EqualTo(MunicipalidadApp.ElActor), "el modulo y el actor son un solo nombre");

				Assert.That(elManifiesto.GetProperty("institucion").GetString(),
					Does.Contain("mock"),
					"Y la institucion sigue siendo un MARCADOR que dice que lo es. Una municipalidad es "
					+ "una institucion publica de verdad y todo nombre plausible es el de una que "
					+ "existe: el nombre real llega cuando una de verdad adopte el modulo.");

				Assert.That(elManifiesto.GetProperty("titulo").GetString(), Is.Not.Empty,
					"Y el titulo es el nombre que la persona lee, con sus acentos.");
			});
		}

		// ------------------------------------------------------ el aparejo

		private static Dictionary<string, MethodInfo> LasToolsDelEnsamblado()
		{
			Dictionary<string, MethodInfo> encontradas = new Dictionary<string, MethodInfo>();

			foreach (Type cada in typeof(PreguntasDelEstado).Assembly.GetTypes())
			{
				bool esTipoDeTools = false;

				foreach (object atributo in cada.GetCustomAttributes(true))
					if (atributo.GetType().Name == "McpServerToolTypeAttribute") esTipoDeTools = true;

				if (!esTipoDeTools) continue;

				foreach (MethodInfo metodo in cada.GetMethods())
					foreach (object atributo in metodo.GetCustomAttributes(true))
						if (atributo.GetType().Name == "McpServerToolAttribute")
							encontradas.Add(
								(string)atributo.GetType().GetProperty("Name").GetValue(atributo),
								metodo);
			}

			return encontradas;
		}

		private static string LaDescripcionDe(MethodInfo metodo)
		{
			foreach (object atributo in metodo.GetCustomAttributes(true))
				if (atributo is System.ComponentModel.DescriptionAttribute dicho) return dicho.Description;

			return "";
		}

		private static string LaDescripcionDe(ParameterInfo parametro)
		{
			foreach (object atributo in parametro.GetCustomAttributes(true))
				if (atributo is System.ComponentModel.DescriptionAttribute dicho) return dicho.Description;

			return "";
		}

		// El formato de 'entrada' llega hasta string y number, asi que un entero se declara 'number'.
		private static string ElTipoDeManifiestoDe(ParameterInfo parametro)
		{
			if (parametro.ParameterType == typeof(int)) return "number";
			if (parametro.ParameterType == typeof(long)) return "number";
			if (parametro.ParameterType == typeof(decimal)) return "number";
			if (parametro.ParameterType == typeof(double)) return "number";
			if (parametro.ParameterType == typeof(bool)) return "boolean";

			return "string";
		}
	}
}
