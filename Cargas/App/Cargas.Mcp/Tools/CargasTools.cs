using System.ComponentModel;
using System.Text.Json.Serialization;
using Cargas.App.Vistas;
using ModelContextProtocol.Server;

namespace Cargas.Mcp.Tools;

// LAS TOOLS INTENCIONALES: una por pregunta, nunca un RPC generico. El criterio de
// aceptacion es de Alvaro: si la IA puede contestar algo que YA ESTA EN UNA PANTALLA,
// el demo no probo nada. Por eso no hay una tool "dame las cargas": hay la autonomia
// (que combina dos derivaciones), el rendimiento contra la propia base (que exige la
// serie), lo que falta (que es una AUSENCIA) y la proxima carga (una prediccion).
//
// UseStructuredContent = true en todas: sin el, el SDK serializa el record a texto y
// los decimales llegan como prosa. Con el, viaja structuredContent y la tool publica
// su outputSchema.
//
// La placa entra como string y no como enum porque NO es un conjunto cerrado: es la
// identidad que publica la estacion. mis_cargas lista las validas; una desconocida se
// contesta con hayVehiculo=false, no con una excepcion a traves del protocolo.
[McpServerToolType]
internal sealed class CargasTools
{
	private readonly LectorDelJournal lector;

	public CargasTools(LectorDelJournal lector)
	{
		ArgumentNullException.ThrowIfNull(lector);
		this.lector = lector;
	}

	[McpServerTool(Name = "mis_cargas", UseStructuredContent = true)]
	[Description("El punto de partida: si la persona ya abrio su app, hasta que evento "
		+ "se leyo, que vehiculos hay (con su placa, que es la llave de las otras tools) "
		+ "y que tan completa esta la serie de cada uno. Llamala primero.")]
	public ResumenDeCargas MisCargas()
	{
		if (!lector.HayJournal) return new ResumenDeCargas { HayDatos = false };

		return lector.Consultar(api =>
		{
			var vehiculos = new List<VehiculoDelResumen>();
			foreach (VehiculoEnLista vehiculo in api.Vehiculos())
			{
				CompletitudDelVehiculo completitud = api.Completitud(vehiculo.Placa);
				vehiculos.Add(new VehiculoDelResumen
				{
					Placa = vehiculo.Placa,
					Nombre = vehiculo.Nombre,
					Cargas = vehiculo.Cargas,
					Completitud = completitud.Expresada,
				});
			}

			EstadoDeLealtad lealtad = api.Lealtad();
			return new ResumenDeCargas
			{
				HayDatos = true,
				UltimoEventoLeido = api.UltimoEventoProcesado(),
				Vehiculos = vehiculos,
				PuntosDeLealtad = lealtad.Saldo,
				NivelDeLealtad = lealtad.Nivel,
			};
		});
	}

	[McpServerTool(Name = "autonomia_estimada", UseStructuredContent = true)]
	[Description("Cuanto puede recorrer el vehiculo con el tanque lleno: la capacidad "
		+ "estimada (de la carga maxima observada) por el rendimiento habitual (la "
		+ "mediana de sus tramos). Combina dos derivaciones y no esta en ninguna "
		+ "pantalla. SIEMPRE es una proyeccion declarada, nunca una medicion.")]
	public AutonomiaDeVehiculo AutonomiaEstimada(
		[Description("La placa del vehiculo, como la lista mis_cargas.")] string placa)
	{
		if (!lector.HayJournal) return new AutonomiaDeVehiculo { HayDatos = false };

		return lector.Consultar(api =>
		{
			if (!ConocePlaca(api, placa))
				return new AutonomiaDeVehiculo { HayDatos = true, HayVehiculo = false };

			AutonomiaDelVehiculo autonomia = api.Autonomia(placa);
			return new AutonomiaDeVehiculo
			{
				HayDatos = true,
				HayVehiculo = true,
				HayEstimacion = autonomia.Hay,
				Kilometros = Math.Round(autonomia.Kilometros),
				CapacidadEnLitros = Math.Round(autonomia.CapacidadEnLitros, 1),
				HabitualKmPorLitro = Math.Round(autonomia.HabitualKmPorLitro, 2),
				EsProyeccion = autonomia.EsProyeccion,
			};
		});
	}

