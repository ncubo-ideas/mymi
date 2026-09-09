using System;
using System.IO;
using Municipalidad.App;
using NUnit.Framework;
using Puppeteer;

namespace Municipalidad.Dominio.Pruebas
{
	// NIVEL B: LA OTRA MITAD DE M-7. Las frases existen en el dominio -- eso lo mira
	// EverySentenceTheDomainComposesNamesItsOwnSubject -- y aca se mira que CRUCEN la puerta, porque una
	// caratula no ve portadores: ve el registro que la puerta le devuelve.
	//
	// EL NIVEL A NO PUEDE VER NADA DE ESTO. Un `Dice()` que el script del DSL no nombre, o que lo nombre
	// mal, deja el build en 0 errores y falla en RUNTIME; y un Out declarado con el tipo equivocado
	// vuelve vacio sin que nada se queje. Las frases de M-7 entraron por SIETE lugares distintos del
	// script -- un Out escalar en dos lecturas, una columna en tres, y un Out escalar mas dentro de las
	// tres respuestas compuestas -- asi que cada uno se corre.
	//
	// LA FRASE VIAJA COMO CAMPO O COMO COLUMNA SEGUN DE QUE SEA UN HECHO, que es la ley que M-6 dejo
	// puesta: `EstadoDicho` describe el registro entero y no tiene largo; `Dice` es un hecho de la fila
	// y es una columna paralela a las otras. Las dos formas se afirman aparte.
	[TestFixture]
	public sealed class TheSentenceOfEveryCarrierCrossesTheDoor
	{
		private const string ElActor = "municipalidad";
		private const string ElRubroPorUnidad = "recoleccion-de-basura";
		private const string ElRubroSobreBase = "bienes-inmuebles";

		private static readonly DateTime LaConsulta = new DateTime(2025, 9, 3);
		private static readonly DateTime UnDiaSinConsulta = new DateTime(2025, 1, 1);
		private static readonly DateTime LaFechaDeCorte = new DateTime(2025, 10, 31);

		private string carpeta;
		private MunicipalidadApp app;
		private MunicipalidadAPI api;

