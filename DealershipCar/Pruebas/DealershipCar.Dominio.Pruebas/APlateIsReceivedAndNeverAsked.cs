using System;
using System.Collections.Generic;
using System.Reflection;
using DealershipCar.App;
using DealershipCar.App.Events;
using NUnit.Framework;
using Puppets.Portador;

namespace DealershipCar.Dominio.Pruebas
{
	// L1 Y §2 DEL BRIEF: la placa LLEGA de la agencia, por la puerta o por el feed. El dueno no es
	// fuente de un hecho de su propio vehiculo, y las TRES invenciones prohibidas aplican sin
	// suavizar: ningun parametro de ninguna tool, ningun default, ningun campo en ninguna pantalla.
	//
	// LA TENTACION AQUI ES ESPECIFICA Y SE VA A SENTIR. El cruce es "compara mi placa contra las de la
	// estacion", y el camino mas corto a una demo es una tool que reciba una placa. ESA ES LA
	// INVENCION. El asistente lee la placa HACIA AFUERA de este modulo y la lleva al otro; nunca trae
	// una.
	//
	// LOS TRES CENSOS QUE SE PONEN ROJOS POR LAS TRES INVENCIONES NO SON EL MISMO BARRIDO, y este
	// archivo se apoya en dos que ya existen y escribe el tercero:
	//
	//   la tool     TheStandingReachesTheAssistantAndNoToolAsksForIt afirma el conjunto EXACTO de
	//               parametros de toda tool. Una tool nueva con una placa adentro lo pone rojo sola:
	//               NO SE ESCRIBE NINGUNA PRUEBA PARALELA, se afirma que el instrumento sigue ahi;
	//
	//   la pantalla TheStandingReachesTheOwnersScreenAndNoControlAsksForIt barre TODO el markup por
	//               FORMAS que recogen un valor de una persona. Una forma no depende del hecho que
	//               recoja, asi que ya cubre la placa: mismo trato;
	//
	//   la puerta   NO LA CUBRE, y es el hueco que LP-0 tiene que tapar.
	//               TheOwnerIsNeverASourceOfAFactAboutTheirOwnVehicle discrimina por TIPO -- un int o
	//               un DateTime que no es clave de catalogo es una medicion -- y una placa es un
	//               string, que en esa puerta se lee como un NOMBRE y pasa. El censo se quedaria
	//               verde con "DescribePlateFor(string plate)" adentro. Por eso esta ley pide su
	//               propia afirmacion, POR NOMBRE, y esta abajo.
	[TestFixture]
	public sealed class APlateIsReceivedAndNeverAsked
	{
		private const string TheVin = ADealershipNarrative.TheVin;

		private const string ThePlateAsPublished = "BQR-482";
		private const string ItsJoinKey = "BQR482";

		private static readonly DateTime TheDayItWasAssigned =
			new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);

		private static readonly DateTime ALaterDay =
			new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

		// ===== PRUEBA 1 · LOS DOS CAMINOS ATERRIZAN EN UN HECHO, APLICADO UNA VEZ ==========

		[Test]
		public void ThePlateArrivesThroughTheDoorAndThroughTheFeedAndBothAnswerTheSame()
		{
			using (AScheduleOnARealJournal byTheDoor = AJournal())
			using (AScheduleOnARealJournal byTheFeed = AJournal())
			{
				APlateOnTheDoorAndInTheDomain.RecordAssignment(
					byTheDoor.Api, ThePlateAsPublished, TheDayItWasAssigned);

				Apply(byTheFeed, ThePlateSignal(41, ThePlateAsPublished, "2026-01-15T09:00:00Z"));

				foreach (KeyValuePair<string, string> each in TheFourAnswersOf(byTheFeed))
					Assert.That(TheFourAnswersOf(byTheDoor)[each.Key], Is.EqualTo(each.Value),
						"'" + each.Key + "' contesta distinto segun por cual de los dos caminos "
						+ "llego la placa. Los dos tienen que aterrizar en el MISMO comando del "
						+ "dominio.");
			}
		}

