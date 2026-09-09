using System;
using System.IO;
using Municipalidad.App;
using NUnit.Framework;
using Puppeteer;

namespace Municipalidad.Dominio.Pruebas
{
	// LO QUE M-5 CIERRA, QUE ES G5. Antes de estas siete lecturas una caratula no tenia de donde sacar
	// los argumentos que la puerta le exige: iterar los rubros pedia tenerlos escritos a mano,
	// PeriodosPagadosSinComprobante pedia un dia que nadie contestaba, y nada de Finca cruzaba la
	// puerta -- dos comandos escribian y ninguna consulta leia de vuelta.
	//
	// EL DESCUBRIMIENTO NO ES UN DATO DE LA PERSONA. Cada una de las siete contesta algo que la app ya
	// sabe y que quien pregunta no puede inventar. Preguntarselo a la caratula seria ponerla como
	// fuente de un hecho del expediente.
	//
	// LAS DOS GUARDADAS VAN CON SU PAR, y eso es la forma que C5 recomienda para las tres que no lo
	// tienen: una PRE-PREGUNTA total, y una lectura que se niega si nadie la hizo. Los dos caminos se
	// corren aca, en dos estados del expediente, porque una lectura guardada corrida solo por el lado
	// que contesta deja el otro sin ejercitar.
	[TestFixture]
	public sealed class TheDiscoveryReadsAnswerWhatAShellCannotSupply
	{
		private const string ElActor = "municipalidad";
		private const string ElRubro = "recoleccion-de-basura";

		private static readonly DateTime LaConsultaVieja = new DateTime(2025, 6, 3);
		private static readonly DateTime LaConsultaNueva = new DateTime(2025, 9, 3);
		private static readonly DateTime LaDeclaracion = new DateTime(2025, 1, 15);

		private string carpeta;
		private MunicipalidadApp app;
		private MunicipalidadAPI api;

