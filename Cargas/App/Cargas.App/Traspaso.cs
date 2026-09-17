using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Cargas.App.Vistas;

namespace Cargas.App
{
	// EL TRASPASO UNICO de lo que Sonia anoto ella misma (especificacion 13 §5).
	//
	// Cuando la app suelta le pasa la mano a Gnothi, el journal NO se mueve: el modulo
	// hospedado nace con cursor 0 y vuelve a pedirle a la estacion todo lo que la estacion
	// publico. Lo unico que cruza, una vez, es lo que la estacion NO sabe porque lo afirmo
	// la persona: los kilometrajes, los tanques que declaro o confirmo, la ficha del carro y
	// en que unidades lo mira. Es exactamente la mitad de cada carga que hace posible el
	// rendimiento, y por eso en Cargas este traspaso pesa mas que en cualquier otro modulo:
	// sin el, la app hospedada solo tendria facturas de combustible.
	//
	// LO QUE NO VIAJA, a proposito:
	//   - los hechos de la estacion (cargas, puntos, niveles, sorteos): se re-piden;
	//   - la credencial (device key, tokens): es contexto de ESTA instalacion, y Gnothi
	//     obtiene la suya propia (13 §3);
	//   - la foto del tablero: el journal guarda solo el NOMBRE y el binario vive en la
	//     carpeta de la persona. Pasar el nombre sin el archivo seria mentir "tiene foto" en
	//     un modulo que no la tiene. Se queda, y la pantalla lo dice.
	//
	// EL SOBRE es JSON legible, con version, el modulo al que pertenece y el publicador del
	// que vienen las cargas: Gnothi usa el publicador para deducir el modulo cuando llega por
	// la hoja de compartir sin que nadie lo nombre (13 §5.2).
	//
	// LA IDEMPOTENCIA ESTA ACA y no en los Check del dominio, igual que en Examenes. Volver
	// a anotar o volver a elegir es legitimo cuando lo hace la persona -cada declaracion es
	// un hecho fechado-, pero un traspaso repetido porque el enlace se abrio dos veces no es
	// la persona corrigiendose: si el valor ya esta, no se asienta nada (omitida).
	//
	// LO QUE ESPERA. El Host entrega el traspaso apenas el modulo arranca, y si Gnothi
	// todavia no consiguio su credencial las cargas aun no llegaron: cada Check las
	// rechazaria. Una anotacion cuya carga no esta no es un rechazo, esta ESPERANDO, y el
	// sobre se vuelve a aplicar despues de cada refresh hasta que nada espere. Repetirlo es
	// inocuo por lo de arriba.
	public static class Traspaso
	{
		public const int Version = 1;
		public const string Modulo = "cargas";

		/// <summary>
		/// EN QUE IDIOMA LE PIDE ESTE TRASPASO LA LISTA DE CARGAS, y da igual cual sea: el sobre
		/// lleva PIEZAS -- el numero, su unidad, su procedencia -- y ninguna frase redactada. Al
		/// journal no le entra texto renderizado: si una frase ya dicha se persistiera, cambiar el
		/// idioma reescribiria la historia. Se nombra el del registro para no fingir una eleccion.
		/// </summary>
		private const string ElIdiomaDeLasPiezas = Frases.Frases.IdiomaDelRegistro;

		// ------------------------------------------------------------------ resumir

		/// <summary>Cuanto hay para pasar, dicho en piezas para que la pantalla lo enuncie
		/// antes de que la persona toque el boton. Cero en todo = no hay nada que pasar.</summary>
		public static ResumenDeTraspaso Resumir(CargasAPI api)
		{
			ArgumentNullException.ThrowIfNull(api);

			var resumen = new ResumenDeTraspaso();
			foreach (VehiculoEnLista vehiculo in api.Vehiculos())
			{
				if (vehiculo.TieneFicha) resumen.Fichas++;
				foreach (CargaEnLista carga in api.Cargas(vehiculo.Placa, ElIdiomaDeLasPiezas))
				{
					if (carga.TieneKilometraje) resumen.Kilometrajes++;
					if (carga.TanqueDeclarado) resumen.Tanques++;
					if (carga.TieneFoto) resumen.FotosQueSeQuedan++;
				}
			}
			return resumen;
		}

		// -------------------------------------------------------------------- armar

