using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Municipalidad.App;
using Municipalidad.Mcp;
using Municipalidad.Mcp.Tools;
using NUnit.Framework;
using Puppeteer;

namespace Municipalidad.Dominio.Pruebas
{
	// LO QUE SE LE OFRECE AL ASISTENTE ES UNA LISTA CERRADA DE PREGUNTAS, no una consola de DSL.
	//
	// LA PUERTA QUE EL LECTOR ENTREGA ES LA ENTERA -- diez comandos y Aplicar entre ellos --, asi que
	// que este servidor sea de solo lectura es, del lado de la puerta, una disciplina y no una
	// estructura. DEC-42 lo dice con esas palabras. Lo que lo sostiene son dos barridos distintos y
	// hacen falta los dos: el de aca enumera lo que se OFRECE, y el de
	// TheReadOnlyHostNeverWritesToThePersonsJournal mide que llamarlo todo no deje ni una entrada.
	//
	// LO QUE ESTE BARRIDO NO PUEDE VER, dicho en vez de omitido: que Program.cs REGISTRE las seis
	// clases. Una clase con [McpServerToolType] que nadie pase a .WithTools sale igual en esta lista y
	// no llega a ningun asistente. Program.cs son sentencias de nivel superior de un Exe y armar aca su
	// contenedor seria copiarlo, no probarlo. Lo unico que atrapa un .WithTools que falta es un
	// tools/list corrido a mano contra el servidor.
	[TestFixture]
	public sealed class TheAssistantIsOfferedSixQuestionsAndNoneOfThemIsACommand
	{
		private const string ElRubro = "recoleccion-de-basura";
		private const string LaFinca = "F-1";
		private const string LaClaveDelDispositivo = "DISPOSITIVO-QUE-NO-TIENE-QUE-SALIR";
		private const string ElToken = "token-que-no-tiene-que-salir";
		private const string ElRefresco = "refresco-que-tampoco";

		private static readonly DateTime LaConsulta = new DateTime(2025, 9, 3);

		private string carpeta;

