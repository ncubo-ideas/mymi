using System;

namespace Cargas.App.Vistas
{
	// Las vistas que la app expone. Viven en la App y no en la caratula porque las
	// consumen DOS caratulas: la UI del browser y las tools del servidor MCP. Que la
	// forma de la vista sea una sola es lo que hace que la IA y la persona vean lo
	// mismo.
	//
	// No son el dominio. El dominio tiene BitacoraDeCargas / Vehiculo / Carga / Tramo
	// con su comportamiento; esto es la proyeccion que la app decidio publicar.

	public sealed class CargaEnLista
	{
		public DateTime Fecha { get; init; }
		public string Estacion { get; init; } = "";
		public string Surtidor { get; init; } = "";
		public string Tipo { get; init; } = "";
		public decimal Litros { get; init; }
		public string Volumen { get; init; } = "";
		public decimal Precio { get; init; }
		public decimal Total { get; init; }
		public string Moneda { get; init; } = "";
		public string Corte { get; init; } = "";

		// El aporte de Sonia, ya dicho con su procedencia: "42 380 km (observado)".
		// Vacio cuando no lo puso, que es informacion y no un cero.
		public string Kilometraje { get; init; } = "";
		public string Tanque { get; init; } = "";

		public string Transaccion { get; init; } = "";
		public bool TieneKilometraje { get; init; }
		public bool TieneFoto { get; init; }
	}

	// UN TRAMO, calculado o bloqueado en la misma lista: el bloqueado no es la ausencia
	// de tramo, es un tramo que dice que le falta.
	public sealed class TramoEnLista
	{
		public DateTime Desde { get; init; }
		public DateTime Hasta { get; init; }
		public bool EstaCalculado { get; init; }
		public decimal Kilometros { get; init; }
		public decimal Litros { get; init; }
		public decimal Rendimiento { get; init; }
		public bool EsComparable { get; init; }
		public string MotivoDeReserva { get; init; } = "";
		public bool EsSospechoso { get; init; }
		public string QueFalta { get; init; } = "";
		public string Expresado { get; init; } = "";
	}

	// LA LECTURA DE LA SERIE: el veredicto con sus hechos. Es la vista que no tiene
	// equivalente en una pantalla de la estacion: una estacion entrega facturas, no
	// rendimientos.
	public sealed class RendimientoDelVehiculo
	{
		// Semantico: "mejoro" / "empeoro", nunca subio o bajo. La flecha y el signo son
		// de la presentacion, y se invierten con la unidad; esto no.
		public string Veredicto { get; init; } = "";

		public string Severidad { get; init; } = "";
		public string ParaSonia { get; init; } = "";
		public string Accion { get; init; } = "";
		public bool PideAlgo { get; init; }
		public decimal Actual { get; init; }
		public decimal Habitual { get; init; }
		public decimal CambioProporcional { get; init; }
		public int CambioPorcentual { get; init; }
		public int TramosCalculados { get; init; }
		public int TramosBloqueados { get; init; }
		public bool HaySerie { get; init; }
		public string QueFalta { get; init; } = "";
		public string MotivoDeReserva { get; init; } = "";
		public bool EstaMejorando { get; init; }
		public bool EstaEmpeorando { get; init; }
		public bool SugiereRegistrarLaCargaQueFalto { get; init; }
	}

	// "Te alcanza para ~X km": combinada de dos derivaciones, siempre proyeccion.
	public sealed class AutonomiaDelVehiculo
	{
		public bool Hay { get; init; }
		public decimal Kilometros { get; init; }
		public decimal CapacidadEnLitros { get; init; }
		public decimal HabitualKmPorLitro { get; init; }
		public bool EsProyeccion { get; init; }
	}

	public sealed class CompletitudDelVehiculo
	{
		public int CargasConKilometraje { get; init; }
		public int TotalDeCargas { get; init; }
		public int TramosCalculados { get; init; }
		public int TramosBloqueados { get; init; }
		public string Expresada { get; init; } = "";
	}

	// UN dato que falta y lo que desbloquea: la vista que motiva el ingreso.
	public sealed class FaltanteEnLista
	{
		public DateTime Fecha { get; init; }
		public string Transaccion { get; init; } = "";
		public string QueFalta { get; init; } = "";
		public string QueDesbloquea { get; init; } = "";
		public string ParaSonia { get; init; } = "";
	}

	// El kilometraje PROPUESTO para el prellenado. Nunca se escribe solo: si Sonia lo
	// acepta entra confirmado, si lo edita entra observado.
	public sealed class OdometroPropuesto
	{
		public bool Hay { get; init; }
		public decimal Kilometros { get; init; }
		public decimal KilometrosPorDia { get; init; }
		public int DiasDesdeLaUltimaLectura { get; init; }
		public string Origen { get; init; } = "";
	}

	public sealed class CostoEnLista
	{
		public string Moneda { get; init; } = "";
		public decimal MontoPorKilometro { get; init; }
		public decimal Kilometros { get; init; }
		public string Expresado { get; init; } = "";
	}

	public sealed class PrecioEnLista
	{
		public DateTime Fecha { get; init; }
		public decimal PrecioPorLitro { get; init; }
		public string Moneda { get; init; } = "";
		public string Surtidor { get; init; } = "";
	}

	public sealed class GastoEnLista
	{
		public string Moneda { get; init; } = "";
		public decimal Monto { get; init; }
		public int Cargas { get; init; }
		public decimal Litros { get; init; }
		public string Expresado { get; init; } = "";
	}

	public sealed class EstadoDeLealtad
	{
		public int Saldo { get; init; }
		public string Nivel { get; init; } = "";
		public int PuntosParaElSiguienteNivel { get; init; }
	}

	public sealed class VehiculoEnLista
	{
		public string Placa { get; init; } = "";

		// El alias si Sonia lo puso; la placa si no. Es el momento de pedir la ficha.
		public string Nombre { get; init; } = "";

		public bool TieneFicha { get; init; }
		public int Cargas { get; init; }
	}
}
