using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Concesionario.App.Wording;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA REGLA 5 DE LA GUIA DE LOCALIZACION, APLICADA AL SOBRE: el dominio deja de componer UN
	// STRING y compone UNA FORMA ESTABLE MAS LOS VALORES YA TIPADOS que van en los huecos. La
	// redaccion vive en App/Wording, una por idioma.
	//
	// LO QUE ESTA FIXTURE DEFIENDE, Y ES LO QUE NINGUNA OTRA VE:
	//
	//   1. QUE NINGUNA VARIANTE VUELVA A REDACTAR. Un Describe() que reaparezca en cualquiera de las
	//      cuatro respuestas, en el par derivado o en el sobre publicado, es la frase rival otra vez
	//      -- y la primera caratula que la use deja de traducirse sin que nada se ponga rojo.
	//
	//   2. QUE TODA COMBINACION DE CODIGOS SE DIGA ENTERA EN LOS DOS IDIOMAS. Una clave que falta no
	//      revienta: sale {envelope.first.Loquesea} en medio de la frase. Asi que lo que se busca es
	//      LA LLAVE ABIERTA, y se busca sobre el PRODUCTO de los codigos y no sobre una lista
	//      escrita aqui -- los codigos se leen de donde el dominio los declara, asi que un miembro
	//      nuevo llega hasta esta prueba sin que nadie lo copie.
	//
	//   3. QUE LOS VALORES NO SE TRADUZCAN. La misma respuesta dicha en los dos idiomas nombra los
	//      MISMOS numeros del registro; lo unico que cambia es el marco y como se escribe una cifra.
	//
	// EL AUTOCONTROL VA ADENTRO (C60): sin el, un catalogo que contestara cualquier cosa y una
	// prueba que no distingue son el mismo verde.
	[TestFixture]
	public sealed class TheEnvelopeAnswerCrossesAsAShapeAndItsValues
	{
		private const string TheVin = "3VW2K7AJ9FM123456";
		private const int ThirtySixMonths = 36;
		private const int SixtyThousand = 60000;
		private const int TheDeliveryReading = 10;

		private static readonly DateTime PublishedAt = new DateTime(2023, 12, 1);
		private static readonly DateTime DeliveredAt = new DateTime(2024, 1, 1);
		private static readonly DateTime ObservedAt = new DateTime(2026, 1, 1);
		private static readonly DateTime InsideBothHalves = new DateTime(2026, 1, 1);

		private VehicleProfile profile;

		[SetUp]
		public void Setup()
		{
			profile = new VehicleProfile(TheVin);
			profile.RecordDelivery(DeliveredAt, TheDeliveryReading, DistanceUnitCode.UnitA);
			profile.PublishWarrantyEnvelope(
				ThirtySixMonths, SixtyThousand, DistanceUnitCode.UnitA, PublishedAt);
		}

		// ===== 1. NADIE DE ESTA FAMILIA REDACTA ==========================================

		// NI LA RESPUESTA, NI EL PAR DERIVADO, NI EL SOBRE PUBLICADO, NI LAS DOS CLAUSULAS. Se mira
		// por REFLEXION sobre los tipos y sus subclases, que es lo que hace que una variante nueva
		// entre a la prueba sola.
		[Test]
		public void NoTypeOfTheEnvelopeFamilyComposesASentenceOfItsOwn()
		{
			List<Type> theFamily = TheEnvelopeFamily();

			Assert.That(theFamily, Has.Count.GreaterThan(8),
				"El barrido dejo de encontrar la familia del sobre y no defiende nada.");

			Assert.Multiple(() =>
			{
				foreach (Type each in theFamily)
					Assert.That(
						each.GetMethods(
								BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
								| BindingFlags.DeclaredOnly)
							.Where(member => member.Name == "Describe"),
						Is.Empty,
						each.Name + " volvio a componer su propia frase. La redaccion vive donde se "
						+ "elige el idioma, y un dominio que redacta tiene que elegir uno.");
			});
		}

		// ===== 2. TODA COMBINACION SE DICE ENTERA, EN LOS DOS IDIOMAS =====================

		// EL PRODUCTO DE LOS CODIGOS, leidos del dominio. Son pocas familias y chicas, asi que el
		// producto entero cabe: cinco formas por tres clausulas por tres mitades por cinco llegadas
		// por dos distancias por dos publicados por dos idiomas.
		[Test]
		public void EveryCombinationOfTheCodesTheDomainCanChooseIsSaidWholeInBothLanguages()
		{
			string[] shapes = TheCodesDeclaredBy(typeof(EnvelopeAnswer));
			string[] clauses = TheCodesDeclaredBy(typeof(AnEnvelopeClause));
			string[] halves = TheCodesDeclaredBy(typeof(EnvelopeDistanceHalf));
			string[] arrivals = TheCodesDeclaredBy(typeof(EnvelopeArrival));
			string[] mismatched = TheCodesDeclaredBy(typeof(MismatchedDistance));
			string[] published = TheCodesDeclaredBy(typeof(WarrantyEnvelope));

			Assert.Multiple(() =>
			{
				Assert.That(shapes, Has.Length.EqualTo(5));
				Assert.That(clauses, Has.Length.EqualTo(3));
				Assert.That(halves, Has.Length.EqualTo(3));
				Assert.That(arrivals, Has.Length.EqualTo(5));
				Assert.That(mismatched, Has.Length.EqualTo(2));
				Assert.That(published, Has.Length.EqualTo(2));
			});

			Assert.Multiple(() =>
			{
				foreach (string shape in shapes)
					foreach (string clause in clauses)
						foreach (string half in halves)
							foreach (string arrival in arrivals)
								foreach (string whose in mismatched)
									foreach (string publishedShape in published)
										foreach (string language in BothLanguages)
											Assert.That(
												ASaid(shape, clause, half, arrival, whose,
													publishedShape, language),
												Does.Not.Contain("{"),
												language + " · " + shape + " / " + clause + " / "
												+ half + " / " + arrival + " / " + whose + " / "
												+ publishedShape
												+ ": sale con una llave abierta, asi que falta una "
												+ "clave o falta llenar un hueco.");
			});
		}

		// Y EL AUTOCONTROL DE ESE BARRIDO: un codigo que NINGUNA lista nombra si deja la llave
		// abierta. Sin este renglon, un renderizador que devolviera texto pase lo que pase daria el
		// mismo verde que uno correcto.
		[Test]
		public void ACodeTheCatalogueDoesNotKnowLeavesItsBraceOpen()
		{
			Assert.That(
				ASaid("SomethingNew", AnEnvelopeClause.Whole, EnvelopeDistanceHalf.Measured,
					EnvelopeArrival.TheTimeHalf, MismatchedDistance.TheOdometer,
					WarrantyEnvelope.Published, TheLanguage.OfTheRecord),
				Does.Contain("{envelope.reason.SomethingNew}"));
		}

		// ===== 3. LOS VALORES SON DEL REGISTRO Y NO SE TRADUCEN ===========================

		// LA MISMA RESPUESTA EN LOS DOS IDIOMAS: dos marcos distintos, los mismos hechos. La cifra
		// se escribe distinto -- el separador de miles es del idioma (regla 8) -- y los DIGITOS son
		// los mismos.
		[Test]
		public void TheSameAnswerNamesTheSameFactsInBothLanguages()
		{
			EnvelopeAnswer answer = profile.TheStandingAsOf(
				25000, DistanceUnitCode.UnitA, ObservedAt, InsideBothHalves);

			string inSpanish = TheSentenceTheDoorWouldCompose.TheWarrantyEnvelope(
				answer, TheLanguage.OfTheRecord);
			string inEnglish = TheSentenceTheDoorWouldCompose.TheWarrantyEnvelope(
				answer, TheLanguage.OfAnEnglishReader);

			Assert.That(inEnglish, Is.Not.EqualTo(inSpanish),
				"Los dos idiomas dieron la misma frase, asi que uno de los dos catalogos no se leyo "
				+ "y esta prueba no esta comparando nada.");

			Assert.Multiple(() =>
			{
				Assert.That(inSpanish, Does.Contain("60.000 km"));
				Assert.That(inEnglish, Does.Contain("60,000 km"),
					"La misma cifra, escrita como la escribe el otro idioma. La unidad NO se traduce: "
					+ "es como el registro la dice.");

				Assert.That(inSpanish, Does.Contain("1 de diciembre de 2023"));
				Assert.That(inEnglish, Does.Contain("1 December 2023"),
					"Y el mismo dia, con el orden que cada idioma le da.");

				foreach (string said in new[] { inSpanish, inEnglish })
					Assert.That(said, Does.Contain("35.010").Or.Contain("35,010"),
						"Lo que le queda a la mitad de distancia es el mismo numero en los dos: 60000 "
						+ "menos los 24990 recorridos desde la entrega.");
			});
		}

		// Y EL SOBRE PUBLICADO SOLO, POR LO MISMO: es la lectura que puede citar quien no tiene ni un
		// dato de ningun carro, y tambien se dice en los dos.
		[Test]
		public void ThePublishedEnvelopeIsAlsoSaidInBothLanguages()
		{
			string inSpanish = TheSentenceTheDoorWouldCompose.ThePublishedWarrantyEnvelope(
				profile, TheLanguage.OfTheRecord);
			string inEnglish = TheSentenceTheDoorWouldCompose.ThePublishedWarrantyEnvelope(
				profile, TheLanguage.OfAnEnglishReader);

			Assert.Multiple(() =>
			{
				Assert.That(
					inSpanish,
					Is.EqualTo("36 meses o 60.000 km, publicado el 1 de diciembre de 2023."));
				Assert.That(
					inEnglish, Is.EqualTo("36 months or 60,000 km, published on 1 December 2023."));
			});
		}

		// Y SIN SOBRE PUBLICADO TAMBIEN, que es la respuesta que no lleva ningun digito: un cero ahi
		// se leeria como un limite que alguien publico.
		[Test]
		public void AnUnpublishedEnvelopeIsSaidInBothLanguagesAndWithoutADigit()
		{
			Assert.Multiple(() =>
			{
				foreach (string language in BothLanguages)
					Assert.That(
						TheSentenceTheDoorWouldCompose.ThePublishedWarrantyEnvelope(
							WarrantyEnvelope.None, language),
						Does.Not.Match(@"\d"),
						language + ": un digito en la frase de un sobre que nadie publico.");
			});
		}

		private static readonly string[] BothLanguages =
			{ TheLanguage.OfTheRecord, TheLanguage.OfAnEnglishReader };

		private static string ASaid(
			string shape, string clause, string half, string arrival, string whose,
			string publishedShape, string language)
		{
			return TheWording.TheWarrantyEnvelope(
				Phrases.Of(language),
				shape, EnvelopeStanding.Inside.Name, DistanceUnit.UnitB.Name,
				clause, publishedShape, ThirtySixMonths, SixtyThousand, DistanceUnit.UnitA.Name,
				"2023-12-01", 12, ThirtySixMonths, half, new[] { 35010 }, new[] { SixtyThousand },
				DistanceUnit.UnitA.Name, DistanceUnit.UnitB.Name, whose, arrival,
				AnOdometerClause.Measured, 24990, DistanceUnit.UnitA.Name, "2026-01-01",
				TheDeliveryReading, DistanceUnit.UnitA.Name, "2024-01-01");
		}

		// LA FAMILIA ENTERA: los cinco tipos que componen la respuesta del sobre, con sus subclases.
		// Se leen del ensamblado, no de una lista, por la razon de C57.
		private static List<Type> TheEnvelopeFamily()
		{
			var roots = new[]
			{
				typeof(EnvelopeAnswer), typeof(RemainingEnvelope), typeof(WarrantyEnvelope),
				typeof(AnEnvelopeClause), typeof(AnOdometerClause),
			};

			var found = new List<Type>();

			foreach (Type each in typeof(EnvelopeAnswer).Assembly.GetTypes())
				foreach (Type root in roots)
					if (each == root || each.IsSubclassOf(root)) found.Add(each);

			return found;
		}

		private static string[] TheCodesDeclaredBy(Type holder)
		{
			return holder
				.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
				.Where(each => each.IsLiteral && each.FieldType == typeof(string))
				.Select(each => (string)each.GetRawConstantValue())
				.ToArray();
		}
	}
}
