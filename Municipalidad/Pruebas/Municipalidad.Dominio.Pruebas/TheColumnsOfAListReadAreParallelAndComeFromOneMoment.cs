using System;
using System.Globalization;
using System.IO;
using Municipalidad.App;
using NUnit.Framework;
using Puppeteer;

namespace Municipalidad.Dominio.Pruebas
{
	// LO QUE M-4 CAMBIO, MEDIDO POR LA PUERTA. Las cuatro lecturas de lista devolvian un JSON sin
	// parsear armado por un `foreach` del DSL; ahora devuelven columnas paralelas tipadas desde UNA
	// sola consulta.
	//
	// UNA FILA ES UNA FILA, y eso es lo que se afirma aca. Dos llamadas a una lectura de una columna
	// serian dos consultas, y dos consultas son dos momentos: la columna de rubros podria venir de un
	// estado y la de montos de otro, y cada fila quedaria emparejada con otra cosa. La ley tiene tres
	// mitades y las tres se afirman por separado:
	//
	//   1. TODAS las columnas de una lectura miden lo mismo.
	//   2. La fila n de cada columna es la MISMA fila -- se afirma un valor que NOMBRA su propia
	//      fila, porque Is.Not.Empty prueba que llego una respuesta y nunca que es de esta columna.
	//   3. Las columnas son COHERENTES entre si: `faltan` es la distancia de `vence` a la fecha de
	//      corte, `dias` la de la fecha de corte a `vencio`, y `monto` es `porPeriodo` por `cubre`.
	//      Esa tercera mitad es la que no se puede satisfacer con columnas de dos momentos distintos.
	//
	// NIVEL B: cruza el perform contra un journal real, porque el nivel A no puede ver un typo del DSL
	// ni el tipo de un parametro Out. Los arreglos de `decimal` y de `bool` cruzando la frontera del
	// parametro se miden aca por primera vez en este repo -- antes solo estaban medidos `string[]` e
	// `int[]`, en Concesionario.
	[TestFixture]
	public sealed class TheColumnsOfAListReadAreParallelAndComeFromOneMoment
	{
		private const string ElActor = "municipalidad";

		// La fecha de corte de las dos lecturas fechadas, y la de la consulta. Son constantes de la
		// prueba porque las tres afirmaciones de coherencia las necesitan.
		private static readonly DateTime LaFechaDeCorte = new DateTime(2025, 10, 31);
		private static readonly DateTime LaFechaDeLaConsulta = new DateTime(2025, 9, 3);
		private const int LosDiasDeAviso = 35;
		private const decimal LaTasaAnual = 0.062m;

		private string carpeta;
		private MunicipalidadApp app;
		private MunicipalidadAPI api;

