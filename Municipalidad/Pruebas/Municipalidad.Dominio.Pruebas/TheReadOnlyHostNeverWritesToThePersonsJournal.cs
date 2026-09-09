using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Municipalidad.App;
using Municipalidad.Mcp;
using Municipalidad.Mcp.Tools;
using NUnit.Framework;
using Puppeteer;

namespace Municipalidad.Dominio.Pruebas
{
	// LA INVARIANTE DE UN SOLO ESCRITOR, que es lo unico que C-1 tiene que demostrar.
	//
	// El servidor MCP y la app instalada comparten la carpeta de la persona. La app escribe; el
	// proceso del asistente solo lee. Si leer escribiera, dos procesos estarian journalizando sobre
	// el mismo actor y la promesa de que el journal reproduce la misma historia dos veces se caeria.
	//
	// SE MIDE POR LA ENTRADA, POR EL LARGO Y POR EL SEGUNDO PROGRAMA, no por el md5 de la carpeta
	// entera, y esa eleccion viene medida del modulo de referencia y no elegida por comodidad: el
	// motor NO tiene modo de solo lectura. Abrir un journal existente le crea index/index.bin, le
	// reescribe meta.bin, y a la septima apertura le escribe tres bytes de cabecera. Nada de eso es
	// un HECHO: la cuenta de entradas no se mueve, el largo del archivo no se mueve, y el segundo
	// programa -- cada define, cada invoke y cada blob de argumentos -- queda igual.
	//
	// ESTO NO ES UN SEGUNDO CENSO DE LA PUERTA. Que cada script exista, parsee y ate sus parametros
	// lo afirma EveryScriptOfTheDoorHasBeenRun; aca las lecturas se corren para que la promesa se
	// mida sobre recorridos que de verdad llegan al dominio, y no sobre el camino corto de una
	// carpeta vacia.
	[TestFixture]
	public sealed class TheReadOnlyHostNeverWritesToThePersonsJournal
	{
		private const string ElRubro = "recoleccion-de-basura";
		private const string LaFinca = "F-1";
		private const string LaClaveDelDispositivo = "DISPOSITIVO-DE-LA-PERSONA";
		private const string ElToken = "token-que-no-tiene-que-salir";
		private const string ElRefresco = "refresco-que-tampoco";

		private static readonly DateTime LaConsulta = new DateTime(2025, 9, 3);
		private static readonly DateTime LaAdopcion = new DateTime(2025, 8, 1);

		private string carpeta;

