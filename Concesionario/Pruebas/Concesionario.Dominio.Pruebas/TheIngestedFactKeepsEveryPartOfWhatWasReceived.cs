using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Concesionario.App.Events;
using NUnit.Framework;
using Puppets.Portador;

namespace Concesionario.Dominio.Pruebas
{
	// QUE CADA CAMPO DEL HECHO RECIBIDO LLEGUE AL CAMPO QUE LE TOCA, y no al de al lado.
	//
	// ESTE ARCHIVO EXISTE POR LA PASADA DE FALSIFICACION DE T8. Cruzar isMandatory con isSafety en
	// la ingesta, y hacer que el ancla tomara la unidad del limite, dejaron las pruebas EN VERDE: la
	// historia que se ingesta no tenia ninguna operacion en la que las dos banderas o las dos
	// unidades fueran distintas, asi que cruzarlas no cambiaba nada. Una historia en la que todo
	// coincide no puede mostrar que las cosas sean distintas.
	//
	// Es la misma clase de hueco que T6 cerro para la puerta -- y no lo hereda: la puerta y la
	// ingesta arman sus parametros por separado, asi que cada una necesita su testigo.
	[TestFixture]
	public sealed class TheIngestedFactKeepsEveryPartOfWhatWasReceived
	{
		private static readonly DateTime AfterSixMonths =
			new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

		// COMO EL JOURNAL ESCRIBE UN INSTANTE. Desde el motor 3.0.0-beta.10044 es el ISO 8601 de ida
		// y vuelta con la Z ("o" sobre un DateTime en UTC): 2025-10-02T02:30:45.0000000Z. Hasta
		// 10029 era MM/dd/yyyy HH:mm:ss, sin marca de zona. Los tres renglones que lo usan afirman
		// que la puerta convirtio a UTC y trunco al segundo; el formato es solo la lente con que se
		// lee el journal, asi que vive aca, en un solo lugar, para que el proximo cambio del motor
		// sea UNA linea.
		private const string TheJournalsInstantFormat = "o";

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

		// LAS DOS BANDERAS DEL PLAN NO SON INTERCAMBIABLES. "De seguridad" es lo que impide aplazar
		// (ley L6); "obligatoria" no. Una operacion de seguridad que NO es obligatoria distingue las
		// dos: si llegaran cruzadas, el aplazamiento entraria.
		[Test]
		public void AnOperationTheManufacturerClassedAsSafetyCannotBeDeferred()
		{
			Apply(ADealershipNarrative.PlanOperation(
				1, "TimedComponent", "TimeOnly", 100, 0, false, true));

			journal.Api.Evaluate(AfterSixMonths);

			Assert.That(journal.Api.ReadDueState("TimedComponent"), Is.EqualTo("Overdue"),
				"Cien dias desde el 1/9/2025 vencen mucho antes del 1/3/2026.");

			journal.Api.DeferOperation("TimedComponent", "Cost", AfterSixMonths);

			Assert.That(journal.Api.DeferralsUsed("TimedComponent"), Is.EqualTo(0),
				"La ley L6 no aplaza una operacion clasificada de seguridad. Con las dos banderas "
				+ "cruzadas en la ingesta esta llegaria como obligatoria-y-no-de-seguridad, el "
				+ "aplazamiento entraria, y este numero seria 1.");
		}

		// LA UNIDAD DEL LIMITE Y LA DEL ANCLA SON DOS COSAS (C18). Sembrada con el limite en UnitA y
		// el ancla en UnitB, la dimension de distancia nace suspendida por desajuste de unidades
		// (ley L7) -- que es la ley funcionando, no una forma a volver irrepresentable.
		[Test]
		public void TheAnchorCarriesItsOwnUnitAndNotTheLimitsOne()
		{
			Apply(ADealershipNarrative.PlanOperationMeasuredIn(
				1, "FluidChange", "TimeOrDistance", 365, 10000, true, false, "UnitA", "UnitB"));

			Apply(ADealershipNarrative.Reading(
				2, 20000, "UnitA", "2026-02-01T00:00:00Z", "Telemetry"));

			journal.Api.Evaluate(AfterSixMonths);

			Assert.That(journal.Api.ReadDueReason("FluidChange", TheLanguage.OfTheRecord),
				Does.Contain("las unidades nunca se convierten"),
				"El ancla esta en UnitB y las mediciones en UnitA: dos lineas de medida que nadie "
				+ "convierte. Si el ancla tomara la unidad del limite las dos serian UnitA, no "
				+ "habria desajuste, y esta frase no aparecerian.");
		}

