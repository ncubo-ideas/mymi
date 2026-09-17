using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Cargas.App.Vistas;
using LaRedaccion = Cargas.App.Frases.LaRedaccion;
using LasFrases = Cargas.App.Frases.Frases;

namespace Cargas.App
{
	// Convierte la emision del print en los records de vista.
	//
	// Esto NO es el antipatron de "parsear el print para sacar un valor tipado". Ese
	// antipatron es re-derivar en C# un ESCALAR que el actor ya calculo, y para eso la
	// app usa parametros Out tipados (CantidadDeCargas, UltimoEventoProcesado). Lo de
	// aca es otra cosa: una VISTA, y el print con formatter JSON es su canal declarado.
	//
	// El formato de fecha es el del formatter: MM/dd/yyyy HH:mm:ss en cultura
	// invariante.
	internal static class ProyeccionDeVistas
	{
		private const string FormatoDeFecha = "MM/dd/yyyy HH:mm:ss";
		private const string FormatoDeFechaSinHora = "MM/dd/yyyy";

		internal static IReadOnlyList<VehiculoEnLista> Vehiculos(string json)
		{
			var vehiculos = new List<VehiculoEnLista>();
			foreach (JsonElement elemento in Arreglo(json, "vehiculo"))
				vehiculos.Add(new VehiculoEnLista
				{
					Placa = Texto(elemento, "placa"),
					Nombre = Texto(elemento, "nombre"),
					TieneFicha = Booleano(elemento, "tieneFicha"),
					Cargas = Entero(elemento, "cargas"),
					Alias = Texto(elemento, "alias"),
					Marca = Texto(elemento, "marca"),
					Modelo = Texto(elemento, "modelo"),
					Anio = Entero(elemento, "anio"),
					Color = Texto(elemento, "color"),
					RendimientoDeFabrica = Decimal(elemento, "rendimientoDeFabrica"),
				});
			return vehiculos;
		}

		internal static IReadOnlyList<CargaEnLista> Cargas(string json, string idioma)
		{
			LasFrases frases = LasFrases.De(idioma);
			var cargas = new List<CargaEnLista>();
			foreach (JsonElement elemento in Arreglo(json, "carga"))
				cargas.Add(new CargaEnLista
				{
					Fecha = Fecha(elemento, "fecha"),
					Estacion = Texto(elemento, "estacion"),
					Surtidor = Texto(elemento, "surtidor"),
					Tipo = Texto(elemento, "tipo"),
					Litros = Decimal(elemento, "litros"),
					Volumen = frases.ConUnidad(Decimal(elemento, "volumenValor"),
						Texto(elemento, "volumenUnidad")),
					Precio = Decimal(elemento, "precio"),
					Total = Decimal(elemento, "total"),
					Moneda = Texto(elemento, "moneda"),
					Corte = Texto(elemento, "corte"),
					Kilometraje = LaRedaccion.Kilometraje(frases, Decimal(elemento, "kilometros"),
						Texto(elemento, "unidad"), Texto(elemento, "origenDelKilometraje")),
					KilometrajeAnotado = Booleano(elemento, "tieneKilometraje")
						? frases.ConUnidadEntera(Decimal(elemento, "kilometros"), Texto(elemento, "unidad"))
						: "",
					OrigenDelKilometraje = Texto(elemento, "origenDelKilometraje"),
					Tanque = LaRedaccion.Tanque(frases, Texto(elemento, "claveDelTanque"),
						Texto(elemento, "origenDelTanqueDicho")),
					ClaveDelTanque = Texto(elemento, "claveDelTanque"),
					Transaccion = Texto(elemento, "transaccion"),
					CuentaParaCalcular = Booleano(elemento, "cuentaParaCalcular"),
				TieneKilometraje = Booleano(elemento, "tieneKilometraje"),
					TieneFoto = Booleano(elemento, "tieneFoto"),
					Foto = Texto(elemento, "foto"),
					Kilometros = Decimal(elemento, "kilometros"),
					UnidadDelKilometraje = Texto(elemento, "unidad"),
					TanqueDeclarado = Booleano(elemento, "tanqueDeclarado"),
					EstadoDelTanque = Texto(elemento, "estadoDelTanque"),
					OrigenDelTanque = Texto(elemento, "origenDelTanque"),
				});
			return cargas;
		}