		[SetUp]
		public void Preparar()
		{
			carpeta = Path.Combine(
				Path.GetTempPath(), "municipalidad-descubrimiento", Guid.NewGuid().ToString("N"));
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

		// EL CATALOGO NO DEPENDE DEL EXPEDIENTE, y por eso esta prueba corre sobre un journal que solo
		// tiene la semilla: los cinco rubros existen antes de que la persona pague nada.
		//
		// Y LO CONTESTA EL CATALOGO, no la cuenta: el script alcanza `Rubro.LosQueHay()`, un metodo
		// ESTATICO de un tipo que no es global de raiz. MEDIDO en M-5 -- el DSL si lo alcanza, y
		// Rubro.cs lo afirmaba en prosa desde el primer dia sin que nada lo ejercitara.
		[Test]
		public void TheCatalogueOfRubrosAnswersOnAJournalThatHoldsNothingButTheSeed()
		{
			string[] losQueHay = api.LosRubrosQueHay();

			Assert.Multiple(() =>
			{
				Assert.That(losQueHay, Has.Length.EqualTo(5), "los cinco rubros del catalogo");

				Assert.That(losQueHay, Is.EqualTo(new[]
				{
					"recoleccion-de-basura",
					"limpieza-de-calles",
					"servicio-policia",
					"bienes-inmuebles",
					"parques-y-zonas-verdes",
				}), "y son estos cinco, en el orden de las fabricas del catalogo");
			});
		}

		// LOS RUBROS CON HISTORIA SON UN SUBCONJUNTO DEL CATALOGO Y NO EL CATALOGO, que es la razon de
		// que sean dos lecturas y no una: el catalogo dice que se puede cobrar, la historia dice de que
		// la app vio pagar. HistorialDe y VariacionDe contestan sobre el segundo juego, no el primero.
		[Test]
		public void TheRubrosWithHistoryAreASubsetOfTheCatalogueAndGrowWithThePayments()
		{
			Assert.That(api.RubrosConHistoria(), Is.Empty,
				"Sin ningun pago, ningun rubro tiene historia -- y es una respuesta, no una negativa.");

			api.RegistrarPago("R-1", new DateTime(2024, 3, 31), 0m);
			api.AgregarLineaAlPago("R-1", ElRubro, "2024.03", 5000m, 0m);

			Assert.Multiple(() =>
			{
				Assert.That(api.RubrosConHistoria(), Is.EqualTo(new[] { ElRubro }),
					"Con un pago de basura, basura tiene historia y los otros cuatro no.");

				Assert.That(api.RubrosConHistoria(), Is.SubsetOf(api.LosRubrosQueHay()),
					"Y lo que tiene historia siempre esta en el catalogo: un rubro desconocido no puede "
					+ "haber entrado por un pago, porque el Check lo niega antes.");
			});
		}

		// LA CONSULTA MAS RECIENTE ES EL ARGUMENTO QUE LA PUERTA EXIGIA Y NADIE CONTESTABA.
		// PeriodosPagadosSinComprobante pide un dia exacto; sin esta lectura, una caratula solo podia
		// adivinarlo o guardarselo por su cuenta, que es tener el hecho dos veces.
		//
		// SE CORREN LOS DOS CAMINOS. Sin ninguna consulta la PRE-PREGUNTA dice que no y la lectura se
		// niega; no devuelve 01/01/0001, que es la leccion de T1 que Finca.cs ya cita.
		[Test]
		public void TheMostRecentConsultaIsGuardedByItsOwnTotalQuestion()
		{
			Assert.Multiple(() =>
			{
				Assert.That(api.HayAlgunaConsulta(), Is.False,
					"Sin ninguna consulta, la PRE-PREGUNTA contesta que no y es TOTAL.");

				Assert.Throws<DomainException>(() => api.ConsultaMasReciente(),
					"Y la lectura se niega en vez de inventar un dia. Preguntar HayAlgunaConsulta antes.");
			});

			api.RegistrarConsulta(LaConsultaVieja);
			api.RegistrarConsulta(LaConsultaNueva);

			Assert.Multiple(() =>
			{
				Assert.That(api.HayAlgunaConsulta(), Is.True);

				Assert.That(api.ConsultaMasReciente(), Is.EqualTo(LaConsultaNueva),
					"La MAS RECIENTE, no la ultima registrada: las dos entraron en orden inverso al "
					+ "calendario y la respuesta es setiembre, no junio.");
			});
		}

		// Y LA MAS RECIENTE SE MIDE POR EL DIA, NO POR EL ORDEN DE ENTRADA. Se registran en orden
		// inverso a proposito: una implementacion que devolviera la ultima de la lista pasaria la
		// prueba de arriba y fallaria aca.
		[Test]
		public void TheMostRecentConsultaIsTheLatestDayAndNotTheLastOneRegistered()
		{
			api.RegistrarConsulta(LaConsultaNueva);
			api.RegistrarConsulta(LaConsultaVieja);

			Assert.That(api.ConsultaMasReciente(), Is.EqualTo(LaConsultaNueva),
				"Setiembre entro primero y junio despues, y la respuesta sigue siendo setiembre.");
		}

		// LA FINCA CRUZA LA PUERTA DE VUELTA. Dos comandos la escribian y ninguna consulta la leia:
		// EstadoDeLaCuenta decia cuantas hay y nada mas, asi que ni el numero ni el valor salian.
		//
		// EL VALOR ES DE LA PERSONA -- es la mitad hibrida de este dominio -- y su ausencia tiene DOS
		// razones que la PRE-PREGUNTA junta en una: la finca no esta, o no tenia valor a esa fecha.
		[Test]
		public void TheFincaReadsCrossBackAndTheValueIsGuardedByOneQuestionForTwoAbsences()
		{
			Assert.That(api.NumerosDeLasFincas(), Is.Empty,
				"Sin ninguna finca, cero filas -- y es una respuesta.");

			api.IncorporarFinca("F-1");
			api.IncorporarFinca("F-2");
			api.DeclararValorDeFinca("F-1", LaDeclaracion, 60000000m);

			DateTime despuesDeLaDeclaracion = new DateTime(2025, 6, 30);
			DateTime antesDeLaDeclaracion = new DateTime(2024, 12, 31);

			Assert.Multiple(() =>
			{
				Assert.That(api.NumerosDeLasFincas(), Is.EqualTo(new[] { "F-1", "F-2" }),
					"Las dos fincas, en el orden en que entraron.");

				Assert.That(
					api.TieneValorDeclaradoLaFinca("F-1", despuesDeLaDeclaracion), Is.True,
					"F-1 tiene valor declarado despues de la declaracion");
				Assert.That(
					api.ValorDeFincaAl("F-1", despuesDeLaDeclaracion), Is.EqualTo(60000000m),
					"y es el monto que la persona declaro");

				// LA PRIMERA AUSENCIA: la finca esta, el valor no.
				Assert.That(
					api.TieneValorDeclaradoLaFinca("F-2", despuesDeLaDeclaracion), Is.False,
					"F-2 esta incorporada y no tiene valor declarado");
				Assert.Throws<DomainException>(
					() => api.ValorDeFincaAl("F-2", despuesDeLaDeclaracion),
					"y la lectura se niega en vez de inventarle uno");

				// LA SEGUNDA AUSENCIA: la finca no esta.
				Assert.That(
					api.TieneValorDeclaradoLaFinca("F-9", despuesDeLaDeclaracion), Is.False,
					"F-9 no existe, y la PRE-PREGUNTA lo contesta sin negarse -- es TOTAL");
				Assert.Throws<DomainException>(
					() => api.ValorDeFincaAl("F-9", despuesDeLaDeclaracion),
					"y la lectura se niega");

				// LA TERCERA: la finca esta y el valor tambien, pero no A ESA FECHA. El valor tiene
				// fecha, asi que antes de declararlo no habia valor vigente.
				Assert.That(
					api.TieneValorDeclaradoLaFinca("F-1", antesDeLaDeclaracion), Is.False,
					"F-1 no tenia valor declarado ANTES de declararlo, aunque hoy lo tenga");
				Assert.Throws<DomainException>(
					() => api.ValorDeFincaAl("F-1", antesDeLaDeclaracion),
					"y la lectura se niega tambien ahi");
			});
		}

		// LOS DOS CAMPOS QUE M-5 LE AGREGO A EstadoDeLaCuenta, y el primero hace que ese script cruce
		// las DOS globales de raiz por primera vez en este modulo.
		//
		// `sePuedeProyectar` ES LA MITAD QUE C5 PEDIA para una de las tres lecturas sin par: con el, una
		// caratula puede preguntar antes de llamar a ReservaPropuestaPara en vez de tener que atrapar la
		// negativa del dominio.
		[Test]
		public void TheStateOfTheAccountSaysHowFarTheAppReadAndWhetherItCanProject()
		{
			Assert.Multiple(() =>
			{
				Assert.That(api.EstadoDeLaCuenta(), Does.Contain("\"ultimoEventoLeido\":0"),
					"Cero es 'todavia no se leyo nada': los eventos se numeran desde uno.");

				Assert.That(api.EstadoDeLaCuenta(), Does.Contain("\"sePuedeProyectar\":false"),
					"Sin ningun pago no hay con que proyectar, y se dice.");
			});

			api.RegistrarPago("R-1", new DateTime(2024, 3, 31), 0m);
			api.AgregarLineaAlPago("R-1", ElRubro, "2024.03", 5000m, 0m);

			Assert.That(api.EstadoDeLaCuenta(), Does.Contain("\"sePuedeProyectar\":true"),
				"Con un pago ya hay con que proyectar, y ReservaPropuestaPara deja de negarse.");
		}

		// Y EL PAR SIRVE PARA LO QUE SE PIDIO: preguntar antes en vez de atrapar. Es la unica de las
		// tres lecturas sin par de C5 que M-5 alcanza, y se afirma que el par CONCUERDA con la lectura
		// en los dos estados -- un predicado que dijera que si mientras la lectura se niega seria peor
		// que no tenerlo.
		//
		// M-7 LE PUSO AL LADO EL PAR TIPADO. `sePuedeProyectar` sigue viajando adentro del documento
		// del estado, que es el canal de la foto; `SePuedeProyectar()` es el canal tipado, y una
		// caratula que solo necesita el si o el no ya no tiene que partir un documento para sacarlo. Se
		// afirman los DOS aca, y que digan lo mismo: son el mismo hecho por dos canales, y si se
		// separaran una caratula preguntaria una cosa y otra caratula otra.
		[Test]
		public void TheProjectionQuestionAgreesWithTheReadItGuardsInBothStates()
		{
			Assert.Multiple(() =>
			{
				Assert.That(api.EstadoDeLaCuenta(), Does.Contain("\"sePuedeProyectar\":false"));
				Assert.That(api.SePuedeProyectar(), Is.False,
					"y el canal tipado dice lo mismo que el documento");
				Assert.Throws<DomainException>(() => api.ReservaPropuestaPara(2025),
					"El predicado dice que no y la lectura se niega: concuerdan.");
			});

			api.RegistrarPago("R-1", new DateTime(2024, 3, 31), 0m);
			api.AgregarLineaAlPago("R-1", ElRubro, "2024.03", 5000m, 0m);

			Assert.Multiple(() =>
			{
				Assert.That(api.EstadoDeLaCuenta(), Does.Contain("\"sePuedeProyectar\":true"));
				Assert.That(api.SePuedeProyectar(), Is.True, "y el tipado tambien de este lado");
				Assert.That(api.ReservaPropuestaPara(2025).Anio, Is.EqualTo(2025),
					"El predicado dice que si y la lectura contesta: concuerdan tambien de este lado.");
			});
		}
	}
}
