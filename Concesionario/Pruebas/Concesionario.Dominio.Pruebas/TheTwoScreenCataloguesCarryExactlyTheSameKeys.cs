using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// EL DEFECTO QUE ESTA FIXTURE EXISTE PARA IMPEDIR: una clave que se agrega a un catalogo de la
	// caratula y se olvida en el otro. Del lado de la pantalla no lo atrapaba nadie.
	//
	// Y se paga en pantalla: Texts.this[clave] contesta "{clave}" cuando la clave no esta, asi que una
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
	// NO se afirma que los dos textos sean distintos: "VIN", "WhatsApp" y "Diagnostic" se dicen igual en
	// los dos idiomas a proposito, y son nombres de cosas y no frases sin traducir.
	//
	// Los dos archivos viajan como DATO -- ver el csproj, que dice por que.
	[TestFixture]
	public sealed class TheTwoScreenCataloguesCarryExactlyTheSameKeys
	{
		private static readonly Regex AHole = new Regex(@"\{(\w+)\}", RegexOptions.Compiled);

		[Test]
		public void TheTwoScreenCataloguesCarryTheSameKeys()
		{
			IReadOnlyDictionary<string, string> inSpanish =
				TheCatalogueOfTheScreen.WhatTheScreenSaysInSpanish();
			IReadOnlyDictionary<string, string> inEnglish =
				TheCatalogueOfTheScreen.WhatTheScreenSaysInEnglish();

			Assert.Multiple(() =>
			{
				Assert.That(TheOnesMissingFrom(inSpanish, inEnglish), Is.Empty,
					"claves que el catalogo en espanol tiene y el ingles no: esas pantallas salen con "
					+ "un {clave} crudo para quien lea en ingles");
				Assert.That(TheOnesMissingFrom(inEnglish, inSpanish), Is.Empty,
					"claves que el catalogo en ingles tiene y el espanol no: al reves, y peor, porque el "
					+ "espanol es el idioma del registro y al que cae toda etiqueta desconocida");
			});
		}

		[Test]
		public void NoKeyOfEitherCatalogueIsLeftWithoutWords()
		{
			Assert.Multiple(() =>
			{
				foreach ((string file, IReadOnlyDictionary<string, string> catalogue) in TheTwo())
					foreach ((string key, string said) in catalogue)
						Assert.That(said.Trim(), Is.Not.Empty,
							file + " · " + key + ": la clave esta y no dice nada, asi que la comparacion "
							+ "de claves la deja pasar y la pantalla queda muda");
			});
		}

		[Test]
		public void EveryKeySaysTheSameHolesInBothLanguages()
		{
			IReadOnlyDictionary<string, string> inSpanish =
				TheCatalogueOfTheScreen.WhatTheScreenSaysInSpanish();
			IReadOnlyDictionary<string, string> inEnglish =
				TheCatalogueOfTheScreen.WhatTheScreenSaysInEnglish();

			Assert.Multiple(() =>
			{
				foreach ((string key, string said) in inSpanish)
				{
					if (!inEnglish.TryGetValue(key, out string itsTranslation)) continue;

					Assert.That(TheHolesOf(itsTranslation), Is.EqualTo(TheHolesOf(said)),
						key + ": los dos idiomas no nombran los mismos huecos. Uno que sobra sale a "
						+ "pantalla con las llaves puestas; uno que falta borra un dato de la frase y se "
						+ "lee bien, que es peor");
				}
			});
		}

		// Y CADA CLAVE QUE TheOwnerIsTold NOMBRA ESTA EN LOS DOS. Es la mitad que la comparacion de
		// arriba no ve: los dos catalogos podrian coincidir perfectamente entre si y no tener ninguna de
		// las once frases que la pantalla pide por esos nombres.
		[Test]
		public void EveryKeyTheOwnerIsToldNamesIsInBothCatalogues()
		{
			IReadOnlyDictionary<string, string> inSpanish =
				TheCatalogueOfTheScreen.WhatTheScreenSaysInSpanish();
			IReadOnlyDictionary<string, string> inEnglish =
				TheCatalogueOfTheScreen.WhatTheScreenSaysInEnglish();

			string[] named = typeof(Concesionario.Wasm.TheOwnerIsTold)
				.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
				.Select(each => (string)each.GetRawConstantValue())
				.ToArray();

			Assert.That(named, Is.Not.Empty,
				"TheOwnerIsTold no nombro ninguna clave, asi que esta prueba no esta afirmando nada.");

			Assert.Multiple(() =>
			{
				foreach (string key in named)
				{
					Assert.That(inSpanish.ContainsKey(key), Is.True,
						key + ": la pantalla la pide y el catalogo en espanol no la tiene.");
					Assert.That(inEnglish.ContainsKey(key), Is.True,
						key + ": la pantalla la pide y el catalogo en ingles no la tiene.");
				}
			});
		}

		private static IEnumerable<(string File, IReadOnlyDictionary<string, string> Catalogue)> TheTwo()
		{
			yield return (TheCatalogueOfTheScreen.TheOneOfTheRecord,
				TheCatalogueOfTheScreen.WhatTheScreenSaysInSpanish());
			yield return (TheCatalogueOfTheScreen.TheOneInEnglish,
				TheCatalogueOfTheScreen.WhatTheScreenSaysInEnglish());
		}

		private static string[] TheOnesMissingFrom(
			IReadOnlyDictionary<string, string> one, IReadOnlyDictionary<string, string> theOther)
		{
			return one.Keys.Where(key => !theOther.ContainsKey(key)).OrderBy(key => key, StringComparer.Ordinal)
				.ToArray();
		}

		private static string[] TheHolesOf(string said)
		{
			return AHole.Matches(said).Select(one => one.Groups[1].Value)
				.OrderBy(one => one, StringComparer.Ordinal).ToArray();
		}
	}
}
