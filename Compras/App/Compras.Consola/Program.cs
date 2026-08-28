using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Compras.App;
using Compras.App.Eventos;
using Compras.App.Vistas;
using Compras.Dominio;
using Puppeteer;
using Puppets.Portador;

namespace Compras.Consola
{
	internal static class Program
	{
		private const string NombreDelActor = "compras";

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
					case "detalle":   return Detalle(args);
					case "canasta":   return CanastaDe(args);
					case "comparar":  return Comparar(args);
					case "garantia":  return GarantiaDe(args);
					case "buscar":    return Buscar(args);
					// Los dos que miran el hueco: lo que la pantalla no puede renderizar.
					case "pendientes": return Pendientes(args);
					case "porvencer":  return PorVencer(args);
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
		private static ComprasApp Abrir(string carpeta)
		{
			Directory.CreateDirectory(carpeta);

			var app = new ComprasApp(NombreDelActor);
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			app.Start();
			return app;
		}

		// ---------------------------------------------------------------- fase 1
		private static int Medir(string carpeta)
		{
			Console.WriteLine($"carpeta : {carpeta}");
			ComprasApp app = Abrir(carpeta);
			var api = new ComprasAPI(app);
			IDiarioDeLaApp diario = api;

			Console.WriteLine($"entrada inicial del journal : {app.Actor.CurrentEntryId}");
			Console.WriteLine($"ultimo evento procesado     : {api.UltimoEventoProcesado()}");

			int aplicados = 0, descartados = 0, rechazados = 0;
			foreach (SenalDeEvento senal in Muestras.LaHistoria())
			{
				IMensajeDeEvento mensaje = RutaDeCompras.Rutear(senal);
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
			bool descartaTipo = RutaDeCompras.Rutear(Muestras.TipoDesconocido()) == null;
			bool descartaCategoria = RutaDeCompras.Rutear(Muestras.CategoriaNoModelada()) == null;
			Console.WriteLine($"descarta tipo desconocido       : {descartaTipo}");
			Console.WriteLine($"descarta categoria no modelada  : {descartaCategoria}");

			ImprimirEstado(app, api, conVeredicto: true);

			// ---- criterio: un evento ya procesado se rechaza y NO deja entrada.
			long antes = app.Actor.CurrentEntryId;
			string mensajeDeRechazo = diario.Aplicar(RutaDeCompras.Rutear(Muestras.LaHistoria()[0]));
			long despues = app.Actor.CurrentEntryId;

			Console.WriteLine();
			Console.WriteLine("-- reintento de un evento ya procesado --");
			Console.WriteLine($"mensaje del Check           : [{mensajeDeRechazo}]");
			Console.WriteLine($"entrada antes / despues     : {antes} / {despues}");
			Console.WriteLine($"NO dejo entrada             : {antes == despues}");
			Console.WriteLine($"cantidad sin cambio         : {api.CantidadDeCompras() == Muestras.ComprasEsperadas}");

			app.Actor.GracefulExit();
			return 0;
		}

		// Se corre en un proceso NUEVO: si las canastas siguen, el replay rehidrato.
		private static int Estado(string carpeta)
		{
			ComprasApp app = Abrir(carpeta);
			ImprimirEstado(app, new ComprasAPI(app), conVeredicto: true);
			app.Actor.GracefulExit();
			return 0;
		}

		private static void ImprimirEstado(ComprasApp app, ComprasAPI api, bool conVeredicto)
		{
			Console.WriteLine();
			Console.WriteLine($"entrada del journal         : {app.Actor.CurrentEntryId}");
			Console.WriteLine($"ultimo evento procesado     : {api.UltimoEventoProcesado()}");
			Console.WriteLine($"cantidad de compras         : {api.CantidadDeCompras()}");
			Console.WriteLine($"esperadas                   : {Muestras.ComprasEsperadas}");
			Console.WriteLine($"gasto total registrado      : {Formato(api.TotalDeCompras())}");
			Console.WriteLine($"token guardado              : [{api.TokenGuardado()}]");

			// LA PORTADA, y va primero por eso: la especificacion de marca pide que la app
			// abra con la firma y con el quiebre, no con la lista de recibos.
			Console.WriteLine();
			Console.WriteLine("-- LA PORTADA: lo estable y lo que cambio --");
			ImprimirPortada(api, new DateTime(2026, 1, 1));

			Console.WriteLine();
			Console.WriteLine("-- lista (print, formatter JSON) --");
			Console.WriteLine(api.ListaDeCompras());

			Console.WriteLine();
			Console.WriteLine("-- LA PREGUNTA QUE NINGUNA PANTALLA CONTESTA --");
			ImprimirComparacion(api, 2025, 8, 2026, 8);

			Console.WriteLine();
			Console.WriteLine("-- y la garantia, con su respaldo --");
			ImprimirGarantia(api, Muestras.ProductoConGarantia, DateTime.Now);

			// LO QUE NO ESTA. Va con el resto porque es parte de lo que la app sabe
			// contestar, aunque no sea un dato del historial.
			Console.WriteLine();
			Console.WriteLine("-- lo que NO esta (el hueco) --");
			foreach (AbastecimientoEnEspera p in api.AbastecimientosPendientes(DateTime.Now))
				Console.WriteLine($"  {p.ParaSonia}  [{p.Severidad}]");

			if (!conVeredicto) return;

			// LOS TRES CRITERIOS, UNO POR LINEA. Antes esto era un solo booleano, y un "False"
			// sin decir cual de los tres fallo obliga a adivinar: la primera vez que fallo
			// costo tres corridas descubrir que era el total. Un veredicto que no dice donde
			// no sirve de veredicto.
			decimal total = api.TotalDeCompras();
			bool totalOk = total == Muestras.TotalEsperado;
			bool comparacionOk = api.Comparacion(2025, 8, 2026, 8).HayAlgoQueLoExplique;
			bool garantiaOk = api.Garantia(Muestras.ProductoConGarantia, DateTime.Now).Encontrada;

			Console.WriteLine();
			Console.WriteLine($"  total        : {(totalOk ? "ok" : $"MAL (esperado [{Muestras.TotalEsperado}], obtenido [{total}])")}");
			Console.WriteLine($"  comparacion  : {(comparacionOk ? "ok" : "MAL (no hay pasillo que explique la diferencia)")}");
			Console.WriteLine($"  garantia     : {(garantiaOk ? "ok" : $"MAL (no se encontro '{Muestras.ProductoConGarantia}')")}");
			Console.WriteLine($"CRITERIOS CORRECTOS         : {totalOk && comparacionOk && garantiaOk}");
		}

		// Las dos lecturas de la portada. Las lineas las escribe el DOMINIO, incluidos los
		// dos casos de "no hay respuesta todavia": aca no se decide nada, se imprime.
		private static void ImprimirPortada(ComprasAPI api, DateTime corte)
		{
			FirmaDeLaSemana firma = api.Firma();
			Console.WriteLine($"  {firma.ParaSonia}");
			foreach (ProductoQueVuelve p in firma.Habituales)
				Console.WriteLine($"    {p.Descripcion,-28} en {p.ComprasEnQueAparece} de {p.DeCuantas}"
					+ $"{(p.EstaEnTodas ? "  (en todas)" : "")}");

			CambioDeLaCanasta cambio = api.Cambio(corte);
			Console.WriteLine($"  {cambio.ParaSonia}");
			foreach (ProductoQueEntroVista p in cambio.Entraron)
				Console.WriteLine($"    entro  {p.Descripcion,-28} desde {p.Desde}");
			foreach (ProductoQueVuelve p in cambio.Salieron)
				Console.WriteLine($"    salio  {p.Descripcion}");

			// CADA CUANTO, que es la otra cara del mismo quiebre: la tarjeta de arriba dice
			// QUE entro, esta dice si lo compra mas seguido.
			IReadOnlyList<RitmoQueCambio> ritmos = api.RitmosQueCambiaron(corte);
			if (ritmos.Count == 0)
			{
				Console.WriteLine("  ningun producto cambio de ritmo");
				return;
			}

			Console.WriteLine("  cambio el ritmo de:");
			foreach (RitmoQueCambio r in ritmos)
				Console.WriteLine($"    {r.Descripcion,-28} {r.Antes,-15} -> {r.Ahora,-16}"
					+ $"{(r.EsMasSeguido ? "mas seguido" : "menos seguido")}");
		}

		private static void ImprimirComparacion(ComprasAPI api, int anioAntes, int mesAntes, int anioDespues, int mesDespues)
		{
			CanastasComparadas c = api.Comparacion(anioAntes, mesAntes, anioDespues, mesDespues);

			// La linea viene del dominio, incluido el caso "no hay con que comparar". Aca no
			// se decide nada: se imprime.
			if (!c.HayComparacion)
			{
				Console.WriteLine($"  {c.ParaSonia}");
				return;
			}

			Console.WriteLine($"  canasta {mesAntes}/{anioAntes}: {Formato(c.GastoAntes)}");
			Console.WriteLine($"  canasta {mesDespues}/{anioDespues}: {Formato(c.GastoDespues)} "
				+ $"({(c.CambioEnElTotal >= 0m ? "+" : "")}{Formato(c.CambioEnElTotal)})");

			foreach (DiferenciaPorDepartamentoVista d in c.Diferencias)
				if (d.ExplicaElCambio)
					Console.WriteLine($"    {d.Titulo}: {Formato(d.MontoAntes)} -> {Formato(d.MontoDespues)}"
						+ $"{(d.EsNuevo ? "  (entro este mes)" : "")}{(d.Desaparecio ? "  (dejo de aparecer)" : "")}");

			Console.WriteLine($"  {c.ParaSonia}");
		}

		private static void ImprimirGarantia(ComprasAPI api, string texto, DateTime fechaDeConsulta)
		{
			GarantiaDeProducto g = api.Garantia(texto, fechaDeConsulta);

			// La linea sale del dominio, incluido el caso "no encontrada".
			Console.WriteLine($"  {g.ParaSonia}");
			if (!g.Encontrada) return;

			Console.WriteLine($"  {g.Descripcion} ({g.Sku})");
			Console.WriteLine($"  comprada el {g.FechaDeCompra:yyyy-MM-dd} en la compra {g.NumeroDeCompra}");
			Console.WriteLine($"  {g.MesesDeCobertura} meses de cobertura: vence el {g.VenceEl:yyyy-MM-dd}"
				+ $" — {(g.Vigente ? "VIGENTE" : "vencida")} al {fechaDeConsulta:yyyy-MM-dd}"
				+ $" [{g.Severidad}]");
			if (g.QueHacer.Length > 0) Console.WriteLine($"  -> {g.QueHacer}");
		}

		// Consultas puntuales, para la demo en vivo.
		private static int Detalle(string[] args)
		{
			string carpeta = Carpeta(args, 1);
			string numero = Opcional(args, 2, Muestras.NumeroDeLaCompraDeLaGarantia);

			ComprasApp app = Abrir(carpeta);
			var api = new ComprasAPI(app);

			CompraDetallada compra = api.Detalle(numero);
			Console.WriteLine($"compra {compra.Numero} del {compra.Fecha:yyyy-MM-dd} — total {Formato(compra.Total)}");
			foreach (LineaDeCompra linea in compra.Lineas)
				Console.WriteLine($"  {linea.Cantidad,2} x {linea.Descripcion,-28} {Formato(linea.Total),10}  "
					+ $"{linea.Categoria}{(linea.TieneGarantia ? "  [garantia]" : "")}");

			Console.WriteLine();
			Console.WriteLine("-- crudo (print, formatter JSON) --");
			Console.WriteLine(api.DetalleDeCompra(numero));

			app.Actor.GracefulExit();
			return 0;
		}

		private static int CanastaDe(string[] args)
		{
			string carpeta = Carpeta(args, 1);
			int anio = EnteroOpcional(args, 2, 2026);
			int mes = EnteroOpcional(args, 3, 8);

			ComprasApp app = Abrir(carpeta);
			var api = new ComprasAPI(app);

			CanastaDelMesVista canasta = api.Canasta(anio, mes);
			Console.WriteLine($"canasta {mes}/{anio} — {canasta.CantidadDeCompras} compras");
			foreach (GastoPorDepartamentoVista d in canasta.Departamentos)
				Console.WriteLine($"  {d.Titulo,-22}: {Formato(d.Monto),12}  {Porcentaje(d.ProporcionDelTotal)}");
			Console.WriteLine($"  {"total",-22}: {Formato(canasta.GastoTotal),12}");
			Console.WriteLine($"    de eso, comida: {Formato(canasta.GastoComestible)}");

			Console.WriteLine();
			Console.WriteLine("-- crudo (print, formatter JSON) --");
			Console.WriteLine(api.CanastaDelMes(anio, mes));

			app.Actor.GracefulExit();
			return 0;
		}

		private static int Comparar(string[] args)
		{
			string carpeta = Carpeta(args, 1);
			int anioAntes = EnteroOpcional(args, 2, 2025);
			int mesAntes = EnteroOpcional(args, 3, 8);
			int anioDespues = EnteroOpcional(args, 4, 2026);
			int mesDespues = EnteroOpcional(args, 5, 8);

			ComprasApp app = Abrir(carpeta);
			var api = new ComprasAPI(app);

			ImprimirComparacion(api, anioAntes, mesAntes, anioDespues, mesDespues);

			Console.WriteLine();
			Console.WriteLine("-- crudo (print, formatter JSON) --");
			Console.WriteLine(api.ComparacionDeCanastas(anioAntes, mesAntes, anioDespues, mesDespues));

			app.Actor.GracefulExit();
			return 0;
		}

		private static int GarantiaDe(string[] args)
		{
			string carpeta = Carpeta(args, 1);
			string texto = Opcional(args, 2, Muestras.ProductoConGarantia);
			// El "hoy" lo pone la superficie; el dominio no lee el reloj.
			DateTime fecha = FechaOpcional(args, 3, DateTime.Now);

			ComprasApp app = Abrir(carpeta);
			var api = new ComprasAPI(app);

			ImprimirGarantia(api, texto, fecha);

			Console.WriteLine();
			Console.WriteLine("-- crudo (print, formatter JSON) --");
			Console.WriteLine(api.GarantiaDe(texto, fecha));

			app.Actor.GracefulExit();
			return 0;
		}

		// EN CUALES COMPRAS APARECIO ESTO. Existe sobre todo para probar la CONSULTA, que es
		// donde vive la trampa: un print plano no puede preguntar Existe() antes de imprimir
		// un campo, y un lector guardado no le blanquea la columna — le mata la consulta
		// entera. Buscar algo que no existe tiene que dar cabecera en cero, no un error.
		private static int Buscar(string[] args)
		{
			string carpeta = Carpeta(args, 1);
			string termino = Opcional(args, 2, "leche");

			ComprasApp app = Abrir(carpeta);
			var api = new ComprasAPI(app);

			ComprasHalladas halladas = api.Halladas(termino);
			Console.WriteLine(halladas.ParaSonia);
			if (halladas.Productos.Count > 0)
				Console.WriteLine($"  coinciden: {string.Join(", ", halladas.Productos)}");
			Console.WriteLine();
			foreach (CompraHallada compra in halladas.Compras)
				Console.WriteLine($"  {compra.Fecha}  {compra.Numero,-10} "
					+ $"{compra.Unidades,3} u  {Plata.Colones(compra.Gasto),12}"
					+ $"   (de {compra.LineasDeLaCompra} lineas, {Plata.Colones(compra.TotalDeLaCompra)})");

			Console.WriteLine();
			Console.WriteLine("-- crudo (print, formatter JSON) --");
			Console.WriteLine(api.ComprasCon(termino));

			app.Actor.GracefulExit();
			return 0;
		}

		// ---------------------------------------------------- lo que NO esta (el hueco)

		private static int Pendientes(string[] args)
		{
			string carpeta = Carpeta(args, 1);
			DateTime fecha = FechaOpcional(args, 2, DateTime.Now);

			ComprasApp app = Abrir(carpeta);
			var api = new ComprasAPI(app);

			IReadOnlyList<AbastecimientoEnEspera> pendientes = api.AbastecimientosPendientes(fecha);
			Console.WriteLine($"al {fecha:yyyy-MM-dd}: {pendientes.Count} "
				+ (pendientes.Count == 1 ? "categoria atrasada" : "categorias atrasadas"));
			foreach (AbastecimientoEnEspera p in pendientes)
			{
				Console.WriteLine($"  {p.Categoria,-18} {p.DiasDesdeLaUltima,4} dias (se repone cada {p.DiasEsperados})"
					+ $"  [{p.Severidad}]");
				Console.WriteLine($"  {"",-18} {p.ParaSonia}");
			}
			if (pendientes.Count == 0) Console.WriteLine("  (al dia con todo lo que se repone)");

			app.Actor.GracefulExit();
			return 0;
		}

		private static int PorVencer(string[] args)
		{
			string carpeta = Carpeta(args, 1);
			DateTime fecha = FechaOpcional(args, 2, DateTime.Now);

			ComprasApp app = Abrir(carpeta);
			var api = new ComprasAPI(app);

			IReadOnlyList<GarantiaDeProducto> porVencer = api.GarantiasPorVencer(fecha);
			Console.WriteLine($"al {fecha:yyyy-MM-dd}: {porVencer.Count} "
				+ (porVencer.Count == 1 ? "garantia pide algo" : "garantias piden algo"));
			foreach (GarantiaDeProducto g in porVencer)
			{
				Console.WriteLine($"  {g.Descripcion,-22} quedan {g.DiasParaVencer,4} dias  [{g.Severidad}]");
				Console.WriteLine($"  {"",-22} {g.ParaSonia}");
				if (g.QueHacer.Length > 0) Console.WriteLine($"  {"",-22} -> {g.QueHacer}");
			}
			if (porVencer.Count == 0)
				Console.WriteLine("  (ninguna esta cerca de vencer; " + api.GarantiasVigentes(fecha).Count + " vigentes)");

			app.Actor.GracefulExit();
			return 0;
		}

		// ---------------------------------------------------------------- fase 2
		private static async Task<int> Refrescar(string[] args)
		{
			string carpeta = Carpeta(args, 1);
			string baseUrl = Opcional(args, 2, "http://localhost:8101/");
			string recurso = Opcional(args, 3, "cliente");
			string id      = Opcional(args, 4, "1");

			using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
			ComprasApp app = Abrir(carpeta);
			var api = new ComprasAPI(app);

			var proveedorDeToken = new ProveedorDeToken(http);

			// La app consigue su credencial SOLA, contra su device key. Ni la consola ni
			// la persona pegan un token: el supermercado lo emite para ESTA instalacion.
			if (api.DeviceKeyGuardado().Length == 0)
				api.RegistrarDevice("device-consola-de-medicion");

			if (api.TokenGuardado().Length == 0)
			{
				Credencial credencial = await proveedorDeToken.Pedir(api.DeviceKeyGuardado(), recurso, id);
				Console.WriteLine($"credencial: scope={credencial.Scope} expiresIn={credencial.ExpiresIn}s");
				api.GuardarCredencial(credencial.AccessToken, credencial.RefreshToken);
			}

			// Que hacer cuando el supermercado rechaza el access token: rotar con el
			// refresh token, que es para lo que existe. El access es corto a proposito.
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
				RutaDeCompras.Rutear, null, Renovar);

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
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Compras-journal")));

		private static string Opcional(string[] args, int i, string porDefecto) =>
			args.Length > i && args[i].Length > 0 ? args[i] : porDefecto;

		private static int EnteroOpcional(string[] args, int i, int porDefecto) =>
			args.Length > i && int.TryParse(args[i], out int valor) ? valor : porDefecto;

		private static DateTime FechaOpcional(string[] args, int i, DateTime porDefecto) =>
			args.Length > i && DateTime.TryParseExact(args[i], "yyyy-MM-dd", CultureInfo.InvariantCulture,
				DateTimeStyles.None, out DateTime valor) ? valor : porDefecto;

		private static string Formato(decimal valor) =>
			valor.ToString("N0", CultureInfo.InvariantCulture);

		private static string Porcentaje(decimal proporcion) =>
			(proporcion * 100m).ToString("0.#", CultureInfo.InvariantCulture) + "%";

		private static void Ayuda()
		{
			Console.WriteLine("Compras - caratula de consola, para medir el dominio y el refresh");
			Console.WriteLine();
			Console.WriteLine("  comprobar                                       comprueba las reglas del dominio puro (sin journal)");
			Console.WriteLine("  medir     [carpeta]                             aplica la historia y mide los criterios de fase 1");
			Console.WriteLine("  estado    [carpeta]                             abre el journal y reporta (correr en proceso nuevo)");
			Console.WriteLine("  detalle   [carpeta] [numero]                    el ticket completo, linea por linea");
			Console.WriteLine("  canasta   [carpeta] [anio] [mes]                la foto de un mes, pasillo por pasillo");
			Console.WriteLine("  comparar  [carpeta] [a1] [m1] [a2] [m2]         como cambio la canasta entre dos meses");
			Console.WriteLine("  garantia  [carpeta] [texto] [yyyy-MM-dd]        la garantia de un producto, con su respaldo");
			Console.WriteLine("  buscar    [carpeta] [texto]                     en cuales compras aparecio un producto o un pasillo");
			Console.WriteLine("  pendientes [carpeta] [yyyy-MM-dd]              LO QUE NO ESTA: que dejo de entrar a la canasta");
			Console.WriteLine("  porvencer  [carpeta] [yyyy-MM-dd]              las garantias cuya ventana se esta cerrando");
			Console.WriteLine("  refrescar [carpeta] [baseUrl] [recurso] [id]    trae eventos del supermercado y los aplica de a uno");
			Console.WriteLine();
			Console.WriteLine("carpeta por defecto: ~/Compras-journal");
			Console.WriteLine("baseUrl por defecto: http://localhost:8101/");
		}
	}
}