		[SetUp]
		public void Preparar()
		{
			carpeta = Path.Combine(
				Path.GetTempPath(), "municipalidad-solo-lectura", Guid.NewGuid().ToString("N"));
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

		// LA APP NUNCA SE ABRIO EN ESTA COMPUTADORA ES UNA RESPUESTA, NO UN FALLO. Es la razon por la
		// que DEC-06 dejo pendiente el ayudante: sin el, la unica manera de averiguarlo seria abrir el
		// journal y ver que revienta, y el asistente le diria a la persona que algo se rompio cuando lo
		// unico que pasa es que todavia no uso su app.
		//
		// SE AFIRMAN LOS DOS SENTIDOS. Solo el `false` dejaria pasar un ayudante que contesta que no
		// siempre, que es la falla que hace inutil la pregunta entera.
		[Test]
		public void AFolderWithNoJournalIsAnAnswerAndNotAFailure()
		{
			LectorDeJournal lector = new LectorDeJournal(carpeta);

			Assert.Multiple(() =>
			{
				Assert.That(MunicipalidadApp.HayJournalEn(carpeta), Is.False,
					"Una carpeta vacia no tiene journal de esta app.");
				Assert.That(lector.HayJournal, Is.False,
					"Y el lector contesta lo mismo, que es lo que una tool va a preguntar.");
				Assert.That(lector.Leer(api => api.EstadoDeLaCuenta(), () => "sin datos"),
					Is.EqualTo("sin datos"),
					"Y leer sobre esa carpeta contesta la ausencia en vez de reventar.");
			});

			UnExpedienteComoElDeUnaPersona();

			Assert.Multiple(() =>
			{
				Assert.That(MunicipalidadApp.HayJournalEn(carpeta), Is.True,
					"Y despues de que la app se abrio una vez, si hay journal. Sin este renglon el "
					+ "ayudante podria contestar que no siempre y las tres afirmaciones de arriba "
					+ "estarian verdes por la razon equivocada.");
				Assert.That(new LectorDeJournal(carpeta).HayJournal, Is.True,
					"y el lector tambien");
			});
		}

		// LAS LECTURAS DE LA PUERTA, VARIAS VUELTAS, COMO PREGUNTA UN ASISTENTE DE VERDAD, y ni una
		// entrada nueva. Cada vuelta abre y cierra el journal por su cuenta, que es como lo hace el
		// LectorDeJournal en cada llamada.
		//
		// LAS CINCO GUARDADAS VAN PRECEDIDAS DE SU PAR, que es la disciplina que este modulo eligio en
		// vez de atrapar: una caratula pregunta primero y por eso nunca recibe la negativa del dominio.
		[Test]
		public void EveryReadThroughTheReadOnlyHostLeavesTheJournalWithoutANewEntry()
		{
			UnExpedienteComoElDeUnaPersona();

			long entradasAntes = LasEntradasDe(carpeta);
			long largoAntes = ElLargoDelJournalDe(carpeta);

			LectorDeJournal lector = new LectorDeJournal(carpeta);

			for (int vuelta = 0; vuelta < 4; vuelta++) TodoLoQueUnaCaratulaPuedePreguntar(lector);

			Assert.Multiple(() =>
			{
				Assert.That(LasEntradasDe(carpeta), Is.EqualTo(entradasAntes),
					"Una pregunta del asistente dejo una entrada en el journal de la persona. Eso es un "
					+ "segundo escritor sobre el mismo actor, que es exactamente lo que el OnHydrated "
					+ "vacio existe para no hacer.");

				Assert.That(ElLargoDelJournalDe(carpeta), Is.EqualTo(largoAntes),
					"El archivo del journal crecio. Ninguna lectura puede hacerlo crecer.");
			});
		}

		// EL SEGUNDO PROGRAMA ES LO QUE TIENE QUE QUEDAR IGUAL: no los bytes de la carpeta -- que el
		// motor toca por su cuenta -- sino las sentencias que el actor pronuncio y los valores que
		// llevaron.
		[Test]
		public void TheSecondProgramIsIdenticalAfterTheReadOnlyHostHasAsked()
		{
			UnExpedienteComoElDeUnaPersona();

			string antes = ElSegundoProgramaDe(carpeta);

			LectorDeJournal lector = new LectorDeJournal(carpeta);

			for (int vuelta = 0; vuelta < 12; vuelta++) TodoLoQueUnaCaratulaPuedePreguntar(lector);

			Assert.That(ElSegundoProgramaDe(carpeta), Is.EqualTo(antes),
				"El programa que quedo journalizado cambio despues de doce vueltas de preguntas. Leer "
				+ "no puede agregar ni una sentencia.");
		}

		// EL GUARDIAN DE LAS DOS DE ARRIBA, Y TIENE QUE SER DISTINTO. Las dos afirman que algo NO
		// cambio; una medicion que no pudiera detectar un cambio las dejaria verdes para siempre.
		// Abrir con el anfitrion DE LA APP si escribe: OnHydrated deja la entrada de la semilla en cada
		// hidratacion, aunque los dos upgrades ya esten aplicados y no hagan nada.
		//
		// ES TAMBIEN LA FALSIFICACION DE C-1: darle cuerpo al OnHydrated del anfitrion de solo lectura
		// pone rojas a las dos de arriba, y este metodo es la prueba de que ese rojo significa algo.
		[Test]
		public void TheAppsOwnHostAddsOneEntryEveryTimeItOpensAndThatIsWhyTheMcpHostIsADifferentOne()
		{
			UnExpedienteComoElDeUnaPersona();

			long entradasAntes = LasEntradasDe(carpeta);
			string programaAntes = ElSegundoProgramaDe(carpeta);

			using (MunicipalidadApp app = new MunicipalidadApp(MunicipalidadApp.ElActor))
			{
				app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
				app.Start();
			}

			Assert.Multiple(() =>
			{
				Assert.That(LasEntradasDe(carpeta), Is.EqualTo(entradasAntes + 1),
					"Reabrir con MunicipalidadApp tiene que costar exactamente una entrada. Si no "
					+ "cuesta ninguna, la medicion no distingue leer de escribir y las dos pruebas de "
					+ "arriba no prueban nada.");

				Assert.That(ElSegundoProgramaDe(carpeta), Is.Not.EqualTo(programaAntes),
					"Y el segundo programa tiene que CAMBIAR, o el extractor no ve nada.");
			});
		}

		// LO QUE CUESTA EL OnHydrated VACIO, DICHO Y MEDIDO EN VEZ DE INFERIDO. El anfitrion del
		// asistente no corre ningun upgrade, asi que a un journal al que le falte una global de raiz le
		// sigue faltando mientras la app no lo abra. DEC-15 midio que un script que nombra las DOS
		// globales no contesta en un journal que solo tiene `cuenta`, y ese es exactamente el estado de
		// todo journal escrito antes de M-2.
		//
		// LA PARTICION ES POR CUANTAS GLOBALES NOMBRA EL SCRIPT, no por que lectura es. LoPagadoEn
		// nombra solo `cuenta` y contesta; EstadoDeLaCuenta y UltimoEventoAplicado nombran `bitacora` y
		// se niegan. El riesgo vivo es cero -- la app aplica los dos upgrades en cada apertura y solo un
		// doble de prueba alcanza este estado -- y va escrito porque el anfitrion de solo lectura es lo
		// primero de este modulo que puede abrir un journal SIN migrarlo.
		//
		// Y LA NEGATIVA ES DEL MOTOR, NO DEL DOMINIO: llega como LanguageException, que es publica y
		// una caratula si puede nombrar. No es la de C5.
		[Test]
		public void TheReadOnlyHostRunsNoUpgradeSoACrossGlobalReadRefusesOnAPreBitacoraJournal()
		{
			UnJournalDeAntesDeLaBitacora();

			LectorDeJournal lector = new LectorDeJournal(carpeta);

			Assert.Multiple(() =>
			{
				Assert.That(lector.HayJournal, Is.True,
					"El journal existe, asi que la ausencia no es lo que esta pasando aca.");

				Assert.That(lector.Leer(api => api.LoPagadoEn(2024), () => -1m), Is.EqualTo(0m),
					"Una lectura que nombra una sola global contesta: `cuenta` esta.");

				Assert.That(() => lector.Leer(api => api.EstadoDeLaCuenta(), () => ""),
					Throws.InstanceOf<LanguageException>(),
					"Y una que cruza las dos se niega, porque este anfitrion no corrio "
					+ "upgrade('bitacora') y no lo va a correr.");

				Assert.That(() => lector.Leer(api => api.UltimoEventoAplicado(), () => 0L),
					Throws.InstanceOf<LanguageException>(),
					"Igual la que nombra solo `bitacora`: lo que decide es la global que falta, no "
					+ "cuantas nombra el script.");
			});

			using (MunicipalidadApp app = new MunicipalidadApp(MunicipalidadApp.ElActor))
			{
				app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
				app.Start();
			}

			Assert.That(lector.Leer(api => api.EstadoDeLaCuenta(), () => ""),
				Does.Contain("\"ultimoEventoLeido\":0"),
				"Y en cuanto la app abre el journal una vez, la lectura cruzada contesta: lo que "
				+ "faltaba era el upgrade, no el dato.");
		}

		// LA CREDENCIAL DE LA PERSONA ESTA EN LA CARPETA QUE ESTE PROCESO LEE, en claro. Lo unico que
		// la separa del asistente es que nadie la pida, asi que la puerta que el lector entrega ya la
		// tiene a mano y este renglon es el control de esa afirmacion para cuando C-2 escriba tools.
		[Test]
		public void TheDoorTheReaderHandsOutCanReachTheCredentialAndThatIsWhyNoToolMayAskForIt()
		{
			UnExpedienteComoElDeUnaPersona();

			LectorDeJournal lector = new LectorDeJournal(carpeta);

			Assert.That(lector.Leer(api => api.TokenGuardado(), () => ""), Is.EqualTo(ElToken),
				"La credencial esta ahi y el lector la alcanza. Que no salga en una respuesta es una "
				+ "propiedad de las tools, no del lector, y se afirma donde vivan ellas.");
		}

		// ------------------------------------------------------ el aparejo

		// TODO LO QUE UNA CARATULA PUEDE PREGUNTARLE A ESTA PUERTA, cada pregunta por su propio
		// LectorDeJournal.Leer, que es una apertura y un cierre por pregunta. Las anclas afirman que la
		// lectura llego de verdad al dominio: sin ellas la promesa se estaria midiendo sobre respuestas
		// vacias.
		private static void TodoLoQueUnaCaratulaPuedePreguntar(LectorDeJournal lector)
		{
			Assert.That(lector.Leer(api => api.EstadoDeLaCuenta(), () => ""),
				Does.Contain("\"fincas\":1"), "EstadoDeLaCuenta llego con el expediente adentro");
			Assert.That(lector.Leer(api => api.LoPagadoEn(2024), () => -1m), Is.EqualTo(23310m),
				"LoPagadoEn llego con el expediente adentro");

			lector.Leer(api => api.UltimoEventoAplicado(), () => 0L);
			lector.Leer(api => api.LosRubrosQueHay(), () => new string[0]);
			lector.Leer(api => api.RubrosConHistoria(), () => new string[0]);
			lector.Leer(api => api.NumerosDeLasFincas(), () => new string[0]);
			lector.Leer(api => api.HistorialDe(ElRubro), () => null);
			lector.Leer(api => api.VencimientosAl(new DateTime(2025, 9, 25), 7), () => null);
			lector.Leer(api => api.AtrasosAl(new DateTime(2025, 10, 5), 0.062m), () => null);
			lector.Leer(api => api.PeriodosPagadosSinComprobante(LaConsulta), () => null);
			lector.Leer(api => api.AhorroPorProntoPago(LaConsulta, 0.062m), () => 0m);
			lector.Leer(api => api.HayAlgunaConsulta(), () => false);
			lector.Leer(api => api.ConsultaMasReciente(), () => DateTime.MinValue);
			lector.Leer(api => api.TieneValorDeclaradoLaFinca(LaFinca, LaConsulta), () => false);
			lector.Leer(api => api.ValorDeFincaAl(LaFinca, LaConsulta), () => 0m);

			// LAS GUARDADAS, CADA UNA DESPUES DE SU PAR. Preguntar primero es lo que hace que esta
			// caratula nunca reciba la negativa del dominio, que es un tipo internal que no puede
			// nombrar.
			if (lector.Leer(api => api.SePuedeVerLaVariacionDe(ElRubro), () => false))
				lector.Leer(api => api.VariacionDe(ElRubro, 0.25m, 0.02m), () => null);

			if (lector.Leer(api => api.SePuedeProyectar(), () => false))
				lector.Leer(api => api.ReservaPropuestaPara(2025), () => null);

			if (lector.Leer(api => api.SePuedeVerElFaltanteDeReserva(), () => false))
				lector.Leer(api => api.FaltanteDeReservaAl(new DateTime(2025, 9, 15), 0.10m), () => null);

			LasSeisPreguntasDelAsistente(lector);
		}

		// Y LAS SEIS TOOLS, EN LA MISMA VUELTA. DEC-42 dejo esta obligacion escrita para C-2: la promesa
		// de que leer no escribe solo atrapa una llamada que ESTA fixture hace, asi que una tool que no
		// entre aca queda fuera de la promesa. Es C57 -- la lista de permitidos que crece en silencio --
		// y el barrido de nombres de TheAssistantIsOfferedSixQuestionsAndNoneOfThemIsACommand es lo que
		// obliga a mirar esta lista cuando entre la septima.
		private static void LasSeisPreguntasDelAsistente(LectorDeJournal lector)
		{
			Assert.That(
				new PreguntasDeLaVariacion(lector).PorQuePagoMas().Rubros, Has.Count.EqualTo(5),
				"por_que_pago_mas contesta por los cinco rubros del catalogo");

			Assert.That(
				new PreguntasDeLosVencimientos(lector).QueSeMeVence().HayDatos, Is.True,
				"que_se_me_vence llego al expediente");

			Assert.That(
				new PreguntasDeLaReserva(lector).CuantoApartar().Adoptado, Is.EqualTo(30000m),
				"cuanto_apartar trae lo que la PERSONA adopto, que es la mitad que la app no calcula");

			Assert.That(
				new PreguntasDeLosComprobantes(lector).QueQuedoSinComprobante().DiaDeLaConsulta,
				Is.EqualTo("2025-09-03"),
				"que_quedo_sin_comprobante coteja contra el dia de la consulta y no contra hoy");

			Assert.That(
				new PreguntasDeLoPagado(lector).CuantoHePagado(2024).TotalDelAnio, Is.EqualTo(23310m),
				"cuanto_he_pagado llego al expediente");

			Assert.That(
				new PreguntasDelEstado(lector).QueTanAlDiaEstaEsto().Fincas, Has.Count.EqualTo(1),
				"que_tan_al_dia_esta_esto ve la finca del expediente");
		}

		// EL EXPEDIENTE DE UNA PERSONA, escrito por el anfitrion DE LA APP y cerrado antes de leer, que
		// es como estan las dos cosas de verdad: la app en un proceso y el servidor MCP en otro.
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

				api.AdoptarReserva(30000m, LaAdopcion);

				api.RegistrarDispositivo(LaClaveDelDispositivo);
				api.GuardarCredencial(ElToken, ElRefresco);
			}
		}

