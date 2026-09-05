using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// L7 Y §12 PRUEBA 11. Dos textos, un hecho: la pantalla dice lo que el concesionario PUBLICO y el
	// cruce usa la LLAVE. Ninguno se deriva del otro en ninguna caratula -- los dos salen de la puerta
	// YA DECIDIDOS.
	//
	// ES UN BARRIDO NUEVO Y EL QUE YA EXISTE NO LO CUBRE. TheShellPerformsNoArithmeticOnADomainValue
	// busca ARITMETICA: un operador entre dos operandos. Esto busca CIRUGIA DE TEXTO sobre un valor del
	// dominio, que es la otra mitad de la misma regla de §7 y no deja ni un caracter de la primera:
	//
	//   una caratula que INSERTA un guion invento el formato de un pais -- que es exactamente lo que
	//   el otro modulo se nego a hacer, y por escrito;
	//
	//   una caratula que QUITA uno re-implemento la normalizacion en un sitio donde ninguna prueba la
	//   defiende, y el dia que las dos reglas se separen nada lo dira.
	//
	// LAS OCHO RAICES, Y SON OCHO Y NO SIETE. El modulo son SIETE CARPETAS; las raices del envio son
	// OCHO porque App\ manda sus cinco proyectos por separado y PaginaDeDescarga es la octava.
	// EndPointDeEventos e InyectorDeEventosDemo son HERMANOS de App\ y no hijos: un barrido que nombre
	// App no los alcanza, que es como un tercer tipo de valor anulable paso cinco tareas sin que nadie
	// lo reportara.
	//
	// LO QUE ESTE DETECTOR NO VE, DICHO Y NO DISIMULADO:
	//
	//   una cirugia partida en dos lineas. La unidad es la LINEA, que es lo que una persona lee;
	//
	//   una CONCATENACION. El '+' esta FUERA de la lista a proposito: pegarle una etiqueta a un valor
	//   del dominio es como imprime cualquier pantalla, y meterlo haria que el barrido reportara todo
	//   renglon que muestre la placa. Lo que construye una frase rival por concatenacion lo atrapa una
	//   afirmacion letra por letra sobre la frase, que es lo que este modulo midio seis veces;
	//
	//   una cirugia sobre una variable cuyo nombre no diga placa. El discriminador es el NOMBRE en la
	//   linea, porque hasta LP-5 no hay ningun tipo que seguir. Es el hueco, y queda escrito.
	[TestFixture]
	public sealed class NoShellDoesStringSurgeryOnAPlate
	{
		// LAS GRAFIAS, las mismas que el censo de la puerta. El nombre varia por pais y reconocer solo
		// "plate" seria reconocer un idioma.
		private static readonly string[] EverySpellingOfAPlate =
		{
			"plate", "placa", "matricula", "patente", "chapa",
		};

		// LA CIRUGIA. Cada una CAMBIA el texto o lo parte; ninguna es formato. Van con el punto y el
		// parentesis para que un identificador que apenas las contenga no cuente.
		private static readonly string[] EveryShapeThatCutsATextApart =
		{
			".Replace(", ".Insert(", ".Substring(", ".Remove(", ".Split(",
			".PadLeft(", ".PadRight(", ".Normalize(",
			".ToUpper(", ".ToUpperInvariant(", ".ToLower(", ".ToLowerInvariant(",
			".Trim(", ".TrimStart(", ".TrimEnd(",
			"string.Join(", "string.Concat(",
		};

		[Test]
		public void NoLineInAnyOfTheEightRootsCutsAPlateApart()
		{
			var hits = new List<string>();

			foreach (string root in TheCodeOfTheShellsAsData.EveryRoot())
				hits.AddRange(TheSurgeryIn(root));

			Assert.That(hits, Is.Empty,
				"Una caratula le hace cirugia a una placa:\n" + string.Join("\n", hits)
				+ "\nInsertar un guion es inventar el formato de un pais; quitarlo es re-implementar "
				+ "la normalizacion donde ninguna prueba la defiende. LOS DOS TEXTOS SALEN DE LA "
				+ "PUERTA YA DECIDIDOS (L7): si hace falta uno que no esta, se agrega a la puerta.");
		}

		// ===== QUE EL BARRIDO HAYA LEIDO ALGO ==============================================

		// LAS OCHO RAICES POR NOMBRE. Un barrido que perdio una raiz tiene menos aciertos y se ve
		// igual de limpio.
		[Test]
		public void TheSweepReadsTheEightRootsAndNoOthers()
		{
			Assert.That(TheCodeOfTheShellsAsData.EveryRoot(), Is.EquivalentTo(new[]
			{
				TheCodeOfTheShellsAsData.App,
				TheCodeOfTheShellsAsData.Frontera,
				TheCodeOfTheShellsAsData.Consola,
				TheCodeOfTheShellsAsData.Mcp,
				TheCodeOfTheShellsAsData.Wasm,
				TheCodeOfTheShellsAsData.EndPointDeEventos,
				TheCodeOfTheShellsAsData.InyectorDeEventosDemo,
				TheCodeOfTheShellsAsData.PaginaDeDescarga,
			}));
		}

		// Y CADA UNA LLEGO CON ARCHIVOS. EveryFileOf revienta sola si una raiz no llego al output;
		// esto ademas dice que no llego vacia.
		[Test]
		public void AndEachOfThemArrivedWithFiles()
		{
			foreach (string root in TheCodeOfTheShellsAsData.EveryRoot())
				Assert.That(TheCodeOfTheShellsAsData.EveryFileOf(root), Is.Not.Empty,
					"La raiz '" + root + "' llego vacia. Un barrido que leyo cero archivos tiene cero "
					+ "aciertos y una lista vacia lo declara limpio.");
		}

		// ===== EL DETECTOR DISPARA ========================================================

		// UN BARRIDO QUE NO ENCUENTRA NADA Y NUNCA SE PROBO CONTRA ALGO REAL no distingue "no hay
		// ninguna" de "no estoy mirando". Estas lineas plantadas son la diferencia.
		[Test]
		public void TheDetectorFiresOnEveryShapeAndOnEverySpelling()
		{
			foreach (string shape in EveryShapeThatCutsATextApart)
				Assert.That(
					TheSurgeryInSource("Wasm", "Plantada.razor", "var x = placa" + shape + "\"-\");"),
					Is.Not.Empty,
					"El detector no ve '" + shape + "'.");

			foreach (string spelling in EverySpellingOfAPlate)
				Assert.That(
					TheSurgeryInSource(
						"Wasm", "Plantada.razor", "var x = la" + spelling + ".Replace(\"-\", \"\");"),
					Is.Not.Empty,
					"El detector no ve la grafia '" + spelling + "'.");

			// Y EL CASO QUE PROVOCA EL DEFECTO, escrito tal cual: meterle el guion a la llave.
			Assert.That(
				TheSurgeryInSource(
					"Wasm", "Plantada.razor",
					"string bonita = ThePlate.JoinKey.Insert(3, \"-\");"),
				Is.Not.Empty);
		}

		// Y NO DISPARA DONDE NO DEBE. Sin esto, un detector demasiado ancho vuelve inutil el verde de
		// arriba: reportaria todo renglon que muestre la placa y alguien lo apagaria entero.
		[Test]
		public void AndItDoesNotFireOnPrintingAPlateOrOnSurgeryDoneToSomethingElse()
		{
			Assert.That(
				TheSurgeryInSource("Wasm", "Plantada.razor", "<span>Placa: @Plate.AsPublished</span>"),
				Is.Empty,
				"Mostrar la placa no es cirugia.");

			Assert.That(
				TheSurgeryInSource(
					"Wasm", "Plantada.razor", "string dicho = \"Placa: \" + Plate.AsPublished;"),
				Is.Empty,
				"Y pegarle una etiqueta tampoco: el '+' esta fuera de la lista a proposito.");

			Assert.That(
				TheSurgeryInSource("Wasm", "Plantada.razor", "var v = vin.Substring(0, 8);"),
				Is.Empty,
				"Y una cirugia sobre otra cosa no es asunto de esta ley.");

			Assert.That(
				TheSurgeryInSource(
					"Wasm", "Plantada.razor", "// aca no se le mete el guion a la placa.Replace"),
				Is.Empty,
				"Y un comentario que habla de la cirugia no es la cirugia. El '///' de "
				+ "Inicio.razor que hoy nombra la placa entra por aca.");
		}

		// ===== EL DETECTOR ================================================================

		private static List<string> TheSurgeryIn(string root)
		{
			var hits = new List<string>();

			foreach (string file in TheCodeOfTheShellsAsData.EveryFileOf(root))
				hits.AddRange(TheSurgeryInSource(
					root,
					TheCodeOfTheShellsAsData.InsideItsRoot(root, file),
					File.ReadAllText(file)));

			return hits;
		}

		// LA UNIDAD ES LA LINEA. Una linea que nombra una placa Y le hace cirugia es un acierto; las
		// dos condiciones juntas, porque cualquiera sola reporta medio modulo.
		private static List<string> TheSurgeryInSource(string root, string file, string source)
		{
			var hits = new List<string>();
			string[] lines = source.Replace("\r\n", "\n").Split('\n');

			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i];

				if (IsAComment(line)) continue;
				if (!NamesAPlate(line)) continue;
				if (!CutsATextApart(line)) continue;

				hits.Add(root + "/" + file + ":" + (i + 1) + "  " + line.Trim());
			}

			return hits;
		}

		// EL APAGADO DE COMENTARIOS ES CRUDO A PROPOSITO: una linea entera que empieza con marca de
		// comentario. No hace falta mas -- codigo vivo no empieza con '//' -- y un apagado fino
		// tendria que saber de cuatro lenguajes para tapar un hueco que no existe.
		private static bool IsAComment(string line)
		{
			string start = line.TrimStart();

			return start.StartsWith("//", StringComparison.Ordinal)
				|| start.StartsWith("*", StringComparison.Ordinal)
				|| start.StartsWith("@*", StringComparison.Ordinal)
				|| start.StartsWith("<!--", StringComparison.Ordinal)
				|| start.StartsWith("#", StringComparison.Ordinal);
		}

		private static bool NamesAPlate(string line)
		{
			foreach (string each in EverySpellingOfAPlate)
				if (line.IndexOf(each, StringComparison.OrdinalIgnoreCase) >= 0) return true;

			return false;
		}

		private static bool CutsATextApart(string line)
		{
			foreach (string each in EveryShapeThatCutsATextApart)
				if (line.IndexOf(each, StringComparison.OrdinalIgnoreCase) >= 0) return true;

			return false;
		}
	}
}
