using System;
using System.IO;
using Municipalidad.App;
using NUnit.Framework;
using Puppeteer;

namespace Municipalidad.Dominio.Pruebas
{
	// NIVEL B: LA OTRA MITAD DE G6. Las hojas con nombre existen en el dominio -- eso lo mira
	// AnAbsenceWithKindsIsOneNamedTypePerReason -- y aca se mira que la razon CRUCE la puerta, porque
	// una caratula no ve tipos: ve el registro que la puerta le devuelve.
	//
	// EL ESTADO Y LAS FILAS VIENEN DE UNA SOLA CONSULTA, y tiene que ser asi: en dos consultas el
	// estado podria decir "no hay consulta de ese dia" tomado en un momento y las filas venir de otro
	// en que si la habia. Es el mismo argumento de M-4 y por eso PeriodosPagadosSinComprobante dejo de
	// usar el ayudante LasColumnas -- que solo sabia declarar columnas de texto -- y pasa por su propio
	// rent-lease con un Out escalar al lado de los dos de columna.
	//
	// EL ESTADO NO ES UNA COLUMNA. Es del registro entero y no tiene largo: por eso no entra en la
	// comprobacion de columnas paralelas y por eso su ausencia se refusa aparte, con el nombre en
	// blanco.
	[TestFixture]
	public sealed class TwoEmptyAnswersReachTheDoorAsTwoDifferentAnswers
	{
		private const string ElActor = "municipalidad";
		private const string ElRubroConHistoria = "limpieza-de-calles";
		private const string ElRubroSinHistoria = "parques-y-zonas-verdes";

		private static readonly DateTime LaConsultaQueDestapa = new DateTime(2025, 9, 3);
		private static readonly DateTime LaConsultaQueNoDestapaNada = new DateTime(2025, 10, 3);
		private static readonly DateTime UnDiaSinConsulta = new DateTime(2025, 1, 1);

		private string carpeta;
		private MunicipalidadApp app;
		private MunicipalidadAPI api;

