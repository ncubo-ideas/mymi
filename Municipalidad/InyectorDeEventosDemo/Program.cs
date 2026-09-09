using System.Net.Http.Json;
using System.Text.Json;

namespace Municipalidad.InyectorDeEventosDemo
{
	/// <summary>
	/// La herramienta del operador: pone hechos en el feed simulado y dice que prueba cada uno.
	/// <para>
	/// NO NOMBRA NI UN TIPO DEL DOMINIO NI DE LA PUERTA, y no los referencia. Escribe el tag y los
	/// campos a mano, como los escribiria el portal, asi que es un lector INDEPENDIENTE del contrato:
	/// si un tag o un nombre de campo se despega, esto lo ve y una prueba que construyera el mensaje
	/// con las clases de la app no.
	/// </para>
	/// <para>
	/// NINGUN INSTANTE SALE DE UN RELOJ. Cada fecha es un literal, porque lo que guiona una secuencia
	/// recibida tiene que poder reproducirla.
	/// </para>
	/// </summary>
	public static class Program
	{
		private const string ElDiaDeLaConsulta = "2025-09-03T00:00:00Z";

		private static HttpClient http;
		private static string recurso;
		private static string id;

		public static async Task<int> Main(string[] args)
		{
			string url = Argumento(args, "--url", "http://localhost:8121/");
			recurso = Argumento(args, "--recurso", "contribuyente");
			id = Argumento(args, "--id", "CEDULA-DEMO-1");

			string comando = args.Length > 0 && !args[0].StartsWith("--") ? args[0] : "ayuda";

			using (http = new HttpClient { BaseAddress = new Uri(url) })
			{
				try
				{
					switch (comando)
					{
						case "listar": await Listar(); break;
						case "ordenamiento": await Ordenamiento(); break;
						case "rechazos": await Rechazos(); break;
						case "alfabetos": await Alfabetos(); break;
						case "casos": await Casos(); break;
						default: Ayuda(); break;
					}

					return 0;
				}
				catch (Exception falla)
				{
					Console.Error.WriteLine("Error: " + falla.GetType().Name + ": " + falla.Message);
					Console.Error.WriteLine("  (esta arriba el endpoint en " + url + "?)");

					return 2;
				}
			}
		}

		private static void Ayuda()
		{
			Console.WriteLine("Municipalidad - el inyector de hechos del feed simulado");
			Console.WriteLine();
			Console.WriteLine("  listar         lo que el feed ya tiene, hecho por hecho");
			Console.WriteLine("  ordenamiento   el encabezado DESPUES de su linea, que es el hueco de D3");
			Console.WriteLine("  rechazos       los hechos que la cuenta refusa, cada uno por su motivo");
			Console.WriteLine("  alfabetos      el mismo numero de periodo como mes y como trimestre");
			Console.WriteLine("  casos          los tres de arriba, en orden");
			Console.WriteLine();
			Console.WriteLine("  --url <base>   por defecto http://localhost:8121/");
			Console.WriteLine("  --recurso <r>  por defecto contribuyente");
			Console.WriteLine("  --id <cedula>  por defecto CEDULA-DEMO-1");
			Console.WriteLine();
			Console.WriteLine("La historia del demo YA VIENE SEMBRADA en el endpoint. Estos comandos");
			Console.WriteLine("agregan los CASOS que esa historia no puede traer, porque son hechos que");
			Console.WriteLine("la cuenta tiene que refusar y una historia sembrada de rechazos no seria");
			Console.WriteLine("una historia.");
		}