		internal static IReadOnlyList<TramoEnLista> Tramos(string json, string idioma)
		{
			LasFrases frases = LasFrases.De(idioma);
			var tramos = new List<TramoEnLista>();
			foreach (JsonElement elemento in Arreglo(json, "tramo"))
				tramos.Add(new TramoEnLista
				{
					Desde = Fecha(elemento, "desde"),
					Hasta = Fecha(elemento, "hasta"),
					EstaCalculado = Booleano(elemento, "estaCalculado"),
					Kilometros = Decimal(elemento, "kilometros"),
					Litros = Decimal(elemento, "litros"),
					Rendimiento = Decimal(elemento, "rendimiento"),
					EsComparable = Booleano(elemento, "esComparable"),
					MotivoDeReserva = LaRedaccion.Reserva(frases,
						Texto(elemento, "motivoDeReserva")),
					EsSospechoso = Booleano(elemento, "esSospechoso"),
					FechaDeLaCargaTrunca = Fecha(elemento, "fechaDeLaCargaTrunca"),
					QueFalta = Booleano(elemento, "estaCalculado")
						? ""
						: frases.Diga("falta.kilometraje-de-una-carga",
							("fecha", frases.Dia(Fecha(elemento, "fechaDeLaCargaTrunca")))),
					Expresado = LaRedaccion.Tramo(frases, Booleano(elemento, "estaCalculado"),
						Decimal(elemento, "kilometros"), Decimal(elemento, "litros"),
						Decimal(elemento, "rendimiento"), Fecha(elemento, "fechaDeLaCargaTrunca")),
					RendimientoComoSeVe = Decimal(elemento, "rendimientoComoSeVe"),
					KilometrosComoSeVen = Decimal(elemento, "kilometrosComoSeVen"),
					SimboloDelRendimiento = Texto(elemento, "simboloDelRendimiento"),
					SimboloDeLaDistancia = Texto(elemento, "simboloDeLaDistancia"),
				});
			return tramos;
		}

		internal static IReadOnlyList<SorteoEnLista> Sorteos(string json, string idioma)
		{
			LasFrases frases = LasFrases.De(idioma);
			var sorteos = new List<SorteoEnLista>();
			foreach (JsonElement elemento in Arreglo(json, "sorteo"))
				sorteos.Add(new SorteoEnLista
				{
					Codigo = Texto(elemento, "codigo"),
					Nombre = Texto(elemento, "nombre"),
					Premio = Texto(elemento, "premio"),
					FueAnunciado = Booleano(elemento, "fueAnunciado"),
					FechaDelSorteo = Fecha(elemento, "fechaDelSorteo"),
					Boletos = Entero(elemento, "boletos"),
					EstaVencido = Booleano(elemento, "estaVencido"),
					DiasParaElSorteo = Entero(elemento, "diasParaElSorteo"),
					MontoPorBoleto = Booleano(elemento, "fueAnunciado")
						? frases.Monto(Decimal(elemento, "montoPorBoleto"),
							Texto(elemento, "monedaPorBoleto"))
						: "",
					ParaSonia = LaRedaccion.Sorteo(frases, Texto(elemento, "clave"),
						Entero(elemento, "boletos"), Fecha(elemento, "fechaDelSorteo"),
						Entero(elemento, "diasParaElSorteo")),
				});
			return sorteos;
		}

