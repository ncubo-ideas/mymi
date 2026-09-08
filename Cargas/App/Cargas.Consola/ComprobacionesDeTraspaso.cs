using System;
using System.IO;
using Cargas.App;
using Cargas.App.Eventos;
using Cargas.App.Vistas;
using Puppeteer;
using Puppets.Portador;

namespace Cargas.Consola
{
	// EL TRASPASO, comprobado de punta a punta con DOS actores reales sobre disco: la app
	// suelta que arma el sobre y el modulo hospedado que lo recibe. No va en Comprobaciones
	// porque aquello es dominio puro sin actor ni journal, y esto es justamente lo contrario:
	// la regla que se comprueba -que un sobre repetido no deja entrada- es del journal.
	//
	// Corre sobre carpetas temporales propias, que se borran al final.
	internal static class ComprobacionesDeTraspaso
	{
		private static int fallas;
		private static int corridas;

		private static readonly DateTime Hoy = new DateTime(2026, 8, 25);
		private const string Publicador = "http://localhost:8111/";

		internal static int Correr()
		{
			fallas = 0; corridas = 0;

			string raiz = Path.Combine(Path.GetTempPath(), "cargas-traspaso", Guid.NewGuid().ToString("N"));
			CargasApp suelta = Abrir(Path.Combine(raiz, "suelta"));
			CargasApp hospedada = Abrir(Path.Combine(raiz, "hospedada"));
			try
			{
				var apiSuelta = new CargasAPI(suelta);
				var apiHospedada = new CargasAPI(hospedada);

				string sobre = LaSueltaArmaElSobreSinCredencial(apiSuelta);
				ElSobreEsperaASusCargas(apiHospedada, sobre);
				ConLasCargasElSobreSeAsientaEntero(hospedada, apiSuelta, apiHospedada, sobre);
				RepetirElTraspasoNoDejaEntrada(hospedada, apiHospedada, sobre);
				UnSobreAjenoORotoNoSeAsienta(hospedada, apiHospedada);
			}
			finally
			{
				suelta.Actor.GracefulExit();
				hospedada.Actor.GracefulExit();
				try { Directory.Delete(raiz, recursive: true); } catch (IOException) { }
			}

			Console.WriteLine();
			Console.WriteLine(fallas == 0
				? $"TODO OK  ({corridas} comprobaciones del traspaso)"
				: $"{fallas} FALLAS de {corridas} comprobaciones del traspaso");
			return fallas == 0 ? 0 : 1;
		}

		// -------------------------------------------------------------- las reglas

		private static string LaSueltaArmaElSobreSinCredencial(CargasAPI api)
		{
			Titulo("la app suelta arma el sobre: solo lo que dijo la persona, sin credencial");

			// La suelta vivio su vida: la historia de la estacion, los aportes de Sonia, una
			// preferencia distinta a la canonica y la credencial con la que leia.
			Historia(api);
			Aportes(api);
			api.ElegirPresentacionDeRendimiento("mpg");
			api.RegistrarDevice("device-de-prueba");
			api.GuardarCredencial("token-secreto", "refresh-secreto");

			ResumenDeTraspaso resumen = Traspaso.Resumir(api);
			Igual("hay tantos kilometrajes como el guion", Muestras.Kilometrajes.Length.ToString(),
				resumen.Kilometrajes.ToString());
			Igual("y tantos tanques confirmados como cargas", Muestras.TanquesPorConfirmar.Length.ToString(),
				resumen.Tanques.ToString());
			Igual("y la ficha", "1", resumen.Fichas.ToString());
			Igual("hay algo que pasar", "True", resumen.HayAlgoQuePasar.ToString());

			string sobre = Traspaso.Armar(api, Publicador, Hoy);
			Igual("el sobre dice de que modulo es", "True", sobre.Contains("\"modulo\": \"cargas\"").ToString());
			Igual("y de que publicador vienen las cargas", "True", sobre.Contains(Publicador).ToString());
			Igual("NO lleva la credencial", "False",
				(sobre.Contains("token-secreto") || sobre.Contains("refresh-secreto") || sobre.Contains("device-de-prueba")).ToString());
			Igual("NO lleva los hechos de la estacion (litros, precio)", "False",
				(sobre.Contains("litros") || sobre.Contains("precio")).ToString());
			Igual("lleva la preferencia de rendimiento", "True", sobre.Contains("\"rendimiento\": \"mpg\"").ToString());

			// EL DATO QUE EL 13 §9 DEJO "A MEDIR": cuanto pesa el sobre de una bitacora real, para
			// saber si cabe en el enlace del handoff o tiene que ir por la hoja de compartir.
			Console.WriteLine($"  (el sobre del guion pesa {sobre.Length} caracteres; "
				+ $"escapado para una URL, {Uri.EscapeDataString(sobre).Length})");
			return sobre;
		}

