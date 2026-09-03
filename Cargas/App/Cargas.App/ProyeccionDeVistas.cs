using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Cargas.App.Vistas;

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

		internal static IReadOnlyList<CargaEnLista> Cargas(string json)
		{
			var cargas = new List<CargaEnLista>();
			foreach (JsonElement elemento in Arreglo(json, "carga"))
				cargas.Add(new CargaEnLista
				{
					Fecha = Fecha(elemento, "fecha"),
					Estacion = Texto(elemento, "estacion"),
					Surtidor = Texto(elemento, "surtidor"),
					Tipo = Texto(elemento, "tipo"),
					Litros = Decimal(elemento, "litros"),
					Volumen = Texto(elemento, "volumen"),
					Precio = Decimal(elemento, "precio"),
					Total = Decimal(elemento, "total"),
					Moneda = Texto(elemento, "moneda"),
					Corte = Texto(elemento, "corte"),
					Kilometraje = Texto(elemento, "kilometraje"),
					Tanque = Texto(elemento, "tanque"),
					Transaccion = Texto(elemento, "transaccion"),
					TieneKilometraje = Booleano(elemento, "tieneKilometraje"),
					TieneFoto = Booleano(elemento, "tieneFoto"),
					Foto = Texto(elemento, "foto"),
				});
			return cargas;
		}

		internal static IReadOnlyList<TramoEnLista> Tramos(string json)
		{
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
					MotivoDeReserva = Texto(elemento, "motivoDeReserva"),
					EsSospechoso = Booleano(elemento, "esSospechoso"),
					QueFalta = Texto(elemento, "queFalta"),
					Expresado = Texto(elemento, "expresado"),
					RendimientoComoSeVe = Decimal(elemento, "rendimientoComoSeVe"),
					KilometrosComoSeVen = Decimal(elemento, "kilometrosComoSeVen"),
					SimboloDelRendimiento = Texto(elemento, "simboloDelRendimiento"),
					SimboloDeLaDistancia = Texto(elemento, "simboloDeLaDistancia"),
				});
			return tramos;
		}

		internal static IReadOnlyList<SorteoEnLista> Sorteos(string json)
		{
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
					MontoPorBoleto = Texto(elemento, "montoPorBoleto"),
					ParaSonia = Texto(elemento, "paraSonia"),
				});
			return sorteos;
		}

		internal static IReadOnlyList<BoletosDeLaCargaEnLista> BoletosDeLasCargas(string json)
		{
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
					Sobrante = Texto(elemento, "sobrante"),
					Cuadra = Booleano(elemento, "cuadra"),
					ProbablementeFaltaUnDato = Booleano(elemento, "probablementeFaltaUnDato"),
					ParaSonia = Texto(elemento, "paraSonia"),
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

		internal static RendimientoDelVehiculo Rendimiento(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new RendimientoDelVehiculo();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			return new RendimientoDelVehiculo
			{
				Veredicto = Texto(raiz, "veredicto"),
				Severidad = Texto(raiz, "severidad"),
				ParaSonia = Texto(raiz, "paraSonia"),
				Accion = Texto(raiz, "accion"),
				PideAlgo = Booleano(raiz, "pideAlgo"),
				Actual = Decimal(raiz, "actual"),
				Habitual = Decimal(raiz, "habitual"),
				CambioProporcional = Decimal(raiz, "cambioProporcional"),
				CambioPorcentual = Entero(raiz, "cambioPorcentual"),
				TramosCalculados = Entero(raiz, "tramosCalculados"),
				TramosBloqueados = Entero(raiz, "tramosBloqueados"),
				HaySerie = Booleano(raiz, "haySerie"),
				QueFalta = Texto(raiz, "queFalta"),
				MotivoDeReserva = Texto(raiz, "motivoDeReserva"),
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

		internal static CompletitudDelVehiculo Completitud(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) return new CompletitudDelVehiculo();

			using JsonDocument documento = JsonDocument.Parse(json);
			JsonElement raiz = documento.RootElement;

			return new CompletitudDelVehiculo
			{
				CargasConKilometraje = Entero(raiz, "cargasConKilometraje"),
				TotalDeCargas = Entero(raiz, "totalDeCargas"),
				TramosCalculados = Entero(raiz, "tramosCalculados"),
				TramosBloqueados = Entero(raiz, "tramosBloqueados"),
				Expresada = Texto(raiz, "expresada"),
			};
		}

		internal static IReadOnlyList<FaltanteEnLista> Faltantes(string json)
		{
			var faltantes = new List<FaltanteEnLista>();
			foreach (JsonElement elemento in Arreglo(json, "faltante"))
				faltantes.Add(new FaltanteEnLista
				{
					Fecha = Fecha(elemento, "fecha"),
					Transaccion = Texto(elemento, "transaccion"),
					QueFalta = Texto(elemento, "queFalta"),
					QueDesbloquea = Texto(elemento, "queDesbloquea"),
					ParaSonia = Texto(elemento, "paraSonia"),
					LoQueSeSupone = Texto(elemento, "loQueSeSupone"),
					LaSuposicion = Texto(elemento, "laSuposicion"),
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

		internal static IReadOnlyList<CostoEnLista> Costos(string json)
		{
			var costos = new List<CostoEnLista>();
			foreach (JsonElement elemento in Arreglo(json, "costo"))
				costos.Add(new CostoEnLista
				{
					Moneda = Texto(elemento, "moneda"),
					MontoPorKilometro = Decimal(elemento, "montoPorKilometro"),
					Kilometros = Decimal(elemento, "kilometros"),
					Expresado = Texto(elemento, "expresado"),
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
		internal static IReadOnlyList<GastoEnLista> Gastos(string json, string llave = "gasto")
		{
			var gastos = new List<GastoEnLista>();
			foreach (JsonElement elemento in Arreglo(json, llave))
				gastos.Add(new GastoEnLista
				{
					Moneda = Texto(elemento, "moneda"),
					Monto = Decimal(elemento, "monto"),
					Cargas = Entero(elemento, "cargas"),
					Litros = Decimal(elemento, "litros"),
					Expresado = Texto(elemento, "expresado"),
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
