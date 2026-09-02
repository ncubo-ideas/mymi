using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using DealershipCar.App;
using DealershipCar.App.Events;
using NUnit.Framework;
using Puppets.Portador;

namespace DealershipCar.Dominio.Pruebas
{
	// EL HALLAZGO DE T8, Y ES SOBRE C22. Escrito como prueba y no como prosa por la misma razon que
	// ARefusedCommandCostsAJournalEntryUnlessItIsGuarded: para que deje de ser cierto en rojo.
	//
	// T6 midio que una guarda del dominio que revienta ADENTRO del cuerpo deja una entrada
	// permanente, porque el motor escribe antes de ejecutar. T7 lo esquivo no ofreciendo los cuatro
	// comandos que lo hacen. T8 los RECIBE de un concesionario y no puede esquivarlos.
	//
	// LO QUE T8 DESCUBRE ES QUE LA MARCA SE MULTIPLICA, y eso no estaba en C22. El punto de control
	// vive en el mismo comando que revento, asi que no avanzo; el proveedor sirve desde ese punto; y
	// el refresh siguiente trae el MISMO hecho y deja OTRA entrada. Un solo hecho contradictorio
	// dejaria la app atascada en el, marcando el journal en cada apertura y sin volver a recibir
	// nada nuevo nunca. De una marca permanente a una marca por refresh, para siempre. Queda escrito
	// en CONTRADICTIONS C28.
	//
	// LA RESPUESTA: el punto de control avanza en un comando aparte, y el hecho se pierde. Perder UN
	// hecho que el dominio declaro imposible es estrictamente mejor que perder todos los que vengan
	// detras -- incluida la correccion que el concesionario mande despues. Y no es silencioso: el
	// motivo vuelve como rechazo y el bucle lo cuenta.
	//
	// LO QUE ESTO NO ES: un arreglo de C22. La entrada de la primera vez sigue quedando, porque
	// evitarla necesita que la fachada de §4 proyecte lo que cada guarda mira, y eso es un cambio de
	// dominio que T8 no hace. Esto acota el dano; no lo elimina.
	[TestFixture]
	public sealed class AFactTheDomainRefusesDoesNotBlockTheNarrative
	{
		private AScheduleOnARealJournal journal;

		[SetUp]
		public void Setup()
		{
			journal = new AScheduleOnARealJournal(ADealershipNarrative.TheVin, "Standard");

			Apply(ADealershipNarrative.Revision(1, "R1", "2025-09-01T00:00:00Z"));
			Apply(ADealershipNarrative.PlanOperation(
				2, "FluidChange", "TimeOrDistance", 365, 10000, true, false));
			Apply(ADealershipNarrative.Reading(
				3, 20000, "UnitA", "2026-03-01T00:00:00Z", "Telemetry"));
		}

		[TearDown]
		public void TearDown()
		{
			journal.Dispose();
		}

		// LOS TRES HECHOS RECIBIDOS QUE TODAVIA CICATRIZAN, nombrados. Ninguno se puede adelantar a
		// la frontera sin que MaintenanceSchedule proyecte lo que la guarda mira: cual revision
		// rige, si una clase esta agendada, y cual es la ultima lectura de esa unidad.
		[Test]
		public void TheseAreTheThreeReceivedFactsThatStillCostAPermanentEntry()
		{
			string before = TheTreeOf(journal.Folder);

			Assert.That(
				Apply(ADealershipNarrative.Reading(
					4, 100, "UnitA", "2026-04-01T00:00:00Z", "Telemetry")),
				Does.Contain("El odometro no va para atras"),
				"Una lectura que contradice la linea de observacion (C21).");

			Assert.That(
				Apply(ADealershipNarrative.Completion(
					5, "RecallRemedy", "2026-04-01T00:00:00Z", 20100, "UnitA")),
				Does.Contain("que esta agenda no tiene"),
				"Una completacion de una clase que no esta agendada (E32).");

			Assert.That(
				Apply(ADealershipNarrative.Revision(6, "R0", "2025-01-01T00:00:00Z")),
				Does.Contain("Las revisiones van hacia adelante"),
				"Una revision fechada antes de la que rige (OINV-19).");

			Assert.That(TheTreeOf(journal.Folder), Is.Not.EqualTo(before),
				"Y los tres dejaron entrada: el motor ya la habia escrito cuando el cuerpo corrio. "
				+ "Esto dice que hoy es asi, no que este bien -- ver C22.");
		}