		internal static IReadOnlyList<BoletosDeLaCargaEnLista> BoletosDeLasCargas(string json,
			string idioma)
		{
			LasFrases frases = LasFrases.De(idioma);
			var filas = new List<BoletosDeLaCargaEnLista>();
			foreach (JsonElement elemento in Arreglo(json, "explicacion"))
				filas.Add(new BoletosDeLaCargaEnLista
				{
					Fecha = Fecha(elemento, "fecha"),
					Transaccion = Texto(elemento, "transaccion"),
					Sorteo = Texto(elemento, "sorteo"),
					NombreDelSorteo = Texto(elemento, "nombreDelSorteo"),
					HayRegla = Booleano(elemento, "hayRegla"),
					QueDaLaRegla = Entero(elemento, "queDaLaRegla"),
					Emitidos = Entero(elemento, "emitidos"),
					Sobrante = frases.Monto(Decimal(elemento, "sobrante"),
						Texto(elemento, "moneda")),
					Cuadra = Booleano(elemento, "cuadra"),
					ProbablementeFaltaUnDato = Booleano(elemento, "probablementeFaltaUnDato"),
					ParaSonia = LaRedaccion.BoletosDeLaCarga(frases, Texto(elemento, "clave"),
						Entero(elemento, "emitidos"), Entero(elemento, "queDaLaRegla"),
						Decimal(elemento, "sobrante"), Texto(elemento, "moneda"),
						Texto(elemento, "monedaDeLaRegla")),
				});
			return filas;
		}

		internal static UnidadesElegidas Unidades(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new UnidadesElegidas();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			return new UnidadesElegidas
			{
				Rendimiento = Texto(raiz, "rendimiento"),
				Distancia = Texto(raiz, "distancia"),
			};
		}

		internal static RendimientoDelVehiculo Rendimiento(string json, string idioma)
		{
			if (string.IsNullOrWhiteSpace(json)) return new RendimientoDelVehiculo();

			LasFrases frases = LasFrases.De(idioma);
			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			return new RendimientoDelVehiculo
			{
				Veredicto = Texto(raiz, "veredicto"),
				Severidad = Texto(raiz, "severidad"),
				ParaSonia = LaRedaccion.Lectura(frases, Texto(raiz, "veredicto"),
					Texto(raiz, "simbolo"), Decimal(raiz, "actualComoSeVe"),
					Decimal(raiz, "habitualComoSeVe"), Texto(raiz, "claveDeLoQueFalta"),
					Fecha(raiz, "fechaDeLaCargaTrunca"), Fecha(raiz, "fechaDelTotalQueNoCuadra"),
					Texto(raiz, "motivoDeReserva"), Texto(raiz, "claveDelCambio"),
					Entero(raiz, "cambioPorcentualComoSeVe")),
				Accion = LaRedaccion.Accion(frases, Texto(raiz, "claveDeLaAccion"),
					Texto(raiz, "claveDelPlazo")),
				PideAlgo = Booleano(raiz, "pideAlgo"),
				Actual = Decimal(raiz, "actual"),
				Habitual = Decimal(raiz, "habitual"),
				CambioProporcional = Decimal(raiz, "cambioProporcional"),
				CambioPorcentual = Entero(raiz, "cambioPorcentual"),
				TramosCalculados = Entero(raiz, "tramosCalculados"),
				TramosBloqueados = Entero(raiz, "tramosBloqueados"),
				HaySerie = Booleano(raiz, "haySerie"),
				QueFalta = frases.Diga("falta." + Texto(raiz, "claveDeLoQueFalta"),
					("fecha", frases.Dia(Fecha(raiz, "fechaDeLaCargaTrunca")))),
				MotivoDeReserva = LaRedaccion.Reserva(frases, Texto(raiz, "motivoDeReserva")),
				EstaMejorando = Booleano(raiz, "estaMejorando"),
				EstaEmpeorando = Booleano(raiz, "estaEmpeorando"),
				SugiereRegistrarLaCargaQueFalto = Booleano(raiz, "sugiereRegistrarLaCargaQueFalto"),
				ActualComoSeVe = Decimal(raiz, "actualComoSeVe"),
				HabitualComoSeVe = Decimal(raiz, "habitualComoSeVe"),
				CambioPorcentualComoSeVe = Entero(raiz, "cambioPorcentualComoSeVe"),
				Simbolo = Texto(raiz, "simbolo"),
			};
		}

