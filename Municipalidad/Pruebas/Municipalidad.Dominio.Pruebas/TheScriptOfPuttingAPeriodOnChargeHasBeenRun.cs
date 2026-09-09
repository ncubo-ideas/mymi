using System;
using System.IO;
using Municipalidad.App;
using NUnit.Framework;
using Puppeteer;

namespace Municipalidad.Dominio.Pruebas
{
	// LA OTRA MITAD DE G3, y es la que el nivel A no puede ver.
	//
	// MEDIDO EN ESTE REPO: un error de tipeo en un script del DSL se descubre en RUNTIME, no al
	// compilar, y deja entradas permanentes en el journal que hacen loguear un fallo de rehidratacion
	// en cada arranque futuro. PonerAlCobro tenia DOS textos de script sin correr nunca -- el del
	// Check y el del comando -- y ninguna prueba del dominio podia verlo, porque el dominio no sabe
	// que existen.
	//
	// LOS DOS TEXTOS SE EJERCITAN APARTE. Un comando aceptado corre el Check y el cuerpo; un comando
	// rechazado corre SOLO el Check. Un script del Check con un typo se veria verde en la corrida
	// aceptada y reventaria la primera vez que alguien fuera rechazado.
	//
	// Y EL RETORNO NO ES LA PRUEBA: lo que prueba que el Cobro entro es leerlo de vuelta. Se lee por
	// la puerta, que es la unica entrada, y no mirando el journal como bytes.
	[TestFixture]
	public sealed class TheScriptOfPuttingAPeriodOnChargeHasBeenRun
	{
		private const string ElActor = "municipalidad";

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

		// La app se abre y se cierra DENTRO de cada prueba, y no en el SetUp, porque una de ellas
		// tiene que cerrarla a la mitad para volver a abrirla.
		private MunicipalidadApp LaAppSobreEstaCarpeta()
		{
			MunicipalidadApp abierta = new MunicipalidadApp(ElActor);
			abierta.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			abierta.Start();
			return abierta;
		}

		[Test]
		public void ThePeriodPutOnChargeCrossesThePerformAndIsReadBack()
		{
			MunicipalidadApp app = LaAppSobreEstaCarpeta();
			MunicipalidadAPI api = new MunicipalidadAPI(app);

			string entro = api.PonerAlCobro(
				"recoleccion-de-basura", "2025.10", 8000m, new DateTime(2025, 10, 1));

			// M-4: la lectura ya no devuelve un JSON que haya que partir, asi que la afirmacion pasa de
			// buscar una subcadena a nombrar la FILA: el rubro y el periodo de la fila 0.
			RegistroDeVencimientos loQueVence = api.VencimientosAl(new DateTime(2025, 10, 15), 7);

			Assert.Multiple(() =>
			{
				Assert.That(entro, Does.Not.Contain("EWI"),
					"El comando se aplico: el marcador de rechazo es la propiedad EWI en la raiz del "
					+ "documento.");

				Assert.That(loQueVence.Periodo, Has.Length.EqualTo(1),
					"Y el Cobro entro de verdad: la lectura trae UNA fila. Sin este renglon la prueba "
					+ "solo diria que la llamada no revento.");
				Assert.That(loQueVence.Rubro[0], Is.EqualTo("recoleccion-de-basura"),
					"MunicipalidadAPI.VencimientosAl, columna rubro, fila 0");
				Assert.That(loQueVence.Periodo[0], Is.EqualTo("2025-10"),
					"MunicipalidadAPI.VencimientosAl, columna periodo, fila 0");
			});

			app.Dispose();
		}

		// EL ALFABETO SE RESUELVE EN LA PUERTA, no en el dominio: "2025.10" se parte en (2025, 10) y
		// la cadencia del rubro decide que es. Un texto que no tenga esa forma no llega al actor.
		[Test]
		public void TheDoorSplitsThePeriodAsTheReceiptWritesItAndRefusesAnythingElse()
		{
			MunicipalidadApp app = LaAppSobreEstaCarpeta();
			MunicipalidadAPI api = new MunicipalidadAPI(app);

			Assert.Multiple(() =>
			{
				Assert.Throws<ArgumentException>(
					() => api.PonerAlCobro("recoleccion-de-basura", "octubre", 8000m, new DateTime(2025, 10, 1)),
					"MunicipalidadAPI.PonerAlCobro, con un periodo que no viene como AAAA.NN");

				Assert.Throws<ArgumentException>(
					() => api.PonerAlCobro("recoleccion-de-basura", "2025.10.1", 8000m, new DateTime(2025, 10, 1)),
					"MunicipalidadAPI.PonerAlCobro, con tres partes");

				Assert.That(
					api.VencimientosAl(new DateTime(2025, 10, 15), 7).Periodo,
					Is.Empty,
					"Y nada de eso llego al actor: la guarda de C# corre antes del perform. Cero filas "
					+ "es el mismo hecho que antes decia la ausencia de la etiqueta 'periodo' en el JSON.");
			});

			app.Dispose();
		}

