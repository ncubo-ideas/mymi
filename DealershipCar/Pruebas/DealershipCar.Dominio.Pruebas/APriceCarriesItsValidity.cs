using System;
using System.Globalization;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L6 del brief, APriceCarriesItsValidity: "todo precio se dice con su vigencia y su
	// revision. Un precio vencido mostrado como vigente es una promesa que la sucursal nunca hizo."
	//
	// ES EL TEST 16 DE §11, y la trampa es la de siempre con las fechas: el numero se ve igual de
	// bien vencido que vigente. Un precio sin su vigencia al lado no se puede leer mal de una sola
	// forma -- se lee mal de la unica forma que importa, como el precio de hoy.
	//
	// LA VIGENCIA SE MIDE CONTRA UN INSTANTE RECIBIDO (D1 del plan): el dominio no lee ningun
	// reloj, asi que "hoy" entra por parametro. Sin ese instante la ley no se puede honrar: no hay
	// forma de decir "vencido" sin saber respecto a cuando.
	//
	// LA CLAVE DE UN PRECIO SON TRES COSAS: la operacion, la distancia del hito Y SU UNIDAD. Dos
	// hitos en unidades distintas no son el mismo hito, porque las unidades no se convierten (L8).
	// Por eso los precios son una LISTA que se recorre con una busqueda con nombre y no un mapa con
	// clave: una clave de tres partes seria una tupla, y una tupla es igualdad de valor con
	// parentesis (E23).
	[TestFixture]
	public sealed class APriceCarriesItsValidity
	{
		private const string TheModel = "SM-SEDAN-2025";
		private const string TheRevision = "REV-2025-01";
		private const string TheNextRevision = "REV-2025-02";
		private const string TheDraft = "REV-2025-99";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);
		private static readonly DateTime ValidFrom = new DateTime(2025, 1, 1, 0, 0, 0);
		private static readonly DateTime ValidUntil = new DateTime(2025, 12, 31, 0, 0, 0);
		private static readonly DateTime WhileItIsInForce = new DateTime(2025, 6, 15, 0, 0, 0);
		private static readonly DateTime AfterItPassed = new DateTime(2026, 2, 1, 0, 0, 0);
		private static readonly DateTime BeforeItStarted = new DateTime(2024, 12, 1, 0, 0, 0);

		private ModelSpecification specification;

		// La hoja sellada que las lecturas usan, Y un borrador aparte abierto al lado. El borrador no
		// es comodidad: las pruebas de RECHAZO tienen que publicar en una revision que acepte
		// contenido, porque si publicaran en la sellada reventaria el sello y un Assert.Throws
		// pasaria sin haber tocado nunca la guarda que la prueba dice defender. Esa es exactamente
		// la forma en que cuatro de estas pruebas sobrevivieron a su primer falsificador.
		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheRevision, PublishedAt);
			TheInspectionPriceEnters();
			specification.SealRevision(TheRevision);
			specification.OpenRevision(TheDraft, PublishedAt.AddDays(1));
		}

		[Test]
		public void TheSentenceSaysTheAmountTheCurrencyTheValidityAndThenItsRevision()
		{
			Assert.That(
				specification.DescribeServicePrice(
					OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA, WhileItIsInForce),
				Is.EqualTo(
					"Revisión a los 10000 km: ₡120.50, vigente del 2025-01-01 al 2025-12-31. "
					+ "Al 2025-06-15 esa vigencia está rigiendo. "
					+ "Publicado en la revisión REV-2025-01."));
		}

		// TEST 16 DE §11. El precio se contesta CON su vigencia, y la frase dice que ya paso. Nunca
		// se muestra como vigente y nunca se calla.
		[Test]
		public void APriceWhoseValidityHasPassedIsAnsweredWithItsValidityAndNotAsCurrent()
		{
			string answer = specification.DescribeServicePrice(
				OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA, AfterItPassed);

			Assert.That(answer, Does.Contain("vigente del 2025-01-01 al 2025-12-31"),
				"La vigencia va en la respuesta para que quien la lea vea contra que la esta "
				+ "leyendo.");
			Assert.That(answer, Does.Contain("no es el vigente"),
				"Y lo dice con palabras. Un precio vencido que solo trae su fecha obliga al de "
				+ "afuera a hacer la comparacion, que es la que este modulo ya hizo.");
			Assert.That(answer, Does.Contain("120.50"),
				"El precio publicado sigue saliendo: vencido no es inexistente, y la persona "
				+ "necesita saber cual era.");
		}

		// La otra punta de la misma ley: todavia no empieza tampoco es vigente.
		[Test]
		public void APriceWhoseValidityHasNotStartedIsNotCurrentEither()
		{
			string answer = specification.DescribeServicePrice(
				OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA, BeforeItStarted);

			Assert.That(answer, Does.Contain("no es el vigente"));
			Assert.That(answer, Does.Contain("vigente del 2025-01-01"));
		}

		// El monto sale COMO SU PROPIA MONEDA LO DICE -- con el signo delante y los decimales que esa
		// moneda tiene -- y la moneda no se convierte nunca. Una moneda convertida es un precio que la
		// sucursal no puso.
		//
		// LA FRASE CAMBIO EN CL-2 y el cambio es visible: antes decia "120.50 CRC" y ahora dice
		// "₡120.50". El monto y la moneda dejaron de ser dos campos sueltos en el precio y son un solo
		// Money, que es quien dice la frase (DEC-85, DEC-87).
		[Test]
		public void TheAmountAndTheCurrencyAreAnsweredAsPublished()
		{
			string answer = specification.DescribeServicePrice(
				OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA, WhileItIsInForce);

			Assert.That(answer, Does.Contain("₡120.50"));
		}

		// EL HITO ES DISTANCIA MAS UNIDAD. El mismo numero en la otra unidad no es el mismo hito, y
		// contestar el precio de uno cuando preguntaron por el otro seria convertir sin decirlo.
		[Test]
		public void TheSameNumberInTheOtherUnitIsADifferentMilestone()
		{
			Assert.That(
				specification.IsServicePricePublished(
					OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA),
				Is.True);
			Assert.That(
				specification.IsServicePricePublished(
					OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitB),
				Is.False,
				"Ley L8: dos hitos en unidades distintas no son comparables, asi que no son el "
				+ "mismo hito. Contestar que si seria una conversion escrita con un signo igual.");
		}

		// Y otro hito de la misma operacion es otro precio.
		[Test]
		public void AnotherMilestoneOfTheSameOperationIsAnotherPrice()
		{
			Assert.That(
				specification.IsServicePricePublished(
					OperationKindCode.Inspection, 20000, DistanceUnitCode.UnitA),
				Is.False);
			Assert.That(
				specification.ReadServicePriceRevision(
					OperationKindCode.Inspection, 20000, DistanceUnitCode.UnitA),
				Is.EqualTo(""));
		}

		// LEY L2 para los precios: lo no publicado se dice con palabras, sin numero adentro. Un
		// monto de cero ahi se leeria como un servicio gratis.
		[Test]
		public void APriceThatWasNeverPublishedAnswersNotPublished()
		{
			string answer = specification.DescribeServicePrice(
				OperationKindCode.Repair, 10000, DistanceUnitCode.UnitA, WhileItIsInForce);

			Assert.That(answer, Does.Contain("reparación"));
			Assert.That(answer, Does.Not.Contain("120.50"));
			Assert.That(HayAlgunDigito(answer), Is.False,
				"Un cero adentro de una ausencia de precio se lee como gratis, que es la peor "
				+ "lectura posible de las tres que L2 prohibe.");
		}

		// LOS CUATRO RECHAZOS PUBLICAN EN EL BORRADOR y afirman QUE dice el mensaje. Las dos cosas
		// son la misma leccion: un Assert.Throws pelado se contenta con cualquier excepcion, asi que
		// una prueba de rechazo que no nombre su motivo pasa aunque la guarda que dice defender no
		// exista.
		[Test]
		public void APriceWithoutACurrencyIsNotAPublishedPrice()
		{
			DomainException error = Assert.Throws<DomainException>(
				() => specification.PublishServicePrice(
					TheDraft, OperationKindCode.Repair, 10000, DistanceUnitCode.UnitA,
					90m, "  ", ValidFrom, ValidUntil));

			Assert.That(error.Message, Does.Contain("moneda"),
				"El mensaje nombra la moneda que falta, no otra cosa que tambien revienta.");
			Assert.That(error.Message, Does.Not.Contain("L6"),
				"C45: el mensaje dice la regla en palabras, no cita un documento.");
		}

		// LOS DOS RECHAZOS DEL PAR CAMBIARON DE DUENO EN CL-2, y siguen siendo el mismo rechazo. Un
		// monto negativo lo niega Money y una moneda en blanco la niega Currency.OfPublished -- los dos
		// tipos que TIENEN el valor -- en vez de una guarda del precio detras de un valor que el precio
		// ya no ve. Es la forma del hermano: las guardas de valor del intervalo viven en
		// PublishedInterval y no en ServiceIntervalEntry.
		//
		// LO QUE SIGUE AFIRMADO ES LO QUE LE IMPORTA A QUIEN PUBLICA: que el rechazo llega, que dice
		// su motivo en palabras, y que es DomainException -- cualquier otro tipo se escapa de Apply y
		// para el bloque entero del refresco (C22, C36, C37).
		[Test]
		public void APriceWithANegativeAmountIsNotAPublishedPrice()
		{
			DomainException error = Assert.Throws<DomainException>(
				() => specification.PublishServicePrice(
					TheDraft, OperationKindCode.Repair, 10000, DistanceUnitCode.UnitA,
					-1m, "CRC", ValidFrom, ValidUntil));

			Assert.That(error.Message, Does.Contain("negativo"));
		}

		// Una vigencia que termina antes de empezar no es una vigencia: no hay ningun instante en
		// que ese precio sea el de la sucursal, asi que publicarlo dejaria un precio que nunca es.
		[Test]
		public void AValidityThatEndsBeforeItStartsIsRefused()
		{
			DomainException error = Assert.Throws<DomainException>(
				() => specification.PublishServicePrice(
					TheDraft, OperationKindCode.Repair, 10000, DistanceUnitCode.UnitA,
					90m, "CRC", ValidUntil, ValidFrom));

			Assert.That(error.Message, Does.Contain("vigencia"));
		}

		[TestCase(0)]
		[TestCase(-1)]
		public void AMilestoneWithoutADistanceIsRefused(int milestone)
		{
			DomainException error = Assert.Throws<DomainException>(
				() => specification.PublishServicePrice(
					TheDraft, OperationKindCode.Repair, milestone, DistanceUnitCode.UnitA,
					90m, "CRC", ValidFrom, ValidUntil));

			Assert.That(error.Message, Does.Contain("hito"));
		}

		// PUBLICAR Y PREGUNTAR TIENEN POLARIDADES OPUESTAS, y esta es la unica clave de la rebanada
		// donde se ven las dos: publicar un hito que no es una distancia positiva REVIENTA -- no
		// nombra ningun servicio --, pero PREGUNTAR por uno contesta que no hay nada publicado ahi,
		// que es la verdad. La distancia de un hito es un rango denso y no un miembro de un
		// conjunto: afuera del rango la respuesta vacia existe, asi que la lectura sigue siendo
		// total como todas las de items.
		[TestCase(0)]
		[TestCase(-1)]
		public void AMilestoneThatIsNotPositiveIsAskedAboutWithoutThrowing(int milestone)
		{
			Assert.That(
				specification.IsServicePricePublished(
					OperationKindCode.Inspection, milestone, DistanceUnitCode.UnitA),
				Is.False);
			Assert.That(
				specification.ReadServicePriceRevision(
					OperationKindCode.Inspection, milestone, DistanceUnitCode.UnitA),
				Is.EqualTo(""));
			Assert.That(
				specification.DescribeServicePrice(
					OperationKindCode.Inspection, milestone, DistanceUnitCode.UnitA,
					WhileItIsInForce),
				Does.Contain("revisión"));
		}

		// Dentro de UN borrador, la ultima publicacion de un precio es la que queda. Es la unica de
		// las cinco correcciones-antes-de-sellar de esta tarea que no cae en un diccionario: los
		// precios viven en una lista, asi que el reemplazo se busca y se pone en su sitio. Si en vez
		// de reemplazar se agregara, la lista tendria dos precios del mismo hito y contestaria el
		// que estuviera primero -- o sea el viejo.
		[Test]
		public void WithinOneDraftTheLastPublicationOfAPriceIsTheOneThatStands()
		{
			specification.OpenRevision(TheNextRevision, PublishedAt.AddDays(30));
			specification.PublishServicePrice(
				TheNextRevision, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
				120.50m, "CRC", ValidFrom, ValidUntil);
			specification.PublishServicePrice(
				TheNextRevision, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
				145.00m, "CRC", ValidFrom, ValidUntil);
			specification.SealRevision(TheNextRevision);

			string answer = specification.DescribeServicePrice(
				OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA, WhileItIsInForce);

			Assert.That(answer, Does.Contain("145.00"));
			Assert.That(answer, Does.Not.Contain("120.50"));
		}

		// Y ENTRE REVISIONES manda la ultima sellada que lo publico: la revision 2 sube el precio y
		// la respuesta sale de ella, nombrandola. Recorrer al reves -- que conteste la primera --
		// dejaria a la gente citando el precio del ano pasado con procedencia firmada.
		[Test]
		public void ALaterSealedRevisionAnswersForThePriceItRepublished()
		{
			specification.OpenRevision(TheNextRevision, PublishedAt.AddDays(30));
			specification.PublishServicePrice(
				TheNextRevision, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
				145.00m, "CRC", ValidFrom, ValidUntil);
			specification.SealRevision(TheNextRevision);

			Assert.That(
				specification.ReadServicePriceRevision(
					OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA),
				Is.EqualTo(TheNextRevision));
			Assert.That(
				specification.DescribeServicePrice(
					OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA, WhileItIsInForce),
				Does.Contain("145.00"));
		}

		// EL NUMERO SE DICE IGUAL EN TODA MAQUINA. Una respuesta que alguien guardo en una maquina
		// con coma decimal tiene que ser la misma que otro lee en una con punto: si no, dos personas
		// con la misma revision leen dos precios distintos y ninguna de las dos lo sabe.
		[Test]
		public void TheAmountReadsTheSameOnEveryMachine()
		{
			CultureInfo laDeAntes = CultureInfo.CurrentCulture;

			try
			{
				CultureInfo.CurrentCulture = new CultureInfo("de-DE");

				Assert.That(
					specification.DescribeServicePrice(
						OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
						WhileItIsInForce),
					Does.Contain("₡120.50"),
					"Bajo una cultura con coma decimal el monto sigue saliendo con punto. Si "
					+ "cambiara, la frase dependeria de la maquina que la compuso.");
			}
			finally
			{
				CultureInfo.CurrentCulture = laDeAntes;
			}
		}

		private void TheInspectionPriceEnters()
		{
			specification.PublishServicePrice(
				TheRevision, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
				120.50m, "CRC", ValidFrom, ValidUntil);
		}

		private static bool HayAlgunDigito(string frase)
		{
			foreach (char letra in frase) if (char.IsDigit(letra)) return true;
			return false;
		}
	}
}
