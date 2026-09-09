using Choreography.Theater;
using Municipalidad.App;
using Municipalidad.App.Events;
using Municipalidad.Wasm.Vistas;
using Puppeteer;
using Puppets.Portador;

namespace Municipalidad.Wasm
{
	/// <summary>
	/// El actor de esta instalacion y todo lo que lo rodea, vivo una sola vez: es un singleton y NO
	/// se crea por pagina. Lee el reloj de las DOS formas vivas y las tiene pegadas a proposito: el
	/// sello de un comando es de instancia y monotono, el instante de una lectura es estatico y no.
	/// </summary>
	public sealed class EstadoDeLaApp : IAsyncDisposable
	{
		private readonly PuenteDeJournal puente;
		private readonly HttpClient http;
		private readonly AjustesDeLaMunicipalidad ajustes;

		private MunicipalidadApp app;
		private MunicipalidadAPI api;
		private BucleDeRefresh bucle;

		private DateTime ultimoSelloMandado = DateTime.MinValue;

		public EstadoDeLaApp(PuenteDeJournal puente, HttpClient http, AjustesDeLaMunicipalidad ajustes)
		{
			ArgumentNullException.ThrowIfNull(puente);
			ArgumentNullException.ThrowIfNull(http);
			ArgumentNullException.ThrowIfNull(ajustes);

			this.puente = puente;
			this.http = http;
			this.ajustes = ajustes;
		}

		public bool EstaArriba => app is not null;

		public bool HayCarpeta => puente.HayCarpeta;

		public bool FaltaReotorgarPermiso => puente.FaltaReotorgarPermiso;

		public string NombreDeLaCarpeta => puente.NombreDeLaCarpeta;

		public string Modo { get; private set; } = "ninguno";

		public bool EnCarpetaPropia => Modo == "carpeta";

		public bool EnElNavegador => Modo == "opfs";

		public string UltimoMensaje { get; private set; } = "";

		/// <summary>No vacio cuando una escritura a la carpeta fallo. Nunca se traga en silencio.</summary>
		public string NoSeEstaGuardando { get; private set; } = "";

		/// <summary>No vacio cuando no se pudo alcanzar la institucion. Abrir igual es lo correcto.</summary>
		public string NoSeAlcanzaLaInstitucion { get; private set; } = "";

		public string DondeEstaElFeed => http.BaseAddress?.ToString() ?? "(sin configurar)";

		public string Recurso => ajustes.Recurso;

		public string Id => ajustes.Id;

		public ValueTask<bool> HaySelectorDeCarpetasAsync() => puente.EstaSoportadoAsync();

		public ValueTask<bool> HayAlmacenamientoDelNavegadorAsync() => puente.OpfsSoportadoAsync();

		public ValueTask<string> MotivoSinAlmacenamientoAsync() => puente.MotivoSinAlmacenamientoAsync();

		public ValueTask<bool> CorreComoAppInstaladaAsync() => puente.EstaInstaladaAsync();

		public ValueTask<string> ModoRecordadoAsync() => puente.ModoRecordadoAsync();

		public ValueTask<string> PermisoActualAsync() => puente.PermisoActualAsync();

		public async ValueTask<ResultadoDeCarpeta> RecuperarCarpetaAsync()
		{
			ResultadoDeCarpeta resultado = await puente.RecuperarCarpetaAsync();
			Modo = await puente.ModoActualAsync();
			UltimoMensaje = $"recuperar carpeta: {resultado}";
			return resultado;
		}

		public async ValueTask<ResultadoDeCarpeta> ConectarCarpetaAsync()
		{
			ResultadoDeCarpeta resultado = await puente.ElegirCarpetaAsync();
			Modo = await puente.ModoActualAsync();
			UltimoMensaje = $"conectar carpeta: {resultado}";

			if (resultado.Ok) SoltarElActor();
			return resultado;
		}

		public async ValueTask<ResultadoDeCarpeta> PedirPermisoAsync()
		{
			ResultadoDeCarpeta resultado = await puente.PedirPermisoAsync();
			UltimoMensaje = $"pedir permiso: {resultado}";

			if (resultado.Ok) SoltarElActor();
			return resultado;
		}

		public async ValueTask<ResultadoDeCarpeta> UsarElNavegadorAsync()
		{
			ResultadoDeCarpeta resultado = await puente.UsarOpfsAsync();
			Modo = await puente.ModoActualAsync();
			UltimoMensaje = $"almacenamiento del navegador: {resultado}";

			if (resultado.Ok) SoltarElActor();
			return resultado;
		}

		public async ValueTask OlvidarLaCarpetaAsync()
		{
			await puente.OlvidarCarpetaAsync();
			Modo = await puente.ModoActualAsync();
			UltimoMensaje = "se olvidó la carpeta guardada; no se tocó nada de lo que hay adentro";
		}

