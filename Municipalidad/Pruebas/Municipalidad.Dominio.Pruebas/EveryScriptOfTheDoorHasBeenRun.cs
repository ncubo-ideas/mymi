using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Municipalidad.App;
using Municipalidad.App.Events;
using NUnit.Framework;
using Puppeteer;
using Puppets.Portador;

namespace Municipalidad.Dominio.Pruebas
{
	// LA PRUEBA QUE DEC-10 DEJO PENDIENTE, Y LA RAZON POR LA QUE NO PODIA ESCRIBIRSE ANTES DE M-4.
	//
	// MEDIDO EN ESTE REPO, dos veces -- en M-3 con `PonerAlCobroo` y en M-4 con
	// `CostosDePagarTardeDeLosAtrazosAl`: un error de tipeo en un script del DSL NO se descubre al
	// compilar. El build queda VERDE en 0 errores y el fallo sale en RUNTIME, y si sale en el journal de
	// una persona la entrada se queda ahi haciendo loguear un fallo de rehidratacion en cada arranque
	// futuro, para siempre, aunque el estado siga siendo correcto.
	//
	// Asi que ninguna linea de DSL de MunicipalidadAPI puede llegar al journal de una persona sin
	// haberse ejecutado antes aca. NO mira reglas de negocio -- de eso se ocupan las otras siete
	// fixtures -- mira que el script exista, parsee, ate sus parametros y devuelva lo que dijo.
	//
	// POR QUE NO ANTES: M-4 reescribio los scripts de las CUATRO lecturas de lista, asi que un censo
	// escrito antes se habria reescrito entero en la tarea siguiente. DEC-10 lo dice con esas palabras
	// y lo agenda "inmediatamente despues de M-4".
	//
	// LAS OPERACIONES SON 38 Y LOS TEXTOS DE SCRIPT SON 66, y son dos cuentas distintas. Re-derivadas
	// con `puerta.ps1 -Modulo Municipalidad -Clase MunicipalidadAPI`, despues del avaluo por el feed:
	//
	//   11 comandos x 2 textos (el del Check y el del cuerpo)         = 22
	//    7 Aplicar... x 2 textos (el del Check y el del cuerpo)       = 14
	//   29 llamadas de consulta x 1 texto                             = 29
	//   MunicipalidadApp.OnHydrated, los dos upgrade en un texto      =  1
	//                                                                  ---
	//                                                                   66
	//
	// LOS SIETE Aplicar... SON PRIVADOS Y MANDAN SCRIPT, que es la primera vez que este modulo tiene esa
	// forma: hasta E-1, puerta.ps1 reportaba `0 llamadas al DSL en miembros no publicos`. Ahora reporta
	// SIETE llamadas ahi -- que son CATORCE textos, dos por llamada --, y una sola operacion publica
	// -- Aplicar -- `sin script propio`, porque despacha por tipo. Es la misma forma que
	// DealershipCarAPI.Apply tiene en el modulo de referencia.
	//
	// Las 29 llamadas de consulta salen de 26 lecturas porque TRES de ellas -- TokenGuardado,
	// TokenDeRefrescoGuardado y ClaveDeDispositivoGuardada -- son PRE-PREGUNTA: preguntan primero si
	// hay, y solo entonces leen. Dos scripts cada una. La cuenta se afirma en prosa y por eso envejece
	// en silencio; lo unico que la mantiene honesta es volver a correr puerta.ps1 y LEER su listado.
	//
	// M-5 LAS SUBIO DE 24 Y 38, y este censo se puso ROJO primero: al entrar las siete lecturas de
	// descubrimiento sin listarlas, el cruce por reflexion reporto
	// `Extra (7): < "ConsultaMasReciente", "HayAlgunaConsulta", "LosRubrosQueHay", "NumerosDeLasFincas",
	// "RubrosConHistoria", "TieneValorDeclaradoLaFinca", "ValorDeFincaAl" >`. Es la falla C57 -- una
	// lista de permitidos que crece en silencio -- vista antes de pagarla.
	//
	// M-7 LAS SUBIO DE 31 Y 45, y este censo se puso ROJO otra vez, esta vez por partida doble: los
	// TRES predicados nuevos -- SePuedeVerLaVariacionDe, SePuedeProyectar y SePuedeVerElFaltanteDeReserva,
	// que son el par de C5 para las tres lecturas compuestas guardadas -- salieron como `Extra (3)`, y
	// `TheOperationsAreThirtyOne` se puso roja con "But was: 34", que es lo que su nombre existe para
	// forzar. M-7 no agrego ningun texto de script fuera de esos tres: las frases entraron como una
	// linea mas adentro de scripts que ya existian.
	//
	// LOS DOS CAMINOS DE UN COMANDO SON DOS TEXTOS Y NO UNO. Un comando ACEPTADO corre el texto del
	// Check y el del cuerpo; uno RECHAZADO corre SOLO el del Check. Eso hace que un tipeo en el cuerpo
	// sea invisible para una prueba que solo es rechazada, que es la leccion medida en M-3 y por la que
	// los dos caminos se corren en dos metodos separados aca abajo.
	//
	// LA PUERTA NO TIENE NINGUN `if` DE DSL -- puerta.ps1 lo afirma: 0 de 48 textos --, asi que ningun
	// script tiene dos ramas y un estado del registro corre cada texto entero. Ese es el contraste con
	// Concesionario, cuyo EveryScriptOfTheDoorHasBeenRun corre varias lecturas en DOS estados porque
	// tres de sus scripts llevan un if. Aca los dos estados hacen falta por otra razon y solo en un
	// lugar: el `if` de las tres PRE-PREGUNTA es de C#, no del DSL, y su rama falsa devuelve "" sin
	// llegar al segundo script. La rama VERDADERA es la que corre los dos textos, asi que es la que
	// este censo necesita; la falsa corre un subconjunto y la mira
	// TheSecondGlobalReachesAJournalThatOnlyHasTheSeed.
	//
	// LO QUE EL CRUCE POR REFLEXION PRUEBA Y LO QUE NO, dicho y no disimulado. Prueba que nadie agrego
	// ni quito un miembro de la puerta sin tocar este archivo. NO prueba que el script de ese miembro se
	// haya corrido: quien agregue el nombre a la lista sin escribir la llamada rompe la obligacion
	// igual, solo que ahora tiene que hacerlo a proposito y por escrito. Es el mismo limite que el censo
	// de Concesionario declara para el suyo.
	[TestFixture]
	public sealed class EveryScriptOfTheDoorHasBeenRun
	{
		private const string ElActor = "municipalidad";
		private const string LaFinca = "F-1";
		private const string ElRubro = "recoleccion-de-basura";
		private const string LaClaveDelDispositivo = "DISPOSITIVO-DEL-CENSO";
		private const string ElToken = "token-del-censo";
		private const string ElRefresco = "refresco-del-censo";