		private static void ElSobreEsperaASusCargas(CargasAPI api, string sobre)
		{
			Titulo("en un modulo que todavia no recibio las cargas, las anotaciones ESPERAN");

			ResultadoDeTraspaso resultado = Traspaso.Aplicar(api, sobre);
			int anotacionesDeCargas = Muestras.Kilometrajes.Length + Muestras.TanquesPorConfirmar.Length;

			Igual("esperan los kilometrajes, los tanques y la ficha", (anotacionesDeCargas + 1).ToString(),
				resultado.Esperando.ToString());
			Igual("nada se rechazo", "0", resultado.Rechazadas.ToString());
			// La preferencia no depende de ninguna carga: entra ya. La de distancia ya era la
			// canonica, asi que se omite.
			Igual("la preferencia de rendimiento entro", "1", resultado.Aplicadas.ToString());
			Igual("la de distancia ya estaba", "1", resultado.Omitidas.ToString());
			Igual("y el traspaso queda pendiente", "True", resultado.Pendiente.ToString());
			Igual("el modulo mira en mpg", "mpg", api.Unidades().Rendimiento);
		}

		private static void ConLasCargasElSobreSeAsientaEntero(CargasApp hospedada, CargasAPI suelta,
			CargasAPI api, string sobre)
		{
			Titulo("cuando la estacion ya publico las cargas, el sobre se asienta entero");

			// Gnothi consiguio su credencial y el modulo trajo su historia desde cursor 0.
			Historia(api);
			Igual("las cargas llegaron sin aportes", "0", Traspaso.Resumir(api).Kilometrajes.ToString());

			long antes = hospedada.Actor.CurrentEntryId;
			ResultadoDeTraspaso resultado = Traspaso.Aplicar(api, sobre);
			int anotacionesDeCargas = Muestras.Kilometrajes.Length + Muestras.TanquesPorConfirmar.Length;

			Igual("se aplicaron los kilometrajes, los tanques y la ficha", (anotacionesDeCargas + 1).ToString(),
				resultado.Aplicadas.ToString());
			Igual("nada quedo esperando", "0", resultado.Esperando.ToString());
			Igual("nada se rechazo", "0", resultado.Rechazadas.ToString());
			Igual("las dos preferencias ya estaban", "2", resultado.Omitidas.ToString());
			Igual("y dejo entradas en el journal", "True", (hospedada.Actor.CurrentEntryId > antes).ToString());

			// LO QUE IMPORTA: el modulo hospedado calcula lo mismo que la suelta. Sin el
			// traspaso solo tendria facturas; con el, tiene tramos y veredicto.
			string placa = Muestras.PlacaDelDemo;
			Igual("el carro tiene el mismo nombre", suelta.Vehiculos()[0].Nombre, api.Vehiculos()[0].Nombre);
			Igual("la misma cantidad de tramos", suelta.Tramos(placa).Count.ToString(), api.Tramos(placa).Count.ToString());
			Igual("y la misma lectura del rendimiento", suelta.EvaluacionDeRendimiento(placa),
				api.EvaluacionDeRendimiento(placa));
			Igual("con el veredicto del guion", Muestras.VeredictoEsperado, api.Rendimiento(placa).Veredicto);
		}