		/// <summary>
		/// El destino cambio, asi que el actor de antes ya no corresponde: se cierra y MEMFS se vacia
		/// para que la precarga siguiente no mezcle dos journals.
		/// </summary>
		private void SoltarElActor()
		{
			app?.Actor?.GracefulExit();
			app = null;
			api = null;
			bucle = null;

			if (Directory.Exists(PuenteDeJournal.RaizEnMemfs))
				Directory.Delete(PuenteDeJournal.RaizEnMemfs, recursive: true);
		}

		/// <summary>
		/// El arranque, y su orden no es negociable: la carpeta baja a MEMFS ANTES de que el actor
		/// exista, porque el replay del motor es sincrono y las APIs del navegador no lo son.
		/// </summary>
		public async ValueTask AbrirAsync()
		{
			if (app is not null) return;

			int precargados = puente.HayCarpeta ? await puente.PrecargarEnMemfsAsync() : 0;

			var abierto = new MunicipalidadApp(MunicipalidadApp.ElActor);
			abierto.ConfigureStorage(DatabaseType.FileSystem, "path=" + PuenteDeJournal.RaizEnMemfs);
			abierto.Start();

			app = abierto;
			api = new MunicipalidadAPI(abierto);

			bucle = new BucleDeRefresh(
				api,
				new FuenteHttpDeEventos(http, ajustes.Recurso, ajustes.Id),
				MunicipalidadEventRoute.Route,
				BajarAsync,
				ct => RenovarLaCredencialAsync(ct));

			try { await BajarAsync(); } catch (EscrituraRechazadaException) { }

			UltimoMensaje = puente.HayCarpeta
				? $"arriba. {precargados} archivos precargados de '{puente.NombreDeLaCarpeta}'"
				: "arriba, pero SIN carpeta: el journal vive solo en memoria y se pierde al cerrar";

			try
			{
				await LaCredencialAsync(CancellationToken.None);
				ResultadoDeRefresh alAbrir = await bucle.Ejecutar(CancellationToken.None);
				await BajarAsync();
				NoSeAlcanzaLaInstitucion = "";
				UltimoMensaje += $" | al abrir: {alAbrir}";
			}
			catch (EscrituraRechazadaException)
			{
				UltimoMensaje += " | la institución contestó, pero la carpeta rechazó la escritura";
			}
			catch (Exception ex)
			{
				NoSeAlcanzaLaInstitucion =
					$"No se pudo alcanzar {DondeEstaElFeed} ({ex.GetType().Name}: {ex.Message}).";
				UltimoMensaje += " | " + NoSeAlcanzaLaInstitucion;
			}
		}

		/// <summary>
		/// Trae lo que la institucion haya publicado desde el punto de control que dice el journal.
		/// </summary>
		public async ValueTask<ResultadoDeRefresh> RefrescarAsync(CancellationToken ct = default)
		{
			Exigir();

			await LaCredencialAsync(ct);

			ResultadoDeRefresh resultado = await bucle.Ejecutar(ct);

			NoSeAlcanzaLaInstitucion = "";
			UltimoMensaje = $"actualización: {resultado}";

			await BajarAsync();
			return resultado;
		}

		/// <summary>
		/// El flush: MEMFS a la carpeta, DESPUES de cada comando y no por timer. Angosta al maximo la
		/// ventana en que un cierre abrupto perderia entradas; no es durabilidad transaccional.
		/// </summary>
		private async Task BajarAsync()
		{
			try
			{
				await puente.BajarDeMemfsAsync();
				NoSeEstaGuardando = "";
			}
			catch (EscrituraRechazadaException ex)
			{
				NoSeEstaGuardando = ex.Message;
				throw;
			}
		}

		/// <summary>
		/// La credencial con que se leen los eventos. Registra esta instalacion la primera vez, y de
		/// ahi en adelante la clave y el token viven en el journal y no en un campo.
		/// </summary>
		private async ValueTask<string> LaCredencialAsync(CancellationToken ct)
		{
			Exigir();

			if (api.ClaveDeDispositivoGuardada().Length == 0)
			{
				string rechazo = api.RegistrarDispositivo(UnaClaveDeDispositivo());
				await BajarAsync();

				if (rechazo.Length > 0)
					UltimoMensaje = "no se pudo registrar esta instalación: " + rechazo;
			}

			string actual = api.TokenGuardado();
			if (actual.Length > 0) return actual;

			return await PedirUnaCredencialNuevaAsync(ct);
		}

