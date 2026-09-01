using System;
using System.Collections.Generic;
using Choreography.Theater;
using Cargas.App.Eventos;
using Cargas.App.Vistas;
using Puppeteer;
using Puppets.Portador;

namespace Cargas.App
{
	// LA SUPERFICIE INTENCIONAL de la app. Todo el mundo entra por aca: la UI, el bucle
	// de refresh y el servidor MCP. Nadie habla con el actor crudo.
	//
	// Es tambien el IDiarioDeLaApp que el Portador necesita: el bucle no sabe hacer
	// performs, sabe pedirle a esto que aplique un mensaje.
	public sealed class CargasAPI : IDiarioDeLaApp
	{
		private readonly PerformanceV2 app;

		public CargasAPI(PerformanceV2 app)
		{
			ArgumentNullException.ThrowIfNull(app);
			this.app = app;
		}

		// -------------------------------------------------- comandos de la credencial

		// La clave de ESTA instalacion ante el proveedor. Se genera en C# y entra como
		// parametro, no dentro del DSL: un Guid.NewGuid() en codigo de dominio daria una
		// clave distinta en cada replay y la app perderia su identidad ante el proveedor.
		public void RegistrarDevice(string deviceKey)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(deviceKey);

			app.Using("bitacora.RegistrarDevice(@device);")
			.WithParameters(p => {
				p["device", typeof(string)] = deviceKey;
			})
			.PerformCommand();
		}

		public void GuardarCredencial(string token, string refreshToken)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(token);