		private static void RepetirElTraspasoNoDejaEntrada(CargasApp hospedada, CargasAPI api, string sobre)
		{
			Titulo("repetir el traspaso -el enlace abierto dos veces- no duplica ni deja entrada");

			long antes = hospedada.Actor.CurrentEntryId;
			ResultadoDeTraspaso resultado = Traspaso.Aplicar(api, sobre);
			int total = Muestras.Kilometrajes.Length + Muestras.TanquesPorConfirmar.Length + 1 + 2;

			Igual("nada se aplico", "0", resultado.Aplicadas.ToString());
			Igual("todo se omitio", total.ToString(), resultado.Omitidas.ToString());
			Igual("nada espera ni se rechazo", "0", (resultado.Esperando + resultado.Rechazadas).ToString());
			Igual("y el journal quedo donde estaba", antes.ToString(), hospedada.Actor.CurrentEntryId.ToString());
			Igual("el traspaso ya no esta pendiente", "False", resultado.Pendiente.ToString());
		}

		private static void UnSobreAjenoORotoNoSeAsienta(CargasApp hospedada, CargasAPI api)
		{
			Titulo("un sobre de otro modulo, o roto, se dice y no se asienta");

			long antes = hospedada.Actor.CurrentEntryId;

			ResultadoDeTraspaso ajeno = Traspaso.Aplicar(api,
				"{\"version\":1,\"modulo\":\"examenes\",\"aserciones\":{\"sexoDeclarado\":\"femenino\"}}");
			Igual("el ajeno se rechaza entero", "1", ajeno.Rechazadas.ToString());
			Igual("y dice por que", "True", (ajeno.Motivos.Count == 1 && ajeno.Motivos[0].Contains("examenes")).ToString());

			ResultadoDeTraspaso roto = Traspaso.Aplicar(api, "esto no es un sobre");
			Igual("el roto se rechaza sin reventar", "1", roto.Rechazadas.ToString());

			ResultadoDeTraspaso vacio = Traspaso.Aplicar(api, "");
			Igual("el vacio no hace nada", "0", (vacio.Aplicadas + vacio.Rechazadas + vacio.Esperando).ToString());

			Igual("y ninguno toco el journal", antes.ToString(), hospedada.Actor.CurrentEntryId.ToString());
		}

		// ------------------------------------------------------------------ el arnes

		private static CargasApp Abrir(string carpeta)
		{
			Directory.CreateDirectory(carpeta);
			var app = new CargasApp("cargas");
			app.ConfigureStorage(DatabaseType.FileSystem, "path=" + carpeta);
			app.Start();
			return app;
		}

		// La narrativa de la estacion, por el mismo camino que el refresh.
		private static void Historia(CargasAPI api)
		{
			IDiarioDeLaApp diario = api;
			foreach (SenalDeEvento senal in Muestras.LaHistoria())
			{
				IMensajeDeEvento mensaje = RutaDeCargas.Rutear(senal);
				if (mensaje != null) diario.Aplicar(mensaje);
			}
		}

		// Los aportes de Sonia, los mismos que siembra `medir`.
		private static void Aportes(CargasAPI api)
		{
			foreach ((string transaccion, decimal kilometros) in Muestras.Kilometrajes)
				api.AnotarOdometro(transaccion, kilometros, "km", "observado");
			foreach (string transaccion in Muestras.TanquesPorConfirmar)
				api.ConfirmarTanque(transaccion);

			var ficha = Muestras.FichaDelDemo;
			api.CompletarFicha(Muestras.PlacaDelDemo, ficha.alias, ficha.marca, ficha.modelo,
				ficha.anio, ficha.color, ficha.rendimientoDeFabrica);
		}

		private static void Titulo(string texto)
		{
			Console.WriteLine();
			Console.WriteLine($"-- {texto} --");
		}

		private static void Igual(string titulo, string esperado, string obtenido)
		{
			corridas++;
			bool ok = esperado == obtenido;
			if (!ok) fallas++;
			Console.WriteLine($"  [{(ok ? "OK  " : "FALLA")}] {titulo}");
			if (!ok) Console.WriteLine($"           esperado=[{esperado}] obtenido=[{obtenido}]");
		}
	}
}