		// Y EL BLOQUE RECIBIDO SE APLICA UNA SOLA VEZ. Sin esto, los dos caminos podrian coincidir en
		// las respuestas y el feed dejar DOS filas por una senal repetida -- que es lo que pasa cuando
		// falta el par TheEventIsNew + logbook.Advance.
		[Test]
		public void TheSameSignalArrivingTwiceLeavesOneAssignmentAndNotTwo()
		{
			using (AScheduleOnARealJournal journal = AJournal())
			{
				SenalDeEvento signal = ThePlateSignal(41, ThePlateAsPublished, "2026-01-15T09:00:00Z");

				Apply(journal, signal);

				IMensajeDeEvento again = DealershipEventRoute.Route(signal);
				journal.Api.Apply(again);

				Assert.That(
					APlateOnTheDoorAndInTheDomain.AssignmentColumn(journal.Api, "AsPublished"),
					Has.Length.EqualTo(1),
					"La misma senal entro dos veces y dejo dos asignaciones. La bitacora solo avanza "
					+ "hacia adelante y un evento ya aplicado no se vuelve a aplicar.");
			}
		}

		// Y UNA SENAL QUE NO SE PUEDE DECIR SE DESCARTA, sin costar entrada. Un texto que no deja nada
		// al normalizar se rechaza en Interpret y no adentro del comando: adentro ya se pago la
		// entrada, y el rechazo quedaria como cicatriz permanente en el journal de una persona.
		[Test]
		public void ASignalWhosePlateNormalizesToNothingIsDiscardedAndCostsNoEntry()
		{
			// EL CONTROL VA PRIMERO, y sin el esta prueba esta VERDE por no existir el tipo de
			// evento: hoy la ruta descarta CUALQUIER senal de tipo PlateAssigned, asi que afirmar
			// que descarta la malformada no distingue "la rechazo" de "no la conoce".
			Assert.That(
				DealershipEventRoute.Route(
					ThePlateSignal(40, ThePlateAsPublished, "2026-01-15T09:00:00Z")),
				Is.Not.Null,
				"Una senal BIEN FORMADA tiene que pasar. Es LP-3, y hasta entonces los dos descartes "
				+ "de abajo no miden nada.");

			SenalDeEvento nothingLeft = ThePlateSignal(42, "---", "2026-01-15T09:00:00Z");

			Assert.That(DealershipEventRoute.Route(nothingLeft), Is.Null,
				"'---' no deja ninguna placa y la senal tiene que descartarse en la frontera. "
				+ "Descartar no cuesta ninguna entrada; rechazar adentro del comando si.");

			Assert.That(
				DealershipEventRoute.Route(ThePlateSignal(43, ThePlateAsPublished, "no-es-un-instante")),
				Is.Null,
				"Un instante que no se puede leer tambien se descarta.");
		}

		// ===== PRUEBAS 2 Y 3 · LOS DOS CENSOS QUE YA EXISTEN SIGUEN EN PIE ================

		// NO SE ESCRIBE NINGUNA PRUEBA PARALELA -- el brief lo pide asi para la tool y vale igual para
		// la pantalla. Lo que si se escribe es esto: que los dos instrumentos SIGAN AHI. Un censo que
		// alguien borra o afloja en LP-4 o LP-5 para que su tool nueva pase se lleva la unica defensa
		// que estas dos leyes tienen, y sin este renglon nada lo notaria.
		[TestCase(
			"TheStandingReachesTheAssistantAndNoToolAsksForIt",
			"TheToolsTakeTheseParametersAndNoOthers")]
		[TestCase(
			"TheStandingReachesTheAssistantAndNoToolAsksForIt",
			"NoParameterOfAnyToolAnywhereIsAMeasurement")]
		[TestCase(
			"TheStandingReachesTheOwnersScreenAndNoControlAsksForIt",
			"NoControlAnywhereOnTheScreenCollectsAFactAboutThisVehicle")]
		[TestCase(
			"TheDomainWritesNoEquality",
			"NoTypeOverridesEqualsOrGetHashCode")]
		public void TheStandingCensusesThatDefendThePlateAreStillInPlace(
			string fixtureName, string testName)
		{
			Type found = typeof(APlateIsReceivedAndNeverAsked).Assembly.GetType(
				"DealershipCar.Dominio.Pruebas." + fixtureName);

			Assert.That(found, Is.Not.Null,
				"El fixture '" + fixtureName + "' ya no existe. Es una de las defensas que esta "
				+ "rebanada NO reescribe porque ya estaban; si se borro, se borro la defensa.");

			Assert.That(found.GetMethod(testName), Is.Not.Null,
				"'" + fixtureName + "' ya no declara '" + testName + "'. Aflojar un censo se ve igual "
				+ "que arreglarlo, y esta es la unica prueba que mira si sigue estando.");
		}