		internal static AutonomiaDelVehiculo Autonomia(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new AutonomiaDelVehiculo();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			return new AutonomiaDelVehiculo
			{
				Hay = Booleano(raiz, "hay"),
				Kilometros = Decimal(raiz, "kilometros"),
				CapacidadEnLitros = Decimal(raiz, "capacidad"),
				HabitualKmPorLitro = Decimal(raiz, "habitual"),
				EsProyeccion = Booleano(raiz, "esProyeccion"),
			};
		}

		internal static CompletitudDelVehiculo Completitud(string json, string idioma)
		{
			if (string.IsNullOrWhiteSpace(json)) return new CompletitudDelVehiculo();

			LasFrases frases = LasFrases.De(idioma);
			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			return new CompletitudDelVehiculo
			{
				CargasConKilometraje = Entero(raiz, "cargasConKilometraje"),
				TotalDeCargas = Entero(raiz, "totalDeCargas"),
				TramosCalculados = Entero(raiz, "tramosCalculados"),
				TramosBloqueados = Entero(raiz, "tramosBloqueados"),
				Expresada = LaRedaccion.Completitud(frases, Entero(raiz, "cargasConKilometraje"),
					Entero(raiz, "totalDeCargas"), Entero(raiz, "tramosCalculados")),
			};
		}

		internal static IReadOnlyList<FaltanteEnLista> Faltantes(string json, string idioma)
		{
			LasFrases frases = LasFrases.De(idioma);
			var faltantes = new List<FaltanteEnLista>();
			foreach (JsonElement elemento in Arreglo(json, "faltante"))
				faltantes.Add(new FaltanteEnLista
				{
					Fecha = Fecha(elemento, "fecha"),
					Transaccion = Texto(elemento, "transaccion"),
					QueFalta = frases.Diga("faltante.falta." + Texto(elemento, "claveDeLoQueFalta")),
					ClaveDeLoQueFalta = Texto(elemento, "claveDeLoQueFalta"),
					QueDesbloquea = frases.Diga(
						"faltante.desbloquea." + Texto(elemento, "claveDeLoQueDesbloquea"),
						("kilometros", frases.Entera(Decimal(elemento, "kilometrosDelTramo"))),
						("desde", frases.Dia(Fecha(elemento, "desdeLaCarga"))),
						("hasta", frases.Dia(Fecha(elemento, "hastaLaCarga")))),
					ParaSonia = LaRedaccion.Faltante(frases, Texto(elemento, "claveDeLoQueFalta"),
						Texto(elemento, "claveDeLoQueDesbloquea"), Fecha(elemento, "fecha"),
						Decimal(elemento, "kilometrosDelTramo"), Fecha(elemento, "desdeLaCarga"),
						Fecha(elemento, "hastaLaCarga"), Texto(elemento, "loQueSeSupone"),
						Texto(elemento, "claveDelCorte")),
					LoQueSeSupone = Texto(elemento, "loQueSeSupone"),
					LaSuposicion = LaRedaccion.Suposicion(frases, Texto(elemento, "loQueSeSupone"),
						Texto(elemento, "claveDelCorte")),
					NaceDeUnaSuposicion = Booleano(elemento, "naceDeUnaSuposicion"),
				});
			return faltantes;
		}