		// UN JOURNAL COMO LOS QUE YA EXISTEN: solo tiene 'semilla'. Es el unico estado en que el costo
		// del OnHydrated vacio se puede ver, y solo un doble de prueba lo alcanza.
		private void UnJournalDeAntesDeLaBitacora()
		{
			using (UnaAppQueSoloSabeDeLaSemilla vieja = new UnaAppQueSoloSabeDeLaSemilla())
			{
				vieja.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
				vieja.Start();
			}
		}

		// LA CUENTA DE ENTRADAS SE LEE CON EL ANFITRION DE SOLO LECTURA, no con el de la app:
		// preguntarle al escritor mediria al escritor. Esto abre como abre la caratula.
		private static long LasEntradasDe(string carpeta)
		{
			AppDeMunicipalidadSoloLectura lector = new AppDeMunicipalidadSoloLectura();
			try
			{
				lector.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
				lector.Start();

				return lector.Actor.CurrentEntryId;
			}
			finally
			{
				try { lector.Actor.GracefulExit(); } catch { }
			}
		}

		private static long ElLargoDelJournalDe(string carpeta)
		{
			long total = 0;

			foreach (string archivo in Directory.GetFiles(
				Path.Combine(carpeta, MunicipalidadApp.ElActor, "journal"), "*.bin"))
				total += new FileInfo(archivo).Length;

			return total;
		}