		// ===== PRUEBA 7 · NINGUNA PREGUNTA DE LA PUERTA TOMA UNA PLACA ====================

		// LAS GRAFIAS. El otro modulo argumenta que el nombre varia por pais -- placa, matricula,
		// patente, chapa -- asi que reconocer solo "plate" seria reconocer un idioma. Y va como
		// DENY-LIST y no como allow-list a proposito: una lista de permitidos crece en silencio (C57),
		// una de prohibidos solo puede quedarse corta, y quedarse corta se arregla agregando un nombre.
		private static readonly string[] EverySpellingOfAPlate =
		{
			"plate", "placa", "matricula", "patente", "chapa", "licence", "license", "registration",
		};

		[Test]
		public void NoQuestionOnTheDoorTakesAPlateInAnySpelling()
		{
			var found = new List<string>();

			foreach (MethodInfo each in TheDoorsMembers())
			{
				if (each.ReturnType == typeof(void)) continue;

				foreach (ParameterInfo parameter in each.GetParameters())
					if (NamesAPlate(parameter.Name))
						found.Add(each.Name + "(" + parameter.Name + ")");
			}

			Assert.That(found, Is.Empty,
				"Una PREGUNTA de la puerta toma una placa: " + string.Join(", ", found) + ". L4 y §2 "
				+ "a la vez. 'Es esta mi placa' existe del OTRO lado del cruce, llaveada por los "
				+ "registros de la estacion; su espejo aca convierte al asistente en fuente de un "
				+ "hecho de este vehiculo. Y el censo de mediciones NO lo ve: una placa es un string, "
				+ "y en esa puerta un string se lee como un nombre.");
		}

		// EL ANCLA, y sin ella lo de arriba esta verde por no haber nada que mirar. Despues de LP-2 la
		// puerta SI tiene una placa -- en el comando que la registra -- y esta prueba dice que esta
		// exactamente ahi y en ningun otro sitio.
		[Test]
		public void AndTheOnlyMemberThatTakesOneAtAllIsTheCommandThatRecordsIt()
		{
			var found = new List<string>();

			foreach (MethodInfo each in TheDoorsMembers())
				foreach (ParameterInfo parameter in each.GetParameters())
					if (NamesAPlate(parameter.Name)) found.Add(each.Name);

			Assert.That(found, Is.EquivalentTo(new[] { APlateOnTheDoorAndInTheDomain.TheCommand }),
				"La placa tiene que entrar por UN miembro de la puerta y ser ese el comando que la "
				+ "registra. Con cero, la prueba de arriba esta verde sin haber mirado nada -- que es "
				+ "el estado de LP-0 y por eso este renglon esta rojo hasta LP-2.");

			Assert.That(
				APlateOnTheDoorAndInTheDomain.TheDoorsMember(
					APlateOnTheDoorAndInTheDomain.TheCommand).ReturnType,
				Is.EqualTo(typeof(void)),
				"Y REGISTRA: devuelve void. Un miembro que tomara una placa y contestara algo seria "
				+ "una pregunta llaveada por placa con otro nombre.");
		}