		// LA PROCEDENCIA DE UNA LECTURA LLEGA AL JOURNAL, aunque ninguna ley de esta rebanada la
		// mire todavia.
		//
		// Y por eso se afirma sobre el SEGUNDO PROGRAMA y no sobre una lectura: OdometerReading
		// guarda su ReadingSource y nada en OS2 lo consulta, asi que reemplazarla por una constante
		// no cambia ni un estado de vencimiento -- medido en la pasada de falsificacion, defecto
		// E11. Lo que si tiene que ser cierto es que el hecho quede journalizado COMO LLEGO: la
		// procedencia es un hecho recibido (OINV-54) y la rebanada que la use la va a leer de ahi.
		[Test]
		public void TheProvenanceOfAReadingReachesTheJournalAsItsOwnName()
		{
			Apply(ADealershipNarrative.Reading(
				1, 13200, "UnitA", "2025-10-01T00:00:00Z", "CustomerDeclared"));

			string secondProgram = TheSecondProgramOf(journal.Folder);

			Assert.That(secondProgram, Does.Contain("'CustomerDeclared'"),
				"La procedencia viaja como el NOMBRE de su miembro, al lado de la sentencia y no "
				+ "dentro de ella: asi renumerar el enum no puede reinterpretar la historia (C25).");

			Assert.That(secondProgram, Does.Contain("schedule.RecordReading(value, unit, observedAt, source)"),
				"Y la sentencia lleva los parametros por nombre, no los valores interpolados.");

			Assert.That(secondProgram, Does.Contain("logbook.Advance(event)"),
				"En el MISMO comando que el hecho: es lo que hace exacto el resume tras una caida.");
		}

		// EL NUMERO DE EVENTO VIAJA COMO ARGUMENTO, que es lo que permite que el replay reuse el
		// punto de control recibido en vez de inventar uno.
		[Test]
		public void TheEventNumberRidesAlongsideTheSentenceAndNotInsideIt()
		{
			Apply(ADealershipNarrative.Reading(
				107, 13200, "UnitA", "2025-10-01T00:00:00Z", "Telemetry"));

			string secondProgram = TheSecondProgramOf(journal.Folder);

			Assert.That(secondProgram, Does.Contain("107,13200,'UnitA'"),
				"Los cinco valores del hecho, en orden, como argumentos.");

			Assert.That(secondProgram, Does.Not.Contain("logbook.Advance(107)"),
				"Y NUNCA el numero metido en el texto del comando: ahi el replay reproduciria el "
				+ "TEXTO en vez de reusar el dato que se recibio.");
		}

		// EL INSTANTE SE NORMALIZA TAMBIEN EN LA INGESTA, y hace falta decirlo aparte: la puerta
		// tiene su propio testigo desde T6 (TheDoorNormalizesEveryInstantItSends), pero prueba
		// RecordReading, no Apply, y los dos arman sus parametros por separado.
		//
		// LA SENAL TRAE UN DESPLAZAMIENTO HORARIO, que es lo unico que hace visible la mitad "a UTC"
		// de C24. Un instante terminado en Z ya viene en UTC, y la fraccion la trunca el journal de
		// todas formas (S4), asi que con la historia normal quitar la normalizacion no cambia NADA
		// -- medido en la pasada de falsificacion, defecto E12. Con un desplazamiento si cambia.
		//
		// EL LIMITE, DICHO: en una maquina que ya corra en UTC este renglon no puede fallar, porque
		// no habria conversion que hacer. Es correcto igual -- lo esperado se calcula con el mismo
		// ToUniversalTime que hace la puerta -- pero discrimina solo donde hay desplazamiento.
		[Test]
		public void AnInstantThatArrivesWithAnOffsetIsJournalizedInUtc()
		{
			const string WithAnOffset = "2025-10-01T20:30:45.987-06:00";

			Apply(ADealershipNarrative.Reading(1, 13200, "UnitA", WithAnOffset, "Telemetry"));

			DateTime received = DateTime.Parse(
				WithAnOffset, System.Globalization.CultureInfo.InvariantCulture);
			DateTime inUtc = received.ToUniversalTime();
			DateTime toTheWholeSecond = new DateTime(
				inUtc.Ticks - inUtc.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);

			Assert.That(
				TheSecondProgramOf(journal.Folder),
				Does.Contain(toTheWholeSecond.ToString(
					TheJournalsInstantFormat, System.Globalization.CultureInfo.InvariantCulture)),
				"El journal guarda el instante ya convertido a UTC y ya truncado al segundo. Sin la "
				+ "normalizacion en la ingesta guardaria la hora local, y la agenda mediria seis "
				+ "horas de diferencia sobre un hecho que llego bien.");
		}

