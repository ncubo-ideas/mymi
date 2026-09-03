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

		// El nombre del archivo dentro de la carpeta, para poder volver a mostrarla. Vacio
		// si no hay foto.
		public string Foto { get; init; } = "";
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

		// Los de arriba son CANONICOS y de ellos salen la curva y la barra; estos son para
		// el texto del trecho. Ver RendimientoDelVehiculo.
		public decimal RendimientoComoSeVe { get; init; }
		public decimal KilometrosComoSeVen { get; init; }
		public string SimboloDelRendimiento { get; init; } = "";
		public string SimboloDeLaDistancia { get; init; } = "";
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

		// LO MISMO, DICHO COMO ELLA LO MIRA. Los de arriba son canonicos y de ahi salen la
		// curva y la barra -la geometria que codifica mejor y peor no se puede invertir con
		// una unidad-; estos son para el texto.
		//
		// El porcentaje NO es el de arriba: en km/L 12.6 -> 9.4 es 25% menos, y esos mismos
		// dos tramos en L/100km son 7.9 -> 10.6, un 33% MAS. Viene con signo, para que la
		// flecha siga al numero mientras el color sigue al veredicto.
		public decimal ActualComoSeVe { get; init; }
		public decimal HabitualComoSeVe { get; init; }
		public int CambioPorcentualComoSeVe { get; init; }
		public string Simbolo { get; init; } = "";
	}

	/// <summary>En que unidades lo mira, para que la pantalla marque la elegida.</summary>
	public sealed class UnidadesElegidas
	{
		public string Rendimiento { get; init; } = "";
		public string Distancia { get; init; } = "";
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

	// EN CUANTOS DIAS VA A TENER QUE CARGAR. Se compone de dos derivaciones que ya
	// existen -la autonomia del tanque lleno y la proyeccion del odometro-, y por eso
	// vive ACA y no en una caratula: la calculaba el servidor MCP para su tool, y el
	// tablero necesitaba la misma cifra. Dos derivaciones separadas es como la IA y la
	// persona terminan diciendo numeros distintos del mismo carro.
	//
	// SIEMPRE es proyeccion, y ademas una aproximacion sobre otra: la ultima lectura de
	// odometro no siempre coincide con la ultima carga llena.
	public sealed class ProximaCargaDelVehiculo
	{
		public bool Hay { get; init; }
		public int DiasEstimados { get; init; }
		public decimal KilometrosRestantes { get; init; }
		public decimal KilometrosDeTanqueLleno { get; init; }
		public decimal KilometrosPorDia { get; init; }
		public bool EsProyeccion { get; init; }

		// La fraccion del tanque que queda, de 0 a 1: lo que mueve la aguja del medidor.
		// Es derivada y no un dato: si no hay estimacion, no hay aguja que mover.
		public decimal FraccionRestante { get; init; }
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

		// LA SUPOSICION, para poder enunciarla antes de preguntar y para que los botones
		// digan que es "así". Vacias cuando el faltante no nace de una inferencia.
		public string LoQueSeSupone { get; init; } = "";
		public string LaSuposicion { get; init; } = "";
		public bool NaceDeUnaSuposicion { get; init; }
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

		// EN LA UNIDAD QUE ELLA MIRA, y acá no es cosmético: el campo del kilometraje llega
		// prellenado con esto, y lo que se teclea se journaliza con la unidad del campo.
		public decimal KilometrosComoSeVen { get; init; }
		public decimal PorDiaComoSeVe { get; init; }
		public string SimboloDeLaDistancia { get; init; } = "";
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

	// UN SORTEO CON SUS BOLETOS, ya juzgado contra hoy. Un vencido se muestra vencido y
	// NUNCA desaparece: que se esfumen boletos de una app de lealtad es la forma mas rapida
	// de perder la confianza que este producto entero intenta construir.
	public sealed class SorteoEnLista
	{
		public string Codigo { get; init; } = "";
		public string Nombre { get; init; } = "";
		public string Premio { get; init; } = "";
		public bool FueAnunciado { get; init; }
		public DateTime FechaDelSorteo { get; init; }
		public int Boletos { get; init; }
		public bool EstaVencido { get; init; }
		public int DiasParaElSorteo { get; init; }
		public string MontoPorBoleto { get; init; } = "";
		public string ParaSonia { get; init; } = "";
	}

	// LA EXPLICACION de una carga: lo unico que la app SI deriva. Cuando no cuadra con lo
	// emitido, se dice que probablemente falta un dato, nunca que la estacion debe algo.
	public sealed class BoletosDeLaCargaEnLista
	{
		public DateTime Fecha { get; init; }
		public string Transaccion { get; init; } = "";
		public string Sorteo { get; init; } = "";
		public string NombreDelSorteo { get; init; } = "";
		public bool HayRegla { get; init; }
		public int QueDaLaRegla { get; init; }
		public int Emitidos { get; init; }
		public string Sobrante { get; init; } = "";
		public bool Cuadra { get; init; }
		public bool ProbablementeFaltaUnDato { get; init; }
		public string ParaSonia { get; init; } = "";
	}

	public sealed class VehiculoEnLista
	{
		public string Placa { get; init; } = "";

		// El alias si Sonia lo puso; la placa si no. Es el momento de pedir la ficha.
		public string Nombre { get; init; } = "";

		public bool TieneFicha { get; init; }
		public int Cargas { get; init; }

		// LA FICHA, para poder EDITARLA. Completar una ficha casi siempre es corregir la
		// que hay, y sin esto el formulario tendria que pedirla en blanco cada vez. Vacias
		// -y el año y el rendimiento en cero- cuando todavia no hay ficha.
		public string Alias { get; init; } = "";
		public string Marca { get; init; } = "";
		public string Modelo { get; init; } = "";
		public int Anio { get; init; }
		public string Color { get; init; } = "";
		public decimal RendimientoDeFabrica { get; init; }
	}
}
