using NUnit.Framework;

namespace DealershipCar.Dominio.Pruebas
{
	// LA BITACORA DE EVENTOS, EN NIVEL A. Es la segunda global de raiz que trajo T8 -- hasta donde
	// leyo la app la narrativa del concesionario, y con que credencial la sigue leyendo -- y no es
	// un hecho de mantenimiento: por eso vive AL LADO de MaintenanceSchedule y no adentro (C26).
	//
	// ESTE ARCHIVO EXISTE POR LA PASADA DE FALSIFICACION DE T8, y esa es la razon que vale la pena
	// leer. Romper la guarda de Advance -- dejarla aceptar el mismo evento otra vez, y despues
	// sacarla entera -- dejo las 395 pruebas EN VERDE y la consola sin cambios. La guarda es la
	// segunda linea de defensa, y la primera (el Check de la puerta contra el punto de control
	// journalizado) no deja pasar nunca un evento repetido, asi que a traves de la puerta la segunda
	// es inalcanzable por construccion.
	//
	// Una guarda que no se puede alcanzar por la puerta se prueba donde SI se alcanza: contra el
	// objeto, que es exactamente para lo que existe el nivel A (E4, clausula sin fixture). El dia
	// que alguien se lleve la guarda "porque el Check ya lo cubre", esto se pone rojo.
	[TestFixture]
	public sealed class TheLogbookOnlyEverMovesForward
	{
		private Logbook logbook;

		[SetUp]
		public void Setup()
		{
			logbook = new Logbook();
		}

		// Cero no es una ausencia: es una posicion legal de esta escala, porque los eventos de un
		// proveedor se numeran desde uno. Por eso LastEvent es TOTAL y no guardada.
		[Test]
		public void ANewLogbookHasReadNothingAndThatIsAValueNotAnAbsence()
		{
			Assert.That(logbook.LastEvent(), Is.EqualTo(0L));
			Assert.That(logbook.HasADevice(), Is.False);
			Assert.That(logbook.HasACredential(), Is.False);
			Assert.That(logbook.CanRotate(), Is.False);
		}

		[Test]
		public void AdvancingMovesTheCheckpointToTheEventThatEntered()
		{
			logbook.Advance(101);

			Assert.That(logbook.LastEvent(), Is.EqualTo(101L));

			logbook.Advance(118);

			Assert.That(logbook.LastEvent(), Is.EqualTo(118L));
		}

		// EL EVENTO REPETIDO. Es la idempotencia de la ingesta vista desde el dominio: la guarda de
		// la puerta lo rechaza antes, sin escribir nada, y esta lo rechazaria igual si alguien
		// llamara al comando por otro camino.
		[Test]
		public void AnEventThatWasAlreadyAppliedDoesNotEnterTwice()
		{
			logbook.Advance(101);

			Assert.Throws<DomainException>(() => logbook.Advance(101));

			Assert.That(logbook.LastEvent(), Is.EqualTo(101L),
				"Y el rechazo no movio nada.");
		}

		[Test]
		public void AnEventBehindTheCheckpointDoesNotEnterEither()
		{
			logbook.Advance(118);

			Assert.Throws<DomainException>(() => logbook.Advance(101));

			Assert.That(logbook.LastEvent(), Is.EqualTo(118L));
		}

		// ------------------------------------------------------ la credencial

		// UNA SOLA VEZ POR INSTALACION. Re-registrarla con otra clave dejaria huerfana la credencial
		// vieja, porque el concesionario veria otro device.
		[Test]
		public void AnInstallationRegistersItselfOnceAndKeepsThatIdentity()
		{
			logbook.RegisterDevice("device-de-esta-instalacion");

			Assert.That(logbook.HasADevice(), Is.True);
			Assert.That(logbook.DeviceKey(), Is.EqualTo("device-de-esta-instalacion"));

			Assert.DoesNotThrow(() => logbook.RegisterDevice("device-de-esta-instalacion"),
				"Registrarse otra vez con LA MISMA clave es un arranque repetido, no un cambio de "
				+ "identidad: es lo que hace idempotente abrir la app dos veces.");

			Assert.Throws<DomainException>(() => logbook.RegisterDevice("otro-device"));

			Assert.That(logbook.DeviceKey(), Is.EqualTo("device-de-esta-instalacion"));
		}

		[Test]
		public void AnInstallationWithoutAKeyCannotRegister()
		{
			Assert.Throws<DomainException>(() => logbook.RegisterDevice(""));
			Assert.Throws<DomainException>(() => logbook.RegisterDevice("   "));
			Assert.Throws<DomainException>(() => logbook.RegisterDevice(null));
		}

		[Test]
		public void TheCredentialIsWhatItWasStoredWith()
		{
			logbook.StoreCredential("at_1234", "rt_5678");

			Assert.That(logbook.HasACredential(), Is.True);
			Assert.That(logbook.Token(), Is.EqualTo("at_1234"));
			Assert.That(logbook.CanRotate(), Is.True);
			Assert.That(logbook.RefreshToken(), Is.EqualTo("rt_5678"));
		}

		// El refresh token es OPCIONAL -- hay proveedores que solo dan el access token -- y su
		// ausencia tiene nombre: CanRotate contesta false, y la instalacion pide credencial nueva
		// con su clave de device en vez de rotar.
		[Test]
		public void ACredentialWithoutARefreshTokenCannotRotateAndSaysSo()
		{
			logbook.StoreCredential("at_1234", "");

			Assert.That(logbook.HasACredential(), Is.True);
			Assert.That(logbook.CanRotate(), Is.False);
			Assert.Throws<DomainException>(() => logbook.RefreshToken());
		}

		[Test]
		public void ACredentialWithoutAnAccessTokenIsNotACredential()
		{
			Assert.Throws<DomainException>(() => logbook.StoreCredential("", "rt_5678"));
			Assert.Throws<DomainException>(() => logbook.StoreCredential(null, "rt_5678"));

			Assert.That(logbook.HasACredential(), Is.False);
		}

		// LAS TRES LECTURAS GUARDADAS (E41). Ausencia de clave no es una clave vacia, y devolver ""
		// haria indistinguible "esta instalacion no se registro" de "se registro sin nombre". La
		// traduccion a "" para el mecanismo la hace la puerta, que es donde el contrato del Portador
		// la pide, y no el dominio.
		[Test]
		public void EachStoredValueIsGuardedByItsOwnPresenceQuestion()
		{
			Assert.That(logbook.HasADevice(), Is.False);
			Assert.Throws<DomainException>(() => logbook.DeviceKey());

			Assert.That(logbook.HasACredential(), Is.False);
			Assert.Throws<DomainException>(() => logbook.Token());

			Assert.That(logbook.CanRotate(), Is.False);
			Assert.Throws<DomainException>(() => logbook.RefreshToken());
		}
	}
}
