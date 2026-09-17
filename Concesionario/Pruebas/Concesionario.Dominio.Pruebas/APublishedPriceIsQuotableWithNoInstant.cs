using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// E48: una respuesta compuesta tiene que exponer TAMBIEN cada mitad que compone, o tener una de
	// las dos se vuelve PRECONDICION de la otra. El precio se contestaba de una sola forma -- con la
	// vigencia medida contra un instante recibido --, asi que quien no tenia un instante que aportar
	// no podia citar ni el precio publicado, que no depende de ningun instante.
	//
	// LA MITAD PUBLICADA YA EXISTIA Y NADIE LA PEDIA. ARevisionAppliesForward(clase, hito) compone la
	// respuesta sin instante desde antes de esta tarea, y dos de las tres lecturas llaveadas ya pasan
	// por ella: IsServicePricePublished le pide IsPublished y ReadServicePriceRevision le pide
	// RevisionCode. Nadie le pedia Describe. El hermano que faltaba es una linea que reusa un
	// resolvedor que ya estaba, no una segunda composicion.
	//
	// Y LA MITAD SIN INSTANTE NO PUEDE TRAER UN VEREDICTO DE VIGENCIA. Es la unica forma de romper
	// esto sin que nada quede vacio: un instante inventado adentro dejaria la frase igual de bien
	// formada y diciendo, de un precio, que rige hoy -- cuando nadie pregunto por hoy.
	[TestFixture]
	public sealed class APublishedPriceIsQuotableWithNoInstant
	{
		private const string TheModel = "YARIS-2019";
		private const string TheRevision = "REV-2025-01";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);
		private static readonly DateTime ValidFrom = new DateTime(2025, 1, 1, 0, 0, 0);
		private static readonly DateTime ValidUntil = new DateTime(2025, 12, 31, 0, 0, 0);

		private ModelSpecification specification;

		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheRevision, PublishedAt);
			specification.PublishServicePrice(
				TheRevision, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
				120.50m, "CRC", ValidFrom, ValidUntil);
			specification.SealRevision(TheRevision);
		}

		// La frase entera, por literal: el precio con su moneda, la vigencia entre la que rige, y la
		// revision. Y nada mas.
		[Test]
		public void ThePublishedHalfSaysThePriceItsValidityAndItsRevision()
		{
			Assert.That(
				TheSentenceTheDoorWouldCompose.ThePublishedServicePrice(
					specification, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
					TheLanguage.OfTheRecord),
				Is.EqualTo(
					"Revisión a los 10.000 km: ₡120,50, vigente del 1 de enero de 2025 al 31 de diciembre de 2025. "
					+ "Publicado en la revisión REV-2025-01."));
		}

		// NI UN VEREDICTO DE VIGENCIA. La forma fuerte y la que atrapa el instante inventado: la
		// clausula con instante siempre empieza por "Al ", asi que su ausencia se afirma por ahi y no
		// por la ausencia de una fecha cualquiera -- la vigencia trae dos fechas y las dos son suyas.
		[Test]
		public void ThePublishedHalfCarriesNoVerdictAboutAnyInstant()
		{
			string said = TheSentenceTheDoorWouldCompose.ThePublishedServicePrice(
				specification, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
				TheLanguage.OfTheRecord);

			Assert.That(said, Does.Not.Contain(" Al "));
			Assert.That(said, Does.Not.Contain("está rigiendo"));
			Assert.That(said, Does.Not.Contain("no es el vigente"));
			Assert.That(said, Does.Contain("vigente del 1 de enero de 2025 al 31 de diciembre de 2025"),
				"La vigencia SI va: es lo que se publico. Lo que no va es el veredicto sobre un "
				+ "instante que nadie pregunto.");
		}

		// EL HERMANO NO RECIBE NINGUN INSTANTE, y se afirma por reflexion: una firma con un DateTime
		// opcional o un instante colado por otro nombre dejaria la ley cierta en la frase y falsa en
		// el contrato.
		[Test]
		public void TheSiblingTakesNoInstantAtAll()
		{
			MethodInfo[] found = typeof(ModelSpecification)
				.GetMethods(BindingFlags.Instance | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(member => member.Name == "ThePublishedServicePriceAnswer")
				.ToArray();

			Assert.That(found, Has.Length.EqualTo(1));
			Assert.That(
				found[0].GetParameters().Where(each => each.ParameterType == typeof(DateTime)),
				Is.Empty);
		}

		// Y SIGUE SIENDO TOTAL. Lo no publicado se dice con palabras y sin ningun numero adentro: un
		// cero en una ausencia de precio se lee como un servicio incluido, que es la peor de las tres
		// lecturas que la ausencia con palabras existe para evitar.
		[Test]
		public void WhatWasNeverPublishedIsSaidWithWordsAndNoNumber()
		{
			string said = TheSentenceTheDoorWouldCompose.ThePublishedServicePrice(
				specification, OperationKindCode.Repair, 10000, DistanceUnitCode.UnitA,
				TheLanguage.OfTheRecord);

			Assert.That(said, Does.Contain("reparación"));
			Assert.That(said, Does.Contain("no se ha publicado nada para este modelo"));
			Assert.That(said.Any(char.IsDigit), Is.False);
		}

		// Y UN HITO QUE NO ES UNA DISTANCIA POSITIVA SE PREGUNTA SIN REVENTAR, igual que en las tres
		// lecturas llaveadas: publicar uno asi revienta, preguntar por uno contesta que no hay nada
		// publicado ahi, que es la verdad.
		[TestCase(0)]
		[TestCase(-1)]
		public void AMilestoneThatIsNotPositiveIsAskedAboutWithoutThrowing(int milestone)
		{
			Assert.That(
				TheSentenceTheDoorWouldCompose.ThePublishedServicePrice(
					specification, OperationKindCode.Inspection, milestone, DistanceUnitCode.UnitA,
					TheLanguage.OfTheRecord),
				Does.Contain("revisión"));
		}

		// LOS DOS HERMANOS DICEN LA MISMA MITAD PUBLICADA. La compuesta contiene a la publicada
		// palabra por palabra menos la clausula de procedencia, que las dos pegan al final: es lo que
		// hace que una pantalla que dibuje las dos imprima la misma frase dos veces, y por eso la
		// decision de que renderizar es de MS-7 y no de aca.
		[Test]
		public void TheComposedAnswerContainsThePublishedHalfVerbatim()
		{
			string published = TheSentenceTheDoorWouldCompose.ThePublishedServicePrice(
				specification, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
				TheLanguage.OfTheRecord);
			string composed = TheSentenceTheDoorWouldCompose.TheServicePrice(
				specification, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
				new DateTime(2025, 6, 15, 0, 0, 0), TheLanguage.OfTheRecord);

			Assert.That(
				composed,
				Does.StartWith("Revisión a los 10.000 km: ₡120,50, vigente del 1 de enero de 2025 al "
					+ "31 de diciembre de 2025."));
			Assert.That(
				published,
				Does.StartWith("Revisión a los 10.000 km: ₡120,50, vigente del 1 de enero de 2025 al "
					+ "31 de diciembre de 2025."));
			Assert.That(composed, Is.Not.EqualTo(published),
				"Y no son la misma respuesta: la compuesta agrega el veredicto sobre el instante "
				+ "preguntado, que es la mitad recibida.");
		}
	}
}