	[McpServerTool(Name = "rendimiento_vs_habitual", UseStructuredContent = true)]
	[Description("La lectura de la SERIE de tramos del vehiculo: el veredicto semantico "
		+ "(mejoro/empeoro/estable, o por que no se puede leer), el ultimo tramo contra "
		+ "el nivel de siempre, y la accion con su plazo si corresponde. Es lo que una "
		+ "hoja de cargas no puede decir: exige el aporte de la persona.")]
	public RendimientoDeVehiculo RendimientoVsHabitual(
		[Description("La placa del vehiculo, como la lista mis_cargas.")] string placa)
	{
		if (!lector.HayJournal) return new RendimientoDeVehiculo { HayDatos = false };

		return lector.Consultar(api =>
		{
			if (!ConocePlaca(api, placa))
				return new RendimientoDeVehiculo { HayDatos = true, HayVehiculo = false };

			RendimientoDelVehiculo lectura = api.Rendimiento(placa);
			return new RendimientoDeVehiculo
			{
				HayDatos = true,
				HayVehiculo = true,
				Veredicto = lectura.Veredicto,
				ParaLaPersona = lectura.ParaSonia,
				ActualKmPorLitro = Math.Round(lectura.Actual, 2),
				HabitualKmPorLitro = Math.Round(lectura.Habitual, 2),
				CambioPorcentual = lectura.CambioPorcentual,
				TramosCalculados = lectura.TramosCalculados,
				TramosBloqueados = lectura.TramosBloqueados,
				HaySerie = lectura.HaySerie,
				Severidad = lectura.Severidad,
				Accion = lectura.Accion,
				SugiereRegistrarLaCargaQueFalto = lectura.SugiereRegistrarLaCargaQueFalto,
			};
		});
	}

	[McpServerTool(Name = "lo_que_falta_para_calcular", UseStructuredContent = true)]
	[Description("Que dato falta y que calculo se desbloquea si la persona lo pone. Es "
		+ "la consulta que ninguna pantalla contesta -una pantalla solo muestra datos "
		+ "que tiene, y esto es una AUSENCIA-. Usala para ayudar a completar la serie, "
		+ "nombrando la carga concreta y lo que aparece.")]
	public FaltantesDeVehiculo LoQueFaltaParaCalcular(
		[Description("La placa del vehiculo, como la lista mis_cargas.")] string placa)
	{
		if (!lector.HayJournal) return new FaltantesDeVehiculo { HayDatos = false };

		return lector.Consultar(api =>
		{
			if (!ConocePlaca(api, placa))
				return new FaltantesDeVehiculo { HayDatos = true, HayVehiculo = false };

			var faltantes = new List<FaltanteDelVehiculo>();
			foreach (FaltanteEnLista faltante in api.Faltantes(placa))
				faltantes.Add(new FaltanteDelVehiculo
				{
					FechaDeLaCarga = faltante.Fecha,
					QueFalta = faltante.QueFalta,
					QueDesbloquea = faltante.QueDesbloquea,
					ParaLaPersona = faltante.ParaSonia,
				});

			return new FaltantesDeVehiculo
			{
				HayDatos = true,
				HayVehiculo = true,
				Faltantes = faltantes,
				Completitud = api.Completitud(placa).Expresada,
			};
		});
	}