			app.Using("bitacora.GuardarCredencial(@token, @refresh);")
			.WithParameters(p => {
				p["token",   typeof(string)] = token;
				p["refresh", typeof(string)] = refreshToken ?? "";
			})
			.PerformCommand();
		}

		// ------------------------------------------- comandos: eventos de la estacion

		// Aplica UN evento como UN comando. Devuelve "" si entro, o el mensaje del Check
		// si se rechazo.
		//
		// El checkpoint (bitacora.Avanzar) va DENTRO del mismo comando que inserta la
		// carga: o entraron los dos o no entro ninguno. El Check es la idempotencia: un
		// evento redelivered se rechaza bajo el read lock y NO deja entrada en el
		// journal - por eso es un Check y no un if en el cuerpo.
		public string AplicarCargaRegistrada(CargaRegistrada evento)
		{
			ArgumentNullException.ThrowIfNull(evento);
			if (evento.Evento <= 0)
				throw new ArgumentOutOfRangeException(nameof(evento), evento.Evento,
					"El numero de evento debe ser positivo.");

			// EL RECONOCIMIENTO DE LOS CONJUNTOS CERRADOS VIVE ACA, no en la frontera.
			// Los nombres de los tipos van DENTRO del script: el motor los resuelve por
			// reflexion -que no tiene frontera de assembly- asi que el dominio se queda
			// entero `internal` y este proyecto no nombra un solo tipo suyo en C#.
			// Rechazar por Check no deja entrada en el journal, igual que la idempotencia.
			return app.Using(
				scriptForChk: @"
					Check(@evento > bitacora.UltimoEvento) Error 'evento ya procesado';
					Check(TipoDeCombustible.EsConocido(@tipo)) Error 'tipo de combustible no modelado';
					Check(UnidadDeVolumen.EsConocida(@volumenUnidad)) Error 'unidad de volumen no modelada';
					Check(Moneda.EsConocida(@precioMoneda)) Error 'moneda del precio no modelada';
					Check(Moneda.EsConocida(@totalMoneda)) Error 'moneda del total no modelada';
					Check(FormaDeCorte.EsConocida(@corte)) Error 'forma de corte no modelada';
				",
				@"
					{
						carga = Carga(@fecha, @estacion, @surtidor, @tipo,
							@volumenValor, @volumenUnidad, @precioMonto, @precioMoneda,
							@totalMonto, @totalMoneda, @corte, @placa, @transaccion);
						bitacoraDeCargas.Agregar(carga);
						bitacora.Avanzar(@evento);
					}
				")
			.WithParameters(p => {
				p["evento",        typeof(long)]     = evento.Evento;
				p["fecha",         typeof(DateTime)] = evento.Fecha;
				p["estacion",      typeof(string)]   = evento.SurtidorEstacion;
				p["surtidor",      typeof(string)]   = evento.SurtidorIdentificacion;
				p["tipo",          typeof(string)]   = evento.TipoDeCombustible;
				p["volumenValor",  typeof(decimal)]  = evento.VolumenValor;
				p["volumenUnidad", typeof(string)]   = evento.VolumenUnidad;
				p["precioMonto",   typeof(decimal)]  = evento.PrecioMonto;
				p["precioMoneda",  typeof(string)]   = evento.PrecioMoneda;
				p["totalMonto",    typeof(decimal)]  = evento.TotalMonto;
				p["totalMoneda",   typeof(string)]   = evento.TotalMoneda;
				p["corte",         typeof(string)]   = evento.FormaDeCorte;
				p["placa",         typeof(string)]   = evento.Placa;
				p["transaccion",   typeof(string)]   = evento.NumeroDeTransaccion;
			})
			.PerformCheckThenCommand();
		}

		public string AplicarPuntosAcreditados(PuntosAcreditados evento)
		{
			ArgumentNullException.ThrowIfNull(evento);

			return app.Using(
				scriptForChk: @"
					Check(@evento > bitacora.UltimoEvento) Error 'evento ya procesado';
				",
				@"
					{
						bitacoraDeCargas.AcreditarPuntos(@puntos);
						bitacora.Avanzar(@evento);
					}
				")
			.WithParameters(p => {
				p["evento", typeof(long)] = evento.Evento;
				p["puntos", typeof(int)]  = evento.Puntos;
			})
			.PerformCheckThenCommand();
		}

		public string AplicarNivelActualizado(NivelActualizado evento)
		{
			ArgumentNullException.ThrowIfNull(evento);

			return app.Using(
				scriptForChk: @"
					Check(@evento > bitacora.UltimoEvento) Error 'evento ya procesado';
					Check(NivelDeLealtad.EsConocido(@nivel)) Error 'nivel de lealtad no modelado';
				",
				@"
					{
						bitacoraDeCargas.ActualizarNivel(@nivel);
						bitacora.Avanzar(@evento);
					}
				")
			.WithParameters(p => {
				p["evento", typeof(long)]   = evento.Evento;
				p["nivel",  typeof(string)] = evento.Nivel;
			})
			.PerformCheckThenCommand();
		}

		// --------------------------------------------- comandos: los aportes de Sonia

		// Los aportes NO llevan numero de evento: no vienen de URL2 y la idempotencia
		// del checkpoint no les aplica. Cada uno lleva SU guarda -la carga existe, el
		// odometro no retrocede- y un aporte rechazado no deja entrada en el journal.
		//
		// Estos comandos son la razon de que journal.reconstruible sea false en el
		// manifiesto: borrar la carpeta y refrescar recupera lo de la institucion, pero
		// esto solo vive aca.

		/// Devuelve "" si entro, o el mensaje del Check si se rechazo (por ejemplo, un
		/// odometro que retrocede: error de captura, no se escribe).
		public string AnotarOdometro(string transaccion, decimal kilometros, string unidad,
			string origen)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(transaccion);

			return app.Using(
				scriptForChk: @"
					Check(bitacoraDeCargas.PuedeAnotarOdometro(@transaccion, @kilometros, @unidad))
						Error 'el kilometraje no es coherente con la serie o la carga no existe';
				",
				@"
					bitacoraDeCargas.AnotarOdometro(@transaccion, @kilometros, @unidad, @origen);
				")
			.WithParameters(p => {
				p["transaccion", typeof(string)]  = transaccion;
				p["kilometros",  typeof(decimal)] = kilometros;
				p["unidad",      typeof(string)]  = unidad;
				p["origen",      typeof(string)]  = origen;
			})
			.PerformCheckThenCommand();
		}

		public string DeclararTanque(string transaccion, string estado)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(transaccion);

			return app.Using(
				scriptForChk: @"
					Check(bitacoraDeCargas.ConoceTransaccion(@transaccion))
						Error 'no hay una carga con esa transaccion';
				",
				@"
					bitacoraDeCargas.DeclararTanque(@transaccion, @estado);
				")
			.WithParameters(p => {
				p["transaccion", typeof(string)] = transaccion;
				p["estado",      typeof(string)] = estado;
			})
			.PerformCheckThenCommand();
		}

		// El visto bueno a la inferencia de la forma de corte: el estimado pasa a
		// confirmado y recien ahi puede abrir o cerrar tramos.
		public string ConfirmarTanque(string transaccion)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(transaccion);

			return app.Using(
				scriptForChk: @"
					Check(bitacoraDeCargas.PuedeConfirmarTanque(@transaccion))
						Error 'no hay una inferencia de tanque esperando confirmacion';
				",
				@"
					bitacoraDeCargas.ConfirmarTanque(@transaccion);
				")
			.WithParameters(p => {
				p["transaccion", typeof(string)] = transaccion;
			})
			.PerformCheckThenCommand();
		}

		public string AdjuntarFoto(string transaccion, string nombreDeArchivo)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(transaccion);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombreDeArchivo);

			return app.Using(
				scriptForChk: @"
					Check(bitacoraDeCargas.ConoceTransaccion(@transaccion))
						Error 'no hay una carga con esa transaccion';
				",
				@"
					bitacoraDeCargas.AdjuntarFoto(@transaccion, @nombre);
				")
			.WithParameters(p => {
				p["transaccion", typeof(string)] = transaccion;
				p["nombre",      typeof(string)] = nombreDeArchivo;
			})
			.PerformCheckThenCommand();
		}

		// El cero en el rendimiento de fabrica es el "no lo se" del formulario; la
		// bitacora lo escala al tipo de ausencia antes de tocar el dominio.
		public string CompletarFicha(string placa, string alias, string marca, string modelo,
			int anio, string color, decimal rendimientoDeFabricaKmPorLitro)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(placa);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(alias);

			return app.Using(
				scriptForChk: @"
					Check(bitacoraDeCargas.ConocePlaca(@placa))
						Error 'no hay un vehiculo con esa placa';
				",
				@"
					bitacoraDeCargas.CompletarFicha(@placa, @alias, @marca, @modelo,
						@anio, @color, @rendimientoDeFabrica);
				")
			.WithParameters(p => {
				p["placa",                typeof(string)]  = placa;
				p["alias",                typeof(string)]  = alias;
				p["marca",                typeof(string)]  = marca ?? "";
				p["modelo",               typeof(string)]  = modelo ?? "";
				p["anio",                 typeof(int)]     = anio;
				p["color",                typeof(string)]  = color ?? "";
				p["rendimientoDeFabrica", typeof(decimal)] = rendimientoDeFabricaKmPorLitro;
			})
			.PerformCheckThenCommand();
		}

		// ---------------------------------------------------------------- consultas

		// Valores TIPADOS, no texto parseado: es lo que le permite a las tools MCP
		// devolver structuredContent con numeros de verdad, y a la UI hacer aritmetica
		// sin adivinar el formato del print.
		public int CantidadDeCargas()
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@cantidad = bitacoraDeCargas.Cantidad;")
				.WithParameters(alquilados, p => {
					p[Parameter.Out, "cantidad", typeof(int)] = default;
				})
				.PerformQuery();

				return alquilados["cantidad"].GetValue<int>();
			}
		}

		public int CantidadDeVehiculos()
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@cantidad = bitacoraDeCargas.CantidadDeVehiculos;")
				.WithParameters(alquilados, p => {
					p[Parameter.Out, "cantidad", typeof(int)] = default;
				})
				.PerformQuery();

				return alquilados["cantidad"].GetValue<int>();
			}
		}

		public long UltimoEventoProcesado()
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@ultimo = bitacora.UltimoEvento;")
				.WithParameters(alquilados, p => {
					p[Parameter.Out, "ultimo", typeof(long)] = default;
				})
				.PerformQuery();

				return alquilados["ultimo"].GetValue<long>();
			}
		}

		public string TokenGuardado() => Texto("bitacora.Token");

		public string RefreshTokenGuardado() => Texto("bitacora.RefreshToken");

		public string DeviceKeyGuardado() => Texto("bitacora.DeviceKey");

		private string Texto(string expresion)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using($"@valor = {expresion};")
				.WithParameters(alquilados, p => {
					p[Parameter.Out, "valor", typeof(string)] = default;
				})
				.PerformQuery();

				return alquilados["valor"].GetValue<string>() ?? "";
			}
		}

		// Los vehiculos que la bitacora conoce. La IA y la UI lo usan para saber por que
		// placa pueden preguntar sin adivinar.
		public string ListaDeVehiculos() =>
			app.Using(@"
				foreach (vehiculo in bitacoraDeCargas.Vehiculos) {
					print vehiculo.TextoDeLaPlaca placa,
						vehiculo.NombreParaMostrar nombre,
						vehiculo.TieneFicha tieneFicha,
						vehiculo.CantidadDeCargas cargas;
				}
			")
			.PerformQuery();

		public IReadOnlyList<VehiculoEnLista> Vehiculos() =>
			ProyeccionDeVistas.Vehiculos(ListaDeVehiculos());

		// El historial de un vehiculo. La proyeccion se AUTORA aca, hoja por hoja: no se
		// puede hacer `print bitacoraDeCargas.CargasDe(...)` porque una coleccion de
		// objetos de dominio es material, no un valor.
		public string ListaDeCargas(string placa)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(placa);

			return app.Using(@"
				foreach (carga in bitacoraDeCargas.CargasDe(@placa)) {
					print carga.Fecha fecha,
						carga.NombreDeLaEstacion estacion,
						carga.IdentificacionDelSurtidor surtidor,
						carga.NombreDelTipo tipo,
						carga.Litros litros,
						carga.VolumenExpresado volumen,
						carga.PrecioPorUnidad precio,
						carga.MontoDelTotal total,
						carga.MonedaDelTotal moneda,
						carga.NombreDelCorte corte,
						carga.KilometrajeExpresado kilometraje,
						carga.TanqueExpresado tanque,
						carga.NumeroDeLaTransaccion transaccion,
						carga.TieneKilometraje tieneKilometraje,
						carga.TieneFoto tieneFoto;
				}
			")
			.WithParameters(p => { p["placa", typeof(string)] = placa; })
			.PerformQuery();
		}

		public IReadOnlyList<CargaEnLista> Cargas(string placa) =>
			ProyeccionDeVistas.Cargas(ListaDeCargas(placa));

		// LOS TRAMOS: los calculados con su rendimiento y los bloqueados con lo que les
		// falta, en la misma lista. Es la consulta que ninguna pantalla de la estacion
		// puede dar, porque exige el aporte de Sonia.
		public string ListaDeTramos(string placa)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(placa);

			return app.Using(@"
				foreach (tramo in bitacoraDeCargas.TramosDe(@placa)) {
					print tramo.Desde desde,
						tramo.Hasta hasta,
						tramo.EstaCalculado estaCalculado,
						tramo.Kilometros kilometros,
						tramo.Litros litros,
						tramo.RendimientoEnKmPorLitro rendimiento,
						tramo.EsComparable esComparable,
						tramo.MotivoDeReserva motivoDeReserva,
						tramo.EsSospechoso esSospechoso,
						tramo.QueFalta queFalta,
						tramo.Expresado expresado;
				}
			")
			.WithParameters(p => { p["placa", typeof(string)] = placa; })
			.PerformQuery();
		}

		public IReadOnlyList<TramoEnLista> Tramos(string placa) =>
			ProyeccionDeVistas.Tramos(ListaDeTramos(placa));

		// LA consulta que justifica el dominio: la lectura de la serie entera, con su
		// veredicto semantico.
		//
		// El bloque mantiene `evaluacion` local: en una consulta una asignacion de
		// primer nivel seria escritura de global, prohibida en contexto read-only.
		public string EvaluacionDeRendimiento(string placa)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(placa);

			return app.Using(@"
				{
					evaluacion = bitacoraDeCargas.EvaluarRendimientoDe(@placa);
					print evaluacion.NombreDelVeredicto veredicto,
						evaluacion.NombreDeLaSeveridad severidad,
						evaluacion.ParaSonia paraSonia,
						evaluacion.AccionLegible accion,
						evaluacion.PideAlgo pideAlgo,
						evaluacion.ActualKmPorLitro actual,
						evaluacion.HabitualKmPorLitro habitual,
						evaluacion.CambioProporcional cambioProporcional,
						evaluacion.CambioPorcentual cambioPorcentual,
						evaluacion.TramosCalculados tramosCalculados,
						evaluacion.TramosBloqueados tramosBloqueados,
						evaluacion.HaySerie haySerie,
						evaluacion.QueFalta queFalta,
						evaluacion.MotivoDeReserva motivoDeReserva,
						evaluacion.EstaMejorando estaMejorando,
						evaluacion.EstaEmpeorando estaEmpeorando,
						evaluacion.SugiereRegistrarLaCargaQueFalto sugiereRegistrarLaCargaQueFalto;
				}
			")
			.WithParameters(p => { p["placa", typeof(string)] = placa; })
			.PerformQuery();
		}

		public RendimientoDelVehiculo Rendimiento(string placa) =>
			ProyeccionDeVistas.Rendimiento(EvaluacionDeRendimiento(placa));

		// LA AUTONOMIA: capacidad estimada por rendimiento habitual. Combina dos
		// derivaciones; no esta en ninguna pantalla; siempre es proyeccion.
		public string AutonomiaEstimada(string placa)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(placa);

			return app.Using(@"
				{
					autonomia = bitacoraDeCargas.AutonomiaDe(@placa);
					print autonomia.Hay hay,
						autonomia.Kilometros kilometros,
						autonomia.CapacidadEnLitros capacidad,
						autonomia.HabitualKmPorLitro habitual,
						autonomia.EsProyeccion esProyeccion;
				}
			")
			.WithParameters(p => { p["placa", typeof(string)] = placa; })
			.PerformQuery();
		}

		public AutonomiaDelVehiculo Autonomia(string placa) =>
			ProyeccionDeVistas.Autonomia(AutonomiaEstimada(placa));

		// Cuanto del aporte de Sonia ya esta: la linea discreta, nunca el reproche.
		public string CompletitudDeDatos(string placa)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(placa);

			return app.Using(@"
				{
					completitud = bitacoraDeCargas.CompletitudDe(@placa);
					print completitud.CargasConKilometraje cargasConKilometraje,
						completitud.TotalDeCargas totalDeCargas,
						completitud.TramosCalculados tramosCalculados,
						completitud.TramosBloqueados tramosBloqueados,
						completitud.Expresada expresada;
				}
			")
			.WithParameters(p => { p["placa", typeof(string)] = placa; })
			.PerformQuery();
		}

		public CompletitudDelVehiculo Completitud(string placa) =>
			ProyeccionDeVistas.Completitud(CompletitudDeDatos(placa));

		// QUE FALTA. La consulta que ninguna pantalla puede contestar, porque una
		// pantalla solo renderiza datos que tiene y esto es una AUSENCIA.
		public string LoQueFaltaParaCalcular(string placa)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(placa);

			return app.Using(@"
				foreach (faltante in bitacoraDeCargas.FaltantesDe(@placa)) {
					print faltante.FechaDeLaCarga fecha,
						faltante.NumeroDeLaTransaccion transaccion,
						faltante.QueFalta queFalta,
						faltante.QueDesbloquea queDesbloquea,
						faltante.ParaSonia paraSonia;
				}
			")
			.WithParameters(p => { p["placa", typeof(string)] = placa; })
			.PerformQuery();
		}

		public IReadOnlyList<FaltanteEnLista> Faltantes(string placa) =>
			ProyeccionDeVistas.Faltantes(LoQueFaltaParaCalcular(placa));

		// El kilometraje esperado para el prellenado. `hoy` viaja como parametro y no lo
		// lee el dominio: el modelo no toca el reloj.
		public string OdometroEstimado(string placa, DateTime hoy)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(placa);

			return app.Using(@"
				{
					proyeccion = bitacoraDeCargas.OdometroEstimadoDe(@placa, @hoy);
					print proyeccion.Hay hay,
						proyeccion.Kilometros kilometros,
						proyeccion.KilometrosPorDia kilometrosPorDia,
						proyeccion.DiasDesdeLaUltimaLectura diasDesdeLaUltimaLectura,
						proyeccion.Origen origen;
				}
			")
			.WithParameters(p => {
				p["placa", typeof(string)]  = placa;
				p["hoy",   typeof(DateTime)] = hoy;
			})
			.PerformQuery();
		}

		public OdometroPropuesto Odometro(string placa, DateTime hoy) =>
			ProyeccionDeVistas.Odometro(OdometroEstimado(placa, hoy));

		// Lo que le importa a la billetera, POR MONEDA: el dominio no convierte dinero.
		public string CostoPorDistancia(string placa)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(placa);

			return app.Using(@"
				foreach (costo in bitacoraDeCargas.CostoPorDistanciaDe(@placa)) {
					print costo.CodigoDeMoneda moneda,
						costo.MontoPorKilometro montoPorKilometro,
						costo.Kilometros kilometros,
						costo.Expresado expresado;
				}
			")
			.WithParameters(p => { p["placa", typeof(string)] = placa; })
			.PerformQuery();
		}

		public IReadOnlyList<CostoEnLista> Costos(string placa) =>
			ProyeccionDeVistas.Costos(CostoPorDistancia(placa));

		// Como evoluciono lo que paga por litro, POR SURTIDOR: el precio es estado del
		// instrumento, no un numero suelto.
		public string PrecioPagadoEnElTiempo(string placa)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(placa);

			return app.Using(@"
				foreach (precio in bitacoraDeCargas.PreciosDe(@placa)) {
					print precio.Fecha fecha,
						precio.PrecioPorLitro precioPorLitro,
						precio.CodigoDeMoneda moneda,
						precio.Surtidor surtidor;
				}
			")
			.WithParameters(p => { p["placa", typeof(string)] = placa; })
			.PerformQuery();
		}

		public IReadOnlyList<PrecioEnLista> Precios(string placa) =>
			ProyeccionDeVistas.Precios(PrecioPagadoEnElTiempo(placa));

		// El gasto de un mes, POR MONEDA y sobre todos los vehiculos.
		public string GastoDelPeriodo(int anio, int mes)
		{
			return app.Using(@"
				foreach (gasto in bitacoraDeCargas.GastoDelPeriodo(@anio, @mes)) {
					print gasto.CodigoDeMoneda moneda,
						gasto.Monto monto,
						gasto.CantidadDeCargas cargas,
						gasto.Litros litros,
						gasto.Expresado expresado;
				}
			")
			.WithParameters(p => {
				p["anio", typeof(int)] = anio;
				p["mes",  typeof(int)] = mes;
			})
			.PerformQuery();
		}

		public IReadOnlyList<GastoEnLista> Gastos(int anio, int mes) =>
			ProyeccionDeVistas.Gastos(GastoDelPeriodo(anio, mes));

		// La lealtad, discreta: presente para que se sienta real, no es el foco.
		public string EstadoDeLealtad() =>
			app.Using(@"
				{
					lealtad = bitacoraDeCargas.Lealtad;
					print lealtad.PuntosDelSaldo saldo,
						lealtad.NombreDelNivel nivel,
						lealtad.PuntosParaElSiguienteNivel paraElSiguiente;
				}
			")
			.PerformQuery();

		public EstadoDeLealtad Lealtad() =>
			ProyeccionDeVistas.Lealtad(EstadoDeLealtad());

		// ------------------------------------------------- IDiarioDeLaApp (Portador)

		string IDiarioDeLaApp.Aplicar(IMensajeDeEvento mensaje)
		{
			ArgumentNullException.ThrowIfNull(mensaje);

			if (mensaje is CargaRegistrada carga) return AplicarCargaRegistrada(carga);
			if (mensaje is PuntosAcreditados puntos) return AplicarPuntosAcreditados(puntos);
			if (mensaje is NivelActualizado nivel) return AplicarNivelActualizado(nivel);

			throw new ArgumentException(
				$"Esta app no tiene comando para un mensaje de tipo '{mensaje.Tipo}'. La ruta debio devolver null "
				+ "para descartarlo.", nameof(mensaje));
		}
	}
}
