using System;
using DealershipCar.Wasm;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// EL ESTADO QUE MAS FACIL SE LEE AL REVES: el carro esta EN EL TALLER ahora mismo y el periodo no
	// tiene ningun dia. Las dos cosas son ciertas a la vez y no se contradicen -- los dias de una
	// estadia llegan CON LA ENTREGA (L4), asi que una estadia abierta todavia no tiene ninguno --, pero
	// una pantalla que solo mostrara la mitad por dias diria "no hay dias registrados" con el carro
	// adentro, sin decir que esta adentro.
	//
	// ES DEC-52 EN LA PANTALLA. Las seis lecturas por dias no alcanzan una estadia abierta; las dos por
	// instante son las unicas que si. Este bloque tiene las dos mitades justamente por esto, y esta
	// prueba es donde se ve para que.
	//
	// Y ES EL ESTADO EN QUE LA DECISION DEL PERIODO SE PAGA. La frase del periodo nombra sus dos
	// limites TAMBIEN cuando no hay ningun dia, asi que la persona sabe entre cuando y cuando se
	// pregunto sin que el encabezado se lo repita.
	[TestFixture]
	public sealed class AStayThatIsOpenRightNowIsSaidBesideAPeriodThatHasNoDays
	{
		private const string TheVin = "3VWFE21C04M000123";

		private const string TheYearFrom = "2026-01-01";
		private const string TheYearTo = "2026-12-31";

		private const string TheBranch = "Taller Heredia";
		private const string TheSubject = "Ruido en la suspensión";

		private static readonly DateTime TheEntry =
			new DateTime(2026, 4, 6, 8, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime TheInstantInsideTheOpenStay =
			new DateTime(2026, 4, 9, 15, 30, 0, DateTimeKind.Utc);

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

		// LA MITAD POR DIAS DICE QUE NO HAY NINGUN DIA REGISTRADO, Y NOMBRA EL PERIODO. No dice cero:
		// dice que no hay ninguna estadia CON DIAS REGISTRADOS, que no es lo mismo que decir que el
		// carro nunca estuvo adentro.
		[Test]
		public void TheDayBoundedHalfSaysNoDaysAreOnRecordAndNamesThePeriodItWasAsked()
		{
			journal.Api.RecordShopEntry(TheBranch, TheSubject, TheEntry);

			TheStaysOfThisVehicle stays = WhatTheScreenIsHanded();

			Assert.That(stays.Says, Does.Contain("días registrados"),
				"La frase del periodo dejo de decir que lo que falta son los DIAS. Un cero o una lista "
				+ "vacia se leen como 'el carro no estuvo en el taller', que es falso: esta adentro.");

			Assert.That(stays.Says, Does.Contain(TheYearFrom).And.Contains(TheYearTo),
				"Y nombra sus DOS limites. Es lo que hace que el encabezado no tenga que repetirlos: "
				+ "la persona sabe entre cuando y cuando se pregunto porque lo dice la frase.");

			Assert.That(stays.Days, Is.Empty, "Days: ningun dia.");
			Assert.That(stays.DayCount, Is.EqualTo(0), "DayCount: ninguno.");
			Assert.That(stays.StayCount, Is.EqualTo(0), "StayCount: ninguna estadia cerrada.");
			Assert.That(stays.Rows, Is.Empty, "Y ninguna fila que pintar.");

			Assert.That(stays.AnyStayInThePeriod, Is.False,
				"AnyStayInThePeriod es lo que decide si el bloque dibuja las filas o dice que no hay "
				+ "ninguna. Con una estadia abierta y ninguna cerrada, no hay filas.");
		}

		// Y LA MITAD POR INSTANTE DICE QUE EL CARRO ESTA ADENTRO, con la sucursal y el asunto cada uno
		// en su propia clausula. Sin esta mitad, lo de arriba seria toda la respuesta y estaria mal
		// leida.
		[Test]
		public void AndTheInstantKeyedHalfSaysTheCarIsInsideRightNow()
		{
			journal.Api.RecordShopEntry(TheBranch, TheSubject, TheEntry);

			TheStaysOfThisVehicle stays = WhatTheScreenIsHanded();

			Assert.That(stays.InsideRightNow, Is.True,
				"InsideRightNow decide si el bloque dibuja la linea de la estadia abierta.");

			Assert.That(stays.TheStayRightNowSays, Does.Contain("en " + TheBranch + " desde el"),
				"La sucursal, en su clausula. Contenerla suelta se queda verde con las columnas "
				+ "cruzadas.");

			Assert.That(stays.TheStayRightNowSays, Does.Contain("Entró por " + TheSubject),
				"Y el asunto en la suya.");

			Assert.That(stays.TheStayRightNowSays, Does.Contain("2026-04-06"),
				"Y el dia en que entro, que es lo unico que la mitad por dias no puede decir de esta "
				+ "estadia.");

			Assert.That(stays.Says, Is.Not.EqualTo(stays.TheStayRightNowSays),
				"Las dos frases son de dos preguntas distintas. Iguales, una de las dos mitades no "
				+ "estaria contestando lo suyo.");
		}

		// EL CONTROL, Y SIN EL LAS DOS DE ARRIBA NO MIDEN NADA: con el registro VACIO, la mitad por
		// dias dice lo mismo y la mitad por instante dice lo contrario. Esa es la unica diferencia
		// entre los dos estados, y es la que el bloque tiene que mostrar.
		[Test]
		public void AndWithNothingOnRecordTheSameTwoHalvesSayTheOppositeAboutTheInstant()
		{
			TheStaysOfThisVehicle stays = WhatTheScreenIsHanded();

			Assert.That(stays.Says, Does.Contain("días registrados"),
				"La mitad por dias dice lo MISMO en los dos estados, que es exactamente por lo que no "
				+ "alcanza sola.");

			Assert.That(stays.InsideRightNow, Is.False,
				"Y la mitad por instante dice lo contrario. Si esto fuera true, la afirmacion de "
				+ "arriba estaria midiendo un metodo que contesta siempre igual.");

			Assert.That(stays.TheStayRightNowSays, Does.Not.Contain(TheBranch),
				"Y la frase por instante no nombra ninguna sucursal, porque no hay ninguna estadia "
				+ "que cubra ese instante.");
		}

		private TheStaysOfThisVehicle WhatTheScreenIsHanded()
		{
			return TheStaysOfThisVehicle.ReadThrough(
				journal.Api, TheYearFrom, TheYearTo, TheInstantInsideTheOpenStay);
		}
	}
}
