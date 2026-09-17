using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Concesionario.App;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LOS DOS COMANDOS DE T4, DE PUNTA A PUNTA SOBRE UN JOURNAL DE VERDAD. El nivel A ya afirma
	// todo lo que el dominio decide; lo que ninguna prueba del nivel A puede tocar es que cada
	// script del DSL parsee y ejecute, que el codigo de la unidad cruce como NOMBRE, que los dos
	// instantes lleguen truncados al segundo en UTC, y que la historia sobreviva al replay.
	//
	// MEDIDO EN T0 (spike S5): un error de tipeo en un script del DSL se descubre en RUNTIME, no al
	// compilar, y deja entradas permanentes que hacen loguear un fallo de rehidratacion en cada
	// arranque futuro. La unica defensa es haber ejecutado cada script antes de que toque el journal
	// de una persona -- y estos dos scripts nunca corrieron hasta aca.
	//
	// TODO EN UnitB Y CON NUMEROS QUE NO SON CERO (C60). UnitA es el PRIMER miembro del enum, asi
	// que una ranura que el motor hubiera dejado por defecto daria la MISMA respuesta y la prueba
	// estaria verde sin haber medido nada. El caso UnitA vive aparte, como espejo.
	//
	// DESDE T6 TODO LO DE ABAJO SE LEE POR LA PUERTA, y el aparejo pasa a ser el compartido. T4 y T5
	// abrian su propia app aqui porque la RESPUESTA COMPUESTA no tenia miembro en la puerta y
	// observarla necesitaba un script crudo contra el actor. T6 le puso el miembro; el script crudo
	// se fue con el, y con el la unica razon que este fixture tenia para no usar
	// AScheduleOnARealJournal. Ver AProfileReadWithoutTheDoor, que se quedo en UN miembro por esto
	// mismo.
	[TestFixture]
	public sealed class AnEnvelopeAndADeliveryArrivePublishedThroughTheDoor
	{
		private const string TheVin = "1HGCM82633A004352";

		private const int ThirtySixMonths = 36;
		private const int SixtyThousand = 60000;
		private const string TheUnitTheLimitIsIn = "UnitB";

		private const int TheOdometerAtDelivery = 25;

		private static readonly DateTime TheDayItWasPublished =
			new DateTime(2023, 11, 15, 9, 30, 45, DateTimeKind.Utc);

		private static readonly DateTime TheDayItWasHandedOver =
			new DateTime(2024, 1, 1, 8, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TwoYearsLater =
			new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;
		private DealershipCarAPI door;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, "Standard");
			door = journal.Api;
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// ===== LOS DOS COMANDOS ENTRAN Y EL PERFIL LOS CONTESTA ============================

		[Test]
		public void TheEnvelopePublishedThroughTheDoorIsTheEnvelopeTheProfileAnswers()
		{
			door.PublishWarrantyEnvelope(
				ThirtySixMonths, SixtyThousand, TheUnitTheLimitIsIn, TheDayItWasPublished);

			Assert.That(door.IsWarrantyEnvelopePublished(), Is.True);
			Assert.That(
				door.ReadEnvelopePublishedAt(),
				Is.EqualTo(TheDayItWasPublished),
				"El instante llega al segundo y en UTC. El journal trunca a segundos enteros y borra "
				+ "el Kind, asi que la puerta le da esa forma ANTES de mandarlo y la respuesta viva y "
				+ "la reproducida son el mismo valor.");

			// LOS DOS LIMITES SE AFIRMAN DENTRO DE LA FRASE y no como escalares, porque es la unica
			// forma en que llegan a un llamador: la puerta no los da sueltos a proposito.
			string published = door.DescribePublishedWarrantyEnvelope(TheLanguage.OfTheRecord);

			Assert.That(published, Does.Contain("36 meses"));
			Assert.That(published, Does.Contain("60.000"));
			Assert.That(published, Does.Contain("mi"),
				"Y la unidad es la que se publico, dicha como la lee una persona. 'mi' es UnitB: si "
				+ "el codigo hubiera cruzado como un nombre que el dominio resuelve al primer "
				+ "miembro, aca diria 'km'.");
			Assert.That(published, Does.Not.Contain("km"));
		}

		[Test]
		public void TheDeliveryRecordedThroughTheDoorIsTheDeliveryTheProfileAnswers()
		{
			door.RecordDelivery(TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitTheLimitIsIn);

			Assert.That(door.HasDeliveryBeenRecorded(), Is.True);
			Assert.That(door.ReadDeliveredAt(), Is.EqualTo(TheDayItWasHandedOver));
			Assert.That(door.ReadDeliveryReading(), Is.EqualTo(TheOdometerAtDelivery));
			Assert.That(door.ReadDeliveryUnit(), Is.EqualTo("mi"),
				"UnitB, no el primer miembro del conjunto.");
		}

		// LA RESPUESTA COMPUESTA CAMBIA CON CADA COMANDO, y eso es lo que prueba que los DOS
		// entraron y no uno. Tres frases distintas: sin nada, con sobre y sin entrega, y con los dos.
		//
		// SE COMPARAN LAS TRES ENTRE SI y no solo se comprueba que ninguna venga vacia (C60): una
		// lectura que contestara siempre lo mismo pasaria un Is.Not.Empty tres veces.
		//
		// SE LEE POR LA PUERTA DESDE T6. T4 y T5 la leian con un script crudo contra el actor, porque
		// la respuesta compuesta no tenia miembro en la puerta. Ahora lo tiene, y como este vehiculo
		// no tiene ninguna lectura del odometro, DescribeWarrantyEnvelope contesta con esta misma
		// frase: es la rama que su script elige cuando no hay lectura. Dejar el script crudo al lado
		// de una lectura de la puerta que contesta lo mismo seria una segunda superficie de lectura.
		[Test]
		public void TheComposedAnswerMovesWithEachOfTheTwoCommands()
		{
			string toldNothing = door.DescribeWarrantyEnvelope(
				TwoYearsLater, TheLanguage.OfTheRecord);

			door.PublishWarrantyEnvelope(
				ThirtySixMonths, SixtyThousand, TheUnitTheLimitIsIn, TheDayItWasPublished);

			string withTheEnvelope = door.DescribeWarrantyEnvelope(
				TwoYearsLater, TheLanguage.OfTheRecord);

			door.RecordDelivery(TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitTheLimitIsIn);

			string withBoth = door.DescribeWarrantyEnvelope(TwoYearsLater, TheLanguage.OfTheRecord);

			Assert.That(toldNothing, Does.Contain("no ha publicado"));

			Assert.That(withTheEnvelope, Does.Contain("36"),
				"Con el sobre puesto, la frase cita los dos limites.");
			Assert.That(withTheEnvelope, Does.Contain("60.000"));
			Assert.That(withTheEnvelope, Is.Not.EqualTo(toldNothing));

			Assert.That(withBoth, Is.Not.EqualTo(withTheEnvelope),
				"Y con la entrega puesta cambia otra vez: sin ancla no habia desde donde medir el "
				+ "plazo, y con ancla la mitad de tiempo se puede decir.");
			Assert.That(withBoth, Does.Contain("12"),
				"Dos anos de un plazo de 36 meses: quedan DOCE. Es el numero que sale del ancla, y es "
				+ "el que separa 'la entrega entro' de 'la entrega se ignoro'.");
			Assert.That(withBoth, Does.Contain("ninguna lectura del odómetro"),
				"Y la mitad de distancia se dice ausente con palabras. Sin lectura no hay numero, y "
				+ "un cero aqui seria una medicion que nadie tomo.");
		}

		// LOS DOS HECHOS SOBREVIVEN AL REPLAY, que es lo que los separa de un campo de C#: se cierra
		// el proceso, se vuelve a abrir sobre el mismo journal, y las dos frases son las mismas.
		[Test]
		public void BothFactsSurviveClosingTheProcessAndReopeningTheJournal()
		{
			door.PublishWarrantyEnvelope(
				ThirtySixMonths, SixtyThousand, TheUnitTheLimitIsIn, TheDayItWasPublished);
			door.RecordDelivery(TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitTheLimitIsIn);

			string live = Everything();

			journal.CloseAndReopenFromTheJournal();
			door = journal.Api;

			Assert.That(Everything(), Is.EqualTo(live),
				"Lo mismo, palabra por palabra y numero por numero. Lo que se reproduce sale del "
				+ "journal y no de la memoria del proceso que lo escribio.");
		}

		// EL ESPEJO EN UnitA, que es el que hace que las afirmaciones de UnitB de arriba discriminen:
		// si el codigo no cruzara, las dos corridas darian la misma unidad.
		[Test]
		public void TheOtherUnitCrossesAsItselfAndNotAsTheFirstMember()
		{
			door.PublishWarrantyEnvelope(
				ThirtySixMonths, SixtyThousand, "UnitA", TheDayItWasPublished);

			Assert.That(
				door.DescribePublishedWarrantyEnvelope(TheLanguage.OfTheRecord), Does.Contain("km"));
			Assert.That(
				door.DescribePublishedWarrantyEnvelope(TheLanguage.OfTheRecord),
				Does.Not.Contain("mi"));
		}

		// ===== D6 Y L10 POR LA PUERTA ======================================================

		// D6: REGISTRAR LA ENTREGA DE NUEVO LA CORRIGE, gana la ultima, y no revienta. El nivel A ya
		// lo afirma del agregado; lo que se agrega aca es que el camino de la puerta tampoco revienta
		// -- porque un throw adentro del cuerpo de un comando deja una entrada permanente (C22), y
		// corregir una fecha de entrega es trabajo ordinario de una agencia.
		[Test]
		public void ASecondDeliveryCorrectsTheFirstAndScarsNothing()
		{
			door.RecordDelivery(TheDayItWasHandedOver, TheOdometerAtDelivery, TheUnitTheLimitIsIn);

			DateTime corrected = new DateTime(2024, 2, 20, 0, 0, 0, DateTimeKind.Utc);

			Assert.DoesNotThrow(() => door.RecordDelivery(corrected, 31, TheUnitTheLimitIsIn));

			Assert.That(door.ReadDeliveredAt(), Is.EqualTo(corrected), "Gana la ultima.");
			Assert.That(door.ReadDeliveryReading(), Is.EqualTo(31),
				"Entera: la entrega se reemplaza como un objeto y no campo por campo.");
		}

		// L10: UN SOBRE FECHADO ANTES DEL QUE RIGE SE ACEPTA Y NO GOBIERNA. Ni revienta ni desplaza,
		// que es la eleccion de ModelSpecification y no la de AdoptPlanRevision, por C22.
		[Test]
		public void AnEnvelopePublishedEarlierArrivesWithoutGoverningAndWithoutThrowing()
		{
			door.PublishWarrantyEnvelope(
				ThirtySixMonths, SixtyThousand, TheUnitTheLimitIsIn, TheDayItWasPublished);

			DateTime earlier = TheDayItWasPublished.AddDays(-100);

			Assert.DoesNotThrow(
				() => door.PublishWarrantyEnvelope(12, 20000, TheUnitTheLimitIsIn, earlier));

			Assert.That(door.DescribePublishedWarrantyEnvelope(TheLanguage.OfTheRecord), Does.Contain("36 meses"),
				"Sigue rigiendo el que se publico despues.");
			Assert.That(
				door.DescribePublishedWarrantyEnvelope(TheLanguage.OfTheRecord), Does.Contain("60.000 mi"));
			Assert.That(
				door.DescribePublishedWarrantyEnvelope(TheLanguage.OfTheRecord), Does.Not.Contain("20000"),
				"Y el que llego fechado antes no dejo ni un numero en la frase.");
			Assert.That(door.ReadEnvelopePublishedAt(), Is.EqualTo(TheDayItWasPublished));
		}

		// ===== D12: UNA MITAD EN CERO SE RECHAZA EN LA PUERTA Y NO CUESTA NADA =============

		// LAS TRES MANERAS DE RECHAZAR CUESTAN COSAS DISTINTAS, y esta guarda es el primer caso del
		// modulo que se podia poner en cualquiera de las tres: months > 0 && distance > 0 es funcion
		// PURA de los argumentos, asi que no depende de nada que la fachada no proyecte -- que es lo
		// que impide mover las cuatro guardas de ARefusedCommandCostsAJournalEntryUnlessItIsGuarded.
		//
		// SE ELIGIO LA PUERTA. Un Check pagaria las dos fases y pondria la regla en una cadena del
		// DSL para preguntarle al actor algo que no necesita al actor; dejarla llegar al dominio
		// escribiria la entrada y despues reventaria, y el rechazo quedaria en el journal de la
		// persona haciendo loguear un fallo de rehidratacion en cada arranque futuro.
		//
		// EL PRECIO ES QUE LA REGLA VIVE EN DOS SITIOS. Este fixture fija la mitad de la puerta; la
		// del dominio la fija el nivel A. Ninguna de las dos sostiene a la otra: si alguien borra la
		// de la puerta, esto se pone rojo por el journal y no por el throw.
		[Test]
		public void AnEnvelopeMissingOneOfItsHalvesIsRefusedAtTheDoorAndCostsNoEntry()
		{
			string before = TheBytesOf(journal.Folder);

			Assert.Throws<ArgumentOutOfRangeException>(
				() => door.PublishWarrantyEnvelope(
					0, SixtyThousand, TheUnitTheLimitIsIn, TheDayItWasPublished));

			Assert.Throws<ArgumentOutOfRangeException>(
				() => door.PublishWarrantyEnvelope(
					ThirtySixMonths, 0, TheUnitTheLimitIsIn, TheDayItWasPublished));

			Assert.Throws<ArgumentOutOfRangeException>(
				() => door.PublishWarrantyEnvelope(
					-1, SixtyThousand, TheUnitTheLimitIsIn, TheDayItWasPublished));

			Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(before),
				"BYTE POR BYTE IGUAL, que es lo unico que este renglon afirma: el rechazo ocurrio "
				+ "antes de performar. Si la guarda estuviera solo en el dominio, la entrada del "
				+ "comando ya estaria escrita y el journal habria crecido tres veces.");

			Assert.That(door.IsWarrantyEnvelopePublished(), Is.False,
				"Y no hay sobre: nada entro a medias.");
		}

		[Test]
		public void ANegativeOdometerAtDeliveryIsRefusedAtTheDoorAndCostsNoEntry()
		{
			string before = TheBytesOf(journal.Folder);

			Assert.Throws<ArgumentOutOfRangeException>(
				() => door.RecordDelivery(TheDayItWasHandedOver, -1, TheUnitTheLimitIsIn));

			Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(before));
			Assert.That(door.HasDeliveryBeenRecorded(), Is.False);
		}

		// UN CERO EN LA LECTURA DE LA ENTREGA SI ES LEGAL, y va escrito para que el renglon de arriba
		// no se lea como "todo cero se rechaza". Un carro entregado con el odometro en cero es un
		// carro nuevo, que es el caso normal; un sobre con una mitad en cero no es nada.
		[Test]
		public void AZeroOdometerAtDeliveryIsALegalReadingAndNotARefusal()
		{
			Assert.DoesNotThrow(
				() => door.RecordDelivery(TheDayItWasHandedOver, 0, TheUnitTheLimitIsIn));

			Assert.That(door.ReadDeliveryReading(), Is.EqualTo(0));
		}

		// Y UN NOMBRE DE UNIDAD QUE NO ES DE NINGUN MIEMBRO tampoco cuesta nada: lo rechaza WireName
		// en la puerta, como en los veintidos comandos que ya existian.
		[Test]
		public void AUnitThatNamesNoMemberIsRefusedAtTheDoorOnBothCommands()
		{
			string before = TheBytesOf(journal.Folder);

			Assert.Throws<ArgumentException>(
				() => door.PublishWarrantyEnvelope(
					ThirtySixMonths, SixtyThousand, "Kilometros", TheDayItWasPublished));

			Assert.Throws<ArgumentException>(
				() => door.RecordDelivery(TheDayItWasHandedOver, TheOdometerAtDelivery, "  "));

			Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(before));
		}

		// Todo lo que el perfil puede contestar hoy, en una sola cadena comparable. Es lo que la
		// prueba del replay compara entre antes y despues de rehidratar. Los dos limites van dentro
		// de la frase publicada, que es donde llegan a un llamador.
		private string Everything()
		{
			return "envelope=" + door.IsWarrantyEnvelopePublished()
				+ " publishedAt=" + door.ReadEnvelopePublishedAt().ToString("O")
				+ "\npublished=" + door.DescribePublishedWarrantyEnvelope(TheLanguage.OfTheRecord)
				+ "\ndelivery=" + door.HasDeliveryBeenRecorded()
				+ " deliveredAt=" + door.ReadDeliveredAt().ToString("O")
				+ " reading=" + door.ReadDeliveryReading()
				+ " unit=" + door.ReadDeliveryUnit()
				+ "\nstanding=" + door.DescribeWarrantyEnvelope(
					TwoYearsLater, TheLanguage.OfTheRecord);
		}

		// El journal como bytes, para las pruebas que afirman que un rechazo no escribio nada. Es la
		// misma forma que usa ARefusedCommandCostsAJournalEntryUnlessItIsGuarded.
		private static string TheBytesOf(string directory)
		{
			List<string> files = new List<string>(
				Directory.GetFiles(directory, "*", SearchOption.AllDirectories));
			files.Sort(StringComparer.Ordinal);

			StringBuilder tree = new StringBuilder();

			foreach (string file in files)
			{
				using (FileStream open = new FileStream(
					file, FileMode.Open, FileAccess.Read,
					FileShare.ReadWrite | FileShare.Delete))
				{
					byte[] content = new byte[open.Length];
					open.ReadExactly(content, 0, content.Length);

					tree.Append(Path.GetRelativePath(directory, file).Replace('\\', '/'));
					tree.Append(' ');
					tree.Append(content.Length);
					tree.Append(' ');
					tree.AppendLine(Convert.ToHexString(MD5.HashData(content)));
				}
			}

			return tree.ToString();
		}
	}
}