		[SetUp]
		public void Preparar()
		{
			carpeta = Path.Combine(
				Path.GetTempPath(), "municipalidad-ausencias", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(carpeta);

			app = new MunicipalidadApp(ElActor);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			app.Start();
			api = new MunicipalidadAPI(app);

			// UN EXPEDIENTE QUE PRODUCE LAS TRES HOJAS DEL COTEJO CON UN SOLO JUEGO DE HECHOS.
			//
			// Limpieza es el unico rubro con un pago, asi que es el unico con historia. La consulta de
			// setiembre trae un pendiente DE LIMPIEZA, asi que la cotejan y destapa lo que la app no
			// vio pagar entre abril y agosto. La de octubre trae un pendiente de POLICIA, un rubro sin
			// historia, asi que no hay nada que cotejar y el cotejo no destapa nada -- que es una
			// respuesta distinta de la del dia sin consulta.
			api.RegistrarPago("R-1", new DateTime(2025, 4, 30), 0m);
			api.AgregarLineaAlPago("R-1", ElRubroConHistoria, "2025.04", 4000m, 0m);

			api.RegistrarConsulta(LaConsultaQueDestapa);
			api.AgregarPendiente(LaConsultaQueDestapa, ElRubroConHistoria, "2025.08", 4000m, 0m);

			api.RegistrarConsulta(LaConsultaQueNoDestapaNada);
			api.AgregarPendiente(LaConsultaQueNoDestapaNada, "servicio-policia", "2025.09", 18300m, 0m);
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

		// LAS DOS SERIES VACIAS, POR LA PUERTA. Las dos traen cero filas y sus estados difieren, que es
		// la unica cosa que una caratula puede usar para no decir "no has pagado nada de cementerio".
		[Test]
		public void TheTwoEmptySeriesCrossTheDoorWithDifferentStates()
		{
			RegistroDelHistorial rubroQueNoExiste = api.HistorialDe("cementerio", "es");
			RegistroDelHistorial rubroSinHistoria = api.HistorialDe(ElRubroSinHistoria, "es");
			RegistroDelHistorial rubroConHistoria = api.HistorialDe(ElRubroConHistoria, "es");

			Assert.Multiple(() =>
			{
				Assert.That(rubroQueNoExiste.Periodo, Is.Empty, "cementerio: cero filas");
				Assert.That(rubroQueNoExiste.Estado, Is.EqualTo("rubro-desconocido"),
					"y el estado dice que el rubro no existe, no que no hayas pagado nada de el");

				Assert.That(rubroSinHistoria.Periodo, Is.Empty, "parques: cero filas");
				Assert.That(rubroSinHistoria.Estado, Is.EqualTo("sin-historia"),
					"y el estado dice que el rubro EXISTE y no tiene pagos, que es otra respuesta");

				Assert.That(rubroConHistoria.Periodo, Has.Length.EqualTo(1), "limpieza: una fila");
				Assert.That(rubroConHistoria.Estado, Is.EqualTo("con-historia"));

				Assert.That(rubroQueNoExiste.Estado, Is.Not.EqualTo(rubroSinHistoria.Estado),
					"Y las dos respuestas de cero filas NO son la misma respuesta.");

				// M-7: Y LA FRASE TAMBIEN ES OTRA. Dos estados con nombres distintos y una sola frase
				// para los dos dejarian a la persona leyendo lo mismo en los dos casos, que es de
				// donde el estado venia a salvarla.
				Assert.That(
					rubroQueNoExiste.EstadoDicho, Is.Not.EqualTo(rubroSinHistoria.EstadoDicho),
					"Y tampoco es la misma FRASE, que es lo que la persona lee.");
			});
		}

		// LAS TRES HOJAS DEL COTEJO, POR LA PUERTA, CON UN SOLO EXPEDIENTE. Las dos primeras traen cero
		// filas: `sin-consulta` dice que la pregunta no se pudo contestar y `nada-sin-comprobante` dice
		// que se contesto y salio limpia.
		[Test]
		public void TheThreeStatesOfTheCrossCheckCrossTheDoorFromOneRecord()
		{
			RegistroDePeriodosSinComprobante sinConsulta =
				api.PeriodosPagadosSinComprobante(UnDiaSinConsulta, "es");
			RegistroDePeriodosSinComprobante noDestapaNada =
				api.PeriodosPagadosSinComprobante(LaConsultaQueNoDestapaNada, "es");
			RegistroDePeriodosSinComprobante destapa =
				api.PeriodosPagadosSinComprobante(LaConsultaQueDestapa, "es");

			Assert.Multiple(() =>
			{
				Assert.That(sinConsulta.Periodo, Is.Empty, "un dia sin consulta: cero filas");
				Assert.That(sinConsulta.Estado, Is.EqualTo("sin-consulta"),
					"y el estado dice que no hubo con que cotejar -- NO que todo este en orden");

				Assert.That(noDestapaNada.Periodo, Is.Empty, "la consulta de octubre: cero filas");
				Assert.That(noDestapaNada.Estado, Is.EqualTo("nada-sin-comprobante"),
					"y el estado dice que SI se cotejo y no destapo nada, que es la respuesta buena");

				Assert.That(destapa.Periodo, Is.Not.Empty, "la consulta de setiembre destapa periodos");
				Assert.That(destapa.Estado, Is.EqualTo("hay-periodos-sin-comprobante"));
				Assert.That(destapa.Rubro, Is.All.EqualTo(ElRubroConHistoria),
					"y son de limpieza, el unico rubro con historia que la consulta coteja");

				Assert.That(sinConsulta.Estado, Is.Not.EqualTo(noDestapaNada.Estado),
					"Y las dos respuestas de cero filas NO son la misma respuesta. Antes de M-6 lo eran.");

				// M-7: Y CADA FRASE NOMBRA SU PROPIO DIA, que es lo que impide que la del dia sin
				// consulta se lea como el cotejo limpio del otro dia.
				Assert.That(sinConsulta.EstadoDicho, Does.Contain("2025-01-01"),
					"la frase del dia sin consulta nombra SU dia");
				Assert.That(noDestapaNada.EstadoDicho, Does.Contain("2025-10-03"),
					"y la del cotejo limpio el suyo");
				Assert.That(sinConsulta.EstadoDicho, Is.Not.EqualTo(noDestapaNada.EstadoDicho),
					"Y tampoco es la misma FRASE.");
			});
		}

		// EL ESTADO Y LAS FILAS CONCUERDAN, que es lo que el registro tiene que garantizar y lo que dos
		// consultas no podrian: un estado que dijera `hay-periodos-sin-comprobante` sobre cero filas, o
		// `sin-historia` sobre una serie con lineas, seria peor que no tener estado.
		[Test]
		public void TheStateAgreesWithTheRowsItCameWith()
		{
			Assert.Multiple(() =>
			{
				foreach (string rubro in new[] { "cementerio", ElRubroSinHistoria, ElRubroConHistoria })
				{
					RegistroDelHistorial serie = api.HistorialDe(rubro, "es");

					Assert.That(serie.Estado == "con-historia", Is.EqualTo(serie.Periodo.Length > 0),
						"HistorialDe('" + rubro + "'): el estado `con-historia` y la presencia de filas "
						+ "son la misma cosa dicha dos veces, y tienen que coincidir");
				}

				foreach (DateTime dia in new[]
					{ UnDiaSinConsulta, LaConsultaQueNoDestapaNada, LaConsultaQueDestapa })
				{
					RegistroDePeriodosSinComprobante cotejo = api.PeriodosPagadosSinComprobante(dia, "es");

					Assert.That(
						cotejo.Estado == "hay-periodos-sin-comprobante",
						Is.EqualTo(cotejo.Periodo.Length > 0),
						"PeriodosPagadosSinComprobante(" + dia.ToString("yyyy-MM-dd") + "): el estado y "
						+ "la presencia de filas tienen que coincidir");
				}
			});
		}
	}
}