		private static readonly DateTime LaConsulta = new DateTime(2025, 9, 3);
		private static readonly DateTime LaAdopcion = new DateTime(2025, 8, 1);

		private string carpeta;
		private MunicipalidadApp app;
		private MunicipalidadAPI api;

		[SetUp]
		public void Preparar()
		{
			carpeta = Path.Combine(
				Path.GetTempPath(), "municipalidad-censo", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(carpeta);

			app = new MunicipalidadApp(ElActor);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			app.Start();
			api = new MunicipalidadAPI(app);
		}

		[TearDown]
		public void Limpiar()
		{
			if (app != null) app.Dispose();

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

		// LOS ONCE COMANDOS POR EL CAMINO QUE ACEPTA, que es el que corre los VEINTIDOS textos: el del
		// Check y el del cuerpo de cada uno. El expediente que dejan es el estado en que las lecturas
		// contestan en vez de negarse.
		private void ElExpedienteCompleto()
		{
			api.IncorporarFinca(LaFinca);
			api.FijarAvaluoDeFinca(LaFinca, new DateTime(2025, 1, 10), 36000000m);
			api.DeclararValorDeFinca(LaFinca, new DateTime(2025, 1, 15), 60000000m);

			api.RegistrarPago("R-1", new DateTime(2024, 3, 31), 0m);
			api.AgregarLineaAlPago("R-1", ElRubro, "2024.03", 5000m, 0m);

			api.RegistrarPago("R-2", new DateTime(2024, 6, 30), 0m);
			api.AgregarLineaAlPago("R-2", ElRubro, "2024.06", 18000m, 310m);

			api.RegistrarConsulta(LaConsulta);
			api.AgregarPendiente(LaConsulta, ElRubro, "2025.09", 24000m, 45m);

			api.PonerAlCobro(ElRubro, "2025.10", 8000m, new DateTime(2025, 9, 10));

			api.AdoptarReserva(30000m, LaAdopcion);

			api.RegistrarDispositivo(LaClaveDelDispositivo);
			api.GuardarCredencial(ElToken, ElRefresco);
		}

		// TODA OPERACION DECLARADA DE LA PUERTA, ENUMERADA, Y COMO IGUALDAD DE CONJUNTOS contra la
		// reflexion. No es una lista de permitidos: se pone roja tambien si alguien BORRA una
		// operacion, y el rojo dice cual.
		private static readonly string[] EveryOperationThisFixtureRuns =
		{
			"AdoptarReserva", "AgregarLineaAlPago", "AgregarPendiente", "AhorroPorProntoPago",
			"AtrasosAl", "ClaveDeDispositivoGuardada", "DeclararValorDeFinca", "EstadoDeLaCuenta",
			"FaltanteDeReservaAl", "GuardarCredencial", "HistorialDe", "IncorporarFinca",
			"LoPagadoEn", "PeriodosPagadosSinComprobante", "PonerAlCobro", "RegistrarConsulta",
			"RegistrarDispositivo", "RegistrarPago", "ReservaPropuestaPara",
			"TokenDeRefrescoGuardado", "TokenGuardado", "UltimoEventoAplicado", "VariacionDe",
			"VencimientosAl",

			// LAS SIETE DE M-5, las lecturas de descubrimiento. Cuando entraron, este censo se puso
			// rojo con `Extra (7)` nombrandolas una por una, que es para lo que existe.
			"ConsultaMasReciente", "HayAlgunaConsulta", "LosRubrosQueHay", "NumerosDeLasFincas",
			"RubrosConHistoria", "TieneValorDeclaradoLaFinca", "ValorDeFincaAl",

			// LAS TRES DEL AVALUO: el comando con que la MUNICIPALIDAD fija el valor con que cobra, y
			// su par pregunta-total + lectura-guardada. Son las gemelas de TieneValorDeclaradoLaFinca y
			// ValorDeFincaAl, y estan las dos parejas porque son dos hechos de dos duenos: E48 pide que
			// cada mitad se pueda leer sola, y aca las mitades ni siquiera son del mismo autor.
			"FijarAvaluoDeFinca", "TieneAvaluoLaFinca", "AvaluoDeFincaAl",

			// LAS TRES DE M-7, el par de las tres lecturas compuestas guardadas. Es C5 tomando su
			// tercer arreglo candidato -- el par-- para que una caratula pueda preguntar antes en vez
			// de atrapar un tipo que no puede nombrar.
			"SePuedeProyectar", "SePuedeVerElFaltanteDeReserva", "SePuedeVerLaVariacionDe",

			// LA UNA DE E-2, la ingesta. Es la unica operacion publica que NO manda un script propio:
			// despacha por tipo a siete Aplicar... privados, y cada uno de esos manda el suyo. Por eso
			// puerta.ps1 ahora reporta `sin script propio 1`, que es la misma forma que
			// DealershipCarAPI.Apply tiene en el modulo de referencia.
			//
			// LOS OTROS DOS MIEMBROS DE IDiarioDeLaApp NO ESTAN EN ESTA LISTA Y NO FALTAN.
			// UltimoEventoProcesado se implementa de forma EXPLICITA, asi que no es un miembro publico
			// de la clase; y TokenGuardado ya estaba, porque la puerta ya lo tenia con ese nombre y esa
			// firma desde M-2.
			"Aplicar",
		};

		[Test]
		public void TheDoorRunsExactlyTheseOperationsAndNothingElseWasAddedWithoutBeingRun()
		{
			List<string> declaradas = new List<string>();

			foreach (MethodInfo cada in typeof(MunicipalidadAPI).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
				declaradas.Add(cada.Name);

			Assert.That(declaradas, Is.EquivalentTo(EveryOperationThisFixtureRuns),
				"La puerta gano o perdio una operacion y este censo no se entero. Un script del DSL que "
				+ "nunca se ejecuto se descubre al correrlo, y si eso pasa en el journal de una persona "
				+ "la entrada queda ahi haciendo loguear un fallo de rehidratacion en cada arranque "
				+ "futuro. Agregue la operacion nueva a esta lista Y escriba la llamada que corre su "
				+ "script.");
		}

		// Y LA CUENTA DE LA PROSA SE AFIRMA AL LADO DE LO QUE CUENTA, con el numero en el NOMBRE para
		// que subirla obligue a renombrar el metodo. Una cuenta afirmada solo en un comentario envejece
		// en silencio, que es lo que le paso cuatro veces al censo de Concesionario.
		[Test]
		public void TheOperationsAreThirtyEight()
		{
			Assert.That(EveryOperationThisFixtureRuns, Has.Length.EqualTo(38));
		}

		// Y SON ONCE COMANDOS Y VEINTISEIS LECTURAS, partido por lo que la puerta devuelve: un comando
		// devuelve el documento del rechazo, que es `string`, y desde M-7 la unica lectura que devuelve
		// un `string` es EstadoDeLaCuenta, que sigue contestando por print. Se afirma la suma y no el
		// reparto, porque el reparto lo dice puerta.ps1 leyendo el perform de cada miembro y la
		// reflexion no lo ve.
		[Test]
		public void TheDoorDeclaresNoOtherPublicMemberThanTheseOperations()
		{
			MethodInfo[] declaradas = typeof(MunicipalidadAPI).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

			Assert.Multiple(() =>
			{
				Assert.That(declaradas, Has.Length.EqualTo(38),
					"MunicipalidadAPI: miembros publicos de instancia declarados");

				Assert.That(
					typeof(MunicipalidadAPI).GetProperties(
						BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly),
					Is.Empty,
					"La puerta no expone ninguna propiedad: una operacion se pide, no se lee de un "
					+ "campo.");
			});
		}

		// LA SEMILLA CORRE EN OnHydrated, asi que para cuando la prueba arranca las DOS globales de raiz
		// existen. Es el texto de script numero 38 y el unico que no esta en MunicipalidadAPI. Si el
		// upgrade no hubiera corrido, la primera lectura reventaria con "cuenta no existe" -- o
		// "bitacora no existe" -- en vez de contestar.
		[Test]
		public void TheSeedRanAndLeftBothRootGlobals()
		{
			Assert.Multiple(() =>
			{
				Assert.That(api.EstadoDeLaCuenta(), Does.Contain("\"fincas\":0"),
					"upgrade('semilla') dejo la global `cuenta`");

				Assert.That(api.UltimoEventoAplicado(), Is.EqualTo(0L),
					"upgrade('bitacora') dejo la global `bitacora`, con el punto de control en cero");
			});
		}

		// LOS ONCE COMANDOS, CADA UNO POR EL CAMINO QUE ACEPTA. Son los VEINTIDOS textos de comando: el
		// del Check corre y contesta que si, y entonces corre el del cuerpo. Lo que se afirma es que
		// ninguno fue rechazado -- el marcador de rechazo es la propiedad EWI en la raiz del documento --
		// y que el journal crecio, que es lo que distingue "el script corrio" de "la llamada no revento".
		[Test]
		public void EveryCommandOfTheDoorParsesBindsAndRuns()
		{
			long antesDeTodo = app.Actor.CurrentEntryId;

			string[] loQueDevolvieron =
			{
				api.IncorporarFinca(LaFinca),
				api.FijarAvaluoDeFinca(LaFinca, new DateTime(2025, 1, 10), 36000000m),
				api.DeclararValorDeFinca(LaFinca, new DateTime(2025, 1, 15), 60000000m),
				api.RegistrarPago("R-1", new DateTime(2024, 3, 31), 0m),
				api.AgregarLineaAlPago("R-1", ElRubro, "2024.03", 5000m, 0m),
				api.RegistrarConsulta(LaConsulta),
				api.AgregarPendiente(LaConsulta, ElRubro, "2025.09", 24000m, 45m),
				api.PonerAlCobro(ElRubro, "2025.10", 8000m, new DateTime(2025, 9, 10)),
				api.AdoptarReserva(30000m, LaAdopcion),
				api.RegistrarDispositivo(LaClaveDelDispositivo),
				api.GuardarCredencial(ElToken, ElRefresco),
			};

			Assert.Multiple(() =>
			{
				Assert.That(loQueDevolvieron, Has.Length.EqualTo(11),
					"los once comandos de la puerta, cada uno una vez");

				for (int cual = 0; cual < loQueDevolvieron.Length; cual++)
					Assert.That(loQueDevolvieron[cual], Does.Not.Contain("EWI"),
						"El comando " + cual + " fue rechazado, asi que el texto de su CUERPO no corrio "
						+ "y un tipeo ahi adentro sigue sin descubrirse.");

				Assert.That(app.Actor.CurrentEntryId, Is.GreaterThan(antesDeTodo),
					"Y los once dejaron entradas: el journal crecio, que es la diferencia entre que el "
					+ "cuerpo se haya ejecutado y que la llamada no haya reventado.");
			});
		}

		// LOS SIETE Aplicar... DE E-2, POR SUS DOS CAMINOS, que son los CATORCE textos que la ingesta agrego.
		// Van aca y no solo en AReceivedEventBlockIsAppliedExactlyOnce por la obligacion que este censo
		// existe para tener: un script del DSL que nunca corrio se descubre al correrlo, y el journal de
		// una persona no es donde se quiere descubrirlo.
		//
		// SE CORREN LOS DOS CAMINOS Y NO SOLO EL QUE ACEPTA, por la leccion de M-3 medida en DEC-10: un
		// tipeo en el CUERPO es invisible para una prueba que solo es rechazada. La segunda vuelta es la
		// que corre solo los siete Check; la primera corre los catorce.
		[Test]
		public void EveryApplyOfTheIngestionParsesBindsAndRunsAndThenRefusesTheSameBlock()
		{
			long antesDeTodo = app.Actor.CurrentEntryId;

			IMensajeDeEvento[] laNarrativa = LosSieteHechos();

			Assert.Multiple(() =>
			{
				Assert.That(laNarrativa, Has.Length.EqualTo(7),
					"los siete hechos que la municipalidad manda, uno por tipo de mensaje");

				foreach (IMensajeDeEvento hecho in laNarrativa)
					Assert.That(api.Aplicar(hecho), Does.Not.Contain("EWI"),
						"El hecho " + hecho.Tipo + " fue rechazado, asi que el texto de su CUERPO no "
						+ "corrio y un tipeo ahi adentro sigue sin descubrirse.");

				Assert.That(app.Actor.CurrentEntryId, Is.GreaterThan(antesDeTodo),
					"Y los siete dejaron entradas.");
			});

			long despuesDeAplicar = app.Actor.CurrentEntryId;

			Assert.Multiple(() =>
			{
				foreach (IMensajeDeEvento hecho in laNarrativa)
					Assert.That(api.Aplicar(hecho), Does.Contain("ese evento ya se proceso"),
						"El mismo hecho otra vez lo rechaza el Check del punto de control: " + hecho.Tipo);

				Assert.That(app.Actor.CurrentEntryId, Is.EqualTo(despuesDeAplicar),
					"Y ninguno de los siete rechazos dejo entrada: es la mitad barata de la idempotencia, "
					+ "y es lo que hace que re-entregar un bloque cueste cero.");
			});
		}

		// LOS SIETE HECHOS, RUTEADOS DESDE SUS SENALES y no construidos a mano: construirlos a mano
		// saltearia la ruta y los interpretes, que es justo el camino que un feed recorre.
		private static IMensajeDeEvento[] LosSieteHechos()
		{
			List<IMensajeDeEvento> hechos = new List<IMensajeDeEvento>();

			foreach (SenalDeEvento senal in UnaNarrativaDeLaMunicipalidad.LaNarrativaEntera())
			{
				IMensajeDeEvento hecho = MunicipalidadEventRoute.Route(senal);

				Assert.That(hecho, Is.Not.Null,
					"La ruta descarto " + senal.Tipo + ", asi que este censo no correria su Aplicar.");

				hechos.Add(hecho);
			}

			return hechos.ToArray();
		}

		// LOS ONCE TEXTOS DE Check POR EL CAMINO QUE NIEGA, que es el otro camino de cada uno y el que
		// una corrida siempre aceptada no toca. Cada rechazo es una razon distinta que cruza la puerta.
		//
		// Y UN RECHAZO NO DEJA ENTRADA: es la unica forma de negar sin escribir en el journal de una
		// persona, y es lo que hace que preguntar adentro del Check valga la pena -- reglas 17 y 18.
		[Test]
		public void EveryCheckScriptOfTheDoorRefusesWithoutLeavingAnEntry()
		{
			ElExpedienteCompleto();

			long despuesDelExpediente = app.Actor.CurrentEntryId;

			string[] rechazos =
			{
				api.IncorporarFinca(LaFinca),
				api.FijarAvaluoDeFinca("F-9", new DateTime(2025, 1, 10), 36000000m),
				api.DeclararValorDeFinca("F-9", new DateTime(2025, 1, 15), 60000000m),
				api.RegistrarPago("R-1", new DateTime(2024, 3, 31), 0m),
				api.AgregarLineaAlPago("R-9", ElRubro, "2024.03", 5000m, 0m),
				api.RegistrarConsulta(LaConsulta),
				api.AgregarPendiente(LaConsulta, "cementerio", "2025.09", 24000m, 45m),
				api.PonerAlCobro(ElRubro, "2025.10", 8000m, new DateTime(2025, 9, 10)),
				api.AdoptarReserva(30000m, LaAdopcion),
				api.RegistrarDispositivo(LaClaveDelDispositivo),
				api.GuardarCredencial(ElToken, ElRefresco),
			};

			Assert.Multiple(() =>
			{
				Assert.That(rechazos, Has.Length.EqualTo(11),
					"los once textos de Check de la puerta, cada uno por su camino de no");

				for (int cual = 0; cual < rechazos.Length; cual++)
					Assert.That(rechazos[cual], Does.Contain("EWI"),
						"El Check " + cual + " NO nego, asi que este metodo no corrio su camino de no y "
						+ "el reparto entre los dos caminos es una ilusion.");

				Assert.That(app.Actor.CurrentEntryId, Is.EqualTo(despuesDelExpediente),
					"Y los once rechazos no dejaron NI UNA entrada: el Check niega antes de que el "
					+ "cuerpo del comando escriba.");
			});
		}

		// LOS TRES Check QUE NIEGAN SIN QUE NINGUN CUERPO HAYA CORRIDO, contra un journal recien
		// abierto. Es el unico metodo de este censo que es INDEPENDIENTE de los diez cuerpos, y existe
		// porque el de arriba no lo es: una negativa por "ya estaba" necesita que algo estuviera, y lo
		// que lo pone es el camino que acepta.
		//
		// MEDIDO, Y CORRIGE DEC-10. Un tipeo en el texto del CUERPO de estos tres deja este metodo
		// VERDE -- ningun cuerpo corre aca -- y un tipeo en el texto de su Check lo pone rojo. La otra
		// mitad de la frase de DEC-10 es falsa: un tipeo en un texto de Check NO es invisible para una
		// prueba siempre aceptada, porque PerformCheckThenCommand corre el Check en los DOS caminos.
		// Medido el 2026-09-08 con `PuedeIncorporarFincaa` y con `IncorporarFincaa`.
		[Test]
		public void TheCheckScriptOfAStateFreeRefusalRunsWithoutAnyCommandBodyHavingRun()
		{
			long recienAbierto = app.Actor.CurrentEntryId;

			string sinFinca = api.DeclararValorDeFinca("F-9", new DateTime(2025, 1, 15), 60000000m);
			string sinRecibo = api.AgregarLineaAlPago("R-9", ElRubro, "2024.03", 5000m, 0m);
			string sinConsulta = api.AgregarPendiente(LaConsulta, ElRubro, "2025.09", 24000m, 45m);

			Assert.Multiple(() =>
			{
				Assert.That(sinFinca, Does.Contain("EWI"),
					"DeclararValorDeFinca de una finca que no esta");
				Assert.That(sinRecibo, Does.Contain("EWI"),
					"AgregarLineaAlPago de un recibo que no esta");
				Assert.That(sinConsulta, Does.Contain("EWI"),
					"AgregarPendiente de una consulta que no esta");

				Assert.That(app.Actor.CurrentEntryId, Is.EqualTo(recienAbierto),
					"Y ninguno dejo entrada, sobre un journal que solo tiene la semilla.");
			});
		}

		// TODAS LAS LECTURAS, cada una contra el expediente completo, que es el unico estado en que
		// todas contestan. Lo que se afirma es la FORMA -- que el script parsee, ate sus parametros y
		// vuelva TIPADO -- mas un valor que NOMBRE su propia lectura: Is.Not.Empty prueba que llego una
		// respuesta y nunca que es de esta lectura.
		//
		// Las tres PRE-PREGUNTA estan aca por su camino de SI, que es el que corre sus DOS textos.
		//
		// LO QUE M-7 CAMBIO ACA ES DE QUE SE AFIRMA. Tres lecturas contestaban un documento de print y
		// se afirmaban con Does.Contain sobre su JSON; ahora vuelven tipadas y se afirma el campo. La
		// frase que cada una trae la mira TheSentenceOfEveryCarrierCrossesTheDoor, que es donde vive su
		// ley; aca solo tiene que haber llegado.
		[Test]
		public void EveryReadOfTheDoorParsesBindsAndComesBackTyped()
		{
			ElExpedienteCompleto();

			Assert.Multiple(() =>
			{
				Assert.That(api.UltimoEventoAplicado(), Is.EqualTo(0L),
					"UltimoEventoAplicado: cero es 'todavia no se leyo nada', y los eventos se numeran "
					+ "desde uno");
				Assert.That(api.TokenGuardado(), Is.EqualTo(ElToken), "TokenGuardado");
				Assert.That(api.TokenDeRefrescoGuardado(), Is.EqualTo(ElRefresco),
					"TokenDeRefrescoGuardado");
				Assert.That(api.ClaveDeDispositivoGuardada(), Is.EqualTo(LaClaveDelDispositivo),
					"ClaveDeDispositivoGuardada");

				Assert.That(api.EstadoDeLaCuenta(), Does.Contain("\"fincas\":1"), "EstadoDeLaCuenta");
				Assert.That(api.LoPagadoEn(2024), Is.EqualTo(23310m),
					"LoPagadoEn: 5000 del R-1 mas 18310 del R-2, que trae 310 de mora");
				Assert.That(api.LoPagadoEn(2023), Is.EqualTo(0m),
					"LoPagadoEn es TOTAL: un anio sin pagos contesta cero y no se niega");

				Assert.That(api.HistorialDe(ElRubro).Periodo, Is.EqualTo(new[] { "2024-03", "2024-06" }),
					"HistorialDe");
				Assert.That(api.HistorialDe(ElRubro).EstadoDicho, Does.Contain(ElRubro),
					"HistorialDe: y la frase del registro llego, nombrando su rubro");

				Assert.That(api.SePuedeVerLaVariacionDe(ElRubro), Is.True,
					"SePuedeVerLaVariacionDe: con dos pagos de basura, si");
				Assert.That(api.VariacionDe(ElRubro, 0.25m, 0.02m).Rubro, Is.EqualTo(ElRubro),
					"VariacionDe");

				Assert.That(
					api.VencimientosAl(new DateTime(2025, 9, 25), 7).Periodo,
					Is.EqualTo(new[] { "2025-09", "2025-10" }),
					"VencimientosAl: el pendiente acuno el cobro de setiembre y PonerAlCobro el de "
					+ "octubre, y ninguno vencio al 25 de setiembre");
				Assert.That(
					api.AtrasosAl(new DateTime(2025, 10, 5), 0.062m).Periodo,
					Is.EqualTo(new[] { "2025-09" }),
					"AtrasosAl: al 5 de octubre vencio el de setiembre y no el de octubre");

				Assert.That(api.SePuedeProyectar(), Is.True,
					"SePuedeProyectar: con un pago registrado, si");
				Assert.That(api.ReservaPropuestaPara(2025).Anio, Is.EqualTo(2025),
					"ReservaPropuestaPara");

				Assert.That(api.SePuedeVerElFaltanteDeReserva(), Is.True,
					"SePuedeVerElFaltanteDeReserva: hay proyeccion y hay reserva adoptada");
				Assert.That(
					api.FaltanteDeReservaAl(new DateTime(2025, 9, 15), 0.10m).Declarado,
					Is.EqualTo(30000m),
					"FaltanteDeReservaAl: los 30000 que la persona adopto, y no lo que la app propone");
				Assert.That(api.AhorroPorProntoPago(LaConsulta, 0.062m), Is.TypeOf<decimal>(),
					"AhorroPorProntoPago");

				Assert.That(
					api.PeriodosPagadosSinComprobante(LaConsulta).Rubro, Is.All.EqualTo(ElRubro),
					"PeriodosPagadosSinComprobante");
				Assert.That(
					api.PeriodosPagadosSinComprobante(LaConsulta).Periodo, Is.Not.Empty,
					"PeriodosPagadosSinComprobante: el expediente deja periodos sin comprobante, asi que "
					+ "esta lectura contesta filas y no la ausencia");
				Assert.That(
					api.PeriodosPagadosSinComprobante(LaConsulta).EstadoDicho, Does.Contain("2025-09-03"),
					"PeriodosPagadosSinComprobante: y la frase del registro llego, nombrando su dia");

				// LAS SIETE DE M-5, por el camino que CONTESTA. Las dos guardadas -- ConsultaMasReciente
				// y ValorDeFincaAl -- van aca con su hecho presente; por el camino que se niega las
				// corre TheDiscoveryReadsAnswerWhatAShellCannotSupply, que es donde vive su ley.
				Assert.That(api.LosRubrosQueHay(), Has.Length.EqualTo(5),
					"LosRubrosQueHay: los cinco del catalogo, y lo contesta el catalogo por un metodo "
					+ "estatico alcanzado desde el script");
				Assert.That(api.RubrosConHistoria(), Is.EqualTo(new[] { ElRubro }),
					"RubrosConHistoria: solo basura tiene pagos en este expediente");

				Assert.That(api.HayAlgunaConsulta(), Is.True, "HayAlgunaConsulta");
				Assert.That(api.ConsultaMasReciente(), Is.EqualTo(LaConsulta),
					"ConsultaMasReciente: el dia de la unica consulta del expediente");

				Assert.That(api.NumerosDeLasFincas(), Is.EqualTo(new[] { LaFinca }),
					"NumerosDeLasFincas");
				Assert.That(
					api.TieneValorDeclaradoLaFinca(LaFinca, new DateTime(2025, 6, 30)), Is.True,
					"TieneValorDeclaradoLaFinca");
				Assert.That(
					api.ValorDeFincaAl(LaFinca, new DateTime(2025, 6, 30)), Is.EqualTo(60000000m),
					"ValorDeFincaAl: lo que la PERSONA declaro, no lo que la municipalidad mando");

				// LAS DOS DEL AVALUO, por el camino que CONTESTA, y anclado al literal que
				// ElExpedienteCompleto escribio. Las dos parejas se corren juntas a proposito: la unica
				// forma de ver que una contesta por la otra es pedirle a cada una su propio numero.
				Assert.That(
					api.TieneAvaluoLaFinca(LaFinca, new DateTime(2025, 6, 30)), Is.True,
					"TieneAvaluoLaFinca");
				Assert.That(
					api.AvaluoDeFincaAl(LaFinca, new DateTime(2025, 6, 30)), Is.EqualTo(36000000m),
					"AvaluoDeFincaAl: lo que la MUNICIPALIDAD mando, no lo que la persona declaro");
			});
		}

		// LAS TRES PRE-PREGUNTA, LOS DOS ESTADOS DE LA BITACORA. El `if` es de C#: en el estado sin
		// credencial la rama falsa devuelve "" SIN llegar al segundo script, asi que el camino de SI es
		// el que este censo necesita y el de NO corre un subconjunto.
		//
		// SE CORREN LOS DOS IGUAL, porque la rama de C# es tambien un camino de la puerta y una lectura
		// que contestara "" por la razon equivocada se veria igual que una que contesta bien.
		[Test]
		public void TheThreeGuardedReadsReachTheirSecondScriptOnlyInTheYesState()
		{
			Assert.Multiple(() =>
			{
				Assert.That(api.TokenGuardado(), Is.Empty,
					"Sin credencial, TokenGuardado devuelve vacio y el segundo script no corre");
				Assert.That(api.TokenDeRefrescoGuardado(), Is.Empty,
					"Sin credencial, TokenDeRefrescoGuardado devuelve vacio");
				Assert.That(api.ClaveDeDispositivoGuardada(), Is.Empty,
					"Sin dispositivo, ClaveDeDispositivoGuardada devuelve vacio");
			});

			api.RegistrarDispositivo(LaClaveDelDispositivo);
			api.GuardarCredencial(ElToken, ElRefresco);

			Assert.Multiple(() =>
			{
				Assert.That(api.TokenGuardado(), Is.EqualTo(ElToken),
					"Con credencial, el SEGUNDO script de TokenGuardado corre y trae el token");
				Assert.That(api.TokenDeRefrescoGuardado(), Is.EqualTo(ElRefresco),
					"Con credencial rotable, el segundo script de TokenDeRefrescoGuardado corre");
				Assert.That(api.ClaveDeDispositivoGuardada(), Is.EqualTo(LaClaveDelDispositivo),
					"Con dispositivo, el segundo script de ClaveDeDispositivoGuardada corre");
			});
		}

		// LAS TRES LECTURAS COMPUESTAS GUARDADAS, contra un registro que no puede contestarlas. El
		// script corre igual -- parsea y ata -- y el dominio se niega adentro, asi que la negativa cruza
		// la puerta.
		//
		// M-7 LES PUSO EL PAR Y ESO CAMBIA LA MITAD DE ESTE HALLAZGO. Las tres tienen ahora su
		// predicado en la puerta -- SePuedeVerLaVariacionDe, SePuedeProyectar,
		// SePuedeVerElFaltanteDeReserva -- asi que una caratula puede preguntar antes y no LLEGAR aca.
		// Quien concuerda con quien lo mira TheSentenceOfEveryCarrierCrossesTheDoor, en los dos estados.
		//
		// LA OTRA MITAD SIGUE ABIERTA, y es lo que esta prueba mide: `DomainException` cruza la frontera
		// de la consulta SIN ENVOLVERSE, asi que esta prueba lo puede nombrar -- Pruebas/ ve el dominio
		// por dentro. NINGUNA CARATULA PUEDE: el tipo es internal y la puerta no lo traduce, al
		// contrario que la de Concesionario, que lo convierte en una excepcion que un shell si puede
		// nombrar. El par quita la NECESIDAD de atrapar; no vuelve el tipo nombrable. Es C5, y esa mitad
		// no se arregla aca.
		[Test]
		public void TheThreeGuardedComposedReadsRaiseTheDomainsRefusalWhereTheRecordCannotAnswer()
		{
			Assert.Multiple(() =>
			{
				Assert.Throws<DomainException>(() => api.VariacionDe(ElRubro, 0.25m, 0.02m),
					"VariacionDe sin dos pagos del rubro");

				Assert.Throws<DomainException>(() => api.ReservaPropuestaPara(2025),
					"ReservaPropuestaPara sin historia con que proyectar");

				Assert.Throws<DomainException>(
					() => api.FaltanteDeReservaAl(new DateTime(2025, 9, 15), 0.10m),
					"FaltanteDeReservaAl sin proyeccion y sin reserva adoptada");
			});
		}
	}
}
