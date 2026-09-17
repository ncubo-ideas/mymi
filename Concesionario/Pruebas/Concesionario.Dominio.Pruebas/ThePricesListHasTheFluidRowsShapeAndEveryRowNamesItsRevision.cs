using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Concesionario.App;
using Concesionario.App.Views;
using Concesionario.Wasm;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA PARTE C EN LA PANTALLA: los precios que el fabricante publico, en la hoja MANUAL de la seccion
	// del vehiculo y en la forma de la fila de un fluido, cada fila con su revision.
	//
	// NADIE NOMBRA UN HITO. Los hitos son del fabricante y esta respuesta los ENUMERA: una pantalla que
	// escribiera "10000 UnitA" a mano pondria el numero del fabricante en una caratula, y mostraria un
	// hueco en silencio el dia que una revision publique otro. El hito llega SOLO en su forma DICHA --
	// devolverlo como un numero mas un codigo de unidad es el mapeo al reves que este modulo ya paga dos
	// veces.
	//
	// Y UNA DECIMAL PUBLICADA POR LA PUERTA SE NORMALIZA: 120.50 vuelve como 120.5 adentro de la frase,
	// asi que un literal de la caratula no es el literal del dominio.
	[TestFixture]
	public sealed class ThePricesListHasTheFluidRowsShapeAndEveryRowNamesItsRevision
	{
		private const string TheVin = "WDD2040012A000013";
		private const string TheRevision = "MY2026-R1";
		private const string TheSheet = "Vehiculo.razor";

		private static readonly DateTime PublishedAt =
			new DateTime(2025, 3, 14, 10, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime ValidFrom =
			new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheInspectionPriceRunsUntil =
			new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheFluidPriceEndedOn =
			new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheInstantTheScreenAsksAt =
			new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(TheVin, AppState.TheProfileAtDelivery);
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// ===== LAS CUATRO COLUMNAS, Y LA FRASE LETRA POR LETRA ============================

		[Test]
		public void TheFourColumnsOfEachPriceRowAreTheDoorsOwnAndTheSentenceIsLetterForLetter()
		{
			ASealedRevisionWithTwoPrices();

			WhatTheServicesCost handed = WhatTheScreenIsHanded();
			PublishedPriceRecord record =
				journal.Api.PublishedServicePricesAsOf(
					TheInstantTheScreenAsksAt, TheLanguage.OfTheRecord);

			Assert.That(
				handed.Rows.Select(each => each.Operation).ToArray(), Is.EqualTo(record.Operation));
			Assert.That(
				handed.Rows.Select(each => each.Milestone).ToArray(), Is.EqualTo(record.Milestone));
			Assert.That(
				handed.Rows.Select(each => each.Says).ToArray(), Is.EqualTo(record.Says),
				"Las frases son las de la puerta LETRA POR LETRA.");
			Assert.That(
				handed.Rows.Select(each => each.Revision).ToArray(), Is.EqualTo(record.Revision));

			// Y CONTRA LO QUE SE PUBLICO, POR LITERAL: comparar el portador con la puerta es comparar dos
			// consumidores de un mismo resolvedor, y se ponen de acuerdo incluso cuando el resolvedor
			// esta uniformemente equivocado.
			APublishedPriceOnTheSheet inspection = TheRowOf(handed, "Inspection");
			APublishedPriceOnTheSheet fluid = TheRowOf(handed, "FluidChange");

			Assert.That(inspection.Milestone, Is.EqualTo("10.000 km"),
				"El hito DICHO con su unidad, como lo lee una persona.");
			// EL MONTO CON SU SIGNO Y SUS DECIMALES, como el dominio lo dice desde CL-2: se publico
			// 120.50, el decimal se NORMALIZA a 120.5 al cruzar la puerta, y Money.Expressed lo vuelve
			// a decir con los dos decimales de su moneda. Asi que la normalizacion sigue ocurriendo y
			// ESTA FRASE ya no la muestra -- lo que la sigue impidiendo es el Normalized de la puerta,
			// que esta en el camino de ESCRITURA y por eso vivo y replayado coinciden.
			Assert.That(inspection.Says, Does.Contain("₡120,50"),
				"Una sola afirmacion en vez de dos: el monto y su moneda son un valor y se dicen "
				+ "juntos, asi que separarlos dejaria pasar una fila con el monto de una y la moneda "
				+ "de otra.");

			Assert.That(fluid.Milestone, Is.EqualTo("20.000 km"),
				"Y el otro hito es OTRO: cruzar las dos filas cambia esta afirmacion y la de arriba.");
			Assert.That(fluid.Says, Does.Contain("340"));
		}

		// LA VIGENCIA SE JUZGA AL INSTANTE EN QUE LA PANTALLA PREGUNTA, que es lo que impide leer un
		// precio viejo como el de hoy. Las dos filas se publicaron juntas y una de las dos ya vencio.
		[Test]
		public void TheValidityIsJudgedAtTheInstantTheScreenAskedAndOneOfTheTwoHasLapsed()
		{
			ASealedRevisionWithTwoPrices();

			WhatTheServicesCost handed = WhatTheScreenIsHanded();

			Assert.That(TheRowOf(handed, "Inspection").Says, Does.Contain("1 de febrero de 2026"),
				"La frase nombra el instante contra el que se juzgo la vigencia. La pantalla no lo dice "
				+ "por su cuenta justamente porque cada fila lo trae adentro.");

			Assert.That(
				TheRowOf(handed, "Inspection").Says, Is.Not.EqualTo(TheRowOf(handed, "FluidChange").Says));

			Assert.That(TheRowOf(handed, "FluidChange").Says, Does.Contain("30 de junio de 2025"),
				"Y la que vencio nombra el dia en que su vigencia termino.");
		}

		// L9: CADA FILA NOMBRA SU REVISION, por valor y en el markup. Un precio sin su procedencia no se
		// puede distinguir de uno viejo.
		[Test]
		public void EveryPriceRowNamesTheRevisionItCameFrom()
		{
			ASealedRevisionWithTwoPrices();

			WhatTheServicesCost handed = WhatTheScreenIsHanded();

			Assert.That(handed.Rows, Is.Not.Empty);

			foreach (APublishedPriceOnTheSheet row in handed.Rows)
				Assert.That(row.Revision, Is.EqualTo(TheRevision),
					"Una fila llego sin su revision.");

			Assert.That(TheSheetAsItIsWritten(), Does.Contain("@precio.Revision"),
				"La hoja dejo de pintar la revision de cada fila. El pill de la revision del encabezado "
				+ "es de la revision que RIGE la ficha, y la enumeracion de precios recorre TODAS las "
				+ "revisiones selladas de la mas nueva a la mas vieja: dos filas de esta lista pueden "
				+ "venir de dos revisiones distintas.");
		}

		// ===== LA FORMA DE LA FILA DE UN FLUIDO ===========================================

		// LA LISTA TIENE LA FORMA DE LA FILA DE UN FLUIDO, que es la que la hoja ya usa para todo lo que
		// el fabricante publico: la clave arriba y la frase del dominio abajo.
		[Test]
		public void TheListHasTheFluidRowsShapeAndPaintsTheDomainsSentenceWhole()
		{
			string markup = TheSheetAsItIsWritten();

			Assert.That(markup, Does.Contain("id=\"precios-publicados\""),
				"La hoja Manual no tiene el bloque de precios.");

			Assert.That(markup, Does.Contain("d-row d-row--stack"),
				"La lista dejo la forma de la fila de un fluido.");

			Assert.That(markup, Does.Contain("@precio.Says"),
				"La fila dejo de pintar la frase del dominio.");

			Assert.That(markup, Does.Contain("@precio.Milestone"),
				"La fila dejo de nombrar su hito, que es la mitad de su clave.");

			foreach (string forbidden in new[]
			{
				"precio.Says +", "+ precio.Says", "Milestone +", "+ Milestone",
			})
				Assert.That(markup, Does.Not.Contain(forbidden),
					"La hoja concatena algo con una frase del dominio ('" + forbidden + "').");
		}

		// EL HITO NO ES DIRECCIONABLE EN NINGUNA FORMA, en el portador ni en la pantalla: todas las
		// propiedades de una fila son TEXTO. Devolverlo como un numero mas un codigo de unidad
		// reintroduce el mapeo al reves.
		[Test]
		public void TheMilestoneIsOnlyEverTheSaidFormAndNoPropertyOfARowIsANumber()
		{
			foreach (PropertyInfo each in typeof(APublishedPriceOnTheSheet).GetProperties())
				Assert.That(each.PropertyType, Is.EqualTo(typeof(string)),
					"'" + each.Name + "' de una fila de precio dejo de ser texto. Un hito como numero mas "
					+ "un codigo de unidad es el mapeo al reves.");

			Assert.That(
				typeof(APublishedPriceOnTheSheet).GetProperties().Select(each => each.Name).ToArray(),
				Is.EquivalentTo(new[] { "Operation", "Milestone", "Says", "Revision" }));
		}

		// ===== LAS DOS QUE NO TOMAN INSTANTE, Y LA REGLA 4 ================================

		// LAS DOS PREGUNTAS QUE NO TOMAN INSTANTE SE PIDEN SIN NINGUNO, y esa asimetria es la
		// afirmacion: sellada es un ESTADO y no un instante, y el instante mueve la FRASE de cada fila y
		// nunca el juego de filas. Igualar las tres firmas seria borrar eso.
		[Test]
		public void TheTwoThatTakeNoInstantAreAskedWithNoneAndTheAsymmetryIsKept()
		{
			ASealedRevisionWithTwoPrices();

			WhatTheServicesCost handed = WhatTheScreenIsHanded();

			Assert.That(handed.AnyPriceOnRecord, Is.True);
			Assert.That(handed.AnyPriceOnRecord, Is.EqualTo(journal.Api.HasAnyPublishedServicePrice()));
			Assert.That(
				handed.PriceCountOnRecord, Is.EqualTo(journal.Api.PublishedServicePriceCount()));

			foreach (string takesNone in new[]
			{
				"HasAnyPublishedServicePrice", "PublishedServicePriceCount",
			})
				Assert.That(
					typeof(DealershipCarAPI).GetMethod(takesNone).GetParameters(), Is.Empty,
					"'" + takesNone + "' gano un instante. Las dos contestan sobre un ESTADO.");

			ParameterInfo[] filas =
				typeof(DealershipCarAPI).GetMethod("PublishedServicePricesAsOf").GetParameters();

			Assert.That(filas, Has.Length.EqualTo(2),
				"Y la de las filas dejo de tomar el suyo. El instante mueve la frase de cada fila, y "
				+ "el idioma en que se dice lo pone quien pregunta.");
			Assert.That(filas[0].Name, Is.EqualTo("asOf"));
			Assert.That(filas[1].Name, Is.EqualTo("language"));
		}

		// REGLA 4 DE LA CONVENCION: la cuenta que el dominio compone y las filas de la pantalla dicen lo
		// mismo. La cuenta NO toma instante y las filas SI, asi que esto no es una tautologia: es la
		// afirmacion de que el instante mueve la frase y nunca el juego de filas.
		[Test]
		public void TheDomainsCountAndTheRowsOnTheScreenAgree()
		{
			ASealedRevisionWithTwoPrices();

			WhatTheServicesCost handed = WhatTheScreenIsHanded();

			Assert.That(handed.PriceCountOnRecord, Is.EqualTo(2));
			Assert.That(handed.Rows, Has.Count.EqualTo(handed.PriceCountOnRecord));

			// Y AL MISMO REGISTRO PREGUNTADO EN OTRO MOMENTO, la cuenta y las filas siguen de acuerdo:
			// las dos frases cambian y ninguna fila aparece ni desaparece.
			WhatTheServicesCost earlier = WhatTheServicesCost.ReadThrough(
				journal.Api, new DateTime(
					2025, 4, 1, 0, 0, 0, DateTimeKind.Utc), TheLanguage.OfTheRecord);

			Assert.That(earlier.Rows, Has.Count.EqualTo(earlier.PriceCountOnRecord));
			Assert.That(earlier.PriceCountOnRecord, Is.EqualTo(handed.PriceCountOnRecord));

			Assert.That(
				TheRowOf(earlier, "FluidChange").Says,
				Is.Not.EqualTo(TheRowOf(handed, "FluidChange").Says),
				"La MISMA fila dice otra cosa en otro momento -- vigente entonces, vencida ahora -- y es "
				+ "la misma fila. Sin este renglon lo de arriba pasaria con un instante que no hace nada.");
		}

		// ===== LA AUSENCIA, Y LO QUE NO SE FUSIONA ========================================

		[Test]
		public void WithNothingPublishedTheScreenSaysSoAndInventsNoSentenceAboutTheModel()
		{
			WhatTheServicesCost handed = WhatTheScreenIsHanded();

			Assert.That(handed.AnyPriceOnRecord, Is.False);
			Assert.That(handed.PriceCountOnRecord, Is.EqualTo(0));
			Assert.That(handed.Rows, Is.Empty);

			Assert.That(TheSheetAsItIsWritten(), Does.Contain("id=\"sin-precios\""),
				"La hoja no tiene la rama de la ausencia, asi que un registro sin precios muestra un "
				+ "bloque con encabezado y nada debajo.");
		}

		// Y Q5 SIGUE ABIERTA EN ESTA HOJA, POR LA FORMA. La hoja Manual es la primera superficie del
		// modulo que podria poner lado a lado lo que le falta a este carro y el intervalo que publico el
		// fabricante: son dos PROCEDENCIAS distintas -- el resto es contra el limite del PLAN escalado
		// por el perfil de uso, y el intervalo publicado es del fabricante -- y juntarlos compone una
		// afirmacion que el dominio nunca hizo. La cuenta atras vive en la pestana Servicios y esta hoja
		// no la recibe.
		[Test]
		public void TheManualSheetIsHandedNoRemainderOfThisVehicleAtAll()
		{
			string markup = TheSheetAsItIsWritten();

			foreach (string forbidden in new[]
			{
				"WhereEachOperationStands", "DistanceRemaining", "DaysRemaining",
				"DescribeServiceInterval", "distanceRemaining",
			})
				Assert.That(markup, Does.Not.Contain(forbidden),
					"La hoja del manual gano '" + forbidden + "'. El resto de este carro es contra el "
					+ "limite del PLAN escalado por su perfil de uso; el intervalo del fabricante es otra "
					+ "procedencia, y ponerlos juntos compone una afirmacion que nadie hizo.");

			// EL CONTROL: la seccion del vehiculo SI recibe los precios, que es lo que hace que la
			// afirmacion de arriba mida la ausencia de la otra mitad y no la de todo.
			Assert.That(markup, Does.Contain("WhatTheServicesCost Costs"),
				"La seccion del vehiculo dejo de recibir los precios.");
		}

		// ===== EL APAREJO =================================================================

		private WhatTheServicesCost WhatTheScreenIsHanded()
		{
			return WhatTheServicesCost.ReadThrough(
				journal.Api, TheInstantTheScreenAsksAt, TheLanguage.OfTheRecord);
		}

		private static APublishedPriceOnTheSheet TheRowOf(WhatTheServicesCost handed, string operation)
		{
			foreach (APublishedPriceOnTheSheet each in handed.Rows)
				if (each.Operation == operation) return each;

			Assert.Fail("No hay ninguna fila de '" + operation + "'.");
			return null;
		}

		// DOS PRECIOS EN UNA REVISION SELLADA, con hitos distintos y con vigencias distintas: una todavia
		// rige al instante en que la pantalla pregunta y la otra ya vencio.
		private void ASealedRevisionWithTwoPrices()
		{
			journal.Api.OpenRevision(TheRevision, PublishedAt);

			journal.Api.PublishServicePrice(
				TheRevision, "Inspection", 10000, "UnitA", 120.50m, "CRC",
				ValidFrom, TheInspectionPriceRunsUntil);

			journal.Api.PublishServicePrice(
				TheRevision, "FluidChange", 20000, "UnitA", 340.00m, "CRC",
				ValidFrom, TheFluidPriceEndedOn);

			journal.Api.SealRevision(TheRevision);
		}

		private static string TheSheetAsItIsWritten()
		{
			string folder = Path.Combine(AppContext.BaseDirectory, "SeccionesComoDato");

			Assert.That(Directory.Exists(folder), Is.True, "Las secciones no llegaron al output.");

			foreach (string file in Directory.GetFiles(folder, "*.razor"))
				if (Path.GetFileName(file) == TheSheet) return File.ReadAllText(file);

			Assert.Fail("'" + TheSheet + "' no llego al output.");
			return "";
		}
	}
}