		/// <summary>El sobre entero, listo para viajar por el enlace o por la hoja de
		/// compartir. Solo aserciones de la persona; ni hechos de la estacion ni credencial.</summary>
		public static string Armar(CargasAPI api, string publicador, DateTime fecha)
		{
			ArgumentNullException.ThrowIfNull(api);

			UnidadesElegidas unidades = api.Unidades();

			var vehiculos = new List<object>();
			var cargas = new List<object>();
			foreach (VehiculoEnLista vehiculo in api.Vehiculos())
			{
				if (vehiculo.TieneFicha)
					vehiculos.Add(new
					{
						placa = vehiculo.Placa,
						ficha = new
						{
							alias = vehiculo.Alias,
							marca = vehiculo.Marca,
							modelo = vehiculo.Modelo,
							anio = vehiculo.Anio,
							color = vehiculo.Color,
							rendimientoDeFabrica = vehiculo.RendimientoDeFabrica,
						},
					});

				foreach (CargaEnLista carga in api.Cargas(vehiculo.Placa, ElIdiomaDeLasPiezas))
				{
					if (!carga.TieneKilometraje && !carga.TanqueDeclarado) continue;

					cargas.Add(new
					{
						transaccion = carga.Transaccion,
						placa = vehiculo.Placa,
						fecha = carga.Fecha.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
						kilometraje = carga.TieneKilometraje
							? new { valor = carga.Kilometros, unidad = carga.UnidadDelKilometraje, origen = carga.OrigenDelKilometraje }
							: null,
						tanque = carga.TanqueDeclarado
							? new { estado = carga.EstadoDelTanque, origen = carga.OrigenDelTanque }
							: null,
					});
				}
			}

			return JsonSerializer.Serialize(new
			{
				version = Version,
				modulo = Modulo,
				publicador = publicador ?? "",
				fecha = fecha.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
				aserciones = new
				{
					preferencias = new { rendimiento = unidades.Rendimiento, distancia = unidades.Distancia },
					vehiculos,
					cargas,
				},
			}, Legible);
		}

		private static readonly JsonSerializerOptions Legible = new JsonSerializerOptions
		{
			WriteIndented = true,
			DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
		};

		// ------------------------------------------------------------------- aplicar

		/// <summary>Asienta en este actor lo que el sobre trae. Nunca tira por el contenido
		/// del sobre: un sobre roto, ajeno o repetido se DICE en el resultado.</summary>
		public static ResultadoDeTraspaso Aplicar(CargasAPI api, string sobreJson)
		{
			ArgumentNullException.ThrowIfNull(api);

			var resultado = new ResultadoDeTraspaso();
			if (string.IsNullOrWhiteSpace(sobreJson)) return resultado;

			JsonDocument documento;
			try { documento = JsonDocument.Parse(sobreJson); }
			catch (JsonException ex)
			{
				return resultado.Rechazar($"el sobre no es JSON: {ex.Message}");
			}

			using (documento)
			{
				JsonElement raiz = documento.RootElement;
				if (raiz.ValueKind != JsonValueKind.Object)
					return resultado.Rechazar("el sobre no tiene la forma esperada");

				// UN SOBRE AJENO NO SE ASIENTA. Si nombra un modulo y no es este, asentar
				// las aserciones de otra institucion aca seria peor que no asentarlas.
				string modulo = Texto(raiz, "modulo");
				if (modulo.Length > 0 && !string.Equals(modulo, Modulo, StringComparison.OrdinalIgnoreCase))
					return resultado.Rechazar($"el sobre es del modulo '{modulo}', no de cargas");

				if (!raiz.TryGetProperty("aserciones", out JsonElement aserciones)
					|| aserciones.ValueKind != JsonValueKind.Object)
					return resultado;

				AplicarPreferencias(api, aserciones, resultado);
				AplicarFichas(api, aserciones, resultado);
				AplicarCargas(api, aserciones, resultado);
			}

			return resultado;
		}

		private static void AplicarPreferencias(CargasAPI api, JsonElement aserciones, ResultadoDeTraspaso resultado)
		{
			if (!aserciones.TryGetProperty("preferencias", out JsonElement preferencias)
				|| preferencias.ValueKind != JsonValueKind.Object) return;

			UnidadesElegidas actuales = api.Unidades();

			string rendimiento = Texto(preferencias, "rendimiento");
			if (rendimiento.Length > 0)
			{
				if (rendimiento == actuales.Rendimiento) resultado.Omitidas++;
				else resultado.Anotar(() => api.ElegirPresentacionDeRendimiento(rendimiento), "preferencia de rendimiento");
			}

			string distancia = Texto(preferencias, "distancia");
			if (distancia.Length > 0)
			{
				if (distancia == actuales.Distancia) resultado.Omitidas++;
				else resultado.Anotar(() => api.ElegirUnidadDeDistancia(distancia), "preferencia de distancia");
			}
		}