		// LOS OCHO HECHOS DE LA ESPECIFICACION QUE CICATRIZAN PORQUE MIRAN LA HOJA. Es la misma ley
		// que arriba, sobre los once hechos que trajo T9: ninguna de estas ocho guardas se puede
		// adelantar a la frontera, porque todas preguntan por el estado del REGISTRO DE REVISIONES --
		// si el codigo esta tomado, si la revision existe, en que estado esta, en que estado esta su
		// sucesora -- y un Check no puede preguntar algo que la fachada no contesta.
		//
		// LA UNICA LECTURA POR CODIGO QUE HAY, ReadRevisionState, REVIENTA con un codigo que nadie
		// abrio (es su polaridad a proposito: la clave es una identidad del registro, no un punto de
		// un rango). Asi que un Check que la usara reventaria adentro del check en el caso que mas
		// importa, y compararla contra 'Draft' en el DSL seria escribir la ley del sello por segunda
		// vez, en otro idioma. Ninguna de las dos es mejor que la cicatriz.
		[Test]
		public void TheseAreTheSpecificationGuardsThatReadTheSheetAndStillCostAPermanentEntry()
		{
			// Cada renglon trae los hechos que la hoja necesita ANTES para llegar al estado que esa
			// guarda mira, y despues el hecho que se rechaza. Los renglones dependen del orden a
			// proposito: una guarda sobre un estado no se puede probar sin llevar la hoja a ese
			// estado, y llevarla es parte de lo que el renglon dice.
			(string Guard, SenalDeEvento[] First, SenalDeEvento Refused, string Says)[] refused =
			{
				("sellar una revision vacia",
					new[] { ADealershipNarrative.RevisionOpened(4, TheRevision, ThePublication) },
					ADealershipNarrative.RevisionSealed(5, TheRevision),
					"sin que se haya publicado nada adentro"),

				("publicar nombrando una revision que nadie abrio",
					NothingFirst,
					AFluidInto(6, "SPEC-QUE-NO-EXISTE"),
					"no tiene ninguna revision"),

				("abrir un codigo que ya esta tomado",
					NothingFirst,
					ADealershipNarrative.RevisionOpened(7, TheRevision, ThePublication),
					"ya tiene una revision"),

				("publicar en una revision que ya sello",
					new[]
					{
						AFluidInto(8, TheRevision),
						ADealershipNarrative.RevisionSealed(9, TheRevision),
					},
					AFluidInto(10, TheRevision),
					"deja de aceptar contenido cuando se sella"),

				("sellar dos veces la misma revision",
					NothingFirst,
					ADealershipNarrative.RevisionSealed(11, TheRevision),
					"Sellar es un hecho que pasa una sola vez"),

				("sustituir una revision por si misma",
					NothingFirst,
					ADealershipNarrative.RevisionSuperseded(12, TheRevision, TheRevision),
					"se sustituya a si misma"),

				("sustituir por una sucesora que todavia es borrador",
					new[]
					{
						ADealershipNarrative.RevisionOpened(
							13, TheSuccessor, "2026-02-01T00:00:00Z"),
					},
					ADealershipNarrative.RevisionSuperseded(14, TheRevision, TheSuccessor),
					"La sucesora tiene que estar sellada"),

				("sustituir una revision que todavia es borrador",
					NothingFirst,
					ADealershipNarrative.RevisionSuperseded(15, TheSuccessor, TheRevision),
					"Solo se sustituye la que estaba contestando"),
			};

			Assert.That(refused.Length, Is.EqualTo(8),
				"Son OCHO guardas de la especificacion que miran la hoja. Contarlas es lo que impide "
				+ "que la lista se acorte sin que nadie lo note.");

			foreach ((string guard, SenalDeEvento[] first, SenalDeEvento signal, string says)
				in refused)
			{
				foreach (SenalDeEvento entering in first)
					Assert.That(Apply(entering), Is.EqualTo(""),
						"El hecho que lleva la hoja al estado que mira '" + guard + "' tenia que "
						+ "entrar; si no entro, ese renglon no mide lo que dice.");

				Assert.That(Apply(signal), Does.Contain(says), guard);
			}

			Assert.That(WhatTheEngineSaysWhenItReopens(),
				Does.Contain("Rehydration execution failed"),
				"Y las ocho dejaron entrada: el motor ya la habia escrito cuando el cuerpo corrio. "
				+ "Esto dice que hoy es asi, no que este bien -- ver C22.");
		}

