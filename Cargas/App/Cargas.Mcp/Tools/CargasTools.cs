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

	/// <summary>
	/// EN QUE IDIOMA LE PREGUNTA ESTE SERVIDOR A LA PUERTA, y es el del registro de la estacion,
	/// NO el de quien pregunta. Esta carátula es voz de la institucion hablandole al modelo: sigue
	/// el idioma del modulo igual que lo siguen el titulo, la descripcion y el texto de cada tool.
	/// El modelo, que si habla los dos, traduce si hace falta.
	/// </summary>
	private const string ElIdioma = Cargas.App.Frases.Frases.IdiomaDelRegistro;

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
				CompletitudDelVehiculo completitud = api.Completitud(vehiculo.Placa, ElIdioma);
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

			RendimientoDelVehiculo lectura = api.Rendimiento(placa, ElIdioma);
			return new RendimientoDeVehiculo
			{
				HayDatos = true,
				HayVehiculo = true,
				Veredicto = lectura.Veredicto,
				ParaLaPersona = lectura.ParaSonia,
				Actual = Math.Round(lectura.ActualComoSeVe, 2),
				Habitual = Math.Round(lectura.HabitualComoSeVe, 2),
				Unidad = lectura.Simbolo,
				CambioPorcentual = lectura.CambioPorcentualComoSeVe,
				TramosCalculados = lectura.TramosCalculados,
				TramosBloqueados = lectura.TramosBloqueados,
				HaySerie = lectura.HaySerie,
				Severidad = lectura.Severidad,
				Accion = lectura.Accion,
				SugiereRegistrarLaCargaQueFalto = lectura.SugiereRegistrarLaCargaQueFalto,
			};
		});
	}

	[McpServerTool(Name = "mi_ritmo_de_manejo", UseStructuredContent = true)]
	[Description("Cuanto se maneja este vehiculo: la distancia por dia leida de la serie "
		+ "del odometro, y las lecturas de las que salio una por una. Usala para "
		+ "'¿cuanto manejo?', '¿cuantos kilometros hago por dia?', y sobre todo PARA "
		+ "CRUZAR CON EL PLAN DE MANTENIMIENTO DE OTRO MODULO: el registro del "
		+ "concesionario dice cuanta distancia y cuantos dias le faltan al carro para "
		+ "cada service, y declara que NO puede decir cual de las dos mitades llega "
		+ "primero porque no tiene el ritmo. Este es ese ritmo. "
		+ "LO QUE DICE dice ES LA RESPUESTA, y la compuso el registro. Trae las dos "
		+ "puntas con sus fechas, la distancia entre ellas, los dias y la distancia por "
		+ "dia. Dila. NO la rearmes: no dividas kilometros entre dias ni saques un "
		+ "promedio de las filas, que daria un ritmo que nadie midio y con otro redondeo. "
		+ "UN RITMO SE PUEDE NO PODER MEDIR, Y ESO ES UNA RESPUESTA. situacion dice en "
		+ "cual de cuatro esta: medido, o no se pudo por una de tres razones con nombre "
		+ "-sin lecturas, una sola lectura, o todas las lecturas del mismo dia-. NINGUNA "
		+ "de las tres quiere decir que el carro no se movio: quiere decir que falta un "
		+ "aporte de la persona, y dice cual. "
		+ "SOLO CUENTAN LAS LECTURAS OBSERVADAS Y CONFIRMADAS. Un kilometraje ESTIMADO lo "
		+ "propuso la app y la persona no lo miro, asi que no entra en el ritmo: entra en "
		+ "las filas con cuentaParaCalcular en false, para que se vea que existe y que no "
		+ "participo. Nunca lo sumes al calculo. "
		+ "EL CRUCE CON EL CONCESIONARIO ES TUYO, NO DE LA ESTACION. Si pones este ritmo "
		+ "al lado de lo que le falta al carro para un service, dilo: la estacion no sabe "
		+ "nada del plan de mantenimiento y el concesionario no sabe nada de estas "
		+ "lecturas. Y la unidad viaja en la respuesta: no la conviertas.")]
	public RitmoDeManejo MiRitmoDeManejo(
		[Description("La placa del vehiculo, como la lista mis_cargas.")] string placa)
	{
		if (!lector.HayJournal) return new RitmoDeManejo { HayDatos = false };

		return lector.Consultar(api =>
		{
			if (!ConocePlaca(api, placa))
				return new RitmoDeManejo { HayDatos = true, HayVehiculo = false };

			RitmoDelVehiculo ritmo = api.Ritmo(placa, ElIdioma);

			// LAS FILAS SALEN DE LA MISMA CONSULTA QUE MIRA LA PERSONA. No hay una lectura
			// aparte para la IA: si la Ruta muestra "42 380 km (observado)", eso es lo que
			// viaja. Van las estimadas tambien, marcadas: ocultarlas haria parecer que el
			// kilometraje de esa carga no existe, y existe -lo que no hace es contar-.
			var lecturas = new List<LecturaDelOdometro>();
			foreach (CargaEnLista carga in api.Cargas(placa, ElIdioma))
			{
				if (!carga.TieneKilometraje) continue;
				lecturas.Add(new LecturaDelOdometro
				{
					Fecha = carga.Fecha,
					Kilometraje = carga.KilometrajeAnotado,
					Origen = carga.OrigenDelKilometraje,
					CuentaParaCalcular = carga.CuentaParaCalcular,
					Expresada = carga.Kilometraje,
				});
			}

			return new RitmoDeManejo
			{
				HayDatos = true,
				HayVehiculo = true,
				Situacion = ritmo.Situacion,
				SePudoMedir = ritmo.SePudoMedir,
				Dice = ritmo.Expresada,
				PorDia = Math.Round(ritmo.PorDiaComoSeVe, 1),
				Distancia = Math.Round(ritmo.KilometrosComoSeVen),
				Unidad = ritmo.SimboloDeLaDistancia,
				Dias = ritmo.Dias,
				Desde = ritmo.Desde,
				Hasta = ritmo.Hasta,
				LecturasQueCuentan = ritmo.Lecturas,
				Lecturas = lecturas,
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
			foreach (FaltanteEnLista faltante in api.Faltantes(placa, ElIdioma))
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
				Completitud = api.Completitud(placa, ElIdioma).Expresada,
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

			// LA MISMA COMPOSICION QUE MIRA LA PERSONA. La resta vivia aca; se subio a
			// CargasAPI cuando el tablero pidio la cifra, porque calcularla dos veces es
			// como la IA y la persona terminan diciendo numeros distintos del mismo
			// carro. Esta tool ya solo la viste de DTO.
			ProximaCargaDelVehiculo proxima = api.ProximaCarga(placa, hoy);
			if (!proxima.Hay)
				return new ProximaCarga { HayDatos = true, HayVehiculo = true, HayEstimacion = false };

			return new ProximaCarga
			{
				HayDatos = true,
				HayVehiculo = true,
				HayEstimacion = true,
				DiasEstimados = proxima.DiasEstimados,
				KilometrosRestantes = proxima.KilometrosRestantes,
				KilometrosPorDia = proxima.KilometrosPorDia,
				EsProyeccion = proxima.EsProyeccion,
			};
		});
	}

	[McpServerTool(Name = "mis_boletos_de_tombola", UseStructuredContent = true)]
	[Description("Los boletos de tombola de la persona, agrupados por sorteo, y de donde "
		+ "salio cada uno. Los boletos los EMITE la estacion: la app no los calcula. Lo "
		+ "que si deriva es la explicacion de cuantos dio cada carga y cuanto sobro.")]
	public BoletosDeTombola MisBoletosDeTombola(
		[Description("La placa del vehiculo, como la lista mis_cargas.")] string placa)
	{
		if (!lector.HayJournal) return new BoletosDeTombola { HayDatos = false };

		// HOY lo pone ESTE proceso, no el dominio. Si el modelo leyera el reloj, el mismo
		// journal reproducido manana diria que un sorteo ya vencio.
		DateTime hoy = DateTime.Now;

		return lector.Consultar(api =>
		{
			if (!ConocePlaca(api, placa))
				return new BoletosDeTombola { HayDatos = true, HayVehiculo = false };

			var sorteos = new List<SorteoDeTombola>();
			foreach (SorteoEnLista sorteo in api.Sorteos(hoy, ElIdioma))
				sorteos.Add(new SorteoDeTombola
				{
					Codigo = sorteo.Codigo,
					Nombre = sorteo.Nombre,
					Premio = sorteo.Premio,
					FueAnunciado = sorteo.FueAnunciado,
					Boletos = sorteo.Boletos,
					EstaVencido = sorteo.EstaVencido,
					DiasParaElSorteo = sorteo.DiasParaElSorteo,
					ParaLaPersona = sorteo.ParaSonia,
				});

			var porCarga = new List<BoletosDeUnaCarga>();
			foreach (BoletosDeLaCargaEnLista fila in api.BoletosDeLasCargas(placa, ElIdioma))
				porCarga.Add(new BoletosDeUnaCarga
				{
					Fecha = fila.Fecha,
					Sorteo = fila.Sorteo,
					QueDaLaRegla = fila.QueDaLaRegla,
					Emitidos = fila.Emitidos,
					Sobrante = fila.Sobrante,
					ProbablementeFaltaUnDato = fila.ProbablementeFaltaUnDato,
					ParaLaPersona = fila.ParaSonia,
				});

			return new BoletosDeTombola
			{
				HayDatos = true,
				HayVehiculo = true,
				Sorteos = sorteos,
				PorCarga = porCarga,
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
	[Description("Semantico, y es un CODIGO estable: mejoro, empeoro, estable, "
		+ "indeterminado, sin-tramo, no-comparable, dato-sospechoso o total-no-cuadra. La "
		+ "frase para la persona viene en paraLaPersona; esto es la llave. No lo "
		+ "reinterpretes por el signo: cambiar la unidad invierte flechas, no juicios.")]
	public string Veredicto { get; init; } = "";

	[JsonPropertyName("paraLaPersona")]
	[Description("La lectura en una linea, armada con los hechos de la persona.")]
	public string ParaLaPersona { get; init; } = "";

	// LOS NUMEROS VAN EN LA UNIDAD QUE LA PERSONA ELIGIO, y los canonicos NO viajan.
	// Tenerlos al lado invitaria justo a lo que las instrucciones prohiben: convertir. Si
	// la IA contesta en km/L mientras su pantalla dice L/100km, para ella son dos apps
	// distintas hablando del mismo carro.
	[JsonPropertyName("actual")]
	[Description("El rendimiento del ultimo tramo, en la unidad que la persona eligio. "
		+ "Decilo tal cual, con su unidad: no lo conviertas.")]
	public decimal Actual { get; init; }

	[JsonPropertyName("habitual")]
	[Description("Su nivel de siempre, en la misma unidad.")]
	public decimal Habitual { get; init; }

	[JsonPropertyName("unidad")]
	[Description("El simbolo de esa unidad: km/L, L/100km o mpg.")]
	public string Unidad { get; init; } = "";

	[JsonPropertyName("cambioPorcentual")]
	[Description("El cambio EN ESA UNIDAD, con signo. No es el mismo numero en todas: la "
		+ "inversa no es lineal, asi que la misma caida es -25 en km/L y +33 en L/100km, y "
		+ "las dos dicen que empeoro. Deci el que te llega y no rehagas la cuenta.")]
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

public sealed class BoletosDeTombola
{
	[JsonPropertyName("hayDatos")]
	public bool HayDatos { get; init; }

	[JsonPropertyName("hayVehiculo")]
	public bool HayVehiculo { get; init; }

	[JsonPropertyName("sorteos")]
	public IReadOnlyList<SorteoDeTombola> Sorteos { get; init; } = new List<SorteoDeTombola>();

	[JsonPropertyName("porCarga")]
	[Description("La explicacion, carga por carga. Es lo unico que la app deriva.")]
	public IReadOnlyList<BoletosDeUnaCarga> PorCarga { get; init; } = new List<BoletosDeUnaCarga>();
}

public sealed class SorteoDeTombola
{
	[JsonPropertyName("codigo")]  public string Codigo { get; init; } = "";
	[JsonPropertyName("nombre")]  public string Nombre { get; init; } = "";
	[JsonPropertyName("premio")]  public string Premio { get; init; } = "";

	[JsonPropertyName("fueAnunciado")]
	[Description("Si la estacion ya publico su premio y su fecha. Uno sin anunciar TIENE "
		+ "boletos y no tiene premio ni fecha: no los inventes.")]
	public bool FueAnunciado { get; init; }

	[JsonPropertyName("boletos")] public int Boletos { get; init; }

	[JsonPropertyName("estaVencido")]
	[Description("Ya se rifo. Un boleto vencido SE DICE vencido: no desaparece ni se calla.")]
	public bool EstaVencido { get; init; }

	[JsonPropertyName("diasParaElSorteo")] public int DiasParaElSorteo { get; init; }
	[JsonPropertyName("paraLaPersona")]    public string ParaLaPersona { get; init; } = "";
}

public sealed class BoletosDeUnaCarga
{
	[JsonPropertyName("fecha")]  public DateTime Fecha { get; init; }
	[JsonPropertyName("sorteo")] public string Sorteo { get; init; } = "";

	[JsonPropertyName("queDaLaRegla")]
	[Description("Cuantos boletos daria la regla del sorteo por el monto de esa carga.")]
	public int QueDaLaRegla { get; init; }

	[JsonPropertyName("emitidos")]
	[Description("Cuantos emitio la estacion de verdad. Esta es la cifra autoritativa.")]
	public int Emitidos { get; init; }

	[JsonPropertyName("sobrante")]
	[Description("Lo que sobro de esa compra y NO se acumula para la siguiente.")]
	public string Sobrante { get; init; } = "";

	[JsonPropertyName("probablementeFaltaUnDato")]
	[Description("La cuenta derivada no cuadra con lo emitido. Deci que probablemente "
		+ "falta un dato por llegar y que abra su app y apriete Actualizar. NUNCA que la "
		+ "estacion le debe boletos: es un problema de datos, no una deuda.")]
	public bool ProbablementeFaltaUnDato { get; init; }

	[JsonPropertyName("paraLaPersona")] public string ParaLaPersona { get; init; } = "";
}

public sealed class RitmoDeManejo
{
	[JsonPropertyName("hayDatos")]
	[Description("false si la app instalada nunca escribio su journal en esta computadora.")]
	public bool HayDatos { get; init; }

	[JsonPropertyName("hayVehiculo")]
	[Description("false si esa placa no esta en la bitacora. Listalas con mis_cargas.")]
	public bool HayVehiculo { get; init; }

	[JsonPropertyName("situacion")]
	[Description("Un CODIGO de cuatro: 'medido', 'sin-lecturas', 'una-sola-lectura', "
		+ "'lecturas-del-mismo-dia'. Las tres ultimas son ausencias con nombre, no un ritmo "
		+ "de cero, y la frase que las dice viene en dice.")]
	public string Situacion { get; init; } = "";

	[JsonPropertyName("sePudoMedir")]
	[Description("true solo con situacion 'medido'. Con false, porDia y distancia vienen "
		+ "en cero y NO significan nada: la respuesta esta en dice.")]
	public bool SePudoMedir { get; init; }

	[JsonPropertyName("dice")]
	[Description("LA RESPUESTA, escrita por el registro. Dila. Cuando no se pudo medir, "
		+ "dice cual de las tres razones es y que aporte falta.")]
	public string Dice { get; init; } = "";

	[JsonPropertyName("porDia")]
	[Description("La distancia por dia, en la unidad de 'unidad'. Es el numero que el "
		+ "registro del concesionario declara no tener.")]
	public decimal PorDia { get; init; }

	[JsonPropertyName("distancia")]
	[Description("Lo recorrido entre las dos puntas, de donde sale el ritmo.")]
	public decimal Distancia { get; init; }

	[JsonPropertyName("unidad")]
	[Description("La unidad que eligio la persona, de porDia y distancia. NO la conviertas.")]
	public string Unidad { get; init; } = "";

	[JsonPropertyName("dias")]
	[Description("Dias entre la primera y la ultima lectura que cuentan.")]
	public int Dias { get; init; }

	[JsonPropertyName("desde")]
	public DateTime Desde { get; init; }

	[JsonPropertyName("hasta")]
	public DateTime Hasta { get; init; }

	[JsonPropertyName("lecturasQueCuentan")]
	[Description("Cuantas lecturas alimentaron el ritmo. Las estimadas no se cuentan aca, "
		+ "aunque aparezcan en la lista.")]
	public int LecturasQueCuentan { get; init; }

	[JsonPropertyName("lecturas")]
	[Description("El registro crudo, una fila por kilometraje anotado. Estan para respaldar "
		+ "la frase, no para rearmar el ritmo.")]
	public IReadOnlyList<LecturaDelOdometro> Lecturas { get; init; } = [];
}

public sealed class LecturaDelOdometro
{
	[JsonPropertyName("fecha")]
	[Description("Cuando se cargo, que es cuando la persona leyo el tablero.")]
	public DateTime Fecha { get; init; }

	[JsonPropertyName("kilometraje")]
	[Description("Lo que marcaba el odometro, con su unidad.")]
	public string Kilometraje { get; init; } = "";

	[JsonPropertyName("origen")]
	[Description("'observado' lo leyo la persona del tablero; 'confirmado' lo propuso la "
		+ "app y ella dijo que si; 'estimado' lo propuso la app y nadie lo miro.")]
	public string Origen { get; init; } = "";

	[JsonPropertyName("cuentaParaCalcular")]
	[Description("Si esta lectura alimenta el ritmo. false para las estimadas: existen, "
		+ "pero no participan. No las sumes.")]
	public bool CuentaParaCalcular { get; init; }

	[JsonPropertyName("expresada")]
	[Description("La lectura con su procedencia, como la dice el registro: '42 380 km (observado)'.")]
	public string Expresada { get; init; } = "";
}
