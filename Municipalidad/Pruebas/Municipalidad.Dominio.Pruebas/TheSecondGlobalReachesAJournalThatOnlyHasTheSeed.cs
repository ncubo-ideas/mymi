using System;
using System.IO;
using Choreography.Theater;
using Municipalidad.App;
using NUnit.Framework;
using Puppeteer;

namespace Municipalidad.Dominio.Pruebas
{
	// EL PRIMER NIVEL B DE ESTE MODULO, y existe por una trampa que ninguna comprobacion pura ve.
	//
	// LA GUARDA DE upgrade ES UNA-VEZ-POR-ACTOR. upgrade('semilla') YA CORRIO en todo journal de
	// Municipalidad que exista -- el de medir, y el de cualquier persona que haya abierto la app.
	// Una linea agregada ADENTRO de 'semilla' no le llegaria a ninguno de ellos, en silencio: el
	// actor ya tiene la marca y el cuerpo no vuelve a correr. Peor: editar el cuerpo de un upgrade
	// ya aplicado revienta con LanguageException.
	//
	// Asi que la global nueva llega bajo un NOMBRE NUEVO. Esta prueba abre un journal con una app
	// que solo sabe de 'semilla' -- la de antes de M-2 -- lo cierra, y lo vuelve a abrir con la de
	// verdad. Si la migracion no llegara, la bitacora no contestaria.
	//
	// LOS DOS UPGRADES VIAJAN EN UN SOLO SCRIPT, y eso tambien se mide aca: cada PerformCommand deja
	// una entrada en CADA apertura aunque los upgrades ya esten aplicados y no hagan nada, asi que
	// dos comandos costarian dos entradas por apertura, para siempre, en el disco de cada persona.
	[TestFixture]
	public sealed class TheSecondGlobalReachesAJournalThatOnlyHasTheSeed
	{
		// DEC-06: un actor por instalacion, y se llama asi.
		private const string ElActor = "municipalidad";

		// Una sola raiz para todas las carpetas temporales del nivel B, para que barrerlas sea una
		// sola operacion.
		private static string LaRaizDeLosJournalsTemporales
		{
			get { return Path.Combine(Path.GetTempPath(), "municipalidad-nivel-b"); }
		}

		private string carpeta;