		[SetUp]
		public void Preparar()
		{
			carpeta = Path.Combine(
				Path.GetTempPath(), "municipalidad-frases", Guid.NewGuid().ToString("N"));
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

		// EL EXPEDIENTE, ESCRITO CON LITERALES Y NO LEIDO DE VUELTA DEL ACTOR (E49). Dos recibos con el
		// salto por tarifa en basura y el salto por avaluo en bienes inmuebles, una consulta que destapa
		// periodos, un cobro por vencer, uno vencido, y la reserva que la persona adopto.
		//
		// LOS DOS PERIODOS DE CADA RUBRO SON CONSECUTIVOS A PROPOSITO. La inferencia del hueco 6 deduce
		// cuantos periodos cubre una linea de la distancia al ultimo pagado, asi que un salto de marzo
		// de 2024 a marzo de 2025 haria que la segunda linea cubriera DOCE periodos y su monto por
		// periodo saliera doce veces mas chico -- un salto de +80 % se leeria como una caida del 85 %.
		// Medido escribiendo el expediente asi la primera vez.
		private void ElExpedienteCompleto()
		{
			api.RegistrarPago("R-1", new DateTime(2024, 3, 31), 0m);
			api.AgregarLineaAlPago("R-1", ElRubroPorUnidad, "2024.03", 5000m, 0m);
			api.AgregarLineaAlPago("R-1", ElRubroSobreBase, "2024.01", 20000m, 0m);

			api.RegistrarPago("R-2", new DateTime(2024, 4, 30), 0m);
			api.AgregarLineaAlPago("R-2", ElRubroPorUnidad, "2024.04", 9000m, 0m);
			api.AgregarLineaAlPago("R-2", ElRubroSobreBase, "2024.02", 36000m, 0m);

			api.RegistrarConsulta(LaConsulta);
			api.AgregarPendiente(LaConsulta, ElRubroPorUnidad, "2025.09", 9000m, 0m);

			api.PonerAlCobro(ElRubroPorUnidad, "2025.11", 9000m, new DateTime(2025, 9, 10));

			api.AdoptarReserva(8000m, new DateTime(2025, 8, 1));
		}

		// LA FRASE DEL REGISTRO ENTERO, EN LAS DOS LECTURAS QUE LA TIENEN, y en los DOS estados que
		// importan de cada una: la respuesta con filas y la que trae cero. Cero filas con la frase
		// equivocada es peor que cero filas sin frase.
		[Test]
		public void TheStateOfARecordCrossesTheDoorAsASentenceAndNotOnlyAsASlug()
		{
			ElExpedienteCompleto();

			RegistroDelHistorial conHistoria = api.HistorialDe(ElRubroPorUnidad);
			RegistroDelHistorial rubroQueNoExiste = api.HistorialDe("cementerio");
			RegistroDelHistorial sinHistoria = api.HistorialDe("parques-y-zonas-verdes");

			RegistroDePeriodosSinComprobante destapa = api.PeriodosPagadosSinComprobante(LaConsulta);
			RegistroDePeriodosSinComprobante sinConsulta =
				api.PeriodosPagadosSinComprobante(UnDiaSinConsulta);

			Assert.Multiple(() =>
			{
				Assert.That(conHistoria.EstadoDicho, Does.Contain(ElRubroPorUnidad),
					"la frase del registro nombra el rubro que se pidio");
				Assert.That(conHistoria.EstadoDicho, Does.Contain("2 líneas"),
					"y cuenta las dos lineas que este expediente escribio");
				Assert.That(conHistoria.EstadoDicho, Is.Not.EqualTo(conHistoria.Estado),
					"y no es el slug: `Estado` y `EstadoDicho` son dos campos y dicen dos cosas");

				Assert.That(rubroQueNoExiste.EstadoDicho, Does.Contain("cementerio"),
					"la del rubro desconocido nombra el nombre que se pidio");
				Assert.That(rubroQueNoExiste.EstadoDicho, Does.Contain("no existe en esta cuenta"));
				Assert.That(sinHistoria.EstadoDicho, Does.Contain("no vio pagar"));
				Assert.That(rubroQueNoExiste.EstadoDicho, Is.Not.EqualTo(sinHistoria.EstadoDicho),
					"Y las dos respuestas de cero filas cruzan la puerta con frases distintas.");

				Assert.That(destapa.EstadoDicho, Does.Contain("2025-09-03"),
					"la frase del cotejo nombra el DIA con que se pidio");
				Assert.That(destapa.EstadoDicho, Does.Contain("destapa"));
				Assert.That(sinConsulta.EstadoDicho, Does.Contain("2025-01-01"),
					"y la del dia sin consulta nombra SU dia, no el de la otra");
				Assert.That(sinConsulta.EstadoDicho, Does.Contain("no dice que todo esté en orden"),
					"y niega la lectura benigna: cero filas aca no es una cuenta en orden");
			});
		}

		// LA FRASE POR FILA, EN LAS TRES LECTURAS QUE LA TIENEN, y es una COLUMNA: paralela a las otras
		// y del mismo largo. Una frase de largo distinto emparejaria cada fila con otra.
		[Test]
		public void TheSentenceOfEachRowCrossesTheDoorAsAColumnParallelToTheOthers()
		{
			ElExpedienteCompleto();

			RegistroDeVencimientos loQueVence = api.VencimientosAl(LaFechaDeCorte, 35);
			RegistroDeAtrasos atrasos = api.AtrasosAl(LaFechaDeCorte, 0.062m);
			RegistroDePeriodosSinComprobante sinComprobante = api.PeriodosPagadosSinComprobante(LaConsulta);

			Assert.Multiple(() =>
			{
				// SIN FILAS, LOS TRES BUCLES DE ABAJO NO CORREN Y ESTA PRUEBA PASA SIN MIRAR NADA. Asi
				// que las tres respuestas se afirman NO VACIAS primero: un expediente que dejara de
				// producir filas volveria este metodo verde y hueco.
				Assert.That(loQueVence.Rubro, Is.Not.Empty, "el expediente deja algo por vencer");
				Assert.That(atrasos.Rubro, Is.Not.Empty, "y algo vencido");
				Assert.That(sinComprobante.Rubro, Is.Not.Empty, "y periodos sin comprobante");

				Assert.That(loQueVence.Dice, Has.Length.EqualTo(loQueVence.Rubro.Length),
					"la columna de frases de lo que vence mide lo mismo que las otras seis");
				Assert.That(atrasos.Dice, Has.Length.EqualTo(atrasos.Rubro.Length),
					"la de los atrasos, lo mismo que las otras siete");
				Assert.That(sinComprobante.Dice, Has.Length.EqualTo(sinComprobante.Rubro.Length),
					"y la del cotejo, lo mismo que las otras dos");

				// CADA FRASE ES DE SU PROPIA FILA, afirmado con el periodo de esa fila adentro: una
				// columna de frases reordenada, o rellenada con la misma frase, se ve solo asi.
				for (int fila = 0; fila < loQueVence.Rubro.Length; fila++)
					Assert.That(loQueVence.Dice[fila], Does.Contain(loQueVence.Periodo[fila]),
						"lo que vence, fila " + fila + ": la frase nombra el periodo de SU fila");

				for (int fila = 0; fila < atrasos.Rubro.Length; fila++)
				{
					Assert.That(atrasos.Dice[fila], Does.Contain(atrasos.Periodo[fila]),
						"los atrasos, fila " + fila + ": la frase nombra el periodo de SU fila");
					Assert.That(atrasos.Dice[fila], Does.Contain("proyección"),
						"y marca su proyeccion COMO proyeccion, fila " + fila);
				}

				for (int fila = 0; fila < sinComprobante.Rubro.Length; fila++)
					Assert.That(sinComprobante.Dice[fila], Does.Contain(sinComprobante.Periodo[fila]),
						"el cotejo, fila " + fila + ": la frase nombra el periodo de SU fila");
			});
		}

		// LAS TRES RESPUESTAS COMPUESTAS DEJARON DE SER UN JSON QUE UNA CARATULA PARTE, y cada una trae
		// su frase en un campo tipado. Es el argumento de M-4 aplicado a las lecturas que M-4 no
		// alcanzo, y lo que lo vuelve M-7 y no una limpieza es que lo que ahora cruza es una FRASE:
		// partir un documento para sacar la unica cosa que una caratula no puede rehacer es el caso
		// peor de G4, no el mismo.
		[Test]
		public void TheThreeComposedReadsComeBackTypedWithTheirSentenceInAField()
		{
			ElExpedienteCompleto();

			RegistroDeLaVariacion basura = api.VariacionDe(ElRubroPorUnidad, 0.25m, 0.02m);
			RegistroDeLaVariacion ibi = api.VariacionDe(ElRubroSobreBase, 0.25m, 0.02m);
			RegistroDeLaReservaPropuesta propuesta = api.ReservaPropuestaPara(2025);
			RegistroDelFaltanteDeReserva faltante =
				api.FaltanteDeReservaAl(new DateTime(2025, 9, 15), 0.10m);

			Assert.Multiple(() =>
			{
				// LOS CAMPOS TIPADOS, cada uno anclado a un hecho que este expediente escribio.
				Assert.That(basura.Rubro, Is.EqualTo(ElRubroPorUnidad), "VariacionDe: el rubro");
				Assert.That(basura.Desde, Is.EqualTo("2024-03"), "el periodo anterior, como MES");
				Assert.That(basura.Hasta, Is.EqualTo("2024-04"), "y el actual");
				Assert.That(basura.Antes, Is.EqualTo(5000m), "el monto por periodo anterior, TIPADO");
				Assert.That(basura.Ahora, Is.EqualTo(9000m), "y el actual");
				Assert.That(basura.Proporcion, Is.EqualTo(0.8m), "el cambio como proporcion");
				Assert.That(basura.Explicacion, Is.EqualTo("cambio-de-tarifa"), "y que lo explica");
				Assert.That(basura.EsSalto, Is.True, "un 80 % pasa el umbral de 25 %");
				Assert.That(basura.Urgencia, Is.EqualTo("ninguna"), "y un aumento ya paso, asi que no apura");

				Assert.That(ibi.Desde, Is.EqualTo("2024-T1"),
					"y el del IBI viaja como TRIMESTRE, que es el mismo numero leido de otra manera");
				Assert.That(ibi.Hasta, Is.EqualTo("2024-T2"), "y el actual, tambien trimestre");

				// LA FRASE, Y EL SUJETO DE CADA UNA.
				Assert.That(basura.Dice, Does.Contain("tarifa"), "la frase de basura dice tarifa");
				Assert.That(basura.Dice, Does.Not.Contain(ElRubroSobreBase),
					"y no nombra el rubro ajeno");
				Assert.That(ibi.Dice, Does.Contain("avalúo"), "la del IBI dice avaluo");
				Assert.That(ibi.Dice, Does.Not.Contain(ElRubroPorUnidad), "y no nombra el rubro ajeno");
				Assert.That(basura.Dice, Is.Not.EqualTo(ibi.Dice),
					"Y el mismo salto en dos rubros cruza la puerta como dos explicaciones.");

				Assert.That(propuesta.Anio, Is.EqualTo(2025), "ReservaPropuestaPara: el anio");
				Assert.That(propuesta.Proyeccion, Is.EqualTo(252000m),
					"la proyeccion: 9000 x 12 de basura mas 36000 x 4 del IBI");
				Assert.That(propuesta.PropuestoPorMes, Is.EqualTo(21000m), "y eso entre doce");
				Assert.That(propuesta.Dice, Does.Contain("21000"),
					"y la frase dice el monto que propone");
				Assert.That(propuesta.Dice, Does.Contain("proyección de esta app"),
					"y dice que es una propuesta de la app");

				Assert.That(faltante.Propuesto, Is.EqualTo(21000m),
					"FaltanteDeReservaAl: lo propuesto, que es la mitad de la app");
				Assert.That(faltante.Declarado, Is.EqualTo(8000m),
					"y lo declarado, que son los 8000 que este expediente adopto");
				Assert.That(faltante.Faltante, Is.EqualTo(13000m), "el faltante");
				Assert.That(faltante.Dice, Does.Contain("La app propone apartar 21000"),
					"y la frase dice de quien es cada mitad, con el numero pegado a su clausula");
				Assert.That(faltante.Dice, Does.Contain("vos adoptaste 8000"),
					"la de la persona igual: intercambiadas, esto se pone rojo");
			});
		}

		// LAS TRES LECTURAS GUARDADAS TIENEN SU PAR EN LA PUERTA, que es lo que M-7 le agrego a C5: una
		// caratula puede preguntar antes en vez de tener que atrapar una excepcion cuyo tipo es internal
		// y que ninguna caratula puede nombrar.
		//
		// Y EL PAR CONCUERDA CON LA LECTURA QUE GUARDA, EN LOS DOS ESTADOS. Un predicado que dijera que
		// si mientras la lectura se niega seria peor que no tener ninguno.
		[Test]
		public void EachGuardedComposedReadHasAPairOnTheDoorAndTheTwoAgreeInBothStates()
		{
			Assert.Multiple(() =>
			{
				Assert.That(api.SePuedeVerLaVariacionDe(ElRubroPorUnidad), Is.False,
					"sobre la semilla no hay dos pagos de basura");
				Assert.That(api.SePuedeProyectar(), Is.False, "ni con que proyectar");
				Assert.That(api.SePuedeVerElFaltanteDeReserva(), Is.False, "ni reserva adoptada");

				Assert.Throws<DomainException>(() => api.VariacionDe(ElRubroPorUnidad, 0.25m, 0.02m),
					"y la lectura se niega, que es con lo que el predicado tiene que concordar");
				Assert.Throws<DomainException>(() => api.ReservaPropuestaPara(2025));
				Assert.Throws<DomainException>(
					() => api.FaltanteDeReservaAl(new DateTime(2025, 9, 15), 0.10m));
			});

			ElExpedienteCompleto();

			Assert.Multiple(() =>
			{
				Assert.That(api.SePuedeVerLaVariacionDe(ElRubroPorUnidad), Is.True,
					"con dos pagos de basura, si");
				Assert.That(api.SePuedeProyectar(), Is.True);
				Assert.That(api.SePuedeVerElFaltanteDeReserva(), Is.True);

				Assert.That(api.VariacionDe(ElRubroPorUnidad, 0.25m, 0.02m).Dice, Is.Not.Empty,
					"y las tres lecturas contestan en vez de negarse");
				Assert.That(api.ReservaPropuestaPara(2025).Dice, Is.Not.Empty);
				Assert.That(api.FaltanteDeReservaAl(new DateTime(2025, 9, 15), 0.10m).Dice, Is.Not.Empty);

				// UN RUBRO QUE NO TIENE DOS PAGOS, con el par contestando que no: es el caso que una
				// caratula tiene enfrente cada vez que itera los cinco rubros del catalogo.
				Assert.That(api.SePuedeVerLaVariacionDe("parques-y-zonas-verdes"), Is.False,
					"parques no tiene ni un pago en este expediente");
				Assert.Throws<DomainException>(
					() => api.VariacionDe("parques-y-zonas-verdes", 0.25m, 0.02m),
					"y la lectura se niega igual que el predicado dijo");
			});
		}

		// E48 POR LA PUERTA: la mitad GENERAL se puede pedir sin nada de la persona, y la compuesta
		// exige las dos. Que una caratula pueda mostrar lo que la app propone SIN que la persona haya
		// adoptado nada es la mitad que se pierde si solo existe la lectura compuesta.
		[Test]
		public void TheGeneralHalfIsAnswerableWithNothingAboutThePersonWhileTheComposedOneNeedsBoth()
		{
			api.RegistrarPago("R-1", new DateTime(2025, 3, 31), 0m);
			api.AgregarLineaAlPago("R-1", ElRubroPorUnidad, "2025.03", 9000m, 0m);

			Assert.Multiple(() =>
			{
				Assert.That(api.SePuedeProyectar(), Is.True,
					"con un pago ya hay con que proyectar, que es todo lo que la mitad general pide");
				Assert.That(api.SePuedeVerElFaltanteDeReserva(), Is.False,
					"y sin reserva adoptada la compuesta todavia no se puede");

				RegistroDeLaReservaPropuesta sola = api.ReservaPropuestaPara(2025);

				Assert.That(sola.PropuestoPorMes, Is.EqualTo(9000m),
					"la mitad general contesta: 9000 x 12 entre doce");
				Assert.That(sola.Dice, Does.Contain("9000"), "y la dice");
				Assert.That(sola.Dice, Does.Not.Contain("adoptaste"),
					"sin nombrar nada que la persona haya adoptado, porque no hay nada");
			});

			api.AdoptarReserva(8000m, new DateTime(2025, 8, 1));

			RegistroDelFaltanteDeReserva compuesta =
				api.FaltanteDeReservaAl(new DateTime(2025, 9, 15), 0.10m);

			Assert.Multiple(() =>
			{
				Assert.That(compuesta.Propuesto, Is.EqualTo(9000m),
					"y la compuesta trae la misma mitad general");
				Assert.That(compuesta.Declarado, Is.EqualTo(8000m), "mas la de la persona");
				Assert.That(compuesta.Dice, Does.Not.Contain(api.ReservaPropuestaPara(2025).Dice),
					"y NO contiene la frase general textual, asi que mostrar las dos no imprime la "
					+ "misma clausula dos veces");
			});
		}
	}
}
