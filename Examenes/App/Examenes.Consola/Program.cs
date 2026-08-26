using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Examenes.App;
using Examenes.App.Eventos;
using Examenes.App.Vistas;
using Puppeteer;
using Puppets.Portador;

namespace Examenes.Consola
{
	internal static class Program
	{
		private const string NombreDelActor = "examenes";

		private static async Task<int> Main(string[] args)
		{
			if (args.Length == 0) { Ayuda(); return 1; }

			try
			{
				switch (args[0])
				{
					// Comprobaciones del dominio puro: sin actor, sin journal, sin disco.
					case "comprobar": return Comprobaciones.Correr();
					case "medir":     return Medir(Carpeta(args, 1));
					case "estado":    return Estado(Carpeta(args, 1));
					case "tendencia": return TendenciaDe(args);
					case "velocidad": return VelocidadDe(args);
					// El token ya no se pega a mano: la app lo pide sola contra su device key.
					case "refrescar": return await Refrescar(args);
					default:          Ayuda(); return 1;
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error: {ex.GetType().Name}: {ex.Message}");
				for (Exception interna = ex.InnerException; interna != null; interna = interna.InnerException)
					Console.Error.WriteLine($"  causa: {interna.GetType().FullName}: {interna.Message}");
				return 2;
			}
		}

		// Abre el puppet sobre una carpeta real. Es el mismo par ConfigureStorage + Start
		// que va a hacer la caratula del browser, solo que ahi el path apunta a MEMFS.
		private static ExamenesApp Abrir(string carpeta)
		{
			Directory.CreateDirectory(carpeta);

			var app = new ExamenesApp(NombreDelActor);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			app.Start();
			return app;
		}

		// ---------------------------------------------------------------- fase 1
		private static int Medir(string carpeta)
		{
			Console.WriteLine($"carpeta : {carpeta}");
			ExamenesApp app = Abrir(carpeta);
			var api = new ExamenesAPI(app);
			IDiarioDeLaApp diario = api;

			Console.WriteLine($"entrada inicial del journal : {app.Actor.CurrentEntryId}");
			Console.WriteLine($"ultimo evento procesado     : {api.UltimoEventoProcesado()}");
			Console.WriteLine($"edad del paciente           : {api.EdadDelPaciente()}");

			int aplicados = 0, descartados = 0, rechazados = 0;
			foreach (SenalDeEvento senal in Muestras.LaHistoria())
			{
				IMensajeDeEvento mensaje = RutaDeExamenes.Rutear(senal);
				if (mensaje == null) { descartados++; continue; }

				string rechazo = diario.Aplicar(mensaje);
				if (rechazo.Length > 0)
				{
					rechazados++;
					Console.WriteLine($"  rechazado {senal.Numero}: {rechazo}");
					continue;
				}
				aplicados++;
			}
			Console.WriteLine($"aplicados={aplicados} descartados={descartados} rechazados={rechazados}");

			// Dos formas distintas de descartar, y las dos tienen que devolver null.
			bool descartaTipo = RutaDeExamenes.Rutear(Muestras.TipoDesconocido()) == null;
			bool descartaExamen = RutaDeExamenes.Rutear(Muestras.ExamenNoModelado()) == null;
			Console.WriteLine($"descarta tipo desconocido   : {descartaTipo}");
			Console.WriteLine($"descarta examen no modelado : {descartaExamen}");

			ImprimirEstado(app, api, conVeredicto: true);

			// ---- criterio: un evento ya procesado se rechaza y NO deja entrada.
			long antes = app.Actor.CurrentEntryId;
			string mensajeDeRechazo = diario.Aplicar(RutaDeExamenes.Rutear(Muestras.LaHistoria()[0]));
			long despues = app.Actor.CurrentEntryId;

			Console.WriteLine();
			Console.WriteLine("-- reintento de un evento ya procesado --");
			Console.WriteLine($"mensaje del Check           : [{mensajeDeRechazo}]");
			Console.WriteLine($"entrada antes / despues     : {antes} / {despues}");
			Console.WriteLine($"NO dejo entrada             : {antes == despues}");
			Console.WriteLine($"cantidad sin cambio         : {api.CantidadDeExamenes() == Muestras.ExamenesEsperados}");

			// ---- criterio: el mismo salto, en un dia, se lee como error de medicion.
			Console.WriteLine();
			Console.WriteLine("-- el mismo salto en UN DIA es otra cosa --");
			diario.Aplicar(RutaDeExamenes.Rutear(Muestras.SaltoInverosimil()));
			TendenciaDeAnalito conSalto = api.Tendencia(Muestras.AnalitoDelColesterol);
			Console.WriteLine($"colesterol tras el salto    : {conSalto.Tendencia}");
			Console.WriteLine($"sugiere repetir el examen   : {conSalto.SugiereRepetirElExamen}");

			app.Actor.GracefulExit();
			return 0;
		}

		// Se corre en un proceso NUEVO: si las tendencias siguen, el replay rehidrato.
		private static int Estado(string carpeta)
		{
			ExamenesApp app = Abrir(carpeta);
			// Sin veredicto: esta carpeta puede venir de un medir, que a proposito le
			// agrega al final una medicion inverosimil para demostrar esa lectura.
			ImprimirEstado(app, new ExamenesAPI(app), conVeredicto: false);
			app.Actor.GracefulExit();
			return 0;
		}

		private static void ImprimirEstado(ExamenesApp app, ExamenesAPI api, bool conVeredicto)
		{
			Console.WriteLine();
			Console.WriteLine($"entrada del journal         : {app.Actor.CurrentEntryId}");
			Console.WriteLine($"ultimo evento procesado     : {api.UltimoEventoProcesado()}");
			Console.WriteLine($"cantidad de examenes        : {api.CantidadDeExamenes()}");
			Console.WriteLine($"esperados                   : {Muestras.ExamenesEsperados}");
			// Ya no es solo "fuera de rango": un binario no tiene rango del que salirse, y
			// una sangre oculta presente cuenta igual. Ver Expediente.AnalitosFueraDeRango.
			Console.WriteLine($"analitos con algo que mirar : {api.AnalitosFueraDeRango()}");
			Console.WriteLine($"token guardado              : [{api.TokenGuardado()}]");

			Console.WriteLine();
			Console.WriteLine("-- lista (print, formatter JSON) --");
			Console.WriteLine(api.ListaDeExamenes());

			Console.WriteLine();
			Console.WriteLine("-- el cuadro historico, ya rearmado desde las celdas planas --");
			ImprimirCuadro(api);

			Console.WriteLine();
			Console.WriteLine("-- lo que FALTA medir, que es lo que ninguna pantalla muestra --");
			ImprimirControles(api);

			Console.WriteLine();
			Console.WriteLine("-- lo que la pantalla necesita para DIBUJAR --");
			ImprimirParaDibujar(api);

			Console.WriteLine();
			Console.WriteLine("-- LA PREGUNTA QUE NINGUNA PANTALLA CONTESTA --");
			foreach (string analito in api.Analitos()) ImprimirTendencia(api, analito);

			if (!conVeredicto) return;

			bool antigenoOk = api.Tendencia(Muestras.AnalitoDelAntigeno).Tendencia
				== Muestras.TendenciaEsperadaDelAntigeno;
			bool colesterolOk = api.Tendencia(Muestras.AnalitoDelColesterol).Tendencia
				== Muestras.TendenciaEsperadaDelColesterol;
			Console.WriteLine($"TENDENCIAS CORRECTAS        : {antigenoOk && colesterolOk}");
		}

		// El cuadro rearmado, que es exactamente lo que hace la pantalla: ubicar cada
		// celda por sus dos indices y dejar vacio lo que el dominio no mando. Aca no se
		// decide nada; si la grilla sale mal, esta mal el dominio.
		private static void ImprimirCuadro(ExamenesAPI api)
		{
			IReadOnlyList<CeldaEnCuadro> celdas = api.Cuadro();
			if (celdas.Count == 0) { Console.WriteLine("  (sin examenes)"); return; }

			int filas = 0, columnas = 0;
			foreach (CeldaEnCuadro c in celdas)
			{
				if (c.Fila + 1 > filas) filas = c.Fila + 1;
				if (c.Columna + 1 > columnas) columnas = c.Columna + 1;
			}

			var grilla = new string[filas, columnas];
			var nombres = new string[filas];
			var fechas = new string[columnas];
			foreach (CeldaEnCuadro c in celdas)
			{
				grilla[c.Fila, c.Columna] = c.Expresado;
				nombres[c.Fila] = c.ParaMostrar;
				fechas[c.Columna] = c.Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
			}

			Console.Write($"  {"",-22}");
			for (int j = 0; j < columnas; j++) Console.Write($"{fechas[j],14}");
			Console.WriteLine();

			for (int i = 0; i < filas; i++)
			{
				Console.Write($"  {nombres[i],-22}");
				for (int j = 0; j < columnas; j++)
					Console.Write($"{(grilla[i, j] ?? "-"),14}");
				Console.WriteLine();
			}
		}

		// Lo que falta medir. Se imprime aca porque es la unica consulta del dominio que
		// mira el hueco y no el dato, y porque verifica que la proyeccion del DSL trae algo:
		// una lista vacia se ve igual que un proyector con la llave mal puesta.
		//
		// La fecha la pone ESTA caratula, no el dominio. Se usa una fija y no DateTime.Now
		// para que la salida de `medir` siga siendo reproducible, igual que las fechas del
		// guion.
		private static void ImprimirControles(ExamenesAPI api)
		{
			IReadOnlyList<ControlEnEspera> controles = api.Controles(FechaDeCorte);
			if (controles.Count == 0) { Console.WriteLine("  (sin analitos con historia)"); return; }

			foreach (ControlEnEspera c in controles)
				Console.WriteLine($"  {c.ParaMostrar,-22} {(c.EstaVencido ? "VENCIDO" : "al dia"),-8} "
					+ $"ultima {c.UltimaMedicion:dd/MM/yyyy}, hace {c.DiasDesdeLaUltima} dias, "
					+ $"cada {c.MesesRecomendados} meses");
		}

		// El "hoy" del arnes. Literal a proposito: con DateTime.Now la salida cambiaria
		// todos los dias y dejaria de servir como criterio de aceptacion.
		private static readonly DateTime FechaDeCorte = new DateTime(2026, 8, 21);

		// Los puntos con NUMERO y las lineas que los acompanan. Se imprime aca porque
		// verifica que la proyeccion del DSL trae algo: una lista vacia se ve igual que un
		// proyector con la llave mal puesta, y eso ya mordio una vez.
		private static void ImprimirParaDibujar(ExamenesAPI api)
		{
			foreach (string analito in api.Analitos())
			{
				InterpretacionDeAnalito lectura = api.Interpretacion(analito);
				IReadOnlyList<PuntoDeSerie> puntos = api.Puntos(analito);
				IReadOnlyList<LineaDeReferencia> lineas = api.Referencias(analito);

				var valores = new List<string>();
				foreach (PuntoDeSerie p in puntos) valores.Add(Formato(p.Valor));

				// LOS BINARIOS NO TIENEN NUMEROS QUE DIBUJAR: tienen presencias. Se piden
				// por otra puerta -Pruebas en vez de Puntos- porque no devuelven lo mismo,
				// y se pintan como lo que son: una tira de si y de no.
				foreach (PruebaEnSerie prueba in api.Pruebas(analito))
					valores.Add(prueba.EsPositivo ? "●" : "○");

				var refs = new List<string>();
				foreach (LineaDeReferencia l in lineas) refs.Add($"{l.Nombre}={Formato(l.Valor)}");

				Console.WriteLine($"  {lectura.ParaMostrar,-22} {lectura.Forma,-26} "
					+ $"[{string.Join(" ", valores)}]  {string.Join(" ", refs)}");
			}
		}

		private static void ImprimirTendencia(ExamenesAPI api, string analito)
		{
			// UN BINARIO NO TIENE TENDENCIA, y preguntarsela devolveria "no aplica" con
			// ceros. Se lee por su propia puerta: cuantas pruebas, cuantas dieron
			// positivas y cuantas SEGUIDAS, que es lo que cambia la conducta.
			if (api.Interpretacion(analito).Forma == FormaDePresencia)
			{
				ImprimirPresencia(api, analito);
				return;
			}

			TendenciaDeAnalito t = api.Tendencia(analito);

			// La frase la arma la SUPERFICIE con los hechos del dominio. El dominio no
			// devuelve prosa, y por eso no puede devolver un consejo de plantilla.
			string relato = t.HaySerie
				? $"paso de {Formato(t.ValorInicial)} a {Formato(t.ValorFinal)} {t.Unidad} en {t.Dias} {(t.Dias == 1 ? "dia" : "dias")}"
				: $"una sola medicion ({Formato(t.ValorFinal)} {t.Unidad}): todavia no hay serie";

			// Se imprime ParaMostrar y no el analito con el que se pregunto: la llave es
			// para buscar, el nombre para mostrar es para leer.
			Console.WriteLine($"  {t.ParaMostrar,-22} {t.Tendencia,-20} {relato}");

			// La LECTURA la da el analito, no esta caratula. Antes esta funcion decidia
			// que imprimir mirando un bool generico; ahora solo muestra lo que el analito
			// concluyo, incluida la accion.
			InterpretacionDeAnalito lectura = api.Interpretacion(analito);
			Console.WriteLine($"  {"",-22} {lectura.Veredicto}: {lectura.ParaSonia}");
			if (lectura.PideAlgo)
				Console.WriteLine($"  {"",-22} ^ {lectura.Severidad} - {lectura.Accion}");

			// La velocidad solo se dice donde significa algo. Que el colesterol no la
			// muestre no es un hueco de la caratula: es el analito contestando que no se
			// interpreta asi.
			VelocidadDeAnalito v = api.Velocidad(analito);
			if (!v.EsAplicable) return;

			Console.WriteLine($"  {"",-22} ritmo: {v.Expresada} (tramo de {v.Dias} dias, umbral {Formato(v.Umbral)})");
			if (v.ExigeAtencion)
				Console.WriteLine($"  {"",-22} ^ la VELOCIDAD sola ya exige atencion");
		}

		// El nombre de la forma tal como lo dice el dominio. Comparar contra este literal
		// es traducir una forma del dominio a una decision de impresion, que es lo unico
		// que esta caratula tiene derecho a decidir.
		private const string FormaDePresencia = "presencia en el tiempo";

		// La lectura de un binario. La tira -○ ○ ●- es el hecho que ninguna medicion sola
		// puede mostrar, y es la razon de que estas series existan.
		private static void ImprimirPresencia(ExamenesAPI api, string analito)
		{
			PresenciaDeAnalito p = api.Presencia(analito);

			var tira = new List<string>();
			foreach (PruebaEnSerie prueba in api.Pruebas(analito))
				tira.Add(prueba.EsPositivo ? "●" : "○");

			string relato = p.HaySerie
				? $"{string.Join(" ", tira)}  sobre {p.Mediciones} pruebas en {p.Dias} dias"
				: $"una sola prueba ({p.UltimoResultado}): todavia no hay serie";

			Console.WriteLine($"  {p.ParaMostrar,-22} {p.Presencia,-20} {relato}");

			InterpretacionDeAnalito lectura = api.Interpretacion(analito);
			Console.WriteLine($"  {"",-22} {lectura.Veredicto}: {lectura.ParaSonia}");
			if (lectura.PideAlgo)
				Console.WriteLine($"  {"",-22} ^ {lectura.Severidad} - {lectura.Accion}");
		}

		private static string Formato(decimal valor) =>
			valor.ToString("0.##", CultureInfo.InvariantCulture);

		// Consulta puntual, para la demo en vivo.
		private static int TendenciaDe(string[] args)
		{
			string carpeta = Carpeta(args, 1);
			string analito = Opcional(args, 2, Muestras.AnalitoDelAntigeno);

			ExamenesApp app = Abrir(carpeta);
			var api = new ExamenesAPI(app);

			Console.WriteLine($"analito: {analito}");
			ImprimirTendencia(api, analito);
			Console.WriteLine();
			Console.WriteLine("-- crudo (print, formatter JSON) --");
			Console.WriteLine(api.TendenciaDe(analito));

			app.Actor.GracefulExit();
			return 0;
		}

		// A que ritmo viene cambiando un analito. Es la consulta del antigeno prostatico:
		// la que no se puede contestar con el ultimo valor ni con una pantalla.
		private static int VelocidadDe(string[] args)
		{
			string carpeta = Carpeta(args, 1);
			string analito = Opcional(args, 2, Muestras.AnalitoDelAntigeno);

			ExamenesApp app = Abrir(carpeta);
			var api = new ExamenesAPI(app);

			VelocidadDeAnalito v = api.Velocidad(analito);
			Console.WriteLine($"analito   : {v.Analito}");
			Console.WriteLine($"ritmo     : {v.Expresada}");
			Console.WriteLine($"tramo     : {v.Dias} dias");
			Console.WriteLine($"umbral    : {Formato(v.Umbral)} {v.Unidad} por ano");
			Console.WriteLine($"aplicable : {v.EsAplicable}");
			Console.WriteLine($"ALARMA    : {v.ExigeAtencion}");
			Console.WriteLine();
			Console.WriteLine("-- crudo (print, formatter JSON) --");
			Console.WriteLine(api.VelocidadDe(analito));

			app.Actor.GracefulExit();
			return 0;
		}

		// ---------------------------------------------------------------- fase 2
		private static async Task<int> Refrescar(string[] args)
		{
			string carpeta = Carpeta(args, 1);
			string baseUrl = Opcional(args, 2, "http://localhost:8091/");
			string recurso = Opcional(args, 3, "expediente");
			string id      = Opcional(args, 4, "1");

			using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
			ExamenesApp app = Abrir(carpeta);
			var api = new ExamenesAPI(app);

			var proveedorDeToken = new ProveedorDeToken(http);

			// La app consigue su credencial SOLA, contra su device key. Ni la consola ni
			// la persona pegan un token: el laboratorio lo emite para ESTA instalacion.
			if (api.DeviceKeyGuardado().Length == 0)
				api.RegistrarDevice("device-consola-de-medicion");

			if (api.TokenGuardado().Length == 0)
			{
				Credencial credencial = await proveedorDeToken.Pedir(api.DeviceKeyGuardado(), recurso, id);
				Console.WriteLine($"credencial: scope={credencial.Scope} expiresIn={credencial.ExpiresIn}s");
				api.GuardarCredencial(credencial.AccessToken, credencial.RefreshToken);
			}

			// Que hacer cuando el laboratorio rechaza el access token: rotar con el
			// refresh token, que es para lo que existe. El access es corto a proposito.
			async Task<string> Renovar(System.Threading.CancellationToken ct)
			{
				string refresh = api.RefreshTokenGuardado();

				// Rotar devuelve null si el refresh ya no vale; ahi se pide una credencial
				// nueva con el device key, que es lo que la instalacion siempre puede hacer.
				Credencial renovada = refresh.Length > 0
					? await proveedorDeToken.Rotar(refresh, ct)
					: null;
				string comoSeObtuvo = renovada != null ? "rotada con el refresh token" : "nueva contra el device key";
				renovada ??= await proveedorDeToken.Pedir(api.DeviceKeyGuardado(), recurso, id, ct);

				Console.WriteLine($"  credencial renovada: {comoSeObtuvo} (scope {renovada.Scope})");
				api.GuardarCredencial(renovada.AccessToken, renovada.RefreshToken);
				return renovada.AccessToken;
			}

			var bucle = new BucleDeRefresh(api, new FuenteHttpDeEventos(http, recurso, id),
				RutaDeExamenes.Rutear, null, Renovar);

			ResultadoDeRefresh primero = await bucle.Ejecutar();
			Console.WriteLine($"refresh 1 : {primero}");

			// Segundo refresh sin novedad: debe traer 0 y no aplicar nada.
			long antes = app.Actor.CurrentEntryId;
			ResultadoDeRefresh segundo = await bucle.Ejecutar();
			long despues = app.Actor.CurrentEntryId;
			Console.WriteLine($"refresh 2 : {segundo}");
			Console.WriteLine($"sin novedad no escribio     : {antes == despues}");

			ImprimirEstado(app, api, conVeredicto: true);

			app.Actor.GracefulExit();
			return 0;
		}

		// ---------------------------------------------------------------- argumentos
		private static string Carpeta(string[] args, int i) =>
			Path.GetFullPath(Opcional(args, i, Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Examenes-journal")));

		private static string Opcional(string[] args, int i, string porDefecto) =>
			args.Length > i && args[i].Length > 0 ? args[i] : porDefecto;

		private static void Ayuda()
		{
			Console.WriteLine("Examenes - caratula de consola, para medir el dominio y el refresh");
			Console.WriteLine();
			Console.WriteLine("  comprobar                                      comprueba las reglas del dominio puro (sin journal)");
			Console.WriteLine("  medir     [carpeta]                            aplica la historia y mide los criterios de fase 1");
			Console.WriteLine("  estado    [carpeta]                            abre el journal y reporta (correr en proceso nuevo)");
			Console.WriteLine("  tendencia [carpeta] [analito]                  evalua la serie de un analito");
			Console.WriteLine("  velocidad [carpeta] [analito]                  a que ritmo viene cambiando (ng/mL por ano)");
			Console.WriteLine("  refrescar [carpeta] [baseUrl] [recurso] [id]   trae eventos del laboratorio y los aplica de a uno");
			Console.WriteLine();
			Console.WriteLine("carpeta por defecto: ~/Examenes-journal");
			Console.WriteLine("baseUrl por defecto: http://localhost:8091/");
			// La lista sale del propio conjunto cerrado, no de un literal: agregar un
			// analito y olvidarse de la ayuda deja de ser posible.
			var nombres = new System.Text.StringBuilder();
			foreach (Examenes.Dominio.Analito analito in Examenes.Dominio.Analito.Todos)
			{
				if (nombres.Length > 0) nombres.Append(", ");
				nombres.Append('\'').Append(analito.Nombre).Append('\'');
			}
			Console.WriteLine($"analitos           : {nombres}");
			Console.WriteLine("                     tambien responden a como los nombra cada laboratorio:");
			Console.WriteLine("                     'PSA', 'HGB', 'LDL', y con o sin acentos y mayusculas.");
		}
	}
}