		internal static OdometroPropuesto Odometro(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new OdometroPropuesto();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			return new OdometroPropuesto
			{
				Hay = Booleano(raiz, "hay"),
				Kilometros = Decimal(raiz, "kilometros"),
				KilometrosPorDia = Decimal(raiz, "kilometrosPorDia"),
				DiasDesdeLaUltimaLectura = Entero(raiz, "diasDesdeLaUltimaLectura"),
				Origen = Texto(raiz, "origen"),
				KilometrosComoSeVen = Decimal(raiz, "kilometrosComoSeVen"),
				PorDiaComoSeVe = Decimal(raiz, "porDiaComoSeVe"),
				SimboloDeLaDistancia = Texto(raiz, "simboloDeLaDistancia"),
			};
		}

		internal static RitmoDelVehiculo Ritmo(string json, string idioma)
		{
			if (string.IsNullOrWhiteSpace(json)) return new RitmoDelVehiculo();

			LasFrases frases = LasFrases.De(idioma);
			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			return new RitmoDelVehiculo
			{
				HayVehiculo = true,
				Situacion = Texto(raiz, "situacion"),
				SePudoMedir = Booleano(raiz, "sePudoMedir"),
				Expresada = LaRedaccion.Ritmo(frases, Texto(raiz, "situacion"),
					Decimal(raiz, "porDiaComoSeVe"), Decimal(raiz, "kilometrosComoSeVen"),
					Texto(raiz, "simboloDeLaDistancia"), Fecha(raiz, "desde"), Fecha(raiz, "hasta"),
					Entero(raiz, "lecturas")),
				KilometrosPorDia = Decimal(raiz, "kilometrosPorDia"),
				Kilometros = Decimal(raiz, "kilometros"),
				Dias = Entero(raiz, "dias"),
				Desde = Fecha(raiz, "desde"),
				Hasta = Fecha(raiz, "hasta"),
				Lecturas = Entero(raiz, "lecturas"),
				PorDiaComoSeVe = Decimal(raiz, "porDiaComoSeVe"),
				KilometrosComoSeVen = Decimal(raiz, "kilometrosComoSeVen"),
				SimboloDeLaDistancia = Texto(raiz, "simboloDeLaDistancia"),
			};
		}

		internal static IReadOnlyList<CostoEnLista> Costos(string json, string idioma)
		{
			LasFrases frases = LasFrases.De(idioma);
			var costos = new List<CostoEnLista>();
			foreach (JsonElement elemento in Arreglo(json, "costo"))
				costos.Add(new CostoEnLista
				{
					Moneda = Texto(elemento, "moneda"),
					MontoPorKilometro = Decimal(elemento, "montoPorKilometro"),
					Kilometros = Decimal(elemento, "kilometros"),
					Expresado = LaRedaccion.Costo(frases, Decimal(elemento, "montoPorKilometro"),
						Texto(elemento, "moneda"), Decimal(elemento, "kilometros")),
				});
			return costos;
		}

		internal static IReadOnlyList<PrecioEnLista> Precios(string json)
		{
			var precios = new List<PrecioEnLista>();
			foreach (JsonElement elemento in Arreglo(json, "precio"))
				precios.Add(new PrecioEnLista
				{
					Fecha = Fecha(elemento, "fecha"),
					PrecioPorLitro = Decimal(elemento, "precioPorLitro"),
					Moneda = Texto(elemento, "moneda"),
					Surtidor = Texto(elemento, "surtidor"),
				});
			return precios;
		}

		// LA LLAVE ES EL NOMBRE DE LA VARIABLE DEL foreach del DSL, no la primera columna,
		// asi que dos consultas que devuelven la misma forma tienen que decir cual usan.
		internal static IReadOnlyList<GastoEnLista> Gastos(string json, string idioma,
			string llave = "gasto")
		{
			LasFrases frases = LasFrases.De(idioma);
			var gastos = new List<GastoEnLista>();
			foreach (JsonElement elemento in Arreglo(json, llave))
				gastos.Add(new GastoEnLista
				{
					Moneda = Texto(elemento, "moneda"),
					Monto = Decimal(elemento, "monto"),
					Cargas = Entero(elemento, "cargas"),
					Litros = Decimal(elemento, "litros"),
					Expresado = LaRedaccion.Gasto(frases, Decimal(elemento, "monto"),
						Texto(elemento, "moneda"), Entero(elemento, "cargas")),
				});
			return gastos;
		}

