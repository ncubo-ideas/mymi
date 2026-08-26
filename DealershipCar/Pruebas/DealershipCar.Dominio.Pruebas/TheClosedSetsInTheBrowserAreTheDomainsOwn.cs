using System;
using DealershipCar.Wasm;
using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LOS CONJUNTOS CERRADOS QUE LA PANTALLA OFRECE SON LOS DEL DOMINIO, MIEMBRO POR MIEMBRO.
	//
	// La caratula del browser no referencia el dominio -- no puede nombrar OperationKindCode, es
	// internal y una caratula no es uno de los dos amigos que AssemblyInfo.cs declara (C1/C25) --
	// asi que sus tres listas estan escritas a mano como texto, igual que las de la consola de T7.
	//
	// De los dos modos en que esa lista puede estar mal, solo uno se cuida solo:
	//
	//   un nombre MAL ESCRITO lo rechaza WireName en la puerta, ruidosamente y sin escribir nada;
	//   un miembro QUE FALTA no lo rechaza nadie -- simplemente no aparece en la pantalla, en
	//   silencio y para siempre.
	//
	// Lo segundo es lo que esta prueba cubre, y es lo que T7 solo pudo dejar dicho porque su
	// caratula no era referenciable desde aca. Va roja si al dominio se le agrega un miembro, se le
	// quita uno, o se le renombra uno, en cualquiera de los tres conjuntos.
	[TestFixture]
	public sealed class TheClosedSetsInTheBrowserAreTheDomainsOwn
	{
		private const string TheVin = "1HGCM82633A004352";

		[Test]
		public void TheOperationsTheScreenListsAreExactlyTheDomainsOperationKinds()
		{
			Assert.That(AppState.TheNineKinds, Is.EqualTo(Enum.GetNames(typeof(OperationKindCode))),
				"La lista de la pantalla se desincronizo del dominio. Una clase que el dominio tiene y "
				+ "la pantalla no es una fila que la persona nunca ve, sin ningun aviso.");
		}

		// EL PERFIL YA NO ES UN DESPLEGABLE -- T10 lo retiro de la pantalla por alcance -- pero la
		// lista sigue cruzando la puerta desde el comando retenido, y el perfil DE ENTREGA cruza en
		// cada apertura de un journal vacio, como argumento del constructor del agregado. Las dos
		// cosas se fijan igual: un nombre que el canon no escribe revienta en la puerta.
		[Test]
		public void TheUsageProfilesTheBrowserCanSendAreExactlyTheDomainsProfiles()
		{
			Assert.That(AppState.TheFourProfiles, Is.EqualTo(Enum.GetNames(typeof(UsageProfileCode))),
				"La lista de perfiles de la caratula se desincronizo del dominio. Un perfil que falta "
				+ "es una declaracion que la persona no va a poder hacer cuando la pantalla la ofrezca "
				+ "(L8), y uno de mas revienta en la puerta.");
		}

		[Test]
		public void TheProfileTheBrowserSeedsAVehicleWithIsOneOfTheDomainsProfiles()
		{
			Assert.That(Enum.GetNames(typeof(UsageProfileCode)), Contains.Item(AppState.TheProfileAtDelivery),
				"El perfil con el que la caratula siembra una agenda nueva no es miembro del conjunto "
				+ "cerrado del dominio. No es cosmetico: cruza en el constructor en CADA apertura de "
				+ "un journal vacio, asi que un nombre mal escrito deja la app sin poder abrir.");
		}

		[Test]
		public void TheDeferralReasonsTheScreenOffersAreExactlyTheDomainsReasons()
		{
			Assert.That(AppState.TheFiveDeferralReasons,
				Is.EqualTo(Enum.GetNames(typeof(DeferralReasonCode))),
				"El desplegable de razones se desincronizo del dominio. Una razon que falta es una que "
				+ "la persona no puede dar al aplazar (L6).");
		}

		// LA VERSION CON DIENTES, y hace falta porque coincidir con Enum.GetNames no demuestra que la
		// puerta acepte los nombres: es la misma disciplina que EveryScriptOfTheDoorHasBeenRun -- un
		// nombre mal escrito se descubre en RUNTIME, no al compilar (S5). Se manda cada miembro de
		// cada una de las tres listas por el comando de la puerta que la pantalla usa con ella.
		[Test]
		public void EveryNameTheScreenCanSendIsAcceptedByTheDoor()
		{
			using (var journal = new AScheduleOnARealJournal(TheVin, "Standard"))
			{
				DateTime at = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc);

				journal.Api.SeedPlanOperation(
					"FluidChange", "TimeOrDistance", 365, 10000, "UnitA", true, false,
					at, 12000, "UnitA");

				// Las nueve clases: la lectura total contesta para todas, agendadas o no (E41).
				foreach (string kind in AppState.TheNineKinds)
				{
					Assert.That(journal.Api.ReadDueState(kind), Is.Not.Empty, $"'{kind}' no dio estado.");
					Assert.That(journal.Api.ReadDueReason(kind), Is.Not.Empty, $"'{kind}' no dio motivo.");
				}

				// Las cinco razones, por el comando que las lleva. Aplazar fuera de un estado vencido
				// es un no-op del dominio (L6) y no tira, asi que lo que esto comprueba es justo lo que
				// interesa: que el NOMBRE cruza la puerta.
				foreach (string reason in AppState.TheFiveDeferralReasons)
					Assert.DoesNotThrow(
						() => journal.Api.DeferOperation("FluidChange", reason, at),
						$"la puerta rechazo la razon '{reason}', que la pantalla ofrece.");

				// Los cuatro perfiles, cada uno vigente DESPUES del anterior: OINV-15 dice que un
				// perfil rige hacia adelante, asi que mandarlos todos al mismo instante seria una
				// prueba que pasa por casualidad.
				int day = 0;
				foreach (string profile in AppState.TheFourProfiles)
				{
					DateTime effectiveFrom = at.AddDays(++day);
					Assert.DoesNotThrow(
						() => journal.Api.DeclareUsageProfile(profile, effectiveFrom),
						$"la puerta rechazo el perfil '{profile}', que la caratula puede mandar.");
				}
			}

			// Y EL PERFIL DE ENTREGA por el camino por el que viaja de verdad: el constructor del
			// agregado, en cada apertura de un journal vacio. No es el mismo camino que
			// DeclareUsageProfile y por eso se ejercita aparte.
			using (var recienNacido = new AScheduleOnARealJournal(TheVin, AppState.TheProfileAtDelivery))
			{
				Assert.That(recienNacido.Api.ReadVin(), Is.EqualTo(TheVin));
			}
		}

		// Y LA CAJA IMPORTA. WireName es estricto con las mayusculas a proposito (C25): el canon manda
		// en los nombres, asi que un desplegable que mandara 'fluidchange' tiene que ser rechazado en
		// la puerta y no llegar al journal con una grafia que el canon no escribe.
		[Test]
		public void TheSameNameInAnotherCaseIsRefusedAtTheDoor()
		{
			using (var journal = new AScheduleOnARealJournal(TheVin, "Standard"))
			{
				Assert.Throws<ArgumentException>(
					() => journal.Api.ReadDueState("fluidchange"));

				Assert.Throws<ArgumentException>(
					() => journal.Api.DeferOperation("FluidChange", "cost",
						new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)));
			}
		}
	}
}