		// EL DISCRIMINADOR DISPARA. Un barrido que no encuentra nada y nunca se probo contra algo real
		// no distingue "no hay ninguna" de "no estoy mirando".
		[Test]
		public void TheDenyListFiresOnEverySpellingAndOnTheOnesWrittenAsPartOfALongerName()
		{
			foreach (string each in EverySpellingOfAPlate)
			{
				Assert.That(NamesAPlate(each), Is.True);
				Assert.That(NamesAPlate(each.ToUpperInvariant()), Is.True);
				Assert.That(NamesAPlate("the" + each + "OfThisVehicle"), Is.True,
					"Quien agregue el parametro de comodidad elige el nombre, asi que reconocer solo "
					+ "el nombre exacto seria un proxy que se rompe justo cuando importa.");
			}

			Assert.That(NamesAPlate("asOf"), Is.False);
			Assert.That(NamesAPlate("revisionCode"), Is.False);
			Assert.That(NamesAPlate("systemCode"), Is.False);
		}

		// ===== EL APAREJO =================================================================

		private static bool NamesAPlate(string name)
		{
			foreach (string each in EverySpellingOfAPlate)
				if (name.IndexOf(each, StringComparison.OrdinalIgnoreCase) >= 0) return true;

			return false;
		}

		private static MethodInfo[] TheDoorsMembers()
		{
			return typeof(DealershipCarAPI).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
		}

		private static AScheduleOnARealJournal AJournal()
		{
			return new AScheduleOnARealJournal(TheVin, "Standard");
		}

		// LAS CUATRO RESPUESTAS QUE SE COMPARAN, cada una NOMBRADA en su renglon. Comparar solo que
		// vienen no vacias no distinguiria una lectura que contesta siempre lo mismo (C60).
		private static Dictionary<string, string> TheFourAnswersOf(AScheduleOnARealJournal journal)
		{
			return new Dictionary<string, string>
			{
				{
					"published",
					APlateOnTheDoorAndInTheDomain.IsPublished(journal.Api, ALaterDay).ToString()
				},
				{
					"asPublished",
					APlateOnTheDoorAndInTheDomain.AsPublished(journal.Api, ALaterDay)
				},
				{
					"joinKey",
					APlateOnTheDoorAndInTheDomain.JoinKeyAsOf(journal.Api, ALaterDay)
				},
				{
					"sentence",
					APlateOnTheDoorAndInTheDomain.Describe(journal.Api, ALaterDay)
				},
			};
		}

		// EL PAYLOAD, con los nombres de campo en ingles camelCase como los de VehicleDelivered --
		// "deliveredAt", "reading", "unit" -- que es el patron que LP-3 copia.
		private static SenalDeEvento ThePlateSignal(long number, string plate, string assignedAt)
		{
			return new SenalDeEvento(number, APlateOnTheDoorAndInTheDomain.TheEventType,
				"{\"id\":" + number
				+ ",\"tipo\":\"" + APlateOnTheDoorAndInTheDomain.TheEventType + "\""
				+ ",\"plate\":\"" + plate + "\""
				+ ",\"assignedAt\":\"" + assignedAt + "\"}");
		}

		private static void Apply(AScheduleOnARealJournal journal, SenalDeEvento signal)
		{
			IMensajeDeEvento message = DealershipEventRoute.Route(signal);

			Assert.That(message, Is.Not.Null,
				"La ruta descarto " + signal.Tipo + ", asi que ese hecho no llego por el feed. Es "
				+ "LP-3: la clase del evento y el renglon en DealershipEventRoute todavia no existen.");

			Assert.That(journal.Api.Apply(message), Is.EqualTo(""),
				"El dominio rechazo " + signal.Tipo + " por el feed.");
		}

		// LA LLAVE QUE ESTE ARCHIVO ESPERA, escrita como literal para que el rojo diga cual es. No se
		// deriva de la placa: derivarla seria pedirle a la prueba la misma cuenta que esta midiendo.
		[Test]
		public void TheKeyThisFileExpectsIsTheOneTheOtherModulesRuleProduces()
		{
			Assert.That(ItsJoinKey, Is.EqualTo("BQR482"));
			Assert.That(ThePlateAsPublished, Is.Not.EqualTo(ItsJoinKey),
				"Las dos formas tienen que ser DISTINTAS o esta rebanada no prueba nada: la publicada "
				+ "lleva guion y la llave no.");
		}
	}
}
