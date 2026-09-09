using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Concesionario.App.Events;
using NUnit.Framework;
using Puppets.Portador;

namespace Concesionario.Dominio.Pruebas
{
	// DOS HECHOS BAJAN POR UN SOLO CABLE Y NO SE ORDENAN IGUAL, y este es el primer sitio del modulo
	// donde los dos se encuentran.
	//
	// EL SOBRE APLICA POR INSTANTE DE PUBLICACION (ley L10). Uno que llega despues pero fue publicado
	// ANTES no desplaza al que rige, y tampoco se rechaza: entra y no gobierna. Asi que un feed puede
	// entregar sobres desordenados y la respuesta no se mueve.
	//
	// LA ENTREGA ES CORRECTIVA Y GANA LA ULTIMA (decision D6 del plan). Asi que un feed puede entregar
	// entregas desordenadas y la respuesta SI se mueve -- a la que llego ultima, no a la mas nueva.
	//
	// LAS DOS SON CORRECTAS Y LAS DOS SON DISTINTAS, y la razon esta en lo que cada hecho ES: un sobre
	// es una REGLA PUBLICADA con vigencia, y una regla publicada antes no puede regir sobre una
	// posterior; una entrega es UN HECHO HISTORICO, y de un hecho historico solo hay una version --
	// la ultima que la agencia dijo. Ninguna de las dos es la que uno esperaria de la otra.
	//
	// POR QUE SE MIDE EN VEZ DE ESCRIBIRSE. Las dos leyes ya tienen prueba en el nivel A y a traves de
	// la puerta; lo que NO tenia testigo es que las dos sobrevivan a llegar por el mismo feed, en el
	// mismo bloque, contra el mismo punto de control. Un feed no elige el orden en que sirve.
	[TestFixture]
	public sealed class TwoFactsOnOneFeedTakeTwoDifferentOrderings
	{
		private const string TheUnit = "UnitB";

		private static readonly DateTime TheInstantAsked =
			new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheFirstPublication =
			new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheEarlierPublication =
			new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(ADealershipNarrative.TheVin, "Standard");
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// ------------------------------------------------------ el sobre: por publicacion

		// UN SOBRE QUE LLEGA DESPUES Y FUE PUBLICADO ANTES ENTRA Y NO GOBIERNA. Los dos hechos de la
		// afirmacion importan por separado: que ENTRE (no hay rechazo, asi que el punto de control
		// avanza como con cualquier hecho aceptado) y que NO GOBIERNE (la frase sigue trayendo las
		// mitades del otro).
		[Test]
		public void AnEnvelopePublishedEarlierArrivingLaterEntersTheFeedAndDoesNotGovern()
		{
			Assert.That(Apply(ADealershipNarrative.Envelope(
				1, 36, 60000, TheUnit, "2026-01-01T00:00:00Z")), Is.EqualTo(""));

			Assert.That(Apply(ADealershipNarrative.Envelope(
					2, 24, 40000, TheUnit, "2025-03-01T00:00:00Z")),
				Is.EqualTo(""),
				"NO SE RECHAZA. Un sobre viejo que llega tarde es un hecho legitimo del feed, y "
				+ "rechazarlo adentro del comando dejaria una entrada permanente por algo que no es un "
				+ "error de nadie.");

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(2L),
				"Y el punto de control avanza por los dos, como con cualquier hecho aceptado.");

			Assert.That(journal.Api.DescribePublishedWarrantyEnvelope(),
				Does.Contain("36 meses").And.Contains("60000 mi"),
				"PERO NO GOBIERNA: sigue rigiendo el publicado despues, que es el que llego primero. "
				+ "El orden de LLEGADA no es el de vigencia.");

			Assert.That(journal.Api.ReadEnvelopePublishedAt(), Is.EqualTo(TheFirstPublication),
				"Y la frase nombra el instante del que rige, que es lo que hace visible cual de los dos "
				+ "contesto.");
		}

