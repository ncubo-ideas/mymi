using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Cargas.App;
using Cargas.App.Eventos;
using Cargas.App.Vistas;
using Puppeteer;
using Puppets.Portador;

namespace Cargas.Consola
{
	internal static class Program
	{
		private const string NombreDelActor = "cargas";

		private static async Task<int> Main(string[] args)
		{
			if (args.Length == 0) { Ayuda(); return 1; }

			try
			{
				switch (args[0])
				{
					// Comprobaciones del dominio puro: sin actor, sin journal, sin disco.
					case "comprobar":       return Comprobaciones.Correr();
					case "qr":              return Qr(args);
					case "medir":           return Medir(Carpeta(args, 1));
					case "estado":          return Estado(Carpeta(args, 1));
					case "rendimiento":     return RendimientoDe(args);
					case "autonomia":       return AutonomiaDe(args);
					// Los aportes de Sonia sobre un journal ya refrescado: el guion del
					// demo sin teclear 17 formularios.
					case "sembrar-aportes": return SembrarAportes(Carpeta(args, 1));
					// El token no se pega a mano: la app lo pide sola contra su device key.
					case "refrescar":       return await Refrescar(args);
					default:                Ayuda(); return 1;
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

		// LA MATRIZ DEL QR, en crudo. Herramienta de verificacion: un codigo QR mal armado
		// se ve perfecto y no lo lee ningun telefono, asi que mirarlo no alcanza como
		// prueba. Esto imprime la matriz para poder compararla, modulo por modulo, contra
		// un codificador de referencia independiente.
		private static int Qr(string[] args)
		{
			string contenido = args.Length > 1 ? args[1] : "SOCIO-000142";

			bool[,] matriz = Cargas.App.Vistas.CodigoQR.Matriz(contenido);
			int lado = matriz.GetLength(0);

			Console.WriteLine($"### {contenido} {lado}");
			for (int fila = 0; fila < lado; fila++)
			{
				var linea = new System.Text.StringBuilder();
				for (int columna = 0; columna < lado; columna++)
					linea.Append(matriz[fila, columna] ? '1' : '0');
				Console.WriteLine(linea.ToString());
			}
			return 0;
		}

		// Abre el puppet sobre una carpeta real. Es el mismo par ConfigureStorage + Start
		// que hace la caratula del browser, solo que ahi el path apunta a MEMFS.
		private static CargasApp Abrir(string carpeta)
		{
			Directory.CreateDirectory(carpeta);

			var app = new CargasApp(NombreDelActor);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			app.Start();
			return app;
		}

		// El "hoy" del arnes. Literal a proposito: con DateTime.Now la salida cambiaria
		// todos los dias y dejaria de servir como criterio de aceptacion. Es la fecha de
		// la carga 9 del guion.
		private static readonly DateTime FechaDeCorte = new DateTime(2026, 8, 25);

		// ---------------------------------------------------------------- medir
		private static int Medir(string carpeta)
		{
			Console.WriteLine($"carpeta : {carpeta}");
			CargasApp app = Abrir(carpeta);
			var api = new CargasAPI(app);
			IDiarioDeLaApp diario = api;

			Console.WriteLine($"entrada inicial del journal : {app.Actor.CurrentEntryId}");
			Console.WriteLine($"ultimo evento procesado     : {api.UltimoEventoProcesado()}");

			int aplicados = 0, descartados = 0, rechazados = 0;
			foreach (SenalDeEvento senal in Muestras.LaHistoria())
			{
				IMensajeDeEvento mensaje = RutaDeCargas.Rutear(senal);
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

			// DOS RECHAZOS EN DOS PLANOS DISTINTOS, y esa diferencia es el diseno.
			//
			// Un TIPO DE EVENTO que esta app no conoce se descarta en la RUTA: no hay
			// mensaje que armar, y eso no necesita al dominio para saberse.
			//
			// Un galon ambiguo -"gal" pelado, que no dice cual galon es- ya no se descarta
			// ahi. Reconocer un conjunto cerrado obligaba a la Superficie a NOMBRAR
			// UnidadDeVolumen en C#, y con eso el dominio entero tenia que ser public. Ahora
			// entra como mensaje y lo rechaza el Check DENTRO del actor, donde el motor
			// resuelve el tipo por reflexion desde el texto del script. Lo que importa que
			// no cambie es el efecto: no se aplica y NO deja entrada en el journal.
			bool descartaTipo = RutaDeCargas.Rutear(Muestras.TipoDesconocido()) == null;
			Console.WriteLine($"descarta tipo desconocido   : {descartaTipo}");

			long antesDelGalon = app.Actor.CurrentEntryId;
			string rechazoDelGalon = diario.Aplicar(RutaDeCargas.Rutear(Muestras.GalonAmbiguo()));
			Console.WriteLine($"rechaza el galon ambiguo    : {rechazoDelGalon.Length > 0}");
			Console.WriteLine($"  y NO deja entrada         : {app.Actor.CurrentEntryId == antesDelGalon}");

			// LOS APORTES DE SONIA: lo que URL2 no sabe. Sin esto todo es SinTramo, que
			// es exactamente la tesis del dominio.
			Aportes(api);

			ImprimirEstado(app, api, conVeredicto: true);

			// ---- criterio: un evento ya procesado se rechaza y NO deja entrada.
			long antes = app.Actor.CurrentEntryId;
			string mensajeDeRechazo = diario.Aplicar(RutaDeCargas.Rutear(Muestras.LaHistoria()[0]));
			long despues = app.Actor.CurrentEntryId;

			Console.WriteLine();
			Console.WriteLine("-- reintento de un evento ya procesado --");
			Console.WriteLine($"mensaje del Check           : [{mensajeDeRechazo}]");
			Console.WriteLine($"entrada antes / despues     : {antes} / {despues}");
			Console.WriteLine($"NO dejo entrada             : {antes == despues}");
			Console.WriteLine($"cantidad sin cambio         : {api.CantidadDeCargas() == Muestras.CargasEsperadas}");

			// ---- criterio: un kilometraje que retrocede se rechaza y NO deja entrada.
			antes = app.Actor.CurrentEntryId;
			string rechazoDeRetroceso = api.AnotarOdometro("T-0209", 41000m, "km", "observado");
			despues = app.Actor.CurrentEntryId;
			Console.WriteLine();
			Console.WriteLine("-- un kilometraje que retrocede es error de captura --");
			Console.WriteLine($"mensaje del Check           : [{rechazoDeRetroceso}]");
			Console.WriteLine($"NO dejo entrada             : {antes == despues}");

			app.Actor.GracefulExit();
			return 0;
		}

		private static void Aportes(CargasAPI api)
		{
			int anotados = 0, confirmados = 0;
			foreach ((string transaccion, decimal kilometros) in Muestras.Kilometrajes)
				if (api.AnotarOdometro(transaccion, kilometros, "km", "observado").Length == 0)
					anotados++;

			foreach (string transaccion in Muestras.TanquesPorConfirmar)
				if (api.ConfirmarTanque(transaccion).Length == 0) confirmados++;

			// La ficha va DESPUES de las cargas y no antes: sin una carga de esa placa no
			// hay vehiculo que completar, y el Check lo rechazaria con razon.
			var ficha = Muestras.FichaDelDemo;
			bool conFicha = api.CompletarFicha(Muestras.PlacaDelDemo, ficha.alias, ficha.marca,
				ficha.modelo, ficha.anio, ficha.color, ficha.rendimientoDeFabrica).Length == 0;

			Console.WriteLine(
				$"aportes: kilometrajes={anotados} tanques confirmados={confirmados} ficha={conFicha}");
		}

		// Los aportes sobre una carpeta ya refrescada contra URL2. Idempotente a su
		// manera: un kilometraje ya coherente se vuelve a escribir igual (Sonia puede
		// corregir), y una confirmacion sin inferencia pendiente se rechaza sin entrada.
		private static int SembrarAportes(string carpeta)
		{
			CargasApp app = Abrir(carpeta);
			var api = new CargasAPI(app);

			Aportes(api);
			ImprimirEstado(app, api, conVeredicto: true);

			app.Actor.GracefulExit();
			return 0;
		}

		// Se corre en un proceso NUEVO: si los tramos siguen, el replay rehidrato.
		private static int Estado(string carpeta)
		{
			CargasApp app = Abrir(carpeta);
			ImprimirEstado(app, new CargasAPI(app), conVeredicto: false);
			app.Actor.GracefulExit();
			return 0;
		}

		private static void ImprimirEstado(CargasApp app, CargasAPI api, bool conVeredicto)
		{
			Console.WriteLine();
			Console.WriteLine($"entrada del journal         : {app.Actor.CurrentEntryId}");
			Console.WriteLine($"ultimo evento procesado     : {api.UltimoEventoProcesado()}");
			Console.WriteLine($"cantidad de cargas          : {api.CantidadDeCargas()}");
			Console.WriteLine($"vehiculos                   : {api.CantidadDeVehiculos()}");
			Console.WriteLine($"token guardado              : [{api.TokenGuardado()}]");

			EstadoDeLealtad lealtad = api.Lealtad();
			Console.WriteLine($"lealtad                     : {lealtad.Saldo} puntos, nivel {lealtad.Nivel}"
				+ (lealtad.PuntosParaElSiguienteNivel > 0
					? $" (faltan {lealtad.PuntosParaElSiguienteNivel})" : ""));

			foreach (VehiculoEnLista vehiculo in api.Vehiculos())
			{
				Console.WriteLine();
				Console.WriteLine($"== {vehiculo.Nombre} ({vehiculo.Cargas} cargas) ==");

				Console.WriteLine();
				Console.WriteLine("-- tus cargas --");
				foreach (CargaEnLista carga in api.Cargas(vehiculo.Placa))
					Console.WriteLine($"  {carga.Fecha:dd/MM/yyyy}  {carga.Surtidor,-4} {carga.Tipo,-8} "
						+ $"{Formato(carga.Litros),6} L  {carga.Moneda}{Formato(carga.Total),10}  "
						+ $"{carga.Corte,-13} km=[{carga.Kilometraje}] tanque=[{carga.Tanque}]");

				Console.WriteLine();
				Console.WriteLine("-- los tramos: la unidad de calculo, no la carga --");
				foreach (TramoEnLista tramo in api.Tramos(vehiculo.Placa))
				{
					// La reserva solo aplica a los calculados: un bloqueado ya dice lo suyo.
					string reserva = tramo.EstaCalculado && !tramo.EsComparable
						? $"  [{tramo.MotivoDeReserva}]" : "";
					string sospecha = tramo.EsSospechoso ? "  [SOSPECHOSO]" : "";
					Console.WriteLine($"  {tramo.Desde:dd/MM/yyyy} -> {tramo.Hasta:dd/MM/yyyy}  "
						+ $"{tramo.Expresado}{reserva}{sospecha}");
				}

				Console.WriteLine();
				Console.WriteLine("-- LA PREGUNTA QUE NINGUNA PANTALLA CONTESTA --");
				ImprimirRendimiento(api, vehiculo.Placa);

				Console.WriteLine();
				Console.WriteLine("-- lo que falta, con lo que desbloquea --");
				IReadOnlyList<FaltanteEnLista> faltantes = api.Faltantes(vehiculo.Placa);
				if (faltantes.Count == 0) Console.WriteLine("  (nada: la serie esta completa)");
				foreach (FaltanteEnLista faltante in faltantes)
					Console.WriteLine($"  {faltante.ParaSonia}");

				CompletitudDelVehiculo completitud = api.Completitud(vehiculo.Placa);
				Console.WriteLine($"  {completitud.Expresada}");

				Console.WriteLine();
				Console.WriteLine("-- lo derivado: la tercera procedencia --");
				AutonomiaDelVehiculo autonomia = api.Autonomia(vehiculo.Placa);
				Console.WriteLine(autonomia.Hay
					? $"  autonomia estimada: ~{Formato(autonomia.Kilometros)} km "
						+ $"({Formato(autonomia.CapacidadEnLitros)} L x {Formato(autonomia.HabitualKmPorLitro)} km/L)"
						+ $" - proyeccion, no medicion"
					: "  autonomia: todavia no se puede estimar");

				OdometroPropuesto odometro = api.Odometro(vehiculo.Placa, FechaDeCorte);
				Console.WriteLine(odometro.Hay
					? $"  odometro esperado hoy: ~{Formato(odometro.Kilometros)} km "
						+ $"({Formato(odometro.KilometrosPorDia)} km/dia, origen {odometro.Origen})"
					: "  odometro esperado: sin lecturas suficientes");

				foreach (CostoEnLista costo in api.Costos(vehiculo.Placa))
					Console.WriteLine($"  costo por distancia: {costo.Expresado}");

				Console.WriteLine();
				Console.WriteLine("-- el precio en el tiempo, por surtidor --");
				foreach (PrecioEnLista precio in api.Precios(vehiculo.Placa))
					Console.WriteLine($"  {precio.Fecha:dd/MM/yyyy}  {precio.Surtidor,-4} "
						+ $"{precio.Moneda}{Formato(precio.PrecioPorLitro)} por litro");
			}

			Console.WriteLine();
			Console.WriteLine("-- el gasto del mes de la ultima carga --");
			foreach (GastoEnLista gasto in api.Gastos(FechaDeCorte.Year, FechaDeCorte.Month))
				Console.WriteLine($"  {gasto.Expresado}");

			if (!conVeredicto) return;

			Console.WriteLine();
			bool veredictoOk = false;
			foreach (VehiculoEnLista vehiculo in api.Vehiculos())
				veredictoOk = api.Rendimiento(vehiculo.Placa).Veredicto == Muestras.VeredictoEsperado;
			bool puntosOk = api.Lealtad().Saldo == Muestras.PuntosEsperados;
			Console.WriteLine($"VEREDICTO Y PUNTOS CORRECTOS: {veredictoOk && puntosOk}");
		}

		private static void ImprimirRendimiento(CargasAPI api, string placa)
		{
			RendimientoDelVehiculo lectura = api.Rendimiento(placa);

			// La frase la trae el dominio armada con hechos; esta caratula solo la
			// muestra, con su severidad y su accion si las hay.
			Console.WriteLine($"  {lectura.Veredicto,-16} {lectura.ParaSonia}");
			if (lectura.PideAlgo)
				Console.WriteLine($"  {"",-16} ^ {lectura.Severidad} - {lectura.Accion}");
			if (lectura.HaySerie)
				Console.WriteLine($"  {"",-16} tu nivel de siempre: {Formato(lectura.Habitual)} km/L "
					+ $"sobre {lectura.TramosCalculados} tramos");
		}

		private static string Formato(decimal valor) =>
			valor.ToString("0.##", CultureInfo.InvariantCulture);

		// Consulta puntual, para la demo en vivo.
		private static int RendimientoDe(string[] args)
		{
			string carpeta = Carpeta(args, 1);
			string placa = Opcional(args, 2, Muestras.PlacaDelDemo);

			CargasApp app = Abrir(carpeta);
			var api = new CargasAPI(app);

			Console.WriteLine($"placa: {placa}");
			ImprimirRendimiento(api, placa);
			Console.WriteLine();
			Console.WriteLine("-- crudo (print, formatter JSON) --");
			Console.WriteLine(api.EvaluacionDeRendimiento(placa));

			app.Actor.GracefulExit();
			return 0;
		}

		// La consulta estrella: combina dos derivaciones y no esta en ninguna pantalla.
		private static int AutonomiaDe(string[] args)
		{
			string carpeta = Carpeta(args, 1);
			string placa = Opcional(args, 2, Muestras.PlacaDelDemo);

			CargasApp app = Abrir(carpeta);
			var api = new CargasAPI(app);

			AutonomiaDelVehiculo autonomia = api.Autonomia(placa);
			Console.WriteLine($"placa       : {placa}");
			Console.WriteLine($"hay         : {autonomia.Hay}");
			Console.WriteLine($"kilometros  : ~{Formato(autonomia.Kilometros)}");
			Console.WriteLine($"capacidad   : {Formato(autonomia.CapacidadEnLitros)} L (estimada)");
			Console.WriteLine($"habitual    : {Formato(autonomia.HabitualKmPorLitro)} km/L");
			Console.WriteLine($"PROYECCION  : {autonomia.EsProyeccion}");
			Console.WriteLine();
			Console.WriteLine("-- crudo (print, formatter JSON) --");
			Console.WriteLine(api.AutonomiaEstimada(placa));

			app.Actor.GracefulExit();
			return 0;
		}

		// ---------------------------------------------------------------- refrescar
		private static async Task<int> Refrescar(string[] args)
		{
			string carpeta = Carpeta(args, 1);
			string baseUrl = Opcional(args, 2, "http://localhost:8111/");
			string recurso = Opcional(args, 3, "cargas");
			string id      = Opcional(args, 4, "1");

			using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
			CargasApp app = Abrir(carpeta);
			var api = new CargasAPI(app);

			var proveedorDeToken = new ProveedorDeToken(http);

			// La app consigue su credencial SOLA, contra su device key. Ni la consola ni
			// la persona pegan un token: la estacion lo emite para ESTA instalacion.
			if (api.DeviceKeyGuardado().Length == 0)
				api.RegistrarDevice("device-consola-de-medicion");

			if (api.TokenGuardado().Length == 0)
			{
				Credencial credencial = await proveedorDeToken.Pedir(api.DeviceKeyGuardado(), recurso, id);
				Console.WriteLine($"credencial: scope={credencial.Scope} expiresIn={credencial.ExpiresIn}s");
				api.GuardarCredencial(credencial.AccessToken, credencial.RefreshToken);
			}

			// Que hacer cuando la estacion rechaza el access token: rotar con el refresh
			// token, que es para lo que existe. El access es corto a proposito.
			async Task<string> Renovar(System.Threading.CancellationToken ct)
			{
				string refresh = api.RefreshTokenGuardado();

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
				RutaDeCargas.Rutear, null, Renovar);

			ResultadoDeRefresh primero = await bucle.Ejecutar();
			Console.WriteLine($"refresh 1 : {primero}");

			// Segundo refresh sin novedad: debe traer 0 y no aplicar nada.
			long antes = app.Actor.CurrentEntryId;
			ResultadoDeRefresh segundo = await bucle.Ejecutar();
			long despues = app.Actor.CurrentEntryId;
			Console.WriteLine($"refresh 2 : {segundo}");
			Console.WriteLine($"sin novedad no escribio     : {antes == despues}");

			ImprimirEstado(app, api, conVeredicto: false);

			app.Actor.GracefulExit();
			return 0;
		}

		// ---------------------------------------------------------------- argumentos
		private static string Carpeta(string[] args, int i) =>
			Path.GetFullPath(Opcional(args, i, Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Cargas-journal")));

		private static string Opcional(string[] args, int i, string porDefecto) =>
			args.Length > i && args[i].Length > 0 ? args[i] : porDefecto;

		private static void Ayuda()
		{
			Console.WriteLine("Cargas - caratula de consola, para medir el dominio y el refresh");
			Console.WriteLine();
			Console.WriteLine("  comprobar                                      comprueba las reglas del dominio puro (sin journal)");
			Console.WriteLine("  medir           [carpeta]                      aplica el guion entero (eventos + aportes) y mide");
			Console.WriteLine("  estado          [carpeta]                      abre el journal y reporta (correr en proceso nuevo)");
			Console.WriteLine("  rendimiento     [carpeta] [placa]              la lectura de la serie de tramos");
			Console.WriteLine("  autonomia       [carpeta] [placa]              capacidad estimada x rendimiento habitual");
			Console.WriteLine("  sembrar-aportes [carpeta]                      los aportes de Sonia sobre un journal refrescado");
			Console.WriteLine("  refrescar       [carpeta] [baseUrl] [recurso] [id]   trae eventos de la estacion y los aplica");
			Console.WriteLine("  qr              [contenido]                    imprime la matriz del QR en 0 y 1, para contrastarla");
			Console.WriteLine();
			Console.WriteLine("carpeta por defecto: ~/Cargas-journal");
			Console.WriteLine("baseUrl por defecto: http://localhost:8111/");
			Console.WriteLine($"placa por defecto  : {Muestras.PlacaDelDemo}");
		}
	}
}