		[SetUp]
		public void Preparar()
		{
			carpeta = Path.Combine(LaRaizDeLosJournalsTemporales, Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(carpeta);
		}

		[TearDown]
		public void Limpiar()
		{
			try
			{
				if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}

		[Test]
		public void AJournalWrittenBeforeTheBitacoraExistedGetsItOnItsNextOpening()
		{
			// Un journal como los que ya existen: solo tiene 'semilla'.
			UnaAppDeAntesDeLaBitacora vieja = new UnaAppDeAntesDeLaBitacora(ElActor);
			vieja.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			vieja.Start();

			MunicipalidadAPI antes = new MunicipalidadAPI(vieja);

			// EL CONTROL NOMBRA UNA SOLA GLOBAL. Era EstadoDeLaCuenta hasta M-5, que le agrego
			// `ultimoEventoLeido` y con eso lo hizo cruzar las DOS globales de raiz -- y un script que
			// nombra dos globales no puede contestar en un journal que solo tiene una, que es
			// exactamente este journal. LoPagadoEn nombra solo `cuenta` y es TOTAL, asi que contesta
			// cero sin negarse.
			Assert.That(antes.LoPagadoEn(2024), Is.EqualTo(0m),
				"La cuenta si existe: 'semilla' corrio.");

			Assert.That(() => antes.UltimoEventoAplicado(), Throws.Exception,
				"Y la bitacora NO existe todavia, que es lo que hace que esta prueba pueda "
				+ "distinguir la migracion de no haber hecho nada. Sin este renglon los de abajo "
				+ "estarian verdes aunque 'semilla' hubiera creado las dos globales.");

			// Y EL CRUCE DE GLOBALES ES UN SEGUNDO TESTIGO DE LA MIGRACION, mas fuerte que el de
			// arriba: no se niega porque le falte un dato, se niega porque le falta una GLOBAL.
			//
			// MEDIDO EN M-5, y el mensaje del motor enganna: una global de raiz que no existe se
			// vuelve a resolver como un NOMBRE DE TIPO, sin distinguir mayusculas, asi que el fallo
			// sale como "Static method 'UltimoEvento' with a compatible signature was not found on
			// class 'Bitacora'" en vez de decir que la global falta.
			Assert.That(() => antes.EstadoDeLaCuenta(), Throws.Exception,
				"EstadoDeLaCuenta cruza cuenta Y bitacora desde M-5, asi que tampoco contesta aca.");

			vieja.Dispose();

			// La app de verdad, sobre el MISMO journal.
			MunicipalidadApp actual = new MunicipalidadApp(ElActor);
			actual.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			actual.Start();

			MunicipalidadAPI despues = new MunicipalidadAPI(actual);

			Assert.Multiple(() =>
			{
				Assert.That(despues.UltimoEventoAplicado(), Is.EqualTo(0L),
					"El upgrade nuevo se aplico donde faltaba. Cero es 'todavia no se leyo nada', "
					+ "un valor legal de esta escala: los eventos se numeran desde uno.");

				Assert.That(despues.ClaveDeDispositivoGuardada(), Is.EqualTo(""),
					"MunicipalidadAPI.ClaveDeDispositivoGuardada");
				Assert.That(despues.TokenGuardado(), Is.EqualTo(""),
					"MunicipalidadAPI.TokenGuardado");
				Assert.That(despues.TokenDeRefrescoGuardado(), Is.EqualTo(""),
					"MunicipalidadAPI.TokenDeRefrescoGuardado");

				Assert.That(despues.EstadoDeLaCuenta(), Does.Contain("fincas"),
					"Y la cuenta que ya estaba no se toco: 'semilla' sigue aplicada y no volvio a "
					+ "correr.");

				Assert.That(despues.EstadoDeLaCuenta(), Does.Contain("\"ultimoEventoLeido\":0"),
					"Y el cruce de las dos globales, que arriba se negaba, ahora contesta: es el "
					+ "segundo testigo de que la migracion corrio, y el que no puede confundirse con "
					+ "un dato faltante.");
			});

			actual.Dispose();
		}

		// LAS TRES LECTURAS GUARDADAS CONTESTAN EN LOS DOS ESTADOS, y por eso hace falta este
		// segundo caso: la prueba de arriba solo las ve VACIAS. Una lectura con dos caminos que se
		// corre una sola vez ejercita uno, y el que falta es justo el que llama al dominio.
		[Test]
		public void TheThreeGuardedReadsAnswerInBothStatesOfTheRecord()
		{
			MunicipalidadApp app = new MunicipalidadApp(ElActor);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			app.Start();

			MunicipalidadAPI api = new MunicipalidadAPI(app);

			Assert.Multiple(() =>
			{
				Assert.That(api.ClaveDeDispositivoGuardada(), Is.EqualTo(""), "sin registro");
				Assert.That(api.TokenGuardado(), Is.EqualTo(""), "sin credencial");
				Assert.That(api.TokenDeRefrescoGuardado(), Is.EqualTo(""), "sin refresco");
			});

			api.RegistrarDispositivo("DISPOSITIVO-1");
			api.GuardarCredencial("token-1", "refresco-1");

			Assert.Multiple(() =>
			{
				Assert.That(api.ClaveDeDispositivoGuardada(), Is.EqualTo("DISPOSITIVO-1"),
					"con registro");
				Assert.That(api.TokenGuardado(), Is.EqualTo("token-1"), "con credencial");
				Assert.That(api.TokenDeRefrescoGuardado(), Is.EqualTo("refresco-1"), "con refresco");
			});

			app.Dispose();
		}

		// LA SEMILLA CUESTA UNA ENTRADA POR APERTURA, no dos. Es lo que compra meter los dos
		// upgrades en un mismo script, y va medido porque es facil de perder sin que nada se rompa:
		// el costo se pagaria en cada apertura de cada persona, para siempre.
		[Test]
		public void TheSeedStillCostsExactlyOneEntryPerOpening()
		{
			MunicipalidadApp app = new MunicipalidadApp(ElActor);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			app.Start();

			long despuesDeLaPrimera = app.Actor.CurrentEntryId;
			app.Dispose();

			MunicipalidadApp otraVez = new MunicipalidadApp(ElActor);
			otraVez.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			otraVez.Start();

			Assert.That(otraVez.Actor.CurrentEntryId, Is.EqualTo(despuesDeLaPrimera + 1),
				"Reabrir crece UNA entrada: la de la semilla, que OnHydrated deja en cada "
				+ "hidratacion aunque los dos upgrades ya esten aplicados y no hagan nada.");

			otraVez.Dispose();
		}

		// La app tal como era antes de M-2: una sola global de raiz. Existe SOLO para que la
		// migracion tenga contra que medirse -- un journal en el que la bitacora falta de verdad.
		private sealed class UnaAppDeAntesDeLaBitacora : PerformanceV2
		{
			internal UnaAppDeAntesDeLaBitacora(string nombreDelActor)
				: base(nombreDelActor, typeof(Cuenta).Assembly)
			{
			}

			protected override void OnHydrated()
			{
				Using(@"
					upgrade('semilla') {
						cuenta = Cuenta();
					}
				")
				.PerformCommand();
			}
		}
	}
}