		// Y EL CAMINO DERECHO DEL MISMO PAR, que es lo que impide que el de arriba quede verde con una
		// ingesta que ignora el segundo sobre siempre: llegando en el otro orden, el segundo SI
		// gobierna.
		[Test]
		public void AnEnvelopePublishedLaterArrivingLaterDoesGovern()
		{
			Assert.That(Apply(ADealershipNarrative.Envelope(
				1, 24, 40000, TheUnit, "2025-03-01T00:00:00Z")), Is.EqualTo(""));

			Assert.That(Apply(ADealershipNarrative.Envelope(
				2, 36, 60000, TheUnit, "2026-01-01T00:00:00Z")), Is.EqualTo(""));

			Assert.That(journal.Api.DescribePublishedWarrantyEnvelope(),
				Does.Contain("36 meses").And.Contains("60000 mi"));

			Assert.That(journal.Api.ReadEnvelopePublishedAt(), Is.EqualTo(TheFirstPublication));
		}

		// ------------------------------------------------------ la entrega: la ultima que llego

		// UNA ENTREGA MAS VIEJA QUE LLEGA DESPUES SI DESPLAZA A LA ANTERIOR, y es la ley opuesta a la
		// del sobre sobre el mismo cable. De un hecho historico hay UNA version: la ultima que la
		// agencia dijo.
		[Test]
		public void ADeliveryArrivingLaterWinsEvenWhenItIsTheOlderOfTheTwo()
		{
			Assert.That(Apply(ADealershipNarrative.Delivered(
				1, "2025-03-01T00:00:00Z", 120, TheUnit)), Is.EqualTo(""));

			Assert.That(journal.Api.ReadDeliveryReading(), Is.EqualTo(120));

			Assert.That(Apply(ADealershipNarrative.Delivered(
					2, "2024-11-01T00:00:00Z", 50, TheUnit)),
				Is.EqualTo(""),
				"Tampoco se rechaza, y por el mismo motivo: corregir una entrega es trabajo ordinario "
				+ "de una agencia.");

			Assert.That(journal.Api.ReadDeliveredAt(),
				Is.EqualTo(new DateTime(2024, 11, 1, 0, 0, 0, DateTimeKind.Utc)),
				"GANA LA ULTIMA QUE LLEGO, aunque sea la mas vieja de las dos. Si aca ganara la mas "
				+ "nueva, una agencia no podria corregir una fecha de entrega hacia atras -- y eso es "
				+ "exactamente el error que se comete.");

			Assert.That(journal.Api.ReadDeliveryReading(), Is.EqualTo(50),
				"Y los tres datos se reemplazan juntos: la entrega es un hecho, no tres.");
		}

		// LAS DOS LEYES, EN UN SOLO BLOQUE Y EN EL MISMO ORDEN DE LLEGADA. Es la prueba que las dos de
		// arriba no dan por separado: que la asimetria sobreviva a que los dos tipos bajen intercalados
		// por el mismo cable contra el mismo punto de control.
		[Test]
		public void TheTwoOrderingsHoldWhenBothFactsArriveInterleavedOnOneBlock()
		{
			SenalDeEvento[] outOfOrder =
			{
				ADealershipNarrative.Delivered(1, "2025-03-01T00:00:00Z", 120, TheUnit),
				ADealershipNarrative.Envelope(2, 36, 60000, TheUnit, "2026-01-01T00:00:00Z"),
				ADealershipNarrative.Delivered(3, "2024-11-01T00:00:00Z", 50, TheUnit),
				ADealershipNarrative.Envelope(4, 24, 40000, TheUnit, "2025-03-01T00:00:00Z"),
			};

			foreach (SenalDeEvento signal in outOfOrder)
				Assert.That(Apply(signal), Is.EqualTo(""), signal.Tipo + " " + signal.Numero);

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(4L),
				"Los cuatro entraron: ninguno de los dos desordenes es un rechazo.");

			Assert.That(journal.Api.ReadDeliveryReading(), Is.EqualTo(50),
				"La entrega se movio a la que llego ultima.");