		internal static EstadoDeLealtad Lealtad(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new EstadoDeLealtad();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			return new EstadoDeLealtad
			{
				Saldo = Entero(raiz, "saldo"),
				Nivel = Texto(raiz, "nivel"),
				PuntosParaElSiguienteNivel = Entero(raiz, "paraElSiguiente"),
				EstaAfiliada = Booleano(raiz, "afiliada"),
				NumeroDeSocio = Texto(raiz, "socio"),
				NombreDelSocio = Texto(raiz, "nombreDelSocio"),
				SocioDesde = Fecha(raiz, "socioDesde"),
			};
		}

		// ------------------------------------------------------------------ plomeria

		// La llave del arreglo es el nombre de la variable del foreach del DSL, no el de
		// la primera columna. Sin filas, el foreach no emite nada y la propiedad no
		// existe: eso es una lista vacia, no un error.
		private static IEnumerable<JsonElement> Arreglo(string json, string llave)
		{
			if (string.IsNullOrWhiteSpace(json)) yield break;

			using JsonDocument documento = JsonDocument.Parse(json);
			if (!documento.RootElement.TryGetProperty(llave, out JsonElement arreglo)) yield break;

			foreach (JsonElement elemento in Elementos(arreglo)) yield return elemento;
		}

		// Un solo print emite un objeto; varios emiten un arreglo. Tratar los dos casos
		// evita que la vista se rompa justo cuando hay una sola fila.
		private static IEnumerable<JsonElement> Elementos(JsonElement elemento)
		{
			if (elemento.ValueKind == JsonValueKind.Array)
			{
				foreach (JsonElement hijo in elemento.EnumerateArray()) yield return hijo;
				yield break;
			}
			if (elemento.ValueKind == JsonValueKind.Object) yield return elemento;
		}

		private static string Texto(JsonElement padre, string nombre) =>
			padre.TryGetProperty(nombre, out JsonElement e) && e.ValueKind == JsonValueKind.String
				? e.GetString() : "";

		private static int Entero(JsonElement padre, string nombre)
		{
			if (!padre.TryGetProperty(nombre, out JsonElement e)) return 0;
			if (e.TryGetInt32(out int valor)) return valor;

			// El formatter puede emitir un entero calculado como decimal ("-25.0").
			return e.TryGetDecimal(out decimal como) ? (int)como : 0;
		}

		private static decimal Decimal(JsonElement padre, string nombre) =>
			padre.TryGetProperty(nombre, out JsonElement e) && e.TryGetDecimal(out decimal valor)
				? valor : 0m;

		private static bool Booleano(JsonElement padre, string nombre)
		{
			if (!padre.TryGetProperty(nombre, out JsonElement e)) return false;
			if (e.ValueKind == JsonValueKind.True) return true;
			if (e.ValueKind == JsonValueKind.False) return false;

			// El formatter puede emitir el booleano como texto; se aceptan las dos formas
			// para que la vista no dependa de ese detalle.
			return e.ValueKind == JsonValueKind.String
				&& bool.TryParse(e.GetString(), out bool valor) && valor;
		}

		private static DateTime Fecha(JsonElement padre, string nombre)
		{
			string texto = Texto(padre, nombre);
			if (texto.Length == 0) return default;

			if (DateTime.TryParseExact(texto, FormatoDeFecha, CultureInfo.InvariantCulture,
					DateTimeStyles.None, out DateTime conHora)) return conHora;
			if (DateTime.TryParseExact(texto, FormatoDeFechaSinHora, CultureInfo.InvariantCulture,
					DateTimeStyles.None, out DateTime soloFecha)) return soloFecha;

			return DateTime.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime suelta)
				? suelta : default;
		}
	}
}