		// Las sentencias que el actor pronuncio y los valores que llevaron, extraidas del binario: las
		// corridas imprimibles de seis caracteres o mas. Se saltea la cabecera del archivo -- los
		// primeros 32 bytes -- porque es donde el motor deja su punto de control de replay a la septima
		// apertura, y eso no es una sentencia.
		private static string ElSegundoProgramaDe(string raiz)
		{
			StringBuilder programa = new StringBuilder();

			List<string> archivos = new List<string>(
				Directory.GetFiles(raiz, "*", SearchOption.AllDirectories));
			archivos.Sort(StringComparer.Ordinal);

			foreach (string archivo in archivos)
			{
				using (FileStream abierto = new FileStream(
					archivo, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					byte[] contenido = new byte[abierto.Length];
					abierto.ReadExactly(contenido, 0, contenido.Length);

					StringBuilder corrida = new StringBuilder();

					for (int en = Math.Min(32, contenido.Length); en < contenido.Length; en++)
					{
						byte cada = contenido[en];

						if (cada >= 0x20 && cada <= 0x7E) { corrida.Append((char)cada); continue; }

						if (corrida.Length >= 6) programa.AppendLine(corrida.ToString());

						corrida.Clear();
					}

					if (corrida.Length >= 6) programa.AppendLine(corrida.ToString());
				}
			}

			return programa.ToString();
		}

		// La app tal como era antes de M-2: una sola global de raiz. Existe SOLO para que el costo del
		// OnHydrated vacio tenga contra que medirse -- un journal en el que la bitacora falta de verdad.
		private sealed class UnaAppQueSoloSabeDeLaSemilla : Choreography.Theater.PerformanceV2
		{
			internal UnaAppQueSoloSabeDeLaSemilla()
				: base(MunicipalidadApp.ElActor, typeof(Cuenta).Assembly)
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