			Assert.That(journal.Api.DescribePublishedWarrantyEnvelope(),
				Does.Contain("36 meses").And.Contains("60000 mi"),
				"Y el sobre NO se movio a la que llego ultima. Un bloque, dos ordenamientos.");
		}

		// ------------------------------------------------------ la re-entrega no cuesta nada

		// RE-ENTREGAR EL BLOQUE NO CUESTA NADA, y se mide sobre los dos tipos nuevos porque cada uno
		// tiene su propio script y la guarda de idempotencia esta escrita una vez por script.
		//
		// LO QUE ESTO SEPARA. Sin la guarda, la re-entrega de una ENTREGA la volveria a aplicar en
		// silencio -- es correctiva, gana la ultima, y volver a aplicar la misma no cambia el estado --
		// asi que ninguna lectura lo notaria: lo unico que lo delata es el journal byte a byte.
		[Test]
		public void RedeliveringTheWholeBlockCostsNothingAndChangesNothing()
		{
			foreach (SenalDeEvento signal in ADealershipNarrative.TheDeliveryTheEnvelopeAndThePlate())
				Assert.That(Apply(signal), Is.EqualTo(""));

			string afterTheFirstPass = TheBytesOf(journal.Folder);

			foreach (SenalDeEvento signal in ADealershipNarrative.TheDeliveryTheEnvelopeAndThePlate())
				Assert.That(Apply(signal), Does.Contain("evento ya procesado"),
					"Rechazado por REPETIDO, bajo el read lock, sin llegar a haber comando: " +
					signal.Tipo);

			Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(afterTheFirstPass),
				"Byte a byte igual. Sin esto, cada refresh volveria a aplicar la entrega y el sobre y "
				+ "el journal de la persona creceria por siempre con hechos que no cambian nada.");

			Assert.That(journal.Api.LastEventApplied(),
				Is.EqualTo(ADealershipNarrative.TheLastProfileEvent));
		}

		// ------------------------------------------------------ el feed que se contradice

		// UN FEED QUE SE CONTRADICE PIERDE UN HECHO Y UNA ENTRADA, y la narrativa sigue. Es la mitad
		// que el sobre tiene y la entrega no: una mitad en CERO es un numero que se puede decir, asi
		// que la frontera lo deja pasar y lo refusa el sobre -- con sus palabras y al precio de una
		// entrada permanente.
		[Test]
		public void AnEnvelopeWithAHalfInZeroLosesTheFactAndTheNarrativeMovesOn()
		{
			Assert.That(Apply(ADealershipNarrative.Envelope(
				1, 36, 60000, TheUnit, "2026-01-01T00:00:00Z")), Is.EqualTo(""));

			Assert.That(Apply(ADealershipNarrative.Envelope(
					2, 0, 60000, TheUnit, "2026-02-01T00:00:00Z")),
				Does.Contain("lleva sus dos mitades siempre"),
				"El sobre lo refusa, no la frontera: cero es sayable y lo que significa es su ley.");

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(2L),
				"Y el punto de control avanza igual, en un comando aparte: se pierde el hecho, no el "
				+ "registro. Sin esto el proveedor volveria a servir el 2 en cada refresh.");

			Assert.That(journal.Api.DescribePublishedWarrantyEnvelope(),
				Does.Contain("36 meses").And.Contains("60000 mi"),
				"El sobre que regia quedo intacto: un hecho refusado adentro del cuerpo no deja el "
				+ "agregado a medio escribir.");

			Assert.That(Apply(ADealershipNarrative.Envelope(
					3, 24, 40000, TheUnit, "2026-03-01T00:00:00Z")),
				Is.EqualTo(""),
				"Y el hecho siguiente entra normal: la narrativa no quedo atascada en el refusado.");

			Assert.That(journal.Api.DescribePublishedWarrantyEnvelope(),
				Does.Contain("24 meses").And.Contains("40000 mi"));
		}

		// LA ENTREGA NO TIENE ESA MITAD, y decirlo es un hallazgo y no una omision: sus DOS guardas del
		// dominio -- una lectura negativa y una unidad ausente -- son las dos cosas que NO SE PUEDEN
		// DECIR, asi que las dos las descarta la frontera. Un hecho de entrega no puede cicatrizar el
		// journal de nadie por ningun camino.
		[Test]
		public void ADeliveryFactCannotScarTheJournalByAnyRoute()
		{
			string before = TheBytesOf(journal.Folder);

			Assert.That(
				DealershipEventRoute.Route(
					ADealershipNarrative.Delivered(1, "2025-03-01T00:00:00Z", -1, TheUnit)),
				Is.Null,
				"Una lectura negativa: descartada en la frontera.");

			Assert.That(
				DealershipEventRoute.Route(
					ADealershipNarrative.Delivered(1, "2025-03-01T00:00:00Z", 120, "Miles")),
				Is.Null,
				"Una unidad que el canon no escribe: descartada en la frontera.");

			Assert.That(TheBytesOf(journal.Folder), Is.EqualTo(before),
				"Y las dos cuestan CERO: no llegaron a haber comando. La entrega es correctiva y no "
				+ "revienta por nada mas, asi que estas dos son TODAS sus guardas.");
		}

		// ------------------------------------------------------ y el feed llega a R6

		// UN FEED PUEDE DEJAR EL REGISTRO CONTRADICIENDOSE, Y LO HACE EN SILENCIO. La entrega es
		// correctiva, asi que corregir la lectura de la entrega HACIA ARRIBA por encima del odometro de
		// hoy es un comando legitimo que entra sin rechazo y sin cicatriz -- y deja dos hechos que no
		// pueden ser los dos.
		//
		// DONDE REVIENTA ES EN LA LECTURA, no en la ingesta, y por eso el feed no puede verlo: el
		// hecho que crea la contradiccion es valido por si solo. Quien paga es quien pregunta despues.
		//
		// NO SE ARREGLA ACA. Esta prueba dice que hoy es asi, no que este bien.
		[Test]
		public void AFeedCanLeaveTheRecordContradictingItselfAndNothingInTheIngestionSaysSo()
		{
			Assert.That(Apply(ADealershipNarrative.Envelope(
				1, 36, 60000, TheUnit, "2025-03-01T00:00:00Z")), Is.EqualTo(""));
			Assert.That(Apply(ADealershipNarrative.Delivered(
				2, "2025-03-01T00:00:00Z", 120, TheUnit)), Is.EqualTo(""));
			Assert.That(Apply(ADealershipNarrative.Reading(
				3, 9000, TheUnit, "2026-05-01T00:00:00Z", "Telemetry")), Is.EqualTo(""));

			Assert.That(journal.Api.DescribeWarrantyEnvelope(TheInstantAsked),
				Does.Contain("Inside").Or.Contains("meses"),
				"Antes de la correccion el registro contesta.");

			string before = TheBytesOf(journal.Folder);

			Assert.That(Apply(ADealershipNarrative.Delivered(
					4, "2025-03-01T00:00:00Z", 50000, TheUnit)),
				Is.EqualTo(""),
				"LA CORRECCION ENTRA SIN RECHAZO: una agencia corrigiendo hacia arriba la lectura del "
				+ "dia de la entrega es un comando legitimo, y la entrega no sabe nada del odometro de "
				+ "hoy.");

			Assert.That(TheBytesOf(journal.Folder), Is.Not.EqualTo(before),
				"Y deja su entrada como cualquier hecho aceptado: no es un rechazo.");

			Assert.Throws<DomainException>(
				() => journal.Api.DescribeWarrantyEnvelope(TheInstantAsked),
				"PERO AHORA LA LECTURA REVIENTA. El odometro de hoy quedo por debajo de la lectura de "
				+ "la entrega, y eso el dominio lo clasifica como un hecho imposible -- correctamente, "
				+ "para quien le pasa los nueve datos, y no para quien solo pregunto por un instante.");

			Assert.Throws<DomainException>(
				() => journal.Api.CanTheEnvelopeBeEvaluated(TheInstantAsked),
				"Tambien la pregunta que se supone que se hace PRIMERO, que es lo que obliga a envolver "
				+ "el bloque compuesto entero y no solo la frase.");

			Assert.That(journal.Api.ReadDeliveryReading(), Is.EqualTo(50000),
				"Los dos hechos sueltos siguen contestando, que es lo que le deja ver a alguien los dos "
				+ "numeros que no pueden ser los dos.");
			Assert.That(journal.Api.ReadOdometer(TheInstantAsked), Is.EqualTo(9000));
		}

		private string Apply(SenalDeEvento signal)
		{
			return journal.Api.Apply(DealershipEventRoute.Route(signal));
		}

		// EL ARBOL DEL JOURNAL como nombre, tamano y hash. Se abre con FileShare porque el motor tiene
		// el archivo abierto mientras el proceso vive: un ReadAllText a secas revienta con IOException.
		private static string TheBytesOf(string folder)
		{
			List<string> files = new List<string>(
				Directory.GetFiles(folder, "*", SearchOption.AllDirectories));
			files.Sort(StringComparer.Ordinal);

			StringBuilder tree = new StringBuilder();

			foreach (string file in files)
			{
				using (FileStream open = new FileStream(
					file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					byte[] content = new byte[open.Length];
					open.ReadExactly(content, 0, content.Length);

					tree.Append(Path.GetRelativePath(folder, file).Replace('\\', '/'));
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