		private static void AplicarFichas(CargasAPI api, JsonElement aserciones, ResultadoDeTraspaso resultado)
		{
			if (!aserciones.TryGetProperty("vehiculos", out JsonElement vehiculos)
				|| vehiculos.ValueKind != JsonValueKind.Array) return;

			var conocidos = new Dictionary<string, VehiculoEnLista>(StringComparer.OrdinalIgnoreCase);
			foreach (VehiculoEnLista v in api.Vehiculos()) conocidos[v.Placa] = v;

			foreach (JsonElement vehiculo in vehiculos.EnumerateArray())
			{
				string placa = Texto(vehiculo, "placa");
				if (placa.Length == 0
					|| !vehiculo.TryGetProperty("ficha", out JsonElement ficha)
					|| ficha.ValueKind != JsonValueKind.Object) continue;

				// La placa la trae la estacion con la primera carga: hasta que llegue, la
				// ficha no tiene vehiculo que completar. Espera, no falla.
				if (!conocidos.TryGetValue(placa, out VehiculoEnLista actual))
				{
					resultado.Esperando++;
					continue;
				}

				string alias = Texto(ficha, "alias");
				string marca = Texto(ficha, "marca");
				string modelo = Texto(ficha, "modelo");
				int anio = Entero(ficha, "anio");
				string color = Texto(ficha, "color");
				decimal rendimientoDeFabrica = Decimal(ficha, "rendimientoDeFabrica");

				bool yaEsta = actual.TieneFicha
					&& actual.Alias == alias && actual.Marca == marca && actual.Modelo == modelo
					&& actual.Anio == anio && actual.Color == color
					&& actual.RendimientoDeFabrica == rendimientoDeFabrica;

				if (yaEsta) { resultado.Omitidas++; continue; }
				if (alias.Length == 0) { resultado.Rechazar($"la ficha de {placa} viene sin alias"); continue; }

				resultado.Anotar(
					() => api.CompletarFicha(placa, alias, marca, modelo, anio, color, rendimientoDeFabrica),
					$"la ficha de {placa}");
			}
		}

		private static void AplicarCargas(CargasAPI api, JsonElement aserciones, ResultadoDeTraspaso resultado)
		{
			if (!aserciones.TryGetProperty("cargas", out JsonElement cargas)
				|| cargas.ValueKind != JsonValueKind.Array) return;

			// EN ORDEN DE FECHA. La guarda del kilometraje exige coherencia con las cargas
			// vecinas, asi que anotar la carga 5 antes que la 3 podria rechazar la 3 sin
			// razon. El sobre trae la fecha de cada carga justamente para esto.
			var ordenadas = new List<JsonElement>();
			foreach (JsonElement carga in cargas.EnumerateArray()) ordenadas.Add(carga);
			ordenadas.Sort((a, b) => string.CompareOrdinal(Texto(a, "fecha"), Texto(b, "fecha")));

			var porPlaca = new Dictionary<string, Dictionary<string, CargaEnLista>>(StringComparer.OrdinalIgnoreCase);
			var placasConocidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (VehiculoEnLista v in api.Vehiculos()) placasConocidas.Add(v.Placa);

			foreach (JsonElement carga in ordenadas)
			{
				string transaccion = Texto(carga, "transaccion");
				string placa = Texto(carga, "placa");
				bool traeKilometraje = carga.TryGetProperty("kilometraje", out JsonElement kilometraje)
					&& kilometraje.ValueKind == JsonValueKind.Object;
				bool traeTanque = carga.TryGetProperty("tanque", out JsonElement tanque)
					&& tanque.ValueKind == JsonValueKind.Object;
				if (transaccion.Length == 0 || (!traeKilometraje && !traeTanque)) continue;

				// La carga todavia no llego de la estacion: sus anotaciones esperan.
				CargaEnLista actual = null;
				if (placa.Length > 0 && placasConocidas.Contains(placa))
				{
					if (!porPlaca.TryGetValue(placa, out Dictionary<string, CargaEnLista> deLaPlaca))
					{
						deLaPlaca = new Dictionary<string, CargaEnLista>(StringComparer.Ordinal);
						foreach (CargaEnLista c in api.Cargas(placa, ElIdiomaDeLasPiezas)) deLaPlaca[c.Transaccion] = c;
						porPlaca[placa] = deLaPlaca;
					}
					deLaPlaca.TryGetValue(transaccion, out actual);
				}
				if (actual is null)
				{
					if (traeKilometraje) resultado.Esperando++;
					if (traeTanque) resultado.Esperando++;
					continue;
				}

				if (traeKilometraje)
				{
					decimal valor = Decimal(kilometraje, "valor");
					string unidad = Texto(kilometraje, "unidad");
					string origen = Texto(kilometraje, "origen");

					bool yaEsta = actual.TieneKilometraje && actual.Kilometros == valor
						&& actual.UnidadDelKilometraje == unidad && actual.OrigenDelKilometraje == origen;

					if (yaEsta) resultado.Omitidas++;
					else if (valor <= 0m || unidad.Length == 0 || origen.Length == 0)
						resultado.Rechazar($"el kilometraje de {transaccion} viene incompleto");
					else
						resultado.Anotar(() => api.AnotarOdometro(transaccion, valor, unidad, origen),
							$"el kilometraje de {transaccion}");
				}

				if (traeTanque)
				{
					string estado = Texto(tanque, "estado");
					string origen = Texto(tanque, "origen");

					// CONFIRMADO es el visto bueno a lo que la estacion infirio: aca se
					// vuelve a dar el visto bueno a la MISMA inferencia, que el modulo nuevo
					// rehizo solo al recibir la carga. OBSERVADO es lo que ella dijo por
					// encima de la inferencia, y se vuelve a decir.
					if (origen == "confirmado")
					{
						if (actual.TanqueDeclarado && actual.OrigenDelTanque == "confirmado")
							resultado.Omitidas++;
						else
							resultado.Anotar(() => api.ConfirmarTanque(transaccion), $"el tanque de {transaccion}");
					}
					else if (origen == "observado" && estado.Length > 0)
					{
						if (actual.TanqueDeclarado && actual.OrigenDelTanque == "observado" && actual.EstadoDelTanque == estado)
							resultado.Omitidas++;
						else
							resultado.Anotar(() => api.DeclararTanque(transaccion, estado), $"el tanque de {transaccion}");
					}
					else
					{
						resultado.Rechazar($"el tanque de {transaccion} trae un origen que no es de la persona ('{origen}')");
					}
				}
			}
		}

