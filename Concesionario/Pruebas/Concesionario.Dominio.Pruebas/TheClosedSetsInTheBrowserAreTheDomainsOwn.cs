using System;
using Concesionario.App.Views;
using Concesionario.Wasm;
using Concesionario.Wasm.Parts;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
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
	// quita uno, o se le renombra uno, en cualquiera de los CINCO conjuntos.
	//
	// DESDE T10 SON CINCO. Las dos secciones nuevas -- Mi vehiculo y Garantia -- recorren las seis
	// clases de fluido y las tres posiciones de llanta, y las dos listas estan escritas a mano por
	// la misma razon que las otras tres. Y EL ORDEN TAMBIEN SE FIJA: la pantalla muestra las filas
	// en el orden de la lista, asi que el canon manda tambien en cual va primero.
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

		// EL PERFIL NO ES UN DESPLEGABLE NI HAY YA COMANDO QUE LO MANDE DESDE LA CARATULA: la app del
		// dueno es de SOLO LECTURA, asi que DeclareUsageProfileAsync se fue con esa regla igual que
		// aplazar. La lista se queda por ESTA prueba -- es el unico punto del ensamblado que espeja
		// UsageProfileCode miembro por miembro -- y el perfil DE ENTREGA si cruza de verdad, en cada
		// apertura de un journal vacio, como argumento del constructor del agregado. Las dos cosas se
		// fijan igual: un nombre que el canon no escribe revienta en la puerta.
		[Test]
		public void TheUsageProfilesTheBrowserMirrorsAreExactlyTheDomainsProfiles()
		{
			Assert.That(AppState.TheFourProfiles, Is.EqualTo(Enum.GetNames(typeof(UsageProfileCode))),
				"La lista de perfiles de la caratula se desincronizo del dominio. Ya no la manda "
				+ "ninguna pantalla, y por eso esta prueba es lo unico que la sostiene: un miembro que "
				+ "el dominio agregue y esta lista no no lo delata nadie mas.");
		}

		[Test]
		public void TheProfileTheBrowserSeedsAVehicleWithIsOneOfTheDomainsProfiles()
		{
			Assert.That(Enum.GetNames(typeof(UsageProfileCode)), Contains.Item(AppState.TheProfileAtDelivery),
				"El perfil con el que la caratula siembra una agenda nueva no es miembro del conjunto "
				+ "cerrado del dominio. No es cosmetico: cruza en el constructor en CADA apertura de "
				+ "un journal vacio, asi que un nombre mal escrito deja la app sin poder abrir.");
		}

		// LAS RAZONES TAMPOCO SON YA UN DESPLEGABLE, y por la misma regla que los perfiles: la app del
		// dueno es de solo lectura, asi que el desplegable de razones y DeferOperationAsync se fueron
		// juntos. La lista se queda por ESTA prueba -- es el unico punto del ensamblado que espeja
		// DeferralReasonCode miembro por miembro -- y aca no hay una constante como TheProfileAtDelivery
		// que cruce por otro camino: si esta lista se desincroniza, no lo delata nadie mas.
		[Test]
		public void TheDeferralReasonsTheBrowserMirrorsAreExactlyTheDomainsReasons()
		{
			Assert.That(AppState.TheFiveDeferralReasons,
				Is.EqualTo(Enum.GetNames(typeof(DeferralReasonCode))),
				"La lista de razones de la caratula se desincronizo del dominio. Ya no la manda ninguna "
				+ "pantalla, y por eso esta prueba es lo unico que la sostiene: una razon que el dominio "
				+ "agregue y esta lista no no la delata nadie mas (L6).");
		}

		// LAS SEIS CLASES DE FLUIDO, con su ORDEN. La seccion Mi vehiculo pinta una fila por miembro y
		// en este orden, asi que una clase que el dominio agregue y esta lista no es una fila que la
		// persona nunca ve -- y dos miembros cambiados de sitio es una hoja que se lee distinto de
		// como la escribe el canon. Is.EqualTo sobre arrays compara elemento por elemento y en orden,
		// que es exactamente lo que hace falta aca.
		[Test]
		public void TheFluidsTheScreenListsAreExactlyTheDomainsFluidKindsAndInThatOrder()
		{
			Assert.That(AppState.TheSixFluidKinds, Is.EqualTo(Enum.GetNames(typeof(FluidKindCode))),
				"La lista de fluidos de la pantalla se desincronizo del dominio. Una clase que el "
				+ "dominio tiene y la pantalla no es una fila que nunca aparece, en silencio y para "
				+ "siempre; y un orden distinto es una ficha que no se lee como la escribe el canon.");
		}

		// LAS TRES POSICIONES DE LLANTA, igual y por lo mismo. La de repuesto es la que mas facil se
		// cae de una lista escrita a mano, y es justo la que nadie mira hasta que la necesita.
		[Test]
		public void TheTyrePositionsTheScreenListsAreExactlyTheDomainsAndInThatOrder()
		{
			Assert.That(
				AppState.TheThreeTyrePositions, Is.EqualTo(Enum.GetNames(typeof(TyrePositionCode))),
				"La lista de posiciones de llanta de la pantalla se desincronizo del dominio.");
		}

		// LA VERSION CON DIENTES, y hace falta porque coincidir con Enum.GetNames no demuestra que la
		// puerta acepte los nombres: es la misma disciplina que EveryScriptOfTheDoorHasBeenRun -- un
		// nombre mal escrito se descubre en RUNTIME, no al compilar (S5). Se manda cada miembro de
		// cada una de las cinco listas por el comando o la lectura de LA PUERTA que le corresponde.
		//
		// DE LAS TRES LISTAS, HOY LA PANTALLA SOLO MANDA UNA. Las nueve clases se leen en cada fila
		// del tablero; los perfiles y las razones ya no salen de la caratula -- la app del dueno es de
		// solo lectura y sus dos comandos se fueron. Se siguen mandando por la puerta igual, porque lo
		// que esta prueba defiende es que el NOMBRE cruza, y la puerta no cambio.
		[Test]
		public void EveryNameTheBrowserMirrorsIsAcceptedByTheDoor()
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
						$"la puerta rechazo la razon '{reason}', que la lista de la caratula espeja.");

				// Los cuatro perfiles, cada uno vigente DESPUES del anterior: OINV-15 dice que un
				// perfil rige hacia adelante, asi que mandarlos todos al mismo instante seria una
				// prueba que pasa por casualidad.
				int day = 0;
				foreach (string profile in AppState.TheFourProfiles)
				{
					DateTime effectiveFrom = at.AddDays(++day);
					Assert.DoesNotThrow(
						() => journal.Api.DeclareUsageProfile(profile, effectiveFrom),
						$"la puerta rechazo el perfil '{profile}', que la lista de la caratula espeja.");
				}

				// LAS SEIS CLASES DE FLUIDO Y LAS TRES POSICIONES, por las lecturas con que la
				// seccion Mi vehiculo las pinta. Nada esta publicado en este journal, que es a
				// proposito: la lectura es TOTAL y contesta igual (L2), y la frase de lo no
				// publicado NOMBRA lo que se pregunto -- que es el discriminador que hace falta
				// (C60). Una lectura que contestara siempre por el aceite dejaria las seis filas
				// con la misma frase y un Is.Not.Empty no podria verlo.
				foreach (string kind in AppState.TheSixFluidKinds)
				{
					Assert.That(journal.Api.IsFluidPublished(kind), Is.False, $"'{kind}'");
					Assert.That(journal.Api.ReadFluidRevision(kind), Is.Empty, $"'{kind}'");
					Assert.That(journal.Api.DescribeFluid(kind), Does.StartWith(HowACanonNameReads.AFluid(kind) + ":"),
						$"la frase de '{kind}' no nombra a '{kind}'.");
				}

				foreach (string position in AppState.TheThreeTyrePositions)
				{
					Assert.That(journal.Api.IsTyrePublished(position), Is.False, $"'{position}'");
					Assert.That(journal.Api.ReadTyreRevision(position), Is.Empty, $"'{position}'");
					Assert.That(
						journal.Api.DescribeTyre(position), Does.StartWith("Llanta " + HowACanonNameReads.ATyre(position).ToLowerInvariant() + ":"),
						$"la frase de '{position}' no nombra a '{position}'.");
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

				// Y en los dos conjuntos de T10 igual, que es donde una lista escrita a mano con la
				// grafia de la pantalla ("Engine oil") en vez de la del canon se descubriria.
				Assert.Throws<ArgumentException>(() => journal.Api.DescribeFluid("engineoil"));
				Assert.Throws<ArgumentException>(() => journal.Api.DescribeTyre("front"));
			}
		}
	}
}