		private static async Task Listar()
		{
			string credencial = await Credencial();

			using (HttpRequestMessage pedido = new HttpRequestMessage(
				HttpMethod.Get, recurso + "/" + Uri.EscapeDataString(id) + "/events?after=0"))
			{
				pedido.Headers.Authorization =
					new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credencial);

				using (HttpResponseMessage respuesta = await http.SendAsync(pedido))
				{
					respuesta.EnsureSuccessStatusCode();

					using (JsonDocument documento = JsonDocument.Parse(
						await respuesta.Content.ReadAsStringAsync()))
					{
						JsonElement eventos = documento.RootElement.GetProperty("events");

						Console.WriteLine(
							eventos.GetArrayLength() + " hechos en " + recurso + "/" + id + ":");

						foreach (JsonElement hecho in eventos.EnumerateArray())
							Console.WriteLine("  #" + hecho.GetProperty("id")
								+ "  " + hecho.GetProperty("tipo").GetString().PadRight(22)
								+ Resumen(hecho));
					}
				}
			}
		}

		/// <summary>
		/// EL HUECO DE D3, INYECTADO A PROPOSITO. Una linea cuyo encabezado no llego se refusa, no deja
		/// entrada, y no se reintenta -- asi que el encabezado llegando DESPUES no la rescata. Los dos
		/// hechos se mandan en ese orden para que el operador vea el resultado en vez de leerlo.
		/// </summary>
		private static async Task Ordenamiento()
		{
			Titulo("ordenamiento: el encabezado DESPUES de su linea");

			await Inyectar("LineaDePagoRegistrada", new
			{
				numeroDeRecibo = "R-DESORDENADO",
				nombreDelRubro = "recoleccion-de-basura",
				periodo = "2025.11",
				monto = 8000m,
				intereses = 0m,
			}, "la linea, SIN su encabezado. La cuenta la refusa: no deja entrada y nadie la reintenta");

			await Inyectar("PagoRegistrado", new
			{
				numeroDeRecibo = "R-DESORDENADO",
				fechaDelPago = "2025-11-30T00:00:00Z",
				descuento = 0m,
			}, "el encabezado, tarde. Entra -- y su linea NO vuelve: ya se paso de largo");

			Console.WriteLine();
			Console.WriteLine("  Lo que hay que ver despues del refresh: el recibo R-DESORDENADO existe y");
			Console.WriteLine("  no trae ninguna linea. El feed tiene que mandar el encabezado ANTES.");
		}

		/// <summary>
		/// Los hechos que la CUENTA refusa, uno por motivo. Ninguno deja entrada en el journal: el Check
		/// los niega antes de escribir, que es lo que hace que un feed ruidoso no cicatrice la cuenta de
		/// una persona.
		/// </summary>
		private static async Task Rechazos()
		{
			Titulo("rechazos: cada uno por su motivo, y ninguno deja entrada");

			await Inyectar("FincaIncorporada", new { numeroDeFinca = "F-1" },
				"la finca F-1 otra vez: ya esta");

			await Inyectar("PagoRegistrado", new
			{
				numeroDeRecibo = "R-1",
				fechaDelPago = "2024-03-31T00:00:00Z",
				descuento = 0m,
			}, "el recibo R-1 otra vez: el numero de recibo es la identidad");

			await Inyectar("LineaDePagoRegistrada", new
			{
				numeroDeRecibo = "R-1",
				nombreDelRubro = "cementerio",
				periodo = "2024.03",
				monto = 1000m,
				intereses = 0m,
			}, "un rubro que el catalogo no tiene");

			await Inyectar("LineaDePagoRegistrada", new
			{
				numeroDeRecibo = "R-1",
				nombreDelRubro = "bienes-inmuebles",
				periodo = "2024.05",
				monto = 20000m,
				intereses = 0m,
			}, "un quinto trimestre: bien escrito, y la cadencia del rubro no lo lee");

			await Inyectar("CobroPuesto", new
			{
				nombreDelRubro = "recoleccion-de-basura",
				periodo = "2025.09",
				monto = 24000m,
				puestoEl = "2025-10-01T00:00:00Z",
			}, "al cobro un periodo que el pendiente de la consulta ya puso");

			Console.WriteLine();
			Console.WriteLine("  Lo que hay que ver despues del refresh: rechazados sube, aplicados no, y");
			Console.WriteLine("  el arbol del journal queda byte a byte igual.");
		}