		// Y LAS CINCO QUE SON UNA FUNCION PURA DEL HECHO Y CICATRIZAN IGUAL. Es el hallazgo de T9 y
		// es una TERCERA categoria, distinta de las dos que C22 y C36 nombran.
		//
		// C22 dice que una guarda que mira el estado no se puede adelantar. C36 dice que la mitad que
		// la fachada SI proyecta si. Estas cinco no miran ningun estado -- solo los valores que llegan
		// en la senal -- y tampoco se pueden adelantar, por una razon de CAPA y no de estado: viven en
		// el CONSTRUCTOR de un tipo del dominio, y alcanzarlo desde la frontera significaria armar el
		// carrier del dominio ahi, que es justo la escalada que E3 manda hacer adentro de la fachada.
		// La unica forma de moverlas es que el dominio exponga un predicado estatico -- como T5 hizo
		// con IntervalRule.BasisIsNeverInferred, que es por eso la unica de las de la agenda que se
		// pudo adelantar -- y eso es un cambio de dominio que T9 no hace. Queda como finding.
		//
		// LO QUE SI SE ADELANTO, y es donde esta la linea: la frontera descarta lo que no se puede
		// DECIR -- un texto en blanco, un nombre que no es de ningun miembro, un negativo en una
		// escala que no los tiene -- y deja al dominio lo que es una LEY sobre valores que si se
		// pueden decir. Un cero de capacidad se puede decir; que signifique "no lleva ninguna" es la
		// ley del fluido, y copiarla a la frontera seria una segunda fuente de verdad.
		[Test]
		public void AndTheseFiveAreAPureFunctionOfTheFactAndStillCostAPermanentEntry()
		{
			Apply(ADealershipNarrative.RevisionOpened(4, TheRevision, ThePublication));

			(string Guard, SenalDeEvento Signal, string Says)[] refused =
			{
				("una capacidad en cero",
					ADealershipNarrative.Fluid(
						5, TheRevision, "EngineOil", "5W-30", "0", "UnitA", 10000, "UnitA", 365),
					"con una capacidad de 0"),

				("una presion en frio en cero",
					ADealershipNarrative.Tyre(
						6, TheRevision, "Front", "205/55R16", "0", "UnitA", "94V"),
					"con una presion en frio de 0"),

				("un intervalo sin ninguna de sus dos mitades",
					ADealershipNarrative.Interval(7, TheRevision, "FluidChange", 0, "UnitA", 0),
					"sin distancia y sin plazo"),

				("un hito en cero",
					ADealershipNarrative.Price(
						8, TheRevision, "FluidChange", 0, "UnitA", "89.90", "USD",
						ThePublication, "2026-12-31T00:00:00Z"),
					"Se nombro un hito de 0"),

				("una vigencia que termina antes de empezar",
					ADealershipNarrative.Price(
						9, TheRevision, "FluidChange", 10000, "UnitA", "89.90", "USD",
						"2026-12-31T00:00:00Z", ThePublication),
					"vigencia termina antes de empezar"),
			};

			Assert.That(refused.Length, Is.EqualTo(5),
				"Son CINCO leyes del dominio que son una funcion pura de la senal y viven en un "
				+ "constructor.");

			foreach ((string guard, SenalDeEvento signal, string says) in refused)
				Assert.That(Apply(signal), Does.Contain(says), guard);

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(9L),
				"Y la narrativa avanzo por las cinco: se pierde el hecho, no la ficha.");

