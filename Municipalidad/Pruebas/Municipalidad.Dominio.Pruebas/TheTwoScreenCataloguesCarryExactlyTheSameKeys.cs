using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// EL DEFECTO QUE ESTA FIXTURE EXISTE PARA IMPEDIR, y es el mismo que
	// EveryClauseOfTheRedactionIsSaidInBothLanguages atrapa del lado del DOMINIO: una clave que se
	// agrega a un catalogo y se olvida en el otro. Del lado de la CARATULA no lo atrapaba nadie.
	//
	// Y se paga en pantalla: Textos.this[clave] contesta "{clave}" cuando la clave no esta, asi que una
	// asimetria de una sola letra sale a la vista de la persona con las llaves puestas, y nada se pone
	// rojo. Los dos catalogos estaban en paridad exacta el dia que se escribio esto; lo que faltaba era
	// algo que los mantuviera asi.
	//
	// LAS TRES SE NECESITAN Y NINGUNA SOBRA:
	//
	//   - Comparar los dos juegos de claves atrapa la asimetria.
	//   - Que ningun valor sea vacio atrapa la clave que se agrego para traducir despues: la clave esta
	//     en los dos, la comparacion pasa, y la pantalla dice nada.
	//   - Comparar los HUECOS de cada clave atrapa la traduccion que se comio un {hueco} o le cambio el
	//     nombre. Un hueco que sobra se queda en pantalla con las llaves puestas; uno que falta borra un
	//     dato de la frase sin dejar rastro, que es peor porque se lee bien.
	//
	// NO se afirma que los dos textos sean distintos, al reves que del lado del dominio: 'Feed' se dice
	// igual en los dos idiomas a proposito, y es el nombre de una cosa y no una frase sin traducir.
	//
	// Los dos archivos viajan como DATO -- ver el csproj, que dice por que.
	[TestFixture]
	public sealed class TheTwoScreenCataloguesCarryExactlyTheSameKeys
	{
		private const string ElCatalogoDelRegistro = "texto.es-CR.json.dato";
		private const string ElCatalogoEnIngles = "texto.en.json.dato";

		private static readonly Regex UnHueco = new Regex(@"\{(\w+)\}", RegexOptions.Compiled);

		[Test]
		public void TheTwoScreenCataloguesCarryTheSameKeys()
		{
			IReadOnlyDictionary<string, string> enEspanol = ElCatalogo(ElCatalogoDelRegistro);
			IReadOnlyDictionary<string, string> enIngles = ElCatalogo(ElCatalogoEnIngles);

			Assert.Multiple(() =>
			{
				Assert.That(LasQueLeFaltanA(enEspanol, enIngles), Is.Empty,
					"claves que el catalogo en espanol tiene y el ingles no: esas pantallas salen con "
					+ "un {clave} crudo para quien lea en ingles");
				Assert.That(LasQueLeFaltanA(enIngles, enEspanol), Is.Empty,
					"claves que el catalogo en ingles tiene y el espanol no: al reves, y peor, porque el "
					+ "espanol es el idioma del registro y al que cae toda etiqueta desconocida");
			});
		}

		[Test]
		public void NoKeyOfEitherCatalogueIsLeftWithoutWords()
		{
			Assert.Multiple(() =>
			{
				foreach ((string archivo, IReadOnlyDictionary<string, string> catalogo) in LosDos())
					foreach ((string clave, string dicho) in catalogo)
						Assert.That(dicho.Trim(), Is.Not.Empty,
							archivo + " · " + clave + ": la clave esta y no dice nada, asi que la "
							+ "comparacion de claves la deja pasar y la pantalla queda muda");
			});
		}

		[Test]
		public void EveryKeySaysTheSameHolesInBothLanguages()
		{
			IReadOnlyDictionary<string, string> enEspanol = ElCatalogo(ElCatalogoDelRegistro);
			IReadOnlyDictionary<string, string> enIngles = ElCatalogo(ElCatalogoEnIngles);

			Assert.Multiple(() =>
			{
				foreach ((string clave, string dicho) in enEspanol)
				{
					if (!enIngles.TryGetValue(clave, out string suTraduccion)) continue;

					Assert.That(LosHuecosDe(suTraduccion), Is.EqualTo(LosHuecosDe(dicho)),
						clave + ": los dos idiomas no nombran los mismos huecos. Uno que sobra sale a "
						+ "pantalla con las llaves puestas; uno que falta borra un dato de la frase y "
						+ "se lee bien, que es peor");
				}
			});
		}

		// ------------------------------------------------------------------ como se leen

		private static IEnumerable<(string Archivo, IReadOnlyDictionary<string, string> Catalogo)> LosDos()
		{
			yield return (ElCatalogoDelRegistro, ElCatalogo(ElCatalogoDelRegistro));
			yield return (ElCatalogoEnIngles, ElCatalogo(ElCatalogoEnIngles));
		}

		// EL MISMO APLANADO QUE HACE Textos, y por la misma razon: el catalogo se escribe anidado por
		// pantalla, para que se lea, y se busca plano. La llave "//" es la nota del archivo y no es texto.
		private static IReadOnlyDictionary<string, string> ElCatalogo(string archivo)
		{
			string ruta = Path.Combine(AppContext.BaseDirectory, archivo);

			Assert.That(File.Exists(ruta), Is.True,
				archivo + ": no viajo al output, asi que esta fixture no esta afirmando nada");

			using JsonDocument documento = JsonDocument.Parse(File.ReadAllText(ruta));

			var plano = new Dictionary<string, string>(StringComparer.Ordinal);
			Aplanar(documento.RootElement, "", plano);
			return plano;
		}

		private static void Aplanar(JsonElement nodo, string prefijo, Dictionary<string, string> plano)
		{
			if (nodo.ValueKind != JsonValueKind.Object)
			{
				if (prefijo.Length > 0 && nodo.ValueKind == JsonValueKind.String)
					plano[prefijo] = nodo.GetString();
				return;
			}

			foreach (JsonProperty hoja in nodo.EnumerateObject())
			{
				if (hoja.Name == "//") continue;
				Aplanar(hoja.Value, prefijo.Length == 0 ? hoja.Name : prefijo + "." + hoja.Name, plano);
			}
		}

		private static string[] LasQueLeFaltanA(
			IReadOnlyDictionary<string, string> unCatalogo, IReadOnlyDictionary<string, string> elOtro)
		{
			return unCatalogo.Keys.Where(clave => !elOtro.ContainsKey(clave)).OrderBy(clave => clave).ToArray();
		}

		private static string[] LosHuecosDe(string dicho)
		{
			return UnHueco.Matches(dicho).Select(uno => uno.Groups[1].Value).OrderBy(uno => uno).ToArray();
		}
	}
}
