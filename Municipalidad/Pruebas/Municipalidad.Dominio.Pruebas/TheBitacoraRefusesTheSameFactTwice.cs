using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// REGLA 18 EN LA SEGUNDA GLOBAL DE RAIZ: el mismo hecho otra vez se rechaza, para que un replay
	// no lo cuente dos veces y un feed que reentrega tampoco.
	//
	// Es la misma leccion que destapo el ciclo con journal corrido dos veces en Cuenta -- huecos 12
	// y 13 -- aplicada a los tres hechos que la bitacora guarda: hasta donde se leyo, con que clave
	// se registro esta instalacion, y con que credencial se sigue leyendo.
	//
	// LOS TRES SON MUTACIONES, asi que niegan con DomainException y cada una tiene su predicado
	// total: adentro de un comando la entrada ya esta pagada, y un Check es la unica forma de
	// rechazar sin dejar rastro. Es la linea de DEC-08.
	[TestFixture]
	public sealed class TheBitacoraRefusesTheSameFactTwice
	{
		// EL PUNTO DE CONTROL SOLO AVANZA. Un evento ya procesado que vuelve a llegar es el mismo
		// hecho otra vez: se descarta, no se re-aplica.
		[Test]
		public void TheCheckpointOnlyEverMovesForward()
		{
			Bitacora bitacora = new Bitacora();

			Assert.That(bitacora.UltimoEvento(), Is.EqualTo(0L),
				"Cero es 'todavia no se leyo nada', no una ausencia: los eventos se numeran desde uno.");

			bitacora.Avanzar(7L);

			Assert.Multiple(() =>
			{
				Assert.That(bitacora.UltimoEvento(), Is.EqualTo(7L), "Bitacora.Avanzar");

				Assert.That(bitacora.PuedeAvanzar(7L), Is.False, "el mismo evento otra vez");
				Assert.That(bitacora.PuedeAvanzar(6L), Is.False, "un evento anterior");
				Assert.That(bitacora.PuedeAvanzar(8L), Is.True, "el siguiente");

				Assert.Throws<DomainException>(() => bitacora.Avanzar(7L), "Bitacora.Avanzar, repetido");
				Assert.Throws<DomainException>(() => bitacora.Avanzar(6L), "Bitacora.Avanzar, hacia atras");

				Assert.That(bitacora.UltimoEvento(), Is.EqualTo(7L),
					"Y una negativa no movio el punto de control.");
			});
		}

		// SE REGISTRA UNA VEZ. La misma clave otra vez es el mismo hecho otra vez, y otra clave
		// dejaria huerfana la credencial que la municipalidad ya emitio para la primera.
		[Test]
		public void TheInstallationRegistersExactlyOnce()
		{
			Bitacora bitacora = new Bitacora();

			Assert.Multiple(() =>
			{
				Assert.That(bitacora.PuedeRegistrarDispositivo(""), Is.False, "una clave vacia");
				Assert.That(bitacora.PuedeRegistrarDispositivo("   "), Is.False, "una clave en blanco");
				Assert.That(bitacora.PuedeRegistrarDispositivo("D-1"), Is.True, "la primera clave");
			});

			bitacora.RegistrarDispositivo("D-1");

			Assert.Multiple(() =>
			{
				Assert.That(bitacora.ClaveDeDispositivo(), Is.EqualTo("D-1"),
					"Bitacora.ClaveDeDispositivo");

				Assert.That(bitacora.PuedeRegistrarDispositivo("D-1"), Is.False, "la misma clave");
				Assert.That(bitacora.PuedeRegistrarDispositivo("D-2"), Is.False, "otra clave");

				Assert.Throws<DomainException>(
					() => bitacora.RegistrarDispositivo("D-1"), "Bitacora.RegistrarDispositivo, repetido");
				Assert.Throws<DomainException>(
					() => bitacora.RegistrarDispositivo("D-2"), "Bitacora.RegistrarDispositivo, otra clave");

				Assert.That(bitacora.ClaveDeDispositivo(), Is.EqualTo("D-1"),
					"Y una negativa no cambio la clave.");
			});
		}

		// LA CREDENCIAL SE ROTA, pero la que ya esta guardada es el mismo hecho otra vez. Las dos
		// mitades importan: sin la primera la app no puede renovar; sin la segunda un replay del
		// mismo bloque de eventos escribiria una entrada de mas en cada corrida.
		[Test]
		public void TheCredentialRotatesButTheStoredOneIsRefused()
		{
			Bitacora bitacora = new Bitacora();

			Assert.Multiple(() =>
			{
				Assert.That(bitacora.PuedeGuardarCredencial("", "r"), Is.False, "sin token de acceso");
				Assert.That(bitacora.PuedeGuardarCredencial("   ", "r"), Is.False, "un token en blanco");
				Assert.That(bitacora.PuedeGuardarCredencial("t-1", ""), Is.True, "sin refresco, que es legal");
			});

			bitacora.GuardarCredencial("t-1", "r-1");

			Assert.Multiple(() =>
			{
				Assert.That(bitacora.Token(), Is.EqualTo("t-1"), "Bitacora.Token");
				Assert.That(bitacora.TokenDeRefresco(), Is.EqualTo("r-1"), "Bitacora.TokenDeRefresco");

				Assert.That(bitacora.PuedeGuardarCredencial("t-1", "r-1"), Is.False, "la misma credencial");
				Assert.That(bitacora.PuedeGuardarCredencial("t-2", "r-1"), Is.True, "otro token de acceso");
				Assert.That(bitacora.PuedeGuardarCredencial("t-1", "r-2"), Is.True, "otro refresco");

				Assert.Throws<DomainException>(
					() => bitacora.GuardarCredencial("t-1", "r-1"), "Bitacora.GuardarCredencial, repetida");
			});

			bitacora.GuardarCredencial("t-2", "r-2");

			Assert.Multiple(() =>
			{
				Assert.That(bitacora.Token(), Is.EqualTo("t-2"), "rotada: el token de acceso");
				Assert.That(bitacora.TokenDeRefresco(), Is.EqualTo("r-2"), "rotada: el refresco");
			});
		}

		// UN PROVEEDOR QUE NO DA REFRESCO NO ES UN OLVIDO, y la bitacora lo dice con PuedeRotar en
		// false en vez de con un texto vacio que se veria igual que un refresco.
		[Test]
		public void ACredentialWithoutARefreshTokenSaysSoRatherThanStoringABlankOne()
		{
			Bitacora bitacora = new Bitacora();
			bitacora.GuardarCredencial("t-1", "");

			Assert.Multiple(() =>
			{
				Assert.That(bitacora.TieneCredencial(), Is.True, "Bitacora.TieneCredencial");
				Assert.That(bitacora.PuedeRotar(), Is.False, "Bitacora.PuedeRotar");
				Assert.Throws<DomainException>(
					() => bitacora.TokenDeRefresco(), "Bitacora.TokenDeRefresco");
			});
		}
	}
}