			Assert.That(WhatTheEngineSaysWhenItReopens(),
				Does.Contain("Rehydration execution failed"),
				"Las cinco dejaron entrada, y esto es lo que las separa de las que la frontera si "
				+ "descarta: esas no llegan a haber comando y no hay entrada que reventar.");
		}

		// Y LAS DOS MITADES DEL SOBRE SON LA MISMA TERCERA CATEGORIA, un agregado mas alla. Se dicen
		// aparte de las cinco de arriba porque son de OTRO agregado y porque las cinco son las cinco:
		// una cuenta que crece cada vez que llega un tipo de mensaje nuevo no dice de que es la cuenta.
		//
		// LA LEY ES LA MISMA: las dos guardas son una funcion pura de la senal -- meses en cero,
		// distancia en cero -- viven en el CONSTRUCTOR del sobre, y alcanzarlas desde la frontera
		// significaria armar el sobre ahi. Asi que las dos cicatrizan, y solo un predicado estatico del
		// dominio las moveria.
		//
		// LA PUERTA SI TIENE SU COPIA -- TheEnvelopeCarriesBothOfItsHalves -- y la ingesta NO LA LLAMA,
		// a proposito: revienta con ArgumentOutOfRangeException, que no es DomainException, asi que
		// saldria de Apply sin que nadie la atrape y se llevaria el bloque de refresh entero. Para un
		// hecho RECIBIDO eso es estrictamente peor que la cicatriz, porque despues del hecho malo
		// vienen los buenos.
		[Test]
		public void AndBothHalvesOfTheEnvelopeAreThatSameThirdCategoryOneAggregateOver()
		{
			(string Guard, SenalDeEvento Signal, string Says)[] refused =
			{
				("un plazo en cero",
					ADealershipNarrative.Envelope(4, 0, 60000, "UnitB", ThePublication),
					"lleva sus dos mitades siempre"),

				("una distancia en cero",
					ADealershipNarrative.Envelope(5, 36, 0, "UnitB", ThePublication),
					"lleva sus dos mitades siempre"),
			};

			Assert.That(refused.Length, Is.EqualTo(2),
				"Son DOS leyes del sobre que son una funcion pura de la senal y viven en un "
				+ "constructor.");

			foreach ((string guard, SenalDeEvento signal, string says) in refused)
				Assert.That(Apply(signal), Does.Contain(says), guard);

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(5L),
				"Y la narrativa avanzo por las dos: se pierde el hecho, no el registro.");

			Assert.That(journal.Api.IsWarrantyEnvelopePublished(), Is.False,
				"Y no quedo medio escrito: un sobre refusado no publica nada.");