		// ---------------------------------------------------------------- lectura

		private static string Texto(JsonElement padre, string nombre) =>
			padre.TryGetProperty(nombre, out JsonElement e) && e.ValueKind == JsonValueKind.String
				? e.GetString() : "";

		private static int Entero(JsonElement padre, string nombre) =>
			padre.TryGetProperty(nombre, out JsonElement e) && e.TryGetInt32(out int valor) ? valor : 0;

		private static decimal Decimal(JsonElement padre, string nombre) =>
			padre.TryGetProperty(nombre, out JsonElement e) && e.TryGetDecimal(out decimal valor) ? valor : 0m;
	}

	/// <summary>Cuanto hay para pasar, en piezas. Las fotos se cuentan aparte porque NO
	/// viajan y la pantalla tiene que decirlo.</summary>
	public sealed class ResumenDeTraspaso
	{
		public int Kilometrajes { get; set; }
		public int Tanques { get; set; }
		public int Fichas { get; set; }
		public int FotosQueSeQuedan { get; set; }

		public bool HayAlgoQuePasar => Kilometrajes + Tanques + Fichas > 0;

		public override string ToString() =>
			$"kilometrajes={Kilometrajes} tanques={Tanques} fichas={Fichas} fotosQueSeQuedan={FotosQueSeQuedan}";
	}

	/// <summary>Que paso con el traspaso, en numeros. "Omitidas" no es un fallo: esa
	/// asercion ya estaba, y repetir el traspaso no ensucio el journal. "Esperando" tampoco:
	/// la carga todavia no llego de la estacion y se vuelve a intentar tras el refresh.</summary>
	public sealed class ResultadoDeTraspaso
	{
		public int Aplicadas { get; set; }
		public int Omitidas { get; set; }
		public int Esperando { get; set; }
		public int Rechazadas { get; set; }

		// Los motivos de los rechazos, para poder decirlos. Acotados: un sobre con
		// cien rechazos no necesita cien lineas para entenderse.
		public List<string> Motivos { get; } = new List<string>();

		/// <summary>Si hay anotaciones cuya carga aun no llego: el sobre se guarda y se
		/// vuelve a aplicar despues del proximo refresh.</summary>
		public bool Pendiente => Esperando > 0;

		internal ResultadoDeTraspaso Rechazar(string motivo)
		{
			Rechazadas++;
			if (Motivos.Count < 5) Motivos.Add(motivo);
			return this;
		}

		// Un comando del actor: "" si entro, el mensaje del Check si no. Y si el dominio
		// revienta -un simbolo que no reconoce- tampoco se propaga: el traspaso sigue con
		// la siguiente asercion y este se cuenta como rechazado con su motivo.
		internal void Anotar(Func<string> comando, string que)
		{
			try
			{
				string rechazo = comando();
				if (rechazo.Length == 0) Aplicadas++;
				else Rechazar($"{que}: {rechazo}");
			}
			catch (Exception ex)
			{
				Rechazar($"{que}: {ex.Message}");
			}
		}

		public override string ToString() =>
			$"aplicadas={Aplicadas} omitidas={Omitidas} esperando={Esperando} rechazadas={Rechazadas}";
	}
}
