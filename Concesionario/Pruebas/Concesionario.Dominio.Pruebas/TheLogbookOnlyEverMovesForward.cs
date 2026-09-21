using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LA BITACORA DE EVENTOS, EN NIVEL A. Es la segunda global de raiz que trajo T8 -- hasta donde
	// leyo la app la narrativa del concesionario -- y no es un hecho de mantenimiento: por eso vive
	// AL LADO de MaintenanceSchedule y no adentro (C26).
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

		// LA CREDENCIAL NO SE PRUEBA ACA PORQUE YA NO VIVE ACA. Hasta el 2026-09-03 este archivo
		// tenia cinco pruebas mas -- registro de device, credencial guardada, refresh opcional, y
		// las tres lecturas guardadas por su pregunta de presencia --. Se fueron con el estado que
		// fijaban: la credencial la sostiene el host, fuera del journal. Lo que queda es lo que
		// justifico este archivo: la guarda de Advance, inalcanzable por la puerta y por eso
		// probada contra el objeto.
	}
}