			Assert.That(WhatTheEngineSaysWhenItReopens(),
				Does.Contain("Rehydration execution failed"),
				"Las dos dejaron entrada. Es lo que las separa de las dos guardas de la ENTREGA, que la "
				+ "frontera si descarta -- una lectura negativa y una unidad que no nombra ningun "
				+ "miembro son las dos cosas que no se pueden DECIR.");
		}

		// EL CONTROL DE LAS DOS DE ARRIBA, y sin el las dos podrian estar midiendo lo que no dicen: lo
		// que la frontera SI descarta no deja cicatriz ninguna, ni la primera vez.
		[Test]
		public void AFactTheBoundaryDiscardsLeavesNoScarAtAll()
		{
			string before = TheTreeOf(journal.Folder);

			Assert.That(
				DealershipEventRoute.Route(ADealershipNarrative.Fluid(
					4, TheRevision, "EngineOil", "5W-30", "-4.2", "UnitA", 10000, "UnitA", 365)),
				Is.Null,
				"Una capacidad NEGATIVA no se puede decir en esa escala: se descarta en la frontera.");

			Assert.That(TheTreeOf(journal.Folder), Is.EqualTo(before),
				"Y descartar cuesta CERO: no llego a haber comando, asi que no hay entrada que "
				+ "reventar.");

			Assert.That(WhatTheEngineSaysWhenItReopens(),
				Does.Not.Contain("Rehydration execution failed"),
				"NO HAY CICATRIZ. Es la misma diferencia que mide la guarda de base y limites, por el "
				+ "otro camino: aca no se adelanto la guarda del dominio, se rechazo la senal antes "
				+ "de que hubiera algo que performar.");
		}

		// LA GUARDA QUE SI SE PUDO ADELANTAR, y la diferencia se mide con el oraculo que importa: si
		// el motor loguea un fallo de rehidratacion al reabrir.
		//
		// La combinacion base/limites no mira nada de la agenda, solo los tres valores de la
		// llamada, asi que la puerta se la pregunta a la guarda DEL DOMINIO -- la misma, no una
		// copia -- ANTES de performar. El hecho se rechaza sin que llegue a haber comando, asi que
		// no hay entrada que reventar y no hay cicatriz.
		[Test]
		public void ASeededOperationWhoseBasisContradictsItsLimitsIsRefusedBeforeItCanScar()
		{
			string rejection = Apply(ADealershipNarrative.PlanOperation(
				4, "TimedComponent", "TimeOnly", 550, 15000, true, true));

			Assert.That(rejection, Does.Contain("TimeOnly no admite limite de distancia"),
				"La base no se infiere: se rechaza.");

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(4L),
				"Y la narrativa igual avanza, como con cualquier otro rechazo.");

			Assert.That(WhatTheEngineSaysWhenItReopens(),
				Does.Not.Contain("Rehydration execution failed"),
				"NO HAY CICATRIZ. Es la unica de las guardas de C22 que se puede adelantar sin "
				+ "tocar el agregado, porque es la unica que no necesita preguntarle nada a la "
				+ "agenda.");
		}

		// Y EL OTRO LADO DEL MISMO ORACULO, para que el de arriba no pueda quedar verde por no
		// medir nada: un hecho que revienta ADENTRO del cuerpo SI deja la marca, y el motor la
		// loguea en cada arranque futuro de esta agenda. Para siempre.
		[Test]
		public void AFactRefusedInsideTheCommandBodyScarsTheJournalOnEveryFutureOpening()
		{
			Apply(ADealershipNarrative.Reading(4, 100, "UnitA", "2026-04-01T00:00:00Z", "Telemetry"));

			Assert.That(WhatTheEngineSaysWhenItReopens(),
				Does.Contain("Rehydration execution failed"),
				"El motor escribio la entrada antes de correr el cuerpo, asi que el rechazo quedo "
				+ "journalizado. Es el costo de C22, medido por su consecuencia y no por el "
				+ "tamano del archivo.");
		}

		// EL PUNTO DE CONTROL AVANZA IGUAL, que es lo que impide el atasco.
		[Test]
		public void TheCheckpointMovesPastARefusedFactSoTheNextOneCanArrive()
		{
			Apply(ADealershipNarrative.Reading(4, 100, "UnitA", "2026-04-01T00:00:00Z", "Telemetry"));

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(4L),
				"La bitacora paso el 4 aunque el hecho no entro. Sin esto el proveedor lo volveria "
				+ "a servir en cada refresh y cada refresh dejaria otra marca.");

			Assert.That(
				Apply(ADealershipNarrative.Reading(
					5, 21000, "UnitA", "2026-05-01T00:00:00Z", "Telemetry")),
				Is.EqualTo(""),
				"Y el hecho siguiente entra normal: la narrativa no quedo atascada.");

			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(5L));
		}

		// LA MARCA NO SE MULTIPLICA. Re-entregar el bloque que contiene el hecho rechazado no vuelve
		// a escribir: la guarda de idempotencia lo rechaza bajo el read lock, como a cualquier otro
		// evento ya procesado.
		[Test]
		public void RedeliveringARefusedFactDoesNotScarTheJournalASecondTime()
		{
			Apply(ADealershipNarrative.Reading(4, 100, "UnitA", "2026-04-01T00:00:00Z", "Telemetry"));

			string afterTheScar = TheTreeOf(journal.Folder);

			string rejection = Apply(
				ADealershipNarrative.Reading(4, 100, "UnitA", "2026-04-01T00:00:00Z", "Telemetry"));

			Assert.That(rejection, Does.Contain("evento ya procesado"),
				"Y lo rechaza por REPETIDO, no por imposible: ya nadie vuelve a ejecutar el hecho.");

			Assert.That(TheTreeOf(journal.Folder), Is.EqualTo(afterTheScar),
				"Byte a byte igual. La cicatriz es UNA, no una por refresh.");
		}

		// Y EL ESTADO SIGUE SIENDO CORRECTO. Es la mitad que S5 corrigio de su propia primera
		// lectura: el journal queda marcado, no envenenado.
		[Test]
		public void TheScarredJournalStillRehydratesToTheRightState()
		{
			Apply(ADealershipNarrative.Reading(4, 100, "UnitA", "2026-04-01T00:00:00Z", "Telemetry"));
			journal.Api.Evaluate(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));

			string live = journal.EveryDueStateItCanAnswer();
			long checkpoint = journal.Api.LastEventApplied();

			journal.CloseAndReopenFromTheJournal();

			Assert.That(journal.EveryDueStateItCanAnswer(), Is.EqualTo(live),
				"La entrada marcada se saltea y el replay sigue.");
			Assert.That(journal.Api.LastEventApplied(), Is.EqualTo(checkpoint),
				"Y el punto de control tambien sobrevivio: el comando que lo avanzo es otro, y ese "
				+ "no revento.");
		}

		private string Apply(SenalDeEvento signal)
		{
			return journal.Api.Apply(DealershipEventRoute.Route(signal));
		}

		private const string TheRevision = "SPEC-A";
		private const string TheSuccessor = "SPEC-B";
		private const string ThePublication = "2025-08-15T00:00:00Z";

		private static readonly SenalDeEvento[] NothingFirst = new SenalDeEvento[0];

		private static SenalDeEvento AFluidInto(long number, string revisionCode)
		{
			return ADealershipNarrative.Fluid(
				number, revisionCode, "EngineOil", "5W-30", "4.2", "UnitA", 10000, "UnitA", 365);
		}

		// LO QUE EL MOTOR DICE AL REHIDRATAR. Es el oraculo de la cicatriz, y es el unico que la
		// mide por lo que DUELE en vez de por el tamano del archivo: una entrada que revento hace
		// loguear "Rehydration execution failed. EntryId=N" en cada arranque futuro de esta agenda.
		//
		// SE ESCUCHAN LAS DOS SALIDAS, y esa es una medicion de T8 que corrige a T7 por la mitad. T7
		// midio que el motor manda "Starting ... Actor" y el avance del replay a la SALIDA ESTANDAR
		// -- cierto -- pero sus lineas de ERROR, incluida la del fallo de rehidratacion, van a
		// STDERR. Escuchar solo una dejaria este oraculo verde para siempre.
		//
		// Las dos originales se reponen pase lo que pase: dejarlas desviadas haria desaparecer la
		// salida del resto de la corrida.
		private string WhatTheEngineSaysWhenItReopens()
		{
			TextWriter originalOut = Console.Out;
			TextWriter originalError = Console.Error;
			StringWriter heard = new StringWriter();

			try
			{
				Console.SetOut(heard);
				Console.SetError(heard);
				journal.CloseAndReopenFromTheJournal();
			}
			finally
			{
				Console.SetOut(originalOut);
				Console.SetError(originalError);
			}

			return heard.ToString();
		}

		private static string TheTreeOf(string folder)
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
