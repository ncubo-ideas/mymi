using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LEY L8 de SLICE-TheMaintenanceScreen.md: "los precios publicados de la revision que rige son
	// enumerables como filas. NINGUN llamador aporta un hito que no aprendio del dominio."
	//
	// ES EL TEST 14 DE §11, y la trampa que L8 nombra no es un numero equivocado: es una pantalla que
	// hardcodea 10.000 y no muestra NADA el dia que una revision publica el precio a los 15.000. El
	// defecto se ve como una tarjeta vacia, no como un error.
	//
	// LA CLAVE DE UN PRECIO SON TRES COSAS (la operacion, la distancia del hito y su unidad), asi que
	// para enumerar hay que sacar la clave de donde vive. Vive en ServicePriceEntry detras de IsFor, y
	// la lista de entradas vive en PublishedRevision: por eso la clave la compone la ENTRADA (es quien
	// la tiene) y la enumeracion la hace la REVISION (es quien tiene la lista). Ninguna de las dos
	// entrega su estado -- la revision entrega claves y no entradas, y la entrada entrega una clave y
	// no dos campos.
	//
	// G4: las filas usan la forma CON INSTANTE, asi que un precio con la vigencia pasada dice que
	// paso. El instante decide la FRASE y no el conjunto de filas -- sellada es un estado y no un
	// instante --, y eso es lo que mantiene a la enumeracion de acuerdo con las tres lecturas
	// llaveadas, que tampoco filtran revisiones por instante.
	[TestFixture]
	public sealed class APriceIsFoundByEnumerationAndNotByGuessing
	{
		private const string TheModel = "YARIS-2019";
		private const string TheRevision = "REV-2025-01";
		private const string TheDraft = "REV-2025-99";

		private static readonly DateTime PublishedAt = new DateTime(2025, 3, 14, 10, 0, 0);
		private static readonly DateTime ValidFrom = new DateTime(2025, 1, 1, 0, 0, 0);
		private static readonly DateTime ValidUntil = new DateTime(2025, 12, 31, 0, 0, 0);
		private static readonly DateTime TheFluidPriceEndsOn = new DateTime(2025, 6, 30, 0, 0, 0);

		private static readonly DateTime WhileBothAreInForce = new DateTime(2025, 6, 15, 0, 0, 0);
		private static readonly DateTime AfterTheFluidPriceLapsed = new DateTime(2025, 9, 1, 0, 0, 0);
		private static readonly DateTime BeforeAnyValidityStarted = new DateTime(2024, 12, 1, 0, 0, 0);
		private static readonly DateTime BeforeTheRevisionWasPublished =
			new DateTime(2025, 2, 1, 0, 0, 0);

		private ModelSpecification specification;

		// La hoja sellada con DOS precios en hitos distintos, y un borrador aparte: publicar en la
		// sellada reventaria el sello, asi que una prueba de rechazo que publicara ahi pasaria sin
		// tocar la guarda que dice defender.
		[SetUp]
		public void Setup()
		{
			specification = new ModelSpecification(TheModel);
			specification.OpenRevision(TheRevision, PublishedAt);
			specification.PublishServicePrice(
				TheRevision, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitA,
				120.50m, "CRC", ValidFrom, ValidUntil);
			specification.PublishServicePrice(
				TheRevision, OperationKindCode.FluidChange, 20000, DistanceUnitCode.UnitA,
				340.00m, "CRC", ValidFrom, TheFluidPriceEndsOn);
			specification.SealRevision(TheRevision);
			specification.OpenRevision(TheDraft, PublishedAt.AddDays(1));
		}

		// TEST 14 DE §11: dos precios en hitos distintos salen como DOS filas, cada una nombrando su
		// hito, y NADIE dijo un hito para preguntar.
		[Test]
		public void TwoPricesAtDifferentMilestonesComeBackAsTwoRows()
		{
			Assert.That(specification.HasAnyPublishedServicePrice(), Is.True);
			Assert.That(specification.PublishedServicePriceCount(), Is.EqualTo(2));

			Assert.That(
				specification.PublishedPriceMilestonesAsOf(WhileBothAreInForce),
				Is.EquivalentTo(new[] { "10000 km", "20000 km" }),
				"Cada fila nombra SU hito, que es lo que releva al llamador de aportar uno.");
		}

		// La mitad direccionable de la fila es la operacion, por el NOMBRE del miembro: es lo que un
		// llamador puede volver a preguntar. El hito ya no es direccionable a proposito -- despues de
		// esta lectura nadie tiene que nombrarlo -- y sale como se dice, con la unidad en palabras.
		[Test]
		public void TheOperationColumnNamesTheMemberAndTheMilestoneColumnSaysItInWords()
		{
			string[] operations = specification.PublishedPriceOperationsAsOf(WhileBothAreInForce);

			Assert.That(operations, Is.EquivalentTo(new[] { "Inspection", "FluidChange" }));
			Assert.That(operations, Has.None.Contains("Revisión"));
			Assert.That(
				specification.PublishedPriceMilestonesAsOf(WhileBothAreInForce),
				Has.All.Contains(" km"),
				"El hito sale con la unidad dicha. La forma direccionable necesitaria el codigo de la "
				+ "unidad, que es una tercera copia de un mapeo que ya vive en dos sitios.");
		}

		// NINGUNA de las lecturas nuevas pide un hito. Es la mitad de L8 que un test de valores no
		// puede ver: un hito por parametro se veria bien en toda respuesta y seguiria siendo el
		// numero del fabricante metido en una pantalla.
		[Test]
		public void NoReadOfThePricesAsksForAMilestone()
		{
			string[] theSixReads =
			{
				"HasAnyPublishedServicePrice", "PublishedServicePriceCount",
				"PublishedPriceOperationsAsOf", "PublishedPriceMilestonesAsOf",
				"DescribeEachPublishedPriceAsOf", "PublishedPriceRevisionsAsOf",
			};

			foreach (string each in theSixReads)
			{
				MethodInfo[] found = typeof(ModelSpecification)
					.GetMethods(BindingFlags.Instance | BindingFlags.Public
						| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
					.Where(member => member.Name == each)
					.ToArray();

				Assert.That(found, Has.Length.EqualTo(1), each + " no existe o esta sobrecargada.");

				string[] asked = found[0].GetParameters().Select(each2 => each2.Name).ToArray();

				Assert.That(asked, Has.None.Contains("milestone"),
					each + " pide un hito: " + string.Join(", ", asked)
					+ ". L8 es que el llamador no aporta un hito que no aprendio del dominio, y la "
					+ "enumeracion existe justamente para que no tenga que aportarlo.");
			}
		}

		// REGLA 4 DE LA CONVENCION: el total del dominio y la suma de las filas concuerdan. Aca la
		// suma es el largo de cada columna, y las cuatro son paralelas porque las cuatro abren con la
		// misma enumeracion.
		[Test]
		public void TheCountAgreesWithTheRowsAndTheFourColumnsAreParallel()
		{
			int count = specification.PublishedServicePriceCount();

			Assert.That(
				specification.PublishedPriceOperationsAsOf(WhileBothAreInForce),
				Has.Length.EqualTo(count));
			Assert.That(
				specification.PublishedPriceMilestonesAsOf(WhileBothAreInForce),
				Has.Length.EqualTo(count));
			Assert.That(
				specification.DescribeEachPublishedPriceAsOf(WhileBothAreInForce),
				Has.Length.EqualTo(count));
			Assert.That(
				specification.PublishedPriceRevisionsAsOf(WhileBothAreInForce),
				Has.Length.EqualTo(count));
		}

		// Y LAS COLUMNAS NO ESTAN CRUZADAS. La forma fuerte: la frase de la fila del hito de 20000
		// nombra el fluido y el monto del fluido, no los de la revision. Dos valores pelados uno al
		// lado del otro pasarian con las columnas cruzadas.
		[Test]
		public void EveryRowSaysItselfWhole()
		{
			string[] milestones = specification.PublishedPriceMilestonesAsOf(WhileBothAreInForce);
			string[] said = specification.DescribeEachPublishedPriceAsOf(WhileBothAreInForce);

			int theFluidRow = Array.IndexOf(milestones, "20000 km");

			Assert.That(theFluidRow, Is.GreaterThanOrEqualTo(0));
			Assert.That(said[theFluidRow], Does.Contain("Cambio de fluido a los 20000 km: ₡340.00"));
			Assert.That(said[theFluidRow], Does.Not.Contain("120.50"));
		}

		// G4, PRIMERA MITAD: en un instante en que la vigencia de una fila ya paso, ESA fila lo dice y
		// la otra no. Medido, no supuesto: el brief solo dice que las filas usan la forma con
		// instante.
		[Test]
		public void AtAnInstantWhereAValidityLapsedThatRowSaysSoAndTheOtherDoesNot()
		{
			string[] milestones =
				specification.PublishedPriceMilestonesAsOf(AfterTheFluidPriceLapsed);
			string[] said = specification.DescribeEachPublishedPriceAsOf(AfterTheFluidPriceLapsed);

			int theFluidRow = Array.IndexOf(milestones, "20000 km");
			int theInspectionRow = Array.IndexOf(milestones, "10000 km");

			Assert.That(said[theFluidRow], Does.Contain("esa vigencia ya pasó"));
			Assert.That(said[theFluidRow], Does.Contain("₡340.00"),
				"Vencido no es inexistente: el monto publicado sigue saliendo.");
			Assert.That(said[theInspectionRow], Does.Contain("esa vigencia está rigiendo"));
		}

		// G4, SEGUNDA MITAD: antes de que ninguna vigencia empiece las filas SIGUEN SALIENDO y dicen
		// que todavia no rigen. Una fila que desapareciera ahi contradiria a la lectura llaveada, que
		// contesta "publicado, y no vigente".
		[Test]
		public void BeforeAnyValidityStartedTheRowsStillComeAndSayTheyAreNotInForce()
		{
			Assert.That(specification.PublishedServicePriceCount(), Is.EqualTo(2));
			Assert.That(
				specification.DescribeEachPublishedPriceAsOf(BeforeAnyValidityStarted),
				Has.All.Contains("todavía no ha empezado"));
		}

		// Y EL CONJUNTO DE FILAS NO DEPENDE DEL INSTANTE. Sellada es un ESTADO: la enumeracion no
		// filtra revisiones por el instante preguntado, ni siquiera por uno anterior a la publicacion
		// de la revision. Es lo que deja que el conteo y la presencia no reciban instante y sigan
		// concordando con las filas.
		[Test]
		public void TheRowSetIsTheSameAtEveryInstantAndOnlyTheSentenceMoves()
		{
			string[] atFirst = specification.PublishedPriceMilestonesAsOf(WhileBothAreInForce);

			Assert.That(
				specification.PublishedPriceMilestonesAsOf(BeforeTheRevisionWasPublished),
				Is.EqualTo(atFirst));
			Assert.That(
				specification.PublishedPriceMilestonesAsOf(AfterTheFluidPriceLapsed),
				Is.EqualTo(atFirst));
			Assert.That(
				specification.DescribeEachPublishedPriceAsOf(BeforeTheRevisionWasPublished),
				Is.Not.EqualTo(specification.DescribeEachPublishedPriceAsOf(AfterTheFluidPriceLapsed)),
				"Si la frase tampoco se moviera, el instante no estaria haciendo nada y G4 no se "
				+ "estaria honrando.");
		}

		// LA AUSENCIA SE PREGUNTA Y NO SE CALLA, y NO tiene un tipo propio. La decision esta escrita:
		// esta especificacion tiene UNA sola clase de ausencia (AnUnpublishedSpecification, alcanzada
		// por UnpublishedIsAnAnswer), y ninguna lectura de §5.3 contesta con una frase de ausencia
		// para el conjunto de filas. Un segundo tipo de ausencia aca seria una segunda CLASE de
		// ausencia en el agregado que a proposito tiene una.
		[Test]
		public void NothingPublishedIsAskedAboutAndNoAbsenceTypeWasInvented()
		{
			ModelSpecification empty = new ModelSpecification(TheModel);

			Assert.That(empty.HasAnyPublishedServicePrice(), Is.False);
			Assert.That(empty.PublishedServicePriceCount(), Is.EqualTo(0));
			Assert.That(empty.PublishedPriceOperationsAsOf(WhileBothAreInForce), Is.Empty);
			Assert.That(empty.PublishedPriceMilestonesAsOf(WhileBothAreInForce), Is.Empty);
			Assert.That(empty.DescribeEachPublishedPriceAsOf(WhileBothAreInForce), Is.Empty);
			Assert.That(empty.PublishedPriceRevisionsAsOf(WhileBothAreInForce), Is.Empty);

			Assert.That(
				typeof(ModelSpecification).Assembly.GetTypes()
					.Where(each => each.Name == "NoPublishedPrice")
					.ToArray(),
				Is.Empty,
				"El canon nombra NoPublishedPrice y NO se construyo, a proposito: ninguna lectura "
				+ "contesta con el, y quien agregue una va a ver este test rojo primero.");
		}

		// Un borrador no cuenta. Publicar un precio y no sellar deja la enumeracion como estaba: es el
		// mismo filtro que usan las tres lecturas llaveadas, y aca es lo que impide que una hoja a
		// medio cargar se cite con procedencia firmada.
		[Test]
		public void APriceInsideADraftIsNotEnumerated()
		{
			specification.PublishServicePrice(
				TheDraft, OperationKindCode.Repair, 30000, DistanceUnitCode.UnitA,
				900.00m, "CRC", ValidFrom, ValidUntil);

			Assert.That(specification.PublishedServicePriceCount(), Is.EqualTo(2));
			Assert.That(
				specification.PublishedPriceMilestonesAsOf(WhileBothAreInForce),
				Does.Not.Contain("30000 km"));
		}

		// EL MISMO NUMERO EN LA OTRA UNIDAD ES OTRO HITO, tambien al enumerar: dos filas, no una.
		[Test]
		public void TheSameNumberInTheOtherUnitIsAnotherRow()
		{
			specification.PublishServicePrice(
				TheDraft, OperationKindCode.Inspection, 10000, DistanceUnitCode.UnitB,
				75.00m, "USD", ValidFrom, ValidUntil);
			specification.SealRevision(TheDraft);

			Assert.That(specification.PublishedServicePriceCount(), Is.EqualTo(3));
			Assert.That(
				specification.PublishedPriceMilestonesAsOf(WhileBothAreInForce),
				Is.EquivalentTo(new[] { "10000 mi", "10000 km", "20000 km" }));
		}

		// NINGUN PORTADOR DE PART C CRUZA LA SUPERFICIE DE LA ESPECIFICACION.
		// NoOdometerCarrierCrossesTheFacade es especifico de sus tipos y no habria visto uno nuevo,
		// asi que este censo se escribe. Es lo que mantiene PRIVADOS a los dos resolvedores: uno
		// internal es un miembro que un script puede nombrar, y ahi el portador cruza.
		[Test]
		public void NoPriceCarrierCrossesTheSpecificationSurface()
		{
			List<string> exposed = typeof(ModelSpecification)
				.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(member => member.IsPublic || member.IsAssembly)
				.Where(member => member.ReturnType == typeof(APublishedPriceRow)
					|| member.ReturnType == typeof(APublishedPriceRow[])
					|| member.ReturnType == typeof(List<APublishedPriceRow>)
					|| member.ReturnType == typeof(APublishedPriceKey)
					|| member.ReturnType == typeof(APublishedPriceKey[])
					|| member.ReturnType == typeof(List<APublishedPriceKey>))
				.Select(member => member.Name)
				.ToList();

			Assert.That(exposed, Is.Empty,
				"La especificacion expone un portador de part C: " + string.Join(", ", exposed)
				+ ". El perform no lo puede llevar de vuelta, y quien lo reciba puede volver a "
				+ "derivar lo que el agregado ya compuso.");
		}

		// NI UN NUMERO SALE DE LA FILA. Las cuatro columnas son texto y la fila no entrega ni el
		// monto, ni la distancia del hito, ni un instante: un monto suelto al lado de otro es una
		// suma esperando a que alguien la escriba en un .razor.
		//
		// LA CLAVE SI ENTREGA SUS DOS MITADES, a proposito: el resolvedor las recibe, y ninguna de
		// las dos es una medicion -- son la operacion y el hito, claves de catalogo. Lo que la sujeta
		// es el censo de portadores de arriba: la clave no cruza la superficie del agregado.
		[Test]
		public void NoMemberOfTheRowHandsOutANumberOrACarrier()
		{
			Type[] refused =
			{
				typeof(int), typeof(long), typeof(decimal), typeof(double), typeof(DateTime),
				typeof(PublishedMilestone), typeof(MaintenanceOperationKind), typeof(DistanceUnit),
				typeof(SpecificationAnswer), typeof(PublishedRevision),
			};

			List<string> offending = typeof(APublishedPriceRow)
				.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
					| BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
				.Where(member => member.IsPublic || member.IsAssembly)
				.Where(member => refused.Contains(member.ReturnType))
				.Select(member => member.Name)
				.ToList();

			Assert.That(offending, Is.Empty,
				"La fila entrega " + string.Join(", ", offending)
				+ ". Una fila es un registro completo; una mitad es un operando.");
		}
	}
}