		private async Task<string> RenovarLaCredencialAsync(CancellationToken ct)
		{
			Exigir();

			string refresco = api.TokenDeRefrescoGuardado();
			if (refresco.Length > 0)
			{
				Credencial rotada = await new ProveedorDeToken(http).Rotar(refresco, ct);
				if (rotada is not null) return await GuardarAsync(rotada, "credencial rotada con el token de refresco");
			}

			return await PedirUnaCredencialNuevaAsync(ct);
		}

		private async Task<string> PedirUnaCredencialNuevaAsync(CancellationToken ct)
		{
			Credencial nueva = await new ProveedorDeToken(http)
				.Pedir(api.ClaveDeDispositivoGuardada(), ajustes.Recurso, ajustes.Id, ct);

			return await GuardarAsync(nueva, $"credencial nueva (alcance {nueva.Scope})");
		}

		private async Task<string> GuardarAsync(Credencial credencial, string quePaso)
		{
			string rechazo = api.GuardarCredencial(credencial.AccessToken, credencial.RefreshToken);
			await BajarAsync();

			UltimoMensaje = rechazo.Length > 0
				? "no se pudo guardar la credencial: " + rechazo
				: quePaso;

			return credencial.AccessToken;
		}

		private static string UnaClaveDeDispositivo() =>
			"dispositivo-" + Guid.NewGuid().ToString("N")[..16];

		/// <summary>La clave con que esta instalacion se dio a conocer, o vacio si nunca se registro.</summary>
		public string ClaveDeDispositivo() { Exigir(); return api.ClaveDeDispositivoGuardada(); }

		/// <summary>Si hay credencial guardada. El token en si no sale de aca.</summary>
		public bool HayCredencial() { Exigir(); return api.TokenGuardado().Length > 0; }

		/// <summary>Hasta donde leyo esta app la narrativa de la institucion.</summary>
		public long UltimoEventoAplicado() { Exigir(); return api.UltimoEventoAplicado(); }

		/// <summary>Cuantas entradas lleva el journal de esta instalacion.</summary>
		public long EntradasDelJournal() { Exigir(); return app.Actor.CurrentEntryId; }

		public (int archivos, long bytes) EstadoDeMemfs() => PuenteDeJournal.EstadoDeMemfs();

		/// <summary>
		/// Por que subio cada rubro, leido entero de una sola pasada para que las cinco filas hablen del
		/// mismo estado de la cuenta.
		/// </summary>
		public LaVariacionDeCadaRubro LeerLaVariacionDeCadaRubro()
		{
			Exigir();
			return LaVariacionDeCadaRubro.LeerPor(api);
		}

		/// <summary>
		/// Lo que se vence y lo que ya vencio, leido entero al instante en que la pantalla pregunta: las
		/// dos listas salen de UNA sola lectura del reloj, asi que cuentan dias contra el mismo dia.
		/// </summary>
		public LoQueSeVence LeerLoQueSeVence()
		{
			Exigir();

			DateTime alDia = ElInstanteEnQueLaPantallaPregunta();

			return LoQueSeVence.LeerPor(api, alDia, ElDiaDe(alDia));
		}

		/// <summary>
		/// La reserva propuesta y la adoptada, leidas al instante en que la pantalla pregunta, porque el
		/// faltante se mide contra lo que vence en el mes de ese instante.
		/// </summary>
		public LaReservaDeEsteAnio LeerLaReserva()
		{
			Exigir();

			DateTime alDia = ElInstanteEnQueLaPantallaPregunta();

			return LaReservaDeEsteAnio.LeerPor(api, alDia, ElDiaDe(alDia));
		}

		/// <summary>
		/// Las fincas de esta cuenta y el valor declarado de cada una, al instante en que la pantalla
		/// pregunta, porque cual valor rige se decide contra un dia.
		/// </summary>
		public LasFincasQueTengo LeerLasFincas()
		{
			Exigir();
			return LasFincasQueTengo.LeerPor(api, ElInstanteEnQueLaPantallaPregunta());
		}

		/// <summary>
		/// El cotejo de la ultima consulta. NO toma el reloj: el dia del cotejo tiene que ser un dia en
		/// que hubo consulta, y ese lo sabe el registro.
		/// </summary>
		public LoSinComprobante LeerLoSinComprobante()
		{
			Exigir();
			return LoSinComprobante.LeerPor(api);
		}

		/// <summary>
		/// EL UNICO aporte que esta app le pide a la persona: cuanto decide apartar por mes. El instante
		/// lo pone esta app con su sello de comando y no se le pide a ella.
		/// <para>
		/// El valor de una finca NO esta aca, y su ausencia es la decision: el avaluo lo publica la
		/// municipalidad y es el valor con que cobra, asi que esta app no le pide a la persona un numero
		/// que la institucion ya tiene. La puerta sigue publicando el comando para la consola.
		/// </para>
		/// </summary>
		public async ValueTask<LoQueDijoElAporte> AdoptarReservaAsync(decimal montoMensual)
		{
			Exigir();

			return await MandarAsync(() => api.AdoptarReserva(montoMensual, UnSelloQueNoVaParaAtras()));
		}