		// EL SCRIPT DEL Check, EJERCITADO APARTE. Los tres rechazos son los tres caminos por los que
		// PuedePonerAlCobro dice no y que cruzan la puerta: rubro desconocido, periodo que la
		// cadencia no lee, y un periodo ya puesto.
		//
		// UN RECHAZO NO DEJA ENTRADA, y eso es lo que hace que preguntar adentro del Check valga: es
		// la unica forma de negar sin escribir en el journal de una persona.
		[Test]
		public void TheCheckScriptRefusesWithoutLeavingAnEntry()
		{
			MunicipalidadApp app = LaAppSobreEstaCarpeta();
			MunicipalidadAPI api = new MunicipalidadAPI(app);

			api.PonerAlCobro("recoleccion-de-basura", "2025.10", 8000m, new DateTime(2025, 10, 1));

			long despuesDeQueEntro = app.Actor.CurrentEntryId;

			string rubroDesconocido = api.PonerAlCobro(
				"cementerio", "2025.10", 8000m, new DateTime(2025, 10, 1));
			string trimestreQueNoExiste = api.PonerAlCobro(
				"bienes-inmuebles", "2025.05", 36000m, new DateTime(2025, 10, 1));
			string yaEstabaPuesto = api.PonerAlCobro(
				"recoleccion-de-basura", "2025.10", 9000m, new DateTime(2025, 10, 1));

			Assert.Multiple(() =>
			{
				Assert.That(rubroDesconocido, Does.Contain("EWI"), "un rubro desconocido");
				Assert.That(trimestreQueNoExiste, Does.Contain("EWI"), "el quinto trimestre");
				Assert.That(yaEstabaPuesto, Does.Contain("EWI"), "un periodo ya puesto al cobro");

				Assert.That(app.Actor.CurrentEntryId, Is.EqualTo(despuesDeQueEntro),
					"Y los tres rechazos no dejaron NI UNA entrada: el Check niega antes de que el "
					+ "cuerpo del comando escriba.");
			});

			app.Dispose();
		}

		// LO QUE ENTRO SOBREVIVE A CERRAR Y REABRIR, que es la unica prueba de que el comando se
		// journalizo y no quedo en memoria. Un campo de C# se veria igual hasta el siguiente arranque.
		[Test]
		public void WhatWasPutOnChargeSurvivesClosingAndReopeningTheJournal()
		{
			MunicipalidadApp app = LaAppSobreEstaCarpeta();
			MunicipalidadAPI api = new MunicipalidadAPI(app);

			api.PonerAlCobro("recoleccion-de-basura", "2025.10", 8000m, new DateTime(2025, 10, 1));
			app.Dispose();

			MunicipalidadApp otraVez = LaAppSobreEstaCarpeta();
			MunicipalidadAPI despues = new MunicipalidadAPI(otraVez);

			RegistroDeVencimientos loQueVence = despues.VencimientosAl(new DateTime(2025, 10, 15), 7);

			Assert.Multiple(() =>
			{
				Assert.That(loQueVence.Periodo, Has.Length.EqualTo(1),
					"El Cobro se rehidrato desde el journal: la lectura trae UNA fila.");
				Assert.That(loQueVence.Rubro[0], Is.EqualTo("recoleccion-de-basura"),
					"MunicipalidadAPI.VencimientosAl despues de reabrir, columna rubro, fila 0");
				Assert.That(loQueVence.Periodo[0], Is.EqualTo("2025-10"),
					"MunicipalidadAPI.VencimientosAl despues de reabrir, columna periodo, fila 0");

				Assert.That(
					despues.PonerAlCobro("recoleccion-de-basura", "2025.10", 8000m, new DateTime(2025, 10, 1)),
					Does.Contain("EWI"),
					"Y por eso el mismo hecho otra vez se rechaza tambien despues de reabrir, que es "
					+ "la idempotencia que la ingesta va a necesitar.");
			});

			otraVez.Dispose();
		}
	}
}