	[McpServerTool(Name = "dias_hasta_la_proxima_carga", UseStructuredContent = true)]
	[Description("En cuantos dias va a tener que cargar, estimado: la autonomia que le "
		+ "queda (segun lo recorrido desde la ultima carga) entre su distancia diaria "
		+ "promedio. Prediccion declarada, no una promesa.")]
	public ProximaCarga DiasHastaLaProximaCarga(
		[Description("La placa del vehiculo, como la lista mis_cargas.")] string placa)
	{
		if (!lector.HayJournal) return new ProximaCarga { HayDatos = false };

		// HOY lo pone ESTE proceso, no el dominio. El modelo no lee el reloj: si lo
		// hiciera, el mismo journal reproducido manana daria otra respuesta.
		DateTime hoy = DateTime.Now;

		return lector.Consultar(api =>
		{
			if (!ConocePlaca(api, placa))
				return new ProximaCarga { HayDatos = true, HayVehiculo = false };

			AutonomiaDelVehiculo autonomia = api.Autonomia(placa);
			OdometroPropuesto proyeccion = api.Odometro(placa, hoy);
			if (!autonomia.Hay || !proyeccion.Hay || proyeccion.KilometrosPorDia <= 0m)
				return new ProximaCarga { HayDatos = true, HayVehiculo = true, HayEstimacion = false };

			// Lo recorrido desde la ultima lectura, proyectado con su promedio; lo que
			// queda es autonomia menos eso. Aproximacion declarada: la ultima lectura
			// no siempre coincide con la ultima carga llena.
			decimal recorridos = proyeccion.KilometrosPorDia * proyeccion.DiasDesdeLaUltimaLectura;
			decimal restantes = Math.Max(0m, autonomia.Kilometros - recorridos);

			return new ProximaCarga
			{
				HayDatos = true,
				HayVehiculo = true,
				HayEstimacion = true,
				DiasEstimados = (int)Math.Round(restantes / proyeccion.KilometrosPorDia),
				KilometrosRestantes = Math.Round(restantes),
				KilometrosPorDia = Math.Round(proyeccion.KilometrosPorDia, 1),
				EsProyeccion = true,
			};
		});
	}

	private static bool ConocePlaca(Cargas.App.CargasAPI api, string placa)
	{
		if (string.IsNullOrWhiteSpace(placa)) return false;
		foreach (VehiculoEnLista vehiculo in api.Vehiculos())
			if (string.Equals(vehiculo.Placa, Normalizar(placa), StringComparison.OrdinalIgnoreCase))
				return true;
		return false;
	}

	// La misma normalizacion de la Placa del dominio, en la frontera de la tool: la IA
	// puede escribir "abc-123" y el vehiculo se llama "ABC123".
	private static string Normalizar(string placa)
	{
		var limpio = new System.Text.StringBuilder(placa.Length);
		foreach (char c in placa.Trim().ToUpperInvariant())
			if (char.IsLetterOrDigit(c)) limpio.Append(c);
		return limpio.ToString();
	}
}

// ------------------------------------------------------------------ los resultados

// hayDatos=false significa que la app nunca se abrio en esta computadora: no es un
// error de nadie, y las instructions le dicen a la IA como explicarlo.

public sealed class ResumenDeCargas
{
	[JsonPropertyName("hayDatos")]
	[Description("false si la app instalada nunca escribio su journal en esta computadora.")]
	public bool HayDatos { get; init; }

	[JsonPropertyName("ultimoEventoLeido")]
	[Description("Hasta que evento de la estacion se leyo. Si sube entre consultas, entraron cosas nuevas.")]
	public long UltimoEventoLeido { get; init; }

	[JsonPropertyName("vehiculos")]
	public IReadOnlyList<VehiculoDelResumen> Vehiculos { get; init; } = [];

	[JsonPropertyName("puntosDeLealtad")]
	public int PuntosDeLealtad { get; init; }

	[JsonPropertyName("nivelDeLealtad")]
	public string NivelDeLealtad { get; init; } = "";
}

public sealed class VehiculoDelResumen
{
	[JsonPropertyName("placa")]
	[Description("La llave para las demas tools.")]
	public string Placa { get; init; } = "";

	[JsonPropertyName("nombre")]
	[Description("El alias que le puso la persona, o la placa si todavia no tiene.")]
	public string Nombre { get; init; } = "";

	[JsonPropertyName("cargas")]
	public int Cargas { get; init; }

	[JsonPropertyName("completitud")]
	public string Completitud { get; init; } = "";
}

public sealed class AutonomiaDeVehiculo
{
	[JsonPropertyName("hayDatos")]
	public bool HayDatos { get; init; }

	[JsonPropertyName("hayVehiculo")]
	public bool HayVehiculo { get; init; }

	[JsonPropertyName("hayEstimacion")]
	[Description("false si todavia no hay capacidad estimada o linea base.")]
	public bool HayEstimacion { get; init; }