		/// <summary>
		/// Manda un comando de la persona y baja el journal despues, y contesta con una frase pase lo
		/// que pase: un rechazo, un fallo al guardar y un fallo cualquiera son tres respuestas y
		/// ninguna es la banda de error del navegador.
		/// </summary>
		private async ValueTask<LoQueDijoElAporte> MandarAsync(Func<string> elComando)
		{
			string rechazo;

			try
			{
				rechazo = elComando();
			}
			catch (Exception ex)
			{
				return LoQueDijoElAporte.NoSePudo(LoQueSePuedeDecirDe(ex));
			}

			if (rechazo.Length > 0) return LoQueDijoElAporte.NoSePudo(Desenvolver(rechazo));

			try
			{
				await BajarAsync();
			}
			catch (EscrituraRechazadaException ex)
			{
				return LoQueDijoElAporte.QuedoSinGuardar(ex.Message);
			}

			return LoQueDijoElAporte.Entro();
		}

		/// <summary>
		/// El motivo que trae un rechazo del Check, que viaja adentro de un sobre y no como frase. Sin
		/// esto la persona leeria el JSON del sobre.
		/// </summary>
		private static string Desenvolver(string rechazo)
		{
			if (string.IsNullOrWhiteSpace(rechazo)) return "";

			try
			{
				using System.Text.Json.JsonDocument sobre = System.Text.Json.JsonDocument.Parse(rechazo);

				if (sobre.RootElement.TryGetProperty("EWI", out System.Text.Json.JsonElement ewi)
					&& ewi.ValueKind == System.Text.Json.JsonValueKind.Array
					&& ewi.GetArrayLength() > 0
					&& ewi[0].TryGetProperty("Error", out System.Text.Json.JsonElement error))
					return error.GetString() ?? rechazo;
			}
			catch (System.Text.Json.JsonException)
			{
				return rechazo;
			}

			return rechazo;
		}

		/// <summary>
		/// Lo que se le puede decir a una persona de una falla que no es un rechazo del dominio. Nombra
		/// el tipo porque sin el nadie puede diagnosticarla, y no pretende explicarla.
		/// </summary>
		private static string LoQueSePuedeDecirDe(Exception ex) =>
			$"La app falló al intentar guardarlo, y eso no es algo que hayas hecho vos: "
			+ $"{ex.GetType().Name}: {ex.Message}";

		/// <summary>
		/// El instante que esta app le manda a un COMANDO. Es monotono dentro de la sesion, y es de
		/// INSTANCIA porque la monotonia necesita el estado de la sesion.
		/// </summary>
		private DateTime UnSelloQueNoVaParaAtras()
		{
			DateTime ahora = ElSegundoEnteroDeAhora();

			if (ahora < ultimoSelloMandado)
				throw new InvalidOperationException(
					"El reloj de esta maquina fue para atras: ahora son las " + Dicho(ahora)
					+ " y esta app ya mando un comando marcado " + Dicho(ultimoSelloMandado)
					+ ". No va a mandar un instante que va hacia atras.");

			ultimoSelloMandado = ahora;
			return ahora;
		}

		private static string Dicho(DateTime instante) =>
			instante.ToString("yyyy-MM-dd HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture);

		/// <summary>
		/// El instante en que la PANTALLA pregunta. Es argumento de una lectura, asi que deliberadamente
		/// NO es monotono: una pregunta no escribe ninguna entrada y una guarda de monotonia solo
		/// rechazaria una segunda pregunta legitima un segundo despues. Es estatico justamente por eso.
		/// </summary>
		private static DateTime ElInstanteEnQueLaPantallaPregunta()
		{
			return ElSegundoEnteroDeAhora();
		}

		/// <summary>Ahora, truncado al segundo entero, que es la resolucion que el journal guarda.</summary>
		private static DateTime ElSegundoEnteroDeAhora()
		{
			DateTime ahora = DateTime.UtcNow;

			return new DateTime(
				ahora.Year, ahora.Month, ahora.Day, ahora.Hour, ahora.Minute, ahora.Second,
				DateTimeKind.Utc);
		}

		/// <summary>Ese instante dicho como el dia de calendario en que cae.</summary>
		private static string ElDiaDe(DateTime instante) =>
			instante.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

		private void Exigir()
		{
			if (api is null) throw new InvalidOperationException("Esta app todavia no se ha abierto.");
		}

		public async ValueTask DisposeAsync()
		{
			app?.Actor.GracefulExit();
			await puente.DisposeAsync();
		}
	}
}