		// ------------------------------------------ los hechos de la ficha, campo por campo
		//
		// LOS DOS PARES DE UNIDADES DEL FLUIDO SON DOS COSAS. La capacidad se publica en una unidad
		// de VOLUMEN y el intervalo en una de DISTANCIA, y las dos declaran UnitA y UnitB: al wire
		// llegan como el mismo par de nombres, y lo unico que dice sobre cual conjunto cerrado cae
		// cada uno es la firma del dominio (C52). Con las dos cruzadas en la ingesta, la hoja diria
		// que el aceite lleva 4.2 de distancia y se cambia cada 10000 de volumen.
		//
		// SE PUBLICAN DISTINTAS a proposito: con las dos en UnitA, cruzarlas no cambiaria nada y este
		// renglon quedaria verde para siempre -- que es exactamente el defecto que T8 midio con las
		// dos banderas del plan.
		[Test]
		public void TheFluidsVolumeUnitAndItsDistanceUnitAreTwoDifferentThings()
		{
			Apply(ADealershipNarrative.RevisionOpened(1, "R1", ThePublication));
			Apply(ADealershipNarrative.Fluid(
				2, "R1", "EngineOil", "5W-30", "4.2", "UnitB", 10000, "UnitA", 365));
			Apply(ADealershipNarrative.RevisionSealed(3, "R1"));

			Assert.That(journal.Api.DescribeFluid("EngineOil", TheLanguage.OfTheRecord),
				Does.Contain("capacidad 4,2 gal").And.Contain("cada 10.000 km"),
				"La capacidad sale en la unidad de volumen en que se publico y el intervalo en la de "
				+ "distancia en que se publico. Cruzadas, las dos mitades dirian lo contrario y las "
				+ "dos seguirian siendo nombres legales.");
		}

		// LA PRESION DE LA LLANTA VIAJA CON SU PROPIA UNIDAD, y es el mismo par de nombres una vez
		// mas: PressureUnit tambien declara UnitA y UnitB.
		[Test]
		public void TheTyresPressureCarriesTheUnitItWasPublishedIn()
		{
			Apply(ADealershipNarrative.RevisionOpened(1, "R1", ThePublication));
			Apply(ADealershipNarrative.Tyre(2, "R1", "Front", "205/55R16", "2.4", "UnitB", "94V"));
			Apply(ADealershipNarrative.RevisionSealed(3, "R1"));

			Assert.That(journal.Api.DescribeTyre("Front", TheLanguage.OfTheRecord),
				Does.Contain("presión en frío 2,4 psi"),
				"Quien la lee la va a marcar en un manometro: la unidad no es un adorno de la frase.");
		}

		// LOS DOS INSTANTES DE LA VIGENCIA NO SON EL MISMO. Cruzados, la vigencia terminaria antes de
		// empezar y el dominio la rechazaria -- asi que el testigo tiene que ser la frase, que dice
		// los dos y en orden.
		[Test]
		public void ThePriceKeepsItsTwoValidityInstantsInTheOrderTheyArrived()
		{
			Apply(ADealershipNarrative.RevisionOpened(1, "R1", ThePublication));
			Apply(ADealershipNarrative.Price(
				2, "R1", "FluidChange", 10000, "UnitA", "89.90", "USD",
				"2025-08-15T00:00:00Z", "2026-12-31T00:00:00Z"));
			Apply(ADealershipNarrative.RevisionSealed(3, "R1"));

			Assert.That(
				journal.Api.DescribeServicePrice(
					"FluidChange", 10000, "UnitA",
					new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), TheLanguage.OfTheRecord),
				Does.Contain("vigente del 2025-08-15 al 2026-12-31"),
				"El desde es el desde y el hasta es el hasta.");
		}