		[SetUp]
		public void Preparar()
		{
			carpeta = Path.Combine(
				Path.GetTempPath(), "municipalidad-columnas", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(carpeta);

			app = new MunicipalidadApp(ElActor);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			app.Start();
			api = new MunicipalidadAPI(app);

			ElExpedienteDeLaPrueba();
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

		// EL EXPEDIENTE ESTA ARMADO PARA QUE NINGUNA COLUMNA SEA CONSTANTE y para que ninguna lectura
		// tenga dos filas que empaten al ordenar: una columna con el mismo valor en todas sus filas no
		// puede delatar que se la reordeno, y un empate deja el orden a merced de List.Sort, que no es
		// estable.
		private void ElExpedienteDeLaPrueba()
		{
			api.RegistrarPago("R-1", new DateTime(2024, 3, 31), 0m);
			api.AgregarLineaAlPago("R-1", "recoleccion-de-basura", "2024.03", 5000m, 0m);

			api.RegistrarPago("R-2", new DateTime(2024, 6, 30), 0m);
			api.AgregarLineaAlPago("R-2", "recoleccion-de-basura", "2024.06", 18000m, 310m);

			api.RegistrarPago("R-3", new DateTime(2025, 4, 30), 0m);
			api.AgregarLineaAlPago("R-3", "limpieza-de-calles", "2025.04", 4000m, 0m);

			api.RegistrarConsulta(LaFechaDeLaConsulta);
			api.AgregarPendiente(LaFechaDeLaConsulta, "limpieza-de-calles", "2025.08", 4000m, 0m);

			DateTime puestoEl = new DateTime(2025, 9, 10);
			api.PonerAlCobro("parques-y-zonas-verdes", "2025.02", 12000m, puestoEl);
			api.PonerAlCobro("recoleccion-de-basura", "2025.07", 7000m, puestoEl);
			api.PonerAlCobro("recoleccion-de-basura", "2025.10", 8000m, puestoEl);
			api.PonerAlCobro("servicio-policia", "2025.11", 9500m, puestoEl);
			api.PonerAlCobro("bienes-inmuebles", "2025.04", 36000m, puestoEl);
		}

		private static DateTime ElDia(string dicho)
		{
			return DateTime.ParseExact(dicho, "yyyy-MM-dd", CultureInfo.InvariantCulture);
		}

		[Test]
		public void TheSeriesOfARubroAnswersSixParallelColumns()
		{
			RegistroDelHistorial serie = api.HistorialDe("recoleccion-de-basura", "es");

			Assert.Multiple(() =>
			{
				// 1. Todas miden lo mismo.
				Assert.That(serie.Periodo, Has.Length.EqualTo(2), "columna periodo");
				Assert.That(serie.Monto, Has.Length.EqualTo(2), "columna monto");
				Assert.That(serie.Mora, Has.Length.EqualTo(2), "columna mora");
				Assert.That(serie.Cubre, Has.Length.EqualTo(2), "columna cubre");
				Assert.That(serie.PorPeriodo, Has.Length.EqualTo(2), "columna porPeriodo");
				Assert.That(serie.Inferido, Has.Length.EqualTo(2), "columna inferido");

				// 2. La fila 0 y la fila 1 nombran cada una su propia fila: los seis valores de una
				// fila son de la MISMA linea de comprobante. Ninguna columna repite valor entre las
				// dos filas, asi que reordenar cualquiera de las seis pone esto rojo.
				Assert.That(serie.Periodo[0], Is.EqualTo("2024-03"), "fila 0, periodo");
				Assert.That(serie.Monto[0], Is.EqualTo(5000m), "fila 0, monto");
				Assert.That(serie.Mora[0], Is.EqualTo(0m), "fila 0, mora");
				Assert.That(serie.Cubre[0], Is.EqualTo(1), "fila 0, cubre");
				Assert.That(serie.PorPeriodo[0], Is.EqualTo(5000m), "fila 0, porPeriodo");
				Assert.That(serie.Inferido[0], Is.True, "fila 0, inferido -- la primera linea de un rubro no tiene con que deducir");

				Assert.That(serie.Periodo[1], Is.EqualTo("2024-06"), "fila 1, periodo");
				Assert.That(serie.Monto[1], Is.EqualTo(18000m), "fila 1, monto");
				Assert.That(serie.Mora[1], Is.EqualTo(310m), "fila 1, mora");
				Assert.That(serie.Cubre[1], Is.EqualTo(3), "fila 1, cubre");
				Assert.That(serie.PorPeriodo[1], Is.EqualTo(6000m), "fila 1, porPeriodo");
				Assert.That(serie.Inferido[1], Is.False, "fila 1, inferido");

				// 3. Coherencia entre columnas de la misma fila.
				for (int fila = 0; fila < serie.Periodo.Length; fila++)
					Assert.That(serie.PorPeriodo[fila] * serie.Cubre[fila], Is.EqualTo(serie.Monto[fila]),
						"fila " + fila + ": el monto es lo que costo un periodo por los periodos que cubre");
			});
		}

		[Test]
		public void WhatHasNotFallenDueYetAnswersSixParallelColumns()
		{
			RegistroDeVencimientos loQueVence = api.VencimientosAl(LaFechaDeCorte, LosDiasDeAviso, "es");

			Assert.Multiple(() =>
			{
				Assert.That(loQueVence.Rubro, Has.Length.EqualTo(3), "columna rubro");
				Assert.That(loQueVence.Periodo, Has.Length.EqualTo(3), "columna periodo");
				Assert.That(loQueVence.Vence, Has.Length.EqualTo(3), "columna vence");
				Assert.That(loQueVence.Monto, Has.Length.EqualTo(3), "columna monto");
				Assert.That(loQueVence.Faltan, Has.Length.EqualTo(3), "columna faltan");
				Assert.That(loQueVence.Urgencia, Has.Length.EqualTo(3), "columna urgencia");
				Assert.That(loQueVence.Dice, Has.Length.EqualTo(3), "columna dice");

				// La fila 0 es la que vence primero, y sus siete valores son de la misma cosa.
				Assert.That(loQueVence.Rubro[0], Is.EqualTo("recoleccion-de-basura"), "fila 0, rubro");
				Assert.That(loQueVence.Periodo[0], Is.EqualTo("2025-10"), "fila 0, periodo");
				Assert.That(loQueVence.Vence[0], Is.EqualTo("2025-10-31"), "fila 0, vence");
				Assert.That(loQueVence.Monto[0], Is.EqualTo(8000m), "fila 0, monto");
				Assert.That(loQueVence.Faltan[0], Is.EqualTo(0), "fila 0, faltan");
				Assert.That(loQueVence.Urgencia[0], Is.EqualTo("urgente"), "fila 0, urgencia");

				Assert.That(loQueVence.Rubro[1], Is.EqualTo("servicio-policia"), "fila 1, rubro");
				Assert.That(loQueVence.Periodo[1], Is.EqualTo("2025-11"), "fila 1, periodo");
				Assert.That(loQueVence.Vence[1], Is.EqualTo("2025-11-30"), "fila 1, vence");
				Assert.That(loQueVence.Monto[1], Is.EqualTo(9500m), "fila 1, monto");
				Assert.That(loQueVence.Faltan[1], Is.EqualTo(30), "fila 1, faltan");
				Assert.That(loQueVence.Urgencia[1], Is.EqualTo("aviso"), "fila 1, urgencia");

				Assert.That(loQueVence.Rubro[2], Is.EqualTo("bienes-inmuebles"), "fila 2, rubro");
				Assert.That(loQueVence.Periodo[2], Is.EqualTo("2025-T4"), "fila 2, periodo");
				Assert.That(loQueVence.Vence[2], Is.EqualTo("2025-12-31"), "fila 2, vence");
				Assert.That(loQueVence.Monto[2], Is.EqualTo(36000m), "fila 2, monto");
				Assert.That(loQueVence.Faltan[2], Is.EqualTo(61), "fila 2, faltan");
				Assert.That(loQueVence.Urgencia[2], Is.EqualTo("ninguna"), "fila 2, urgencia");

				// Coherencia: la columna de dias que faltan es la distancia de la columna de dias de
				// vencimiento a la fecha de corte. Dos columnas tomadas en dos momentos no la cumplen.
				for (int fila = 0; fila < loQueVence.Rubro.Length; fila++)
					Assert.That(loQueVence.Faltan[fila],
						Is.EqualTo((ElDia(loQueVence.Vence[fila]) - LaFechaDeCorte).Days),
						"fila " + fila + ": los dias que faltan son los que hay de la fecha de corte al dia en que vence");
			});
		}

		[Test]
		public void WhatHasAlreadyFallenDueAnswersSevenParallelColumns()
		{
			RegistroDeAtrasos atrasos = api.AtrasosAl(LaFechaDeCorte, LaTasaAnual, "es");

			Assert.Multiple(() =>
			{
				Assert.That(atrasos.Rubro, Has.Length.EqualTo(3), "columna rubro");
				Assert.That(atrasos.Periodo, Has.Length.EqualTo(3), "columna periodo");
				Assert.That(atrasos.Vencio, Has.Length.EqualTo(3), "columna vencio");
				Assert.That(atrasos.Monto, Has.Length.EqualTo(3), "columna monto");
				Assert.That(atrasos.Dias, Has.Length.EqualTo(3), "columna dias");
				Assert.That(atrasos.CostoProyectado, Has.Length.EqualTo(3), "columna costoProyectado");
				Assert.That(atrasos.Urgencia, Has.Length.EqualTo(3), "columna urgencia");
				Assert.That(atrasos.Dice, Has.Length.EqualTo(3), "columna dice");

				Assert.That(atrasos.Rubro[0], Is.EqualTo("parques-y-zonas-verdes"), "fila 0, rubro");
				Assert.That(atrasos.Periodo[0], Is.EqualTo("2025-T2"), "fila 0, periodo");
				Assert.That(atrasos.Vencio[0], Is.EqualTo("2025-06-30"), "fila 0, vencio");
				Assert.That(atrasos.Monto[0], Is.EqualTo(12000m), "fila 0, monto");
				Assert.That(atrasos.Dias[0], Is.EqualTo(123), "fila 0, dias");
				Assert.That(atrasos.CostoProyectado[0], Is.EqualTo(250.72m), "fila 0, costoProyectado");

				Assert.That(atrasos.Rubro[1], Is.EqualTo("recoleccion-de-basura"), "fila 1, rubro");
				Assert.That(atrasos.Periodo[1], Is.EqualTo("2025-07"), "fila 1, periodo");
				Assert.That(atrasos.Vencio[1], Is.EqualTo("2025-07-31"), "fila 1, vencio");
				Assert.That(atrasos.Monto[1], Is.EqualTo(7000m), "fila 1, monto");
				Assert.That(atrasos.Dias[1], Is.EqualTo(92), "fila 1, dias");
				Assert.That(atrasos.CostoProyectado[1], Is.EqualTo(109.39m), "fila 1, costoProyectado");

				// La fila 2 es el Cobro que AgregarPendiente acuno con la fecha de la consulta.
				Assert.That(atrasos.Rubro[2], Is.EqualTo("limpieza-de-calles"), "fila 2, rubro");
				Assert.That(atrasos.Periodo[2], Is.EqualTo("2025-08"), "fila 2, periodo");
				Assert.That(atrasos.Vencio[2], Is.EqualTo("2025-08-31"), "fila 2, vencio");
				Assert.That(atrasos.Monto[2], Is.EqualTo(4000m), "fila 2, monto");
				Assert.That(atrasos.Dias[2], Is.EqualTo(61), "fila 2, dias");
				Assert.That(atrasos.CostoProyectado[2], Is.EqualTo(41.45m), "fila 2, costoProyectado");

				// Lo vencido es urgente por CLASE, asi que esta columna no discrimina la fila. Se
				// afirma igual porque su LARGO si es parte de la ley.
				Assert.That(atrasos.Urgencia, Is.All.EqualTo("urgente"), "columna urgencia");

				for (int fila = 0; fila < atrasos.Rubro.Length; fila++)
					Assert.That(atrasos.Dias[fila],
						Is.EqualTo((LaFechaDeCorte - ElDia(atrasos.Vencio[fila])).Days),
						"fila " + fila + ": los dias de atraso son los que hay del dia en que vencio a la fecha de corte");
			});
		}

		[Test]
		public void WhatWasPaidWithNoReceiptAnswersTwoParallelColumns()
		{
			RegistroDePeriodosSinComprobante sinComprobante =
				api.PeriodosPagadosSinComprobante(LaFechaDeLaConsulta, "es");

			Assert.Multiple(() =>
			{
				Assert.That(sinComprobante.Rubro, Has.Length.EqualTo(3), "columna rubro");
				Assert.That(sinComprobante.Periodo, Has.Length.EqualTo(3), "columna periodo");
				Assert.That(sinComprobante.Dice, Has.Length.EqualTo(3), "columna dice");

				// Un solo rubro llega hasta aca, asi que la columna de rubros no discrimina la fila y
				// la de periodos es la unica que puede delatar un reordenamiento. Se dice en vez de
				// dejarlo implicito.
				Assert.That(sinComprobante.Rubro, Is.All.EqualTo("limpieza-de-calles"), "columna rubro");

				Assert.That(sinComprobante.Periodo[0], Is.EqualTo("2025-05"), "fila 0, periodo");
				Assert.That(sinComprobante.Periodo[1], Is.EqualTo("2025-06"), "fila 1, periodo");
				Assert.That(sinComprobante.Periodo[2], Is.EqualTo("2025-07"), "fila 2, periodo");
			});
		}

		// LA OTRA MITAD DEL ARREGLO DE M-4: el formateador del print rendia el DateTime en MM/dd/yyyy
		// y una caratula tenia que volver a interpretarlo. Ahora el dia lo dice el DOMINIO, en el orden
		// que no se puede leer al reves y sin hora, porque un periodo cierra un dia y no un instante.
		[Test]
		public void ADayColumnIsSaidByTheDomainAsACalendarDayAndNeverAsAMonthFirstDate()
		{
			RegistroDeVencimientos loQueVence = api.VencimientosAl(LaFechaDeCorte, LosDiasDeAviso, "es");
			RegistroDeAtrasos atrasos = api.AtrasosAl(LaFechaDeCorte, LaTasaAnual, "es");

			Assert.Multiple(() =>
			{
				foreach (string dicho in loQueVence.Vence)
					Assert.That(dicho, Does.Match(@"^\d{4}-\d{2}-\d{2}$"),
						"RegistroDeVencimientos.Vence: anio-mes-dia, sin hora");

				foreach (string dicho in atrasos.Vencio)
					Assert.That(dicho, Does.Match(@"^\d{4}-\d{2}-\d{2}$"),
						"RegistroDeAtrasos.Vencio: anio-mes-dia, sin hora");

				// El dia 31 en la posicion del dia, y el mes 10 en la posicion del mes: en MM/dd/yyyy
				// esta misma fecha salia "10/31/2025" y un 13 de enero salia indistinguible de un 1 de
				// trece. Esta afirmacion es la que separa las dos formas.
				Assert.That(loQueVence.Vence[0], Is.EqualTo("2025-10-31"),
					"el ultimo dia de octubre de 2025, dicho una sola manera");
			});
		}

		// CERO FILAS ES UNA RESPUESTA, NO UN ERROR NI UNA COLUMNA AUSENTE. Las cuatro lecturas son
		// totales, asi que un rubro que no existe y un rubro sin historia contestan lo mismo -- que es
		// el hueco que M-6 va a separar en dos respuestas con nombre -- y ninguno revienta.
		//
		// Y ES LA MEDICION DE UN ARREGLO VACIO CRUZANDO LA FRONTERA DEL PARAMETRO: si volviera nulo en
		// vez de vacio, el registro lo refusaria como columna ausente y esta prueba lo diria.
		[Test]
		public void AnEmptyAnswerIsStillAFullSetOfParallelColumns()
		{
			RegistroDelHistorial rubroQueNoExiste = api.HistorialDe("cementerio", "es");
			RegistroDelHistorial rubroSinHistoria = api.HistorialDe("parques-y-zonas-verdes", "es");
			RegistroDePeriodosSinComprobante sinConsulta =
				api.PeriodosPagadosSinComprobante(new DateTime(2025, 1, 1), "es");

			Assert.Multiple(() =>
			{
				Assert.That(rubroQueNoExiste.Periodo, Is.Empty, "cementerio, columna periodo");
				Assert.That(rubroQueNoExiste.Monto, Is.Empty, "cementerio, columna monto");
				Assert.That(rubroQueNoExiste.Mora, Is.Empty, "cementerio, columna mora");
				Assert.That(rubroQueNoExiste.Cubre, Is.Empty, "cementerio, columna cubre");
				Assert.That(rubroQueNoExiste.PorPeriodo, Is.Empty, "cementerio, columna porPeriodo");
				Assert.That(rubroQueNoExiste.Inferido, Is.Empty, "cementerio, columna inferido");
				Assert.That(rubroQueNoExiste.EstadoDicho, Is.Not.Empty,
					"cementerio: y la frase del registro NO esta vacia, que es la diferencia entre una "
					+ "respuesta de cero filas y una pantalla en blanco sin motivo");

				Assert.That(rubroSinHistoria.Periodo, Is.Empty, "parques, columna periodo");
				Assert.That(rubroSinHistoria.Inferido, Is.Empty, "parques, columna inferido");

				Assert.That(sinConsulta.Rubro, Is.Empty, "sin consulta de ese dia, columna rubro");
				Assert.That(sinConsulta.Periodo, Is.Empty, "sin consulta de ese dia, columna periodo");
				Assert.That(sinConsulta.Dice, Is.Empty, "sin consulta de ese dia, columna dice");
			});
		}
	}
}