		[SetUp]
		public void Preparar()
		{
			carpeta = Path.Combine(
				Path.GetTempPath(), "municipalidad-tools", Guid.NewGuid().ToString("N"));
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

		// LAS SEIS PREGUNTAS QUE Preguntas.md SE COMPROMETE A CONTESTAR, escritas aca para que agregar
		// una septima obligue a mirar esta lista -- y a agregarla al barrido de que leer no escribe, que
		// es la otra mitad de DEC-42. La cuenta va en el NOMBRE de la prueba, que es donde se puede
		// mover a proposito; en un comentario se quedaba vieja sin ponerse roja.
		//
		// SE BARRE EL ENSAMBLADO Y NO UNA CLASE: un barrido por las clases que uno se acuerda de
		// escribir no ve la septima clase que alguien agregue. Es C57 en esta frontera.
		[Test]
		public void TheAssistantIsOfferedSixQuestionsAndNoneOfThemIsACommandItself()
		{
			List<string> ofrecidas = new List<string>();

			foreach (MethodInfo cada in LosMetodosQueSonTool())
				ofrecidas.Add(ElNombreDeLaTool(cada));

			ofrecidas.Sort(StringComparer.Ordinal);

			Assert.That(ofrecidas, Is.EqualTo(new[]
			{
				"cuanto_apartar", "cuanto_he_pagado", "por_que_pago_mas", "que_quedo_sin_comprobante",
				"que_se_me_vence", "que_tan_al_dia_esta_esto",
			}), "Una tool que se agrega sin pensarla es como entra un comando a una superficie de solo "
				+ "lectura. La lista se escribe aca para que agregar una obligue a mirarla, y para que "
				+ "quien la agregue se acuerde de meterla en LasSeisPreguntasDelAsistente.");
		}

		// Y LAS CLASES QUE LAS LLEVAN TAMBIEN SE ESCRIBEN, por la misma razon: son los sitios que
		// Program.cs tiene que registrar, y una que aparezca sin que nadie la registre queda invisible
		// para el asistente y para la prueba de arriba.
		//
		// LA FRONTERA DE CLASE ES DE QUE HABLA LA RESPUESTA, que es la regla del modulo de referencia.
		// Seis preguntas de seis sujetos distintos dan seis clases, y ninguna lleva dos.
		[Test]
		public void TheToolsLiveInExactlySixClassesAndAllOfThemAreAboutReading()
		{
			List<string> nombradas = new List<string>();

			foreach (Type cada in LasClasesDeToolsDelServidor()) nombradas.Add(cada.Name);

			nombradas.Sort(StringComparer.Ordinal);

			Assert.That(nombradas, Is.EqualTo(new[]
			{
				"PreguntasDeLaReserva", "PreguntasDeLaVariacion", "PreguntasDeLoPagado",
				"PreguntasDeLosComprobantes", "PreguntasDeLosVencimientos", "PreguntasDelEstado",
			}), "Una clase de tools nueva tiene que pasar por aca, porque tiene que pasar tambien por el "
				+ ".WithTools de Program.cs y esto es lo unico que lo recuerda.");
		}

		// NINGUN PARAMETRO DE NINGUNA TOOL ES UN HECHO DE LA CUENTA DE LA PERSONA, que es el "Done when"
		// del plan y la invencion prohibida del modulo de referencia: un parametro que pide un hecho es
		// la misma invencion con un esquema encima.
		//
		// HAY UNO SOLO Y ES EL ALCANCE DE LA PREGUNTA, no un hecho: de que anio se pregunta. Un anio lo
		// elige quien pregunta y no sale de la cuenta, igual que los limites de un periodo en el modulo
		// de referencia. Los umbrales NO estan aca a proposito -- DEC-46 --: si el asistente los
		// eligiera, dos preguntas iguales darian dos respuestas distintas.
		//
		// Y LA FECHA DE LA CONSULTA TAMPOCO ESTA, que es el caso que mas facil se habria colado:
		// PeriodosPagadosSinComprobante toma un dia, pero tiene que ser un dia en que hubo consulta, y
		// eso es un hecho de la cuenta. La tool lo saca de ConsultaMasReciente.
		[Test]
		public void NoToolParameterIsAFactAboutThePersonsOwnAccount()
		{
			List<string> pedidos = new List<string>();

			foreach (MethodInfo cada in LosMetodosQueSonTool())
				foreach (ParameterInfo parametro in cada.GetParameters())
					pedidos.Add(ElNombreDeLaTool(cada) + "." + parametro.Name
						+ " : " + parametro.ParameterType.Name);

			pedidos.Sort(StringComparer.Ordinal);

			Assert.That(pedidos, Is.EqualTo(new[]
			{
				"cuanto_he_pagado.anio : Int32",
			}), "Un parametro nuevo tiene que pasar por aca y decir por escrito que NO es un hecho de la "
				+ "cuenta de esta persona. Un anio es el alcance de la pregunta; un monto, una fecha de "
				+ "consulta o un umbral no lo son.");
		}

		// LA CREDENCIAL DE LA PERSONA NO VIAJA EN NINGUNA RESPUESTA, y se comprueba por el VALOR y no
		// por la lista de campos: una tool nueva que la copiara en un 'detalle' pasaria cualquier prueba
		// escrita sobre nombres de propiedad. El token esta EN CLARO en el journal, en una carpeta que
		// este proceso ademas lee, asi que lo unico que lo separa del asistente es que ninguna tool lo
		// pida.
		[Test]
		public void NoAnswerTheAssistantGetsCarriesThePersonsCredential()
		{
			UnExpedienteComoElDeUnaPersona();

			LectorDeJournal lector = new LectorDeJournal(carpeta);

			string respuestas = TodasLasRespuestasSerializadas(lector);

			Assert.Multiple(() =>
			{
				Assert.That(respuestas, Does.Not.Contain(ElToken), "el token de acceso");
				Assert.That(respuestas, Does.Not.Contain(ElRefresco), "el token de refresco");
				Assert.That(respuestas, Does.Not.Contain(LaClaveDelDispositivo), "la clave del equipo");

				// Y EL CONTROL: la puerta SI las tiene guardadas, asi que lo de arriba mide que ninguna
				// tool las pida y no que el journal no las tenga.
				Assert.That(lector.Leer(api => api.TokenGuardado(), () => ""), Is.EqualTo(ElToken),
					"La puerta que el lector entrega si alcanza la credencial.");
			});
		}

		// LAS SEIS CONTESTAN SOBRE UN JOURNAL EN QUE TODAS LAS GUARDAS DICEN QUE NO, y ninguna revienta.
		// Es C5 visto desde la caratula: el modulo tomo el tercer arreglo candidato -- el par -- y esta
		// prueba es lo que obliga a que cada tool pregunte el par antes de leer. Una tool que llamara la
		// lectura guardada sin preguntar se pondria roja aca con la negativa del dominio, que es
		// justamente el tipo que ninguna caratula puede nombrar.
		//
		// EL ESTADO ES UN JOURNAL RECIEN SEMBRADO: sin pagos, sin consulta, sin reserva adoptada y sin
		// fincas. Es el estado en que una persona instala la app y todavia no ha abierto nada.
		[Test]
		public void EveryToolAnswersOnAJournalWhereEveryGuardSaysNo()
		{
			using (MunicipalidadApp app = new MunicipalidadApp(MunicipalidadApp.ElActor))
			{
				app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
				app.Start();
			}

			LectorDeJournal lector = new LectorDeJournal(carpeta);

			Assert.Multiple(() =>
			{
				VariacionesDeLaCuenta variacion = new PreguntasDeLaVariacion(lector).PorQuePagoMas();
				Assert.That(variacion.Rubros, Has.Count.EqualTo(5), "por_que_pago_mas: los cinco rubros");
				foreach (VariacionDeUnRubro cada in variacion.Rubros)
					Assert.That(cada.Dice, Is.Not.Empty,
						"por_que_pago_mas: " + cada.Rubro + " dice por que no tiene variacion");

				LoQueSeVence vence = new PreguntasDeLosVencimientos(lector).QueSeMeVence();
				Assert.That(vence.PorVencer, Is.Empty, "que_se_me_vence: nada por vencer");
				Assert.That(vence.Vencidos, Is.Empty, "que_se_me_vence: nada vencido");

				LaReservaDeEstaPersona reserva = new PreguntasDeLaReserva(lector).CuantoApartar();
				Assert.That(reserva.SePuedeProponer, Is.False, "cuanto_apartar: sin pagos no hay que proponer");
				Assert.That(reserva.HayReservaAdoptada, Is.False, "cuanto_apartar: nadie adopto nada");
				Assert.That(reserva.DiceLaPropuesta, Is.Not.Empty, "cuanto_apartar: y lo dice");
				Assert.That(reserva.DiceElFaltante, Is.Not.Empty, "cuanto_apartar: y lo dice la otra mitad");

				LoQueQuedoSinComprobante cotejo =
					new PreguntasDeLosComprobantes(lector).QueQuedoSinComprobante();
				Assert.That(cotejo.HayAlgunaConsulta, Is.False, "que_quedo_sin_comprobante: sin consulta");
				Assert.That(cotejo.Estado, Is.EqualTo("sin-consulta"),
					"que_quedo_sin_comprobante: y el estado NO dice que todo este en orden");

				LoPagadoPorEstaPersona pagado = new PreguntasDeLoPagado(lector).CuantoHePagado();
				Assert.That(pagado.TotalDelAnio, Is.EqualTo(0m), "cuanto_he_pagado: cero");
				Assert.That(pagado.Series, Has.Count.EqualTo(5), "cuanto_he_pagado: las cinco series");

				QueTanAlDiaEstaEsto estado = new PreguntasDelEstado(lector).QueTanAlDiaEstaEsto();
				Assert.That(estado.UltimoEventoLeido, Is.EqualTo(0L),
					"que_tan_al_dia_esta_esto: todavia no entro ningun hecho");
				Assert.That(estado.Fincas, Is.Empty, "que_tan_al_dia_esta_esto: sin fincas");
			});
		}

		// Y LAS SEIS CONTESTAN SOBRE UN RUBRO CON UN SOLO COMPROBANTE, que es el unico estado en que la
		// disciplina del par se puede ver fallar. Hace falta esta prueba ADEMAS de la de arriba, y eso se
		// midio en vez de suponerse: con el par de la variacion desactivado a mano, la de arriba quedo
		// VERDE -- sobre un journal sin pagos ningun rubro llega siquiera a la lectura guardada, asi que
		// la promesa se estaba comprobando sobre un camino que nunca se recorre. Con UN comprobante el
		// rubro si tiene historia, la tool si llega, y sin el par revienta con la negativa del dominio:
		// el tipo internal que ninguna caratula puede nombrar. Es C61 -- una comparacion vale lo que vale
		// el MOMENTO en que se toma.
		[Test]
		public void EveryToolAnswersOnARubroThatHasExactlyOneComprobante()
		{
			using (MunicipalidadApp app = new MunicipalidadApp(MunicipalidadApp.ElActor))
			{
				app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
				app.Start();

				MunicipalidadAPI api = new MunicipalidadAPI(app);

				api.RegistrarPago("R-1", new DateTime(2024, 3, 31), 0m);
				api.AgregarLineaAlPago("R-1", ElRubro, "2024.03", 5000m, 0m);
			}

			LectorDeJournal lector = new LectorDeJournal(carpeta);

			Assert.Multiple(() =>
			{
				VariacionesDeLaCuenta variacion = new PreguntasDeLaVariacion(lector).PorQuePagoMas();

				foreach (VariacionDeUnRubro cada in variacion.Rubros)
					if (cada.Rubro == ElRubro)
					{
						Assert.That(cada.TieneHistoria, Is.True,
							"por_que_pago_mas: el rubro SI tiene historia, que es lo que hace llegar la "
							+ "tool hasta la lectura guardada");
						Assert.That(cada.SePuedeVer, Is.False,
							"por_que_pago_mas: y con un solo comprobante NO se puede ver la variacion, "
							+ "asi que la tool tuvo que preguntar el par antes de leer");
						Assert.That(cada.Dice, Is.Not.Empty,
							"por_que_pago_mas: y lo dice con palabras en vez de reventar");
					}

				Assert.That(new PreguntasDeLosVencimientos(lector).QueSeMeVence().HayDatos, Is.True,
					"que_se_me_vence");
				Assert.That(new PreguntasDeLaReserva(lector).CuantoApartar().SePuedeProponer, Is.True,
					"cuanto_apartar: con un pago ya se puede proyectar");
				Assert.That(
					new PreguntasDeLaReserva(lector).CuantoApartar().HayReservaAdoptada, Is.False,
					"cuanto_apartar: y todavia nadie adopto nada, que es la otra mitad sola");
				Assert.That(
					new PreguntasDeLosComprobantes(lector).QueQuedoSinComprobante().Estado,
					Is.EqualTo("sin-consulta"), "que_quedo_sin_comprobante");
				Assert.That(new PreguntasDeLoPagado(lector).CuantoHePagado(2024).TotalDelAnio,
					Is.EqualTo(5000m), "cuanto_he_pagado");
				Assert.That(new PreguntasDelEstado(lector).QueTanAlDiaEstaEsto().RubrosConHistoria,
					Is.EqualTo(new[] { ElRubro }), "que_tan_al_dia_esta_esto");
			});
		}

		// Y LAS SEIS CONTESTAN LA AUSENCIA SOBRE UNA CARPETA SIN JOURNAL, en vez de reventar. Es la otra
		// mitad de HayJournalEn: la app nunca se abrio en esta computadora es un estado, no un error.
		[Test]
		public void EveryToolAnswersTheAbsenceWhenTheAppWasNeverOpened()
		{
			LectorDeJournal lector = new LectorDeJournal(carpeta);

			Assert.Multiple(() =>
			{
				Assert.That(new PreguntasDeLaVariacion(lector).PorQuePagoMas().HayDatos, Is.False,
					"por_que_pago_mas");
				Assert.That(new PreguntasDeLosVencimientos(lector).QueSeMeVence().HayDatos, Is.False,
					"que_se_me_vence");
				Assert.That(new PreguntasDeLaReserva(lector).CuantoApartar().HayDatos, Is.False,
					"cuanto_apartar");
				Assert.That(
					new PreguntasDeLosComprobantes(lector).QueQuedoSinComprobante().HayDatos, Is.False,
					"que_quedo_sin_comprobante");
				Assert.That(new PreguntasDeLoPagado(lector).CuantoHePagado().HayDatos, Is.False,
					"cuanto_he_pagado");
				Assert.That(new PreguntasDelEstado(lector).QueTanAlDiaEstaEsto().HayDatos, Is.False,
					"que_tan_al_dia_esta_esto");

				Assert.That(new PreguntasDeLaVariacion(lector).PorQuePagoMas().Detalle,
					Does.Contain("todavía no se ha abierto"),
					"y la ausencia se dice con palabras, no con campos vacios");
			});
		}

		// EL NUMERO CON QUE SE PREGUNTO VIAJA EN LA RESPUESTA, y es la ley de DEC-46: cinco de los seis
		// umbrales no los ha firmado nadie, asi que una respuesta que no diga contra que se midio no se
		// puede citar. Se afirma contra la constante y no contra un literal: cambiar el umbral tiene que
		// mover la respuesta, no romper esta prueba.
		[Test]
		public void EveryThresholdTheToolAskedWithTravelsBackInTheAnswer()
		{
			UnExpedienteComoElDeUnaPersona();

			LectorDeJournal lector = new LectorDeJournal(carpeta);

			VariacionesDeLaCuenta variacion = new PreguntasDeLaVariacion(lector).PorQuePagoMas();
			LoQueSeVence vence = new PreguntasDeLosVencimientos(lector).QueSeMeVence();
			LaReservaDeEstaPersona reserva = new PreguntasDeLaReserva(lector).CuantoApartar();

			Assert.Multiple(() =>
			{
				Assert.That(variacion.UmbralDeSalto, Is.EqualTo(LosJuiciosSinFirmar.UmbralDeSalto),
					"por_que_pago_mas dice con que umbral midio");
				Assert.That(variacion.Ruido, Is.EqualTo(LosJuiciosSinFirmar.Ruido),
					"por_que_pago_mas dice cual es el ruido");
				Assert.That(vence.DiasDeAviso, Is.EqualTo(LosJuiciosSinFirmar.DiasDeAviso),
					"que_se_me_vence dice cuantos dias de aviso");
				Assert.That(vence.TasaConQueSeProyecto, Is.EqualTo(LosJuiciosSinFirmar.TasaDeMora),
					"que_se_me_vence dice con que tasa proyecto");
				Assert.That(reserva.UmbralConQueSeAvisa, Is.EqualTo(LosJuiciosSinFirmar.UmbralDeReserva),
					"cuanto_apartar dice con que umbral avisa");

				Assert.That(variacion.Detalle, Does.Contain("NADIE LO HA FIRMADO"),
					"y se dice con palabras que nadie los firmo, no solo con el numero");
				Assert.That(reserva.Detalle, Does.Contain("2026-09-03"),
					"y el unico firmado dice que lo esta, y cuando");
			});
		}

		// ------------------------------------------------------ el aparejo

		private static IEnumerable<MethodInfo> LosMetodosQueSonTool()
		{
			List<MethodInfo> encontrados = new List<MethodInfo>();

			foreach (Type cada in LasClasesDeToolsDelServidor())
				foreach (MethodInfo metodo in cada.GetMethods())
					if (ElNombreDeLaTool(metodo).Length > 0)
						encontrados.Add(metodo);

			return encontrados;
		}

		private static string ElNombreDeLaTool(MethodInfo metodo)
		{
			foreach (object atributo in metodo.GetCustomAttributes(true))
				if (atributo.GetType().Name == "McpServerToolAttribute")
					return (string)atributo.GetType().GetProperty("Name").GetValue(atributo);

			return "";
		}

		private static List<Type> LasClasesDeToolsDelServidor()
		{
			List<Type> encontradas = new List<Type>();

			foreach (Type cada in typeof(PreguntasDelEstado).Assembly.GetTypes())
				foreach (object atributo in cada.GetCustomAttributes(true))
					if (atributo.GetType().Name == "McpServerToolTypeAttribute")
						encontradas.Add(cada);

			return encontradas;
		}

		private static string TodasLasRespuestasSerializadas(LectorDeJournal lector)
		{
			StringBuilder todo = new StringBuilder();

			todo.Append(JsonSerializer.Serialize(new PreguntasDeLaVariacion(lector).PorQuePagoMas()));
			todo.Append(JsonSerializer.Serialize(new PreguntasDeLosVencimientos(lector).QueSeMeVence()));
			todo.Append(JsonSerializer.Serialize(new PreguntasDeLaReserva(lector).CuantoApartar()));
			todo.Append(JsonSerializer.Serialize(
				new PreguntasDeLosComprobantes(lector).QueQuedoSinComprobante()));
			todo.Append(JsonSerializer.Serialize(new PreguntasDeLoPagado(lector).CuantoHePagado()));
			todo.Append(JsonSerializer.Serialize(new PreguntasDeLoPagado(lector).CuantoHePagado(2024)));
			todo.Append(JsonSerializer.Serialize(new PreguntasDelEstado(lector).QueTanAlDiaEstaEsto()));

			return todo.ToString();
		}

		// El mismo expediente que usa la fixture del anfitrion, escrito por la app y cerrado antes de
		// leer, que es como estan las dos cosas de verdad.
		private void UnExpedienteComoElDeUnaPersona()
		{
			using (MunicipalidadApp app = new MunicipalidadApp(MunicipalidadApp.ElActor))
			{
				app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
				app.Start();

				MunicipalidadAPI api = new MunicipalidadAPI(app);

				api.IncorporarFinca(LaFinca);
				api.DeclararValorDeFinca(LaFinca, new DateTime(2025, 1, 15), 60000000m);

				api.RegistrarPago("R-1", new DateTime(2024, 3, 31), 0m);
				api.AgregarLineaAlPago("R-1", ElRubro, "2024.03", 5000m, 0m);

				api.RegistrarPago("R-2", new DateTime(2024, 6, 30), 0m);
				api.AgregarLineaAlPago("R-2", ElRubro, "2024.06", 18000m, 310m);

				api.RegistrarConsulta(LaConsulta);
				api.AgregarPendiente(LaConsulta, ElRubro, "2025.09", 24000m, 45m);

				api.PonerAlCobro(ElRubro, "2025.10", 8000m, new DateTime(2025, 9, 10));

				api.AdoptarReserva(30000m, new DateTime(2025, 8, 1));

				api.RegistrarDispositivo(LaClaveDelDispositivo);
				api.GuardarCredencial(ElToken, ElRefresco);
			}
		}
	}
}
