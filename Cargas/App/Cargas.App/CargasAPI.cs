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

		// LA TOMBOLA LLEGA, NO SE CALCULA, igual que los puntos: el sorteo se rifa sobre
		// numeros y un boleto fabricado aca no sirve para reclamar nada. Lo unico que la
		// app deriva es la explicacion de cuantos dio cada carga.
		public string AplicarSorteoAnunciado(SorteoAnunciado evento)
		{
			ArgumentNullException.ThrowIfNull(evento);

			return app.Using(
				scriptForChk: @"
					Check(@evento > bitacora.UltimoEvento) Error 'evento ya procesado';
					Check(Moneda.EsConocida(@monedaDeLaRegla)) Error 'moneda de la regla no modelada';
				",
				@"
					{
						bitacoraDeCargas.AnunciarSorteo(@codigo, @nombre, @premio,
							@fechaDelSorteo, @montoPorBoleto, @monedaDeLaRegla);
						bitacora.Avanzar(@evento);
					}
				")
			.WithParameters(p => {
				p["evento",         typeof(long)]     = evento.Evento;
				p["codigo",         typeof(string)]   = evento.Codigo;
				p["nombre",         typeof(string)]   = evento.Nombre;
				p["premio",         typeof(string)]   = evento.Premio;
				p["fechaDelSorteo", typeof(DateTime)] = evento.FechaDelSorteo;
				p["montoPorBoleto", typeof(decimal)]  = evento.MontoPorBoleto;
				// EL PARAMETRO NO SE PUEDE LLAMAR @moneda: el script nombra al tipo Moneda
				// para reconocerlo, y el motor resuelve primero el parametro homonimo —
				// "Unknown property or method 'EsConocida' on type 'String'". Por eso la
				// carga usa precioMoneda y totalMoneda, y no era casualidad.
				p["monedaDeLaRegla", typeof(string)]  = evento.Moneda;
			})
			.PerformCheckThenCommand();
		}

		// UN COMANDO POR BOLETO. El numero es su identidad, asi que un boleto repetido es
		// un defecto del emisor y el dominio lo dice; la guarda del checkpoint es la que
		// para los reintentos del refresh.
		public string AplicarBoletoEmitido(BoletoEmitido evento)
		{
			ArgumentNullException.ThrowIfNull(evento);

			return app.Using(
				scriptForChk: @"
					Check(@evento > bitacora.UltimoEvento) Error 'evento ya procesado';
				",
				@"
					{
						bitacoraDeCargas.EmitirBoleto(@numero, @sorteo, @fecha, @transaccion);
						bitacora.Avanzar(@evento);
					}
				")
			.WithParameters(p => {
				p["evento",      typeof(long)]     = evento.Evento;
				p["numero",      typeof(string)]   = evento.NumeroDeBoleto;
				p["sorteo",      typeof(string)]   = evento.CodigoDelSorteo;
				p["fecha",       typeof(DateTime)] = evento.Fecha;
				p["transaccion", typeof(string)]   = evento.NumeroDeTransaccion;
			})
			.PerformCheckThenCommand();
		}

		// LA MEMBRESIA LLEGA, no se inventa. Un numero de socio fabricado aca se imprime
		// igual de lindo en el carnet y no acredita nada en la caja.
		public string AplicarMembresiaEmitida(MembresiaEmitida evento)
		{
			ArgumentNullException.ThrowIfNull(evento);

			return app.Using(
				scriptForChk: @"
					Check(@evento > bitacora.UltimoEvento) Error 'evento ya procesado';
				",
				@"
					{
						bitacoraDeCargas.Afiliar(@socio, @nombre, @desde);
						bitacora.Avanzar(@evento);
					}
				")
			.WithParameters(p => {
				p["evento", typeof(long)]     = evento.Evento;
				p["socio",  typeof(string)]   = evento.NumeroDeSocio;
				p["nombre", typeof(string)]   = evento.Nombre;
				p["desde",  typeof(DateTime)] = evento.Desde;
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
		// EN QUE UNIDADES LO MIRA ELLA. Va al journal y no al browser porque cambia lo que
		// la app DICE y no solo lo que se ve: con la preferencia en IndexedDB, la pantalla
		// mostraria mpg mientras su asistente le contesta en km/L sobre la misma bitacora.
		//
		// SIN Check, como DeclararSexo en examenes: viene de un boton y volver a elegir lo
		// mismo es legitimo. El simbolo viaja como @param -nunca concatenado al script- y
		// el dominio lo RECONOCE antes de guardarlo: uno desconocido journalizado dejaria
		// la bitacora irreproducible para cualquier binario que no lo conozca.
		public string ElegirPresentacionDeRendimiento(string simbolo)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(simbolo);

			return app.Using("bitacoraDeCargas.ElegirPresentacionDeRendimiento(@simbolo);")
				.WithParameters(p => { p["simbolo", typeof(string)] = simbolo; })
				.PerformCommand();
		}

		public string ElegirUnidadDeDistancia(string simbolo)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(simbolo);

			return app.Using("bitacoraDeCargas.ElegirUnidadDeDistancia(@simbolo);")
				.WithParameters(p => { p["simbolo", typeof(string)] = simbolo; })
				.PerformCommand();
		}

		/// <summary>Como lo mira hoy, para que la pantalla pueda marcar la opcion elegida.</summary>
		public string ComoLoMira() =>
			app.Using(@"
				{
					print bitacoraDeCargas.SimboloDelRendimiento rendimiento,
						bitacoraDeCargas.SimboloDeLaDistancia distancia;
				}
			").PerformQuery();

		public UnidadesElegidas Unidades() => ProyeccionDeVistas.Unidades(ComoLoMira());

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
						vehiculo.CantidadDeCargas cargas,
						vehiculo.AliasDeLaFicha alias,
						vehiculo.MarcaDeLaFicha marca,
						vehiculo.ModeloDeLaFicha modelo,
						vehiculo.AnioDeLaFicha anio,
						vehiculo.ColorDeLaFicha color,
						vehiculo.RendimientoDeFabricaDeLaFicha rendimientoDeFabrica;
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
						carga.KilometrajeAnotado kilometrajeAnotado,
						carga.OrigenDelKilometraje origenDelKilometraje,
						carga.TanqueExpresado tanque,
						carga.ElKilometrajeCuentaParaCalcular cuentaParaCalcular,
						carga.NumeroDeLaTransaccion transaccion,
						carga.TieneKilometraje tieneKilometraje,
						carga.TieneFoto tieneFoto,
						carga.NombreDeLaFoto foto,
						carga.KilometrosAnotados kilometros,
						carga.UnidadDelKilometraje unidad,
						carga.TanqueFueDeclarado tanqueDeclarado,
						carga.EstadoDelTanqueDeclarado estadoDelTanque,
						carga.OrigenDelTanque origenDelTanque;
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
						tramo.Expresado expresado,
						tramo.RendimientoComoSeVe rendimientoComoSeVe,
						tramo.KilometrosComoSeVen kilometrosComoSeVen,
						tramo.SimboloDelRendimiento simboloDelRendimiento,
						tramo.SimboloDeLaDistancia simboloDeLaDistancia;
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
						evaluacion.SugiereRegistrarLaCargaQueFalto sugiereRegistrarLaCargaQueFalto,
						evaluacion.ActualComoSeVe actualComoSeVe,
						evaluacion.HabitualComoSeVe habitualComoSeVe,
						evaluacion.CambioPorcentualComoSeVe cambioPorcentualComoSeVe,
						evaluacion.SimboloDelRendimiento simbolo;
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

		// EL RITMO: cuanto se maneja este carro, de la serie del odometro. Es la mitad que
		// el concesionario declara no tener -su what_is_left_before_each_service dice que
		// no puede decir cual mitad del intervalo llega primero "porque este registro no
		// tiene ese ritmo"-, y la unica que puede darla es esta bitacora, que ve el
		// odometro en cada carga y no una vez por service.
		//
		// La situacion viaja con el numero: cuando no se pudo medir, cual de las tres
		// razones es. Y la frase la compone el dominio, no esta capa.
		public string RitmoDeManejo(string placa)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(placa);

			return app.Using(@"
				{
					ritmo = bitacoraDeCargas.RitmoDe(@placa);
					print ritmo.NombreDeLaSituacion situacion,
						ritmo.SePudoMedir sePudoMedir,
						ritmo.Expresada expresada,
						ritmo.KilometrosPorDia kilometrosPorDia,
						ritmo.Kilometros kilometros,
						ritmo.Dias dias,
						ritmo.Desde desde,
						ritmo.Hasta hasta,
						ritmo.Lecturas lecturas,
						ritmo.PorDiaComoSeVe porDiaComoSeVe,
						ritmo.KilometrosComoSeVen kilometrosComoSeVen,
						ritmo.SimboloDeLaDistancia simboloDeLaDistancia;
				}
			")
			.WithParameters(p => { p["placa", typeof(string)] = placa; })
			.PerformQuery();
		}

		public RitmoDelVehiculo Ritmo(string placa) =>
			ProyeccionDeVistas.Ritmo(RitmoDeManejo(placa));

		// EN CUANTOS DIAS VA A TENER QUE CARGAR. No hay un PerformQuery propio: es la
		// COMPOSICION de dos consultas que ya existen, y por eso es un metodo de la App
		// y no un verbo del dominio. Lo unico que agrega es la resta, y la resta no es
		// una regla de negocio: es aritmetica sobre dos derivaciones ya juzgadas.
		//
		// HOY LO PONE EL LLAMADOR. El dominio no lee el reloj -si lo hiciera, el mismo
		// journal reproducido manana daria otra respuesta-, asi que la caratula pasa su
		// "hoy" igual que se lo pasa a Odometro().
		//
		// Estaba en la tool del MCP y se subio aca cuando el tablero pidio la misma
		// cifra: dos derivaciones separadas es como la IA y la persona terminan diciendo
		// numeros distintos del mismo carro.
		public ProximaCargaDelVehiculo ProximaCarga(string placa, DateTime hoy)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(placa);

			AutonomiaDelVehiculo autonomia = Autonomia(placa);
			OdometroPropuesto proyeccion = Odometro(placa, hoy);
			if (!autonomia.Hay || !proyeccion.Hay || proyeccion.KilometrosPorDia <= 0m
				|| autonomia.Kilometros <= 0m)
				return new ProximaCargaDelVehiculo();

			// Lo recorrido desde la ultima lectura, proyectado con su promedio; lo que
			// queda es la autonomia menos eso. Aproximacion declarada: la ultima lectura
			// no siempre coincide con la ultima carga llena.
			decimal recorridos = proyeccion.KilometrosPorDia * proyeccion.DiasDesdeLaUltimaLectura;
			decimal restantes = Math.Max(0m, autonomia.Kilometros - recorridos);

			return new ProximaCargaDelVehiculo
			{
				Hay = true,
				DiasEstimados = (int)Math.Round(restantes / proyeccion.KilometrosPorDia),
				KilometrosRestantes = Math.Round(restantes),
				KilometrosDeTanqueLleno = Math.Round(autonomia.Kilometros),
				KilometrosPorDia = Math.Round(proyeccion.KilometrosPorDia, 1),
				EsProyeccion = true,
				FraccionRestante = restantes / autonomia.Kilometros,
			};
		}

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
						faltante.ParaSonia paraSonia,
						faltante.LoQueSeSupone loQueSeSupone,
						faltante.LaSuposicion laSuposicion,
						faltante.NaceDeUnaSuposicion naceDeUnaSuposicion;
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
						proyeccion.Origen origen,
						proyeccion.KilometrosComoSeVen kilometrosComoSeVen,
						proyeccion.PorDiaComoSeVe porDiaComoSeVe,
						proyeccion.SimboloDeLaDistancia simboloDeLaDistancia;
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

		// EL GASTO DE UN CARRO. El tablero es de un vehiculo, asi que su instrumento de
		// gasto tiene que serlo tambien: con dos carros, el gasto de todos conviviendo con
		// la autonomia y el rendimiento de uno solo miente por omision.
		public string GastoDelPeriodoDe(string placa, int anio, int mes)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(placa);

			return app.Using(@"
				foreach (gastoDelCarro in bitacoraDeCargas.GastoDelPeriodoDe(@placa, @anio, @mes)) {
					print gastoDelCarro.CodigoDeMoneda moneda,
						gastoDelCarro.Monto monto,
						gastoDelCarro.CantidadDeCargas cargas,
						gastoDelCarro.Litros litros,
						gastoDelCarro.Expresado expresado;
				}
			")
			.WithParameters(p => {
				p["placa", typeof(string)] = placa;
				p["anio",  typeof(int)]    = anio;
				p["mes",   typeof(int)]    = mes;
			})
			.PerformQuery();
		}

		public IReadOnlyList<GastoEnLista> GastosDe(string placa, int anio, int mes) =>
			ProyeccionDeVistas.Gastos(GastoDelPeriodoDe(placa, anio, mes), "gastoDelCarro");

		// La lealtad, discreta: presente para que se sienta real, no es el foco.
		// LAS TOMBOLAS. El "hoy" lo pone la caratula, como en Odometro y ProximaCarga: el
		// dominio no lee el reloj, y si lo hiciera el mismo journal reproducido manana
		// diria que un sorteo ya venció.
		//
		// Las tres variables del foreach son DISTINTAS a proposito: el parser llavea por el
		// nombre de la variable, no por la primera columna, y dos vistas con la misma llave
		// se pisarian sin error.
		public string ListaDeSorteos(DateTime hoy) =>
			app.Using(@"
				foreach (sorteo in bitacoraDeCargas.BoletosPorSorteo(@hoy)) {
					print sorteo.Codigo codigo,
						sorteo.Nombre nombre,
						sorteo.Premio premio,
						sorteo.FueAnunciado fueAnunciado,
						sorteo.FechaDelSorteo fechaDelSorteo,
						sorteo.CantidadDeBoletos boletos,
						sorteo.EstaVencido estaVencido,
						sorteo.DiasParaElSorteo diasParaElSorteo,
						sorteo.MontoPorBoletoExpresado montoPorBoleto,
						sorteo.ParaSonia paraSonia;
				}
			")
			.WithParameters(p => { p["hoy", typeof(DateTime)] = hoy; })
			.PerformQuery();

		public IReadOnlyList<SorteoEnLista> Sorteos(DateTime hoy) =>
			ProyeccionDeVistas.Sorteos(ListaDeSorteos(hoy));

		public string BoletosPorCarga(string placa)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(placa);

			return app.Using(@"
				foreach (explicacion in bitacoraDeCargas.BoletosPorCargaDe(@placa)) {
					print explicacion.FechaDeLaCarga fecha,
						explicacion.NumeroDeLaTransaccion transaccion,
						explicacion.CodigoDelSorteo sorteo,
						explicacion.NombreDelSorteo nombreDelSorteo,
						explicacion.HayRegla hayRegla,
						explicacion.BoletosQueDaLaRegla queDaLaRegla,
						explicacion.BoletosEmitidos emitidos,
						explicacion.SobranteExpresado sobrante,
						explicacion.LaCuentaCuadra cuadra,
						explicacion.ProbablementeFaltaUnDato probablementeFaltaUnDato,
						explicacion.ParaSonia paraSonia;
				}
			")
			.WithParameters(p => { p["placa", typeof(string)] = placa; })
			.PerformQuery();
		}

		public IReadOnlyList<BoletosDeLaCargaEnLista> BoletosDeLasCargas(string placa) =>
			ProyeccionDeVistas.BoletosDeLasCargas(BoletosPorCarga(placa));

		public string EstadoDeLealtad() =>
			app.Using(@"
				{
					lealtad = bitacoraDeCargas.Lealtad;
					print lealtad.PuntosDelSaldo saldo,
						lealtad.NombreDelNivel nivel,
						lealtad.PuntosParaElSiguienteNivel paraElSiguiente,
						lealtad.EstaAfiliada afiliada,
						lealtad.NumeroDeSocio socio,
						lealtad.NombreDelSocio nombreDelSocio,
						lealtad.SocioDesde socioDesde;
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
			if (mensaje is SorteoAnunciado sorteo) return AplicarSorteoAnunciado(sorteo);
			if (mensaje is BoletoEmitido boleto) return AplicarBoletoEmitido(boleto);
			if (mensaje is MembresiaEmitida membresia) return AplicarMembresiaEmitida(membresia);

			throw new ArgumentException(
				$"Esta app no tiene comando para un mensaje de tipo '{mensaje.Tipo}'. La ruta debio devolver null "
				+ "para descartarlo.", nameof(mensaje));
		}
	}
}