	[JsonPropertyName("kilometros")]
	public decimal Kilometros { get; init; }

	[JsonPropertyName("capacidadEnLitros")]
	public decimal CapacidadEnLitros { get; init; }

	[JsonPropertyName("habitualKmPorLitro")]
	public decimal HabitualKmPorLitro { get; init; }

	[JsonPropertyName("esProyeccion")]
	[Description("Siempre true: declarala como proyeccion, nunca como medicion.")]
	public bool EsProyeccion { get; init; }
}

public sealed class RendimientoDeVehiculo
{
	[JsonPropertyName("hayDatos")]
	public bool HayDatos { get; init; }

	[JsonPropertyName("hayVehiculo")]
	public bool HayVehiculo { get; init; }

	[JsonPropertyName("veredicto")]
	[Description("Semantico: mejoro/empeoro/estable/indeterminado, o sin tramo / no "
		+ "comparable / dato sospechoso / total no cuadra. No lo reinterpretes por el "
		+ "signo: cambiar la unidad invierte flechas, no juicios.")]
	public string Veredicto { get; init; } = "";

	[JsonPropertyName("paraLaPersona")]
	[Description("La lectura en una linea, armada con los hechos de la persona.")]
	public string ParaLaPersona { get; init; } = "";

	[JsonPropertyName("actualKmPorLitro")]
	public decimal ActualKmPorLitro { get; init; }

	[JsonPropertyName("habitualKmPorLitro")]
	public decimal HabitualKmPorLitro { get; init; }

	[JsonPropertyName("cambioPorcentual")]
	public int CambioPorcentual { get; init; }

	[JsonPropertyName("tramosCalculados")]
	public int TramosCalculados { get; init; }

	[JsonPropertyName("tramosBloqueados")]
	public int TramosBloqueados { get; init; }

	[JsonPropertyName("haySerie")]
	public bool HaySerie { get; init; }

	[JsonPropertyName("severidad")]
	public string Severidad { get; init; } = "";

	[JsonPropertyName("accion")]
	[Description("Que hacer y cuando, ya dicho entero. Vacio si no hay nada que hacer.")]
	public string Accion { get; init; } = "";

	[JsonPropertyName("sugiereRegistrarLaCargaQueFalto")]
	[Description("true = el numero es implausible y casi seguro falta una carga: sugeri "
		+ "registrarla, NO alarmes.")]
	public bool SugiereRegistrarLaCargaQueFalto { get; init; }
}

public sealed class FaltantesDeVehiculo
{
	[JsonPropertyName("hayDatos")]
	public bool HayDatos { get; init; }

	[JsonPropertyName("hayVehiculo")]
	public bool HayVehiculo { get; init; }

	[JsonPropertyName("faltantes")]
	public IReadOnlyList<FaltanteDelVehiculo> Faltantes { get; init; } = [];

	[JsonPropertyName("completitud")]
	public string Completitud { get; init; } = "";
}

public sealed class FaltanteDelVehiculo
{
	[JsonPropertyName("fechaDeLaCarga")]
	public DateTime FechaDeLaCarga { get; init; }

	[JsonPropertyName("queFalta")]
	public string QueFalta { get; init; } = "";

	[JsonPropertyName("queDesbloquea")]
	public string QueDesbloquea { get; init; } = "";

	[JsonPropertyName("paraLaPersona")]
	public string ParaLaPersona { get; init; } = "";
}

public sealed class ProximaCarga
{
	[JsonPropertyName("hayDatos")]
	public bool HayDatos { get; init; }

	[JsonPropertyName("hayVehiculo")]
	public bool HayVehiculo { get; init; }

	[JsonPropertyName("hayEstimacion")]
	public bool HayEstimacion { get; init; }

	[JsonPropertyName("diasEstimados")]
	public int DiasEstimados { get; init; }

	[JsonPropertyName("kilometrosRestantes")]
	public decimal KilometrosRestantes { get; init; }

	[JsonPropertyName("kilometrosPorDia")]
	public decimal KilometrosPorDia { get; init; }

	[JsonPropertyName("esProyeccion")]
	public bool EsProyeccion { get; init; }
}