		// LAS DOS MITADES DEL LIMITE PROPIO DE UN SISTEMA TAMPOCO SON INTERCAMBIABLES: una son meses
		// y la otra una distancia, y cruzadas darian un limite de sesenta unidades de distancia y de
		// cien mil meses -- los dos numeros legales, la frase entera equivocada.
		[Test]
		public void TheSystemsOwnLimitKeepsItsMonthsApartFromItsDistance()
		{
			Apply(ADealershipNarrative.RevisionOpened(1, "R1", ThePublication));
			Apply(ADealershipNarrative.CoveredSystem(2, "R1", "POWERTRAIN", 60, 100000, "UnitA"));
			Apply(ADealershipNarrative.RevisionSealed(3, "R1"));

			Assert.That(
				journal.Api.DescribeCoverage(
					"POWERTRAIN", 36, 60000, "UnitA",
					new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc), 30000, 35000, "UnitA",
					new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), TheLanguage.OfTheRecord),
				Does.Contain("60 meses o 100.000 km"),
				"Y son los limites PROPIOS del sistema los que rigen sobre el sobre del vehiculo, "
				+ "que llego con 36 y 60000: mas especifico manda.");
		}

		// CADA GRADO DE COMBUSTIBLE LLEGA CON SU PROPIO TEXTO Y SU PROPIA POSTURA. Con el texto
		// cableado a una constante los tres grados serian el mismo, la especificacion los tomaria por
		// correcciones del anterior, y la hoja acabaria diciendo UN grado con la ultima postura -- que
		// aca es Prohibited, o sea la lista de permitidos vacia y el minimo vacio.
		//
		// LOS TRES SE PUBLICAN CON POSTURAS DISTINTAS y el minimo NO va primero: con el minimo
		// primero, una lectura que devolviera "el primer grado" acertaria por casualidad.
		[Test]
		public void EachFuelGradeKeepsItsOwnTextAndItsOwnStanding()
		{
			Apply(ADealershipNarrative.RevisionOpened(1, "R1", ThePublication));
			Apply(ADealershipNarrative.Fuel(2, "R1", "95 RON Premium", "Permitted"));
			Apply(ADealershipNarrative.Fuel(3, "R1", "91 RON regular", "Minimum"));
			Apply(ADealershipNarrative.Fuel(4, "R1", "E85 Flex", "Prohibited"));
			Apply(ADealershipNarrative.RevisionSealed(5, "R1"));

			Assert.That(journal.Api.ReadMinimumFuelGrade(), Is.EqualTo("91 RON regular"));
			Assert.That(journal.Api.PermittedFuelGrades(), Is.EqualTo(new[] { "95 RON Premium" }));
			Assert.That(journal.Api.ProhibitedFuelGrades(), Is.EqualTo(new[] { "E85 Flex" }));

			Assert.That(journal.Api.DescribeFuel(TheLanguage.OfTheRecord),
				Does.Contain("95 RON Premium (permitido)")
					.And.Contain("91 RON regular (mínimo)")
					.And.Contain("E85 Flex (prohibido)"));
		}

		// LA DISTANCIA Y LOS DIAS DEL INTERVALO PUBLICADO SON DOS COSAS, y cruzados la hoja diria que
		// el servicio se hace cada 365 unidades de distancia o cada 10000 dias -- los dos numeros
		// legales, la frase entera al reves.
		//
		// SE PUBLICAN DISTINTOS a proposito, por la misma razon que las dos unidades del fluido.
		[Test]
		public void ThePublishedIntervalKeepsItsDistanceApartFromItsTerm()
		{
			Apply(ADealershipNarrative.RevisionOpened(1, "R1", ThePublication));
			Apply(ADealershipNarrative.Interval(2, "R1", "FluidChange", 10000, "UnitA", 365));
			Apply(ADealershipNarrative.RevisionSealed(3, "R1"));

			Assert.That(
				journal.Api.DescribeServiceInterval(
					"FluidChange", 2000, "UnitA", TheLanguage.OfTheRecord),
				Does.Contain("cada 10.000 km o 365 días"));
		}

		// LO QUE UNA EXCLUSION EXCLUYE Y LA CONDICION BAJO LA CUAL MUERDE SON DOS TEXTOS. Cruzados, la
		// persona leeria la condicion como la exclusion y viceversa -- y los dos son texto publicado,
		// asi que ninguna ley los distingue: el unico testigo posible es la frase.
		[Test]
		public void AnExclusionKeepsWhatItExcludesApartFromTheConditionItBitesUnder()
		{
			Apply(ADealershipNarrative.RevisionOpened(1, "R1", ThePublication));
			Apply(ADealershipNarrative.CoveredSystem(2, "R1", "POWERTRAIN", 0, 0, "UnitA"));
			Apply(ADealershipNarrative.Exclusion(
				3, "R1", "POWERTRAIN", "wear parts", "when the part is a consumable"));
			Apply(ADealershipNarrative.RevisionSealed(4, "R1"));

			Assert.That(journal.Api.DescribeCoverageSummary(TheLanguage.OfTheRecord),
				Does.Contain(
					"Exclusión sobre 'POWERTRAIN': wear parts Publicada para aplicar cuando: when the part is a "
					+ "consumable."));
		}

		// LOS DOS TEXTOS DE LA REGLA DE OTRO TALLER SON DOS, y cruzados la persona leeria como regla
		// lo que conserva la cobertura. Los dos son texto publicado, asi que ninguna ley los
		// distingue: el unico testigo posible es la frase.
		[Test]
		public void TheServicingRuleKeepsItsTwoTextsApart()
		{
			Apply(ADealershipNarrative.RevisionOpened(1, "R1", ThePublication));
			Apply(ADealershipNarrative.ServicingPolicy(
				2, "R1", "service anywhere qualified", "keep every invoice"));
			Apply(ADealershipNarrative.RevisionSealed(3, "R1"));

			Assert.That(journal.Api.DescribeServicingElsewhereRule(
				2, 1, 0, TheLanguage.OfTheRecord),
				Does.Contain("Servicio en otro taller: \"service anywhere qualified\"")
					.And.Contain("Qué conserva la cobertura: \"keep every invoice\""));
		}

		// LOS DOS CODIGOS DE UNA SUSTITUCION SON DOS. Cruzados, la sucesora quedaria apagada y la
		// vieja contestando, que es la especificacion al reves.
		[Test]
		public void ASupersessionKeepsTheRetiredCodeApartFromTheOneThatReplacesIt()
		{
			Ingest(ADealershipNarrative.TheWholeSpecSheet());

			Assert.That(journal.Api.ReadRevisionState(ADealershipNarrative.TheFirstRevision),
				Is.EqualTo("Superseded"));
			Assert.That(journal.Api.ReadRevisionState(ADealershipNarrative.TheSecondRevision),
				Is.EqualTo("Sealed"));
			Assert.That(journal.Api.ReadCurrentRevisionCode(),
				Is.EqualTo(ADealershipNarrative.TheSecondRevision));
		}

		// EL CODIGO DE REVISION SE RECORTA EN LA PUERTA, y hace falta que se recorte en UN solo sitio:
		// quien publica y quien pregunta tienen que nombrar la misma cosa, y un codigo con espacios
		// seria otra clave del registro.
		//
		// SE AFIRMA SOBRE EL SEGUNDO PROGRAMA porque el recorte es algo que el journal NO hace (C53):
		// lo que se guarda es lo que la puerta MANDO, asi que el journal si puede ser testigo. Y se
		// afirma tambien por la lectura, porque un recorte que pasara solo en la frontera dejaria el
		// journal limpio y la clave del registro sucia.
		[Test]
		public void ARevisionCodeThatArrivesPaddedIsSentTrimmedAndAnswersUntrimmed()
		{
			Apply(ADealershipNarrative.RevisionOpened(1, "   R1   ", ThePublication));

			Assert.That(TheSecondProgramOf(journal.Folder), Does.Contain("'R1'"),
				"El journal guarda el codigo ya recortado.");
			Assert.That(TheSecondProgramOf(journal.Folder), Does.Not.Contain("'   R1   '"),
				"Y no guarda el crudo: sin el recorte, ese codigo con espacios seria la clave con "
				+ "que la revision queda nombrada para siempre.");

			Assert.That(journal.Api.ReadRevisionState("R1"), Is.EqualTo("Draft"),
				"Y la revision se llama R1, asi que quien pregunta por R1 la encuentra.");
		}

		// LOS TRES DECIMALES DE LA FICHA SE NORMALIZAN, y son TRES porque hay tres sitios que llaman a
		// la normalizacion -- la capacidad del fluido, la presion de la llanta y el monto del precio
		// -- no porque haya tres hechos. Un barrido de uno solo deja los otros dos sin testigo, que
		// es lo que la pasada de falsificacion de T9 midio: quitarle la normalizacion a la presion y
		// al monto dejo la suite en verde.
		//
		// LOS TRES LLEGAN CON UN CERO A LA DERECHA a proposito: el journal escribe un decimal sin los
		// ceros que sobran, asi que con 4.2 la normalizacion no cambia nada y el renglon quedaria
		// verde para siempre.
		//
		// SE COMPARA LO VIVO CONTRA LO REPRODUCIDO y no contra el journal, que es lo que C53 pide
		// para una normalizacion: el journal HACE la misma cosa, asi que mirarlo ahi no distingue.
		[Test]
		public void EveryDecimalOfTheSheetIsSentInTheShapeTheJournalWrites()
		{
			Apply(ADealershipNarrative.RevisionOpened(1, "R1", ThePublication));
			Apply(ADealershipNarrative.Fluid(
				2, "R1", "EngineOil", "5W-30", "4.20", "UnitA", 10000, "UnitA", 365));
			Apply(ADealershipNarrative.Tyre(3, "R1", "Front", "205/55R16", "2.40", "UnitA", "94V"));
			Apply(ADealershipNarrative.Price(
				4, "R1", "FluidChange", 10000, "UnitA", "89.90", "USD",
				ThePublication, "2026-12-31T00:00:00Z"));
			Apply(ADealershipNarrative.RevisionSealed(5, "R1"));

			(string Where, string Says, Func<string> Read)[] normalized =
			{
				("la capacidad del fluido", "capacidad 4,2 L",
					() => journal.Api.DescribeFluid("EngineOil", TheLanguage.OfTheRecord)),
				("la presión de la llanta", "presión en frío 2,4 bar",
					() => journal.Api.DescribeTyre("Front", TheLanguage.OfTheRecord)),
				("el monto del precio", "$89.90",
					() => journal.Api.DescribeServicePrice(
						"FluidChange", 10000, "UnitA",
						new DateTime(
							2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), TheLanguage.OfTheRecord)),
			};

			Assert.That(normalized.Length, Is.EqualTo(3),
				"Son TRES sitios donde la ingesta de la especificacion normaliza un decimal.");

			string[] live = new string[normalized.Length];

			for (int each = 0; each < normalized.Length; each++)
			{
				live[each] = normalized[each].Read();

				Assert.That(live[each], Does.Contain(normalized[each].Says),
					"La frase de " + normalized[each].Where + " dice el numero sin el cero que "
					+ "sobra.");
			}

			journal.CloseAndReopenFromTheJournal();

			for (int each = 0; each < normalized.Length; each++)
				Assert.That(normalized[each].Read(), Is.EqualTo(live[each]),
					"Y la respuesta reproducida desde el journal es la MISMA cadena para "
					+ normalized[each].Where + ". Sin la normalizacion, la viva llevaria el cero y la "
					+ "reproducida no: una respuesta que cambia sola al reabrir la app.");
		}

		// LOS TRES INSTANTES DE LA FICHA SE NORMALIZAN, y son TRES por la misma razon: el de la
		// publicacion de la revision y los dos de la vigencia del precio. La pasada de falsificacion
		// midio que quitarle la normalizacion al 'desde' del precio dejaba la suite en verde.
		//
		// LOS TRES LLEGAN CON DESPLAZAMIENTO HORARIO, que es lo unico que hace visible la mitad "a
		// UTC": un instante terminado en Z ya viene en UTC y la fraccion la trunca el journal de
		// todas formas.
		//
		// EL LIMITE, DICHO: en una maquina que ya corra en UTC estos renglones no pueden fallar,
		// porque no habria conversion que hacer. Son correctos igual -- lo esperado se calcula con el
		// mismo ToUniversalTime que hace la puerta -- pero discriminan solo donde hay desplazamiento.
		[Test]
		public void EveryInstantOfTheSheetThatArrivesWithAnOffsetIsJournalizedInUtc()
		{
			const string ThePublicationWithAnOffset = "2025-08-15T20:30:45.987-06:00";
			const string TheValidFromWithAnOffset = "2025-09-01T18:15:30.123-06:00";
			const string TheValidUntilWithAnOffset = "2026-12-31T22:45:10.456-06:00";

			Apply(ADealershipNarrative.RevisionOpened(1, "R1", ThePublicationWithAnOffset));
			Apply(ADealershipNarrative.Price(
				2, "R1", "FluidChange", 10000, "UnitA", "89.90", "USD",
				TheValidFromWithAnOffset, TheValidUntilWithAnOffset));

			string secondProgram = TheSecondProgramOf(journal.Folder);

			(string Where, string Received)[] offset =
			{
				("el instante de la publicacion", ThePublicationWithAnOffset),
				("el desde de la vigencia", TheValidFromWithAnOffset),
				("el hasta de la vigencia", TheValidUntilWithAnOffset),
			};

			Assert.That(offset.Length, Is.EqualTo(3),
				"Son TRES instantes que la ingesta de la especificacion normaliza.");

			Assert.Multiple(() =>
			{
				foreach ((string where, string received) in offset)
					Assert.That(secondProgram, Does.Contain(InUtcToTheWholeSecond(received)),
						"El journal guarda " + where + " ya convertido a UTC y ya truncado al "
						+ "segundo. Sin la normalizacion guardaria la hora local, y la procedencia o "
						+ "la vigencia quedarian seis horas corridas.");
			});
		}

		private static string InUtcToTheWholeSecond(string received)
		{
			DateTime read = DateTime.Parse(
				received, System.Globalization.CultureInfo.InvariantCulture);
			DateTime inUtc = read.ToUniversalTime();

			return new DateTime(
				inUtc.Ticks - inUtc.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc)
				.ToString(
					TheJournalsInstantFormat, System.Globalization.CultureInfo.InvariantCulture);
		}

		// Y LOS ONCE HECHOS MANDAN SUS VALORES COMO ARGUMENTOS Y NO INTERPOLADOS EN EL TEXTO. Es lo
		// que deja que el replay reuse el dato recibido en vez de reproducir una sentencia con el
		// valor cocido adentro -- y es un barrido sobre los once, porque un solo script que
		// interpolara alcanzaria para romper la promesa.
		[Test]
		public void EverySpecificationSentenceCarriesItsValuesAsArgumentsAndNotInsideTheText()
		{
			Ingest(ADealershipNarrative.TheWholeSpecSheet());

			string secondProgram = TheSecondProgramOf(journal.Folder);

			string[] sentences =
			{
				"specification.OpenRevision(revisionCode, publishedAt)",
				"specification.PublishFluidSpecification(",
				"specification.PublishTyreSpecification(",
				"specification.PublishFuelGrade(revisionCode, grade, standing)",
				"specification.PublishServiceInterval(",
				"specification.PublishServicePrice(",
				"specification.PublishCoveredSystem(",
				"specification.PublishCoverageExclusion(",
				"specification.PublishServicingPolicy(",
				"specification.SealRevision(revisionCode)",
				"specification.SupersedeRevision(revisionCode, supersededByCode)",
			};

			Assert.That(sentences.Length, Is.EqualTo(11));

			Assert.Multiple(() =>
			{
				foreach (string sentence in sentences)
					Assert.That(secondProgram, Does.Contain(sentence),
						"La sentencia lleva los parametros POR NOMBRE: " + sentence);
			});

			Assert.That(secondProgram, Does.Not.Contain("specification.SealRevision('MY2019-R1')"),
				"Y nunca el valor metido en el texto del comando: ahi el replay reproduciria el TEXTO "
				+ "en vez de reusar el dato que se recibio.");

			Assert.That(secondProgram, Does.Contain("logbook.Advance(event)"),
				"En el MISMO comando que el hecho, los once: es lo que hace exacto el resume tras "
				+ "una caida.");
		}

		// EL DESTINO DE ESTA PRUEBA ES DEFENDER A LAS DOS DE ARRIBA. Un extractor que devolviera
		// vacio -- o cualquier cosa constante -- las dejaria verdes para siempre, que es la leccion
		// que T6 y T7 aprendieron cada uno por su lado.
		[Test]
		public void TheExtractorUsedByThisFixtureCanTellTwoJournalsApart()
		{
			string empty = TheSecondProgramOf(journal.Folder);

			Apply(ADealershipNarrative.Reading(
				1, 13200, "UnitA", "2025-10-01T00:00:00Z", "Telemetry"));

			Assert.That(TheSecondProgramOf(journal.Folder), Is.Not.EqualTo(empty));
			Assert.That(empty, Does.Not.Contain("'Telemetry'"));
		}

		// ------------------------ los dos hechos del perfil del vehiculo, campo por campo

		// LA UNIDAD DEL SOBRE Y LA DE LA ENTREGA SON DOS COSAS, igual que las dos del fluido. Las dos
		// son DistanceUnit, asi que al wire llegan como el mismo par de nombres y cruzarlas es un cruce
		// entre dos hechos y no entre dos conjuntos.
		//
		// SE PUBLICAN DISTINTAS a proposito -- el sobre en millas y la entrega en kilometros -- porque
		// con las dos iguales cruzarlas no cambiaria nada. Y el resultado de la ley es que la mitad de
		// distancia queda SUSPENDIDA por desajuste de unidades, que es la ley funcionando: aca lo que
		// se afirma es de donde salio cada nombre.
		[Test]
		public void TheEnvelopesUnitAndTheDeliverysUnitAreTwoDifferentThings()
		{
			Apply(ADealershipNarrative.Envelope(1, 36, 60000, "UnitB", ThePublication));
			Apply(ADealershipNarrative.Delivered(2, "2025-03-01T00:00:00Z", 120, "UnitA"));

			Assert.That(journal.Api.DescribePublishedWarrantyEnvelope(TheLanguage.OfTheRecord),
				Does.Contain("60.000 mi"),
				"La distancia del sobre sale en la unidad en que se publico el SOBRE.");

			Assert.That(journal.Api.ReadDeliveryUnit(), Is.EqualTo("km"),
				"Y la de la entrega en la que se leyo LA ENTREGA. Cruzadas, las dos seguirian siendo "
				+ "nombres legales y la mitad de distancia dejaria de estar suspendida -- o empezaria a "
				+ "estarlo -- sin que nada revente.");
		}

		// LAS DOS MITADES DEL SOBRE NO SON INTERCAMBIABLES. Un plazo de 36 y una distancia de 60000 no
		// se pueden cruzar sin que la frase lo diga, y con 36/60000 el cruce daria un sobre de 60000
		// meses -- que es un numero perfectamente legal.
		[Test]
		public void TheEnvelopesTermAndItsDistanceDoNotCross()
		{
			Apply(ADealershipNarrative.Envelope(1, 36, 60000, "UnitB", ThePublication));

			Assert.That(journal.Api.DescribePublishedWarrantyEnvelope(TheLanguage.OfTheRecord),
				Does.Contain("36 meses").And.Contains("60.000 mi"));
		}

		// LOS DOS INSTANTES DE ESTE PAR TAMPOCO. El de la entrega y el de la publicacion del sobre son
		// dos hechos distintos que a menudo caen el mismo dia -- por eso la narrativa los pone
		// distintos: con los dos iguales, cruzarlos no cambiaria nada.
		[Test]
		public void TheDeliveryInstantAndThePublicationInstantAreTwoDifferentThings()
		{
			Apply(ADealershipNarrative.Delivered(1, "2025-03-01T00:00:00Z", 120, "UnitB"));
			Apply(ADealershipNarrative.Envelope(2, 36, 60000, "UnitB", "2026-01-01T00:00:00Z"));

			Assert.That(journal.Api.ReadDeliveredAt(),
				Is.EqualTo(new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc)));

			Assert.That(journal.Api.ReadEnvelopePublishedAt(),
				Is.EqualTo(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
		}

		// Y LOS DOS INSTANTES SE NORMALIZAN TAMBIEN POR ESTOS DOS SCRIPTS. Mismo motivo y mismo limite
		// que el renglon de la lectura de arriba: cada script arma sus parametros por separado, asi que
		// el testigo de uno no cubre al otro.
		[Test]
		public void TheTwoVehicleRecordInstantsAreJournalizedInUtcToo()
		{
			const string DeliveredWithAnOffset = "2025-03-01T20:30:45.987-06:00";
			const string PublishedWithAnOffset = "2026-01-01T20:30:45.987-06:00";

			Apply(ADealershipNarrative.Delivered(1, DeliveredWithAnOffset, 120, "UnitB"));
			Apply(ADealershipNarrative.Envelope(2, 36, 60000, "UnitB", PublishedWithAnOffset));

			string secondProgram = TheSecondProgramOf(journal.Folder);

			Assert.That(secondProgram, Does.Contain(AsTheJournalWritesIt(DeliveredWithAnOffset)),
				"El instante de la entrega, ya en UTC y ya truncado al segundo.");

			Assert.That(secondProgram, Does.Contain(AsTheJournalWritesIt(PublishedWithAnOffset)),
				"Y el de la publicacion del sobre. Sin la normalizacion en cada uno de los dos scripts "
				+ "el journal guardaria la hora local, y un sobre publicado a las 20:30 locales quedaria "
				+ "seis horas antes de donde esta.");
		}

		private static string AsTheJournalWritesIt(string withAnOffset)
		{
			DateTime inUtc = DateTime.Parse(
				withAnOffset, System.Globalization.CultureInfo.InvariantCulture).ToUniversalTime();

			DateTime toTheWholeSecond = new DateTime(
				inUtc.Ticks - inUtc.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);

			return toTheWholeSecond.ToString(
				TheJournalsInstantFormat, System.Globalization.CultureInfo.InvariantCulture);
		}

		private void Apply(SenalDeEvento signal)
		{
			Assert.That(journal.Api.Apply(DealershipEventRoute.Route(signal)), Is.EqualTo(""),
				"El hecho tenia que entrar; si no entro, lo que sigue no mide lo que dice.");
		}

		private void Ingest(System.Collections.Generic.IReadOnlyList<SenalDeEvento> signals)
		{
			foreach (SenalDeEvento signal in signals) Apply(signal);
		}

		private static readonly string ThePublication = "2025-08-15T00:00:00Z";

		// EL SEGUNDO PROGRAMA: las tiras imprimibles del binario del journal, que son las sentencias
		// del DSL y los blobs de argumentos. Es la misma tecnica que
		// TheJournalReplaysTheSameReceivedSequenceTheSame usa desde T6.
		private static string TheSecondProgramOf(string folder)
		{
			StringBuilder program = new StringBuilder();

			List<string> files = new List<string>(
				Directory.GetFiles(folder, "*.bin", SearchOption.AllDirectories));
			files.Sort(StringComparer.Ordinal);

			foreach (string file in files)
			{
				byte[] content;

				using (FileStream open = new FileStream(
					file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					content = new byte[open.Length];
					open.ReadExactly(content, 0, content.Length);
				}

				StringBuilder run = new StringBuilder();

				foreach (byte each in content)
				{
					bool printable = each >= 0x20 && each <= 0x7e;

					if (printable) run.Append((char)each);
					else
					{
						if (run.Length >= 6) program.Append(run).Append('\n');
						run.Clear();
					}
				}

				if (run.Length >= 6) program.Append(run).Append('\n');
			}

			return program.ToString();
		}
	}
}