		/// <summary>
		/// LOS DOS ALFABETOS, sobre el mismo numero. `2026.02` es FEBRERO para un rubro mensual y el
		/// SEGUNDO TRIMESTRE para uno que se cobra por trimestre: el texto es del recibo y quien decide
		/// que significa es la cadencia del rubro, no el que lo escribio.
		/// </summary>
		private static async Task Alfabetos()
		{
			Titulo("alfabetos: el mismo 2026.02 como mes y como trimestre");

			await Inyectar("PagoRegistrado", new
			{
				numeroDeRecibo = "R-ALFABETOS",
				fechaDelPago = "2026-06-30T00:00:00Z",
				descuento = 0m,
			}, "el encabezado, primero");

			await Inyectar("LineaDePagoRegistrada", new
			{
				numeroDeRecibo = "R-ALFABETOS",
				nombreDelRubro = "recoleccion-de-basura",
				periodo = "2026.02",
				monto = 8000m,
				intereses = 0m,
			}, "para basura, 2026.02 es FEBRERO");

			await Inyectar("LineaDePagoRegistrada", new
			{
				numeroDeRecibo = "R-ALFABETOS",
				nombreDelRubro = "bienes-inmuebles",
				periodo = "2026.02",
				monto = 36000m,
				intereses = 0m,
			}, "para bienes inmuebles, el MISMO texto es el segundo trimestre");

			Console.WriteLine();
			Console.WriteLine("  Lo que hay que ver despues del refresh: las dos lineas entran, y el");
			Console.WriteLine("  historial de cada rubro dice un dia de vencimiento distinto para el");
			Console.WriteLine("  mismo texto de periodo.");
		}

		private static async Task Casos()
		{
			await Alfabetos();
			Console.WriteLine();
			await Ordenamiento();
			Console.WriteLine();
			await Rechazos();
		}

		private static async Task Inyectar(string tipo, object campos, string queDemuestra)
		{
			using (HttpResponseMessage respuesta = await http.PostAsJsonAsync(
				recurso + "/" + Uri.EscapeDataString(id) + "/events/hecho", new { tipo, campos }))
			{
				string cuerpo = await respuesta.Content.ReadAsStringAsync();

				if (!respuesta.IsSuccessStatusCode)
				{
					Console.WriteLine("  FALLO  " + tipo + "  " + cuerpo);
					return;
				}

				using (JsonDocument documento = JsonDocument.Parse(cuerpo))
					Console.WriteLine("  #" + documento.RootElement.GetProperty("agregado")
						+ "  " + tipo.PadRight(22) + queDemuestra);
			}
		}

		private static async Task<string> Credencial()
		{
			using (HttpResponseMessage respuesta = await http.PostAsJsonAsync(
				"device/token", new { deviceKey = "INYECTOR-DEMO", recurso, id }))
			{
				respuesta.EnsureSuccessStatusCode();

				using (JsonDocument documento = JsonDocument.Parse(
					await respuesta.Content.ReadAsStringAsync()))
					return documento.RootElement.GetProperty("accessToken").GetString();
			}
		}

		private static string Resumen(JsonElement hecho)
		{
			List<string> dicho = new List<string>();

			foreach (JsonProperty campo in hecho.EnumerateObject())
			{
				if (campo.Name == "id" || campo.Name == "tipo") continue;

				dicho.Add(campo.Name + "=" + (campo.Value.ValueKind == JsonValueKind.String
					? campo.Value.GetString()
					: campo.Value.GetRawText()));
			}

			return string.Join("  ", dicho);
		}

		private static void Titulo(string texto)
		{
			Console.WriteLine(texto);
			Console.WriteLine(new string('-', texto.Length));
		}

		private static string Argumento(string[] args, string nombre, string porDefecto)
		{
			for (int cual = 0; cual < args.Length - 1; cual++)
				if (string.Equals(args[cual], nombre, StringComparison.OrdinalIgnoreCase))
					return args[cual + 1];

			return porDefecto;
		}
	}
}
