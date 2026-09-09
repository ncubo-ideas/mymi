using System;
using System.Globalization;
using Choreography.Theater;
using Municipalidad.App.Events;
using Puppeteer;
using Puppets.Portador;

namespace Municipalidad.App
{
	// LA SUPERFICIE INTENCIONAL. Un metodo por intencion, con nombre de negocio.
	//
	// ACA SE RESUELVEN LOS DOS ALFABETOS DE `Periodo`. El recibo escribe "2026.04" y eso
	// es abril en una fila y cuarto trimestre en otra. Este nodo parte el texto en anio y
	// numero, y el dominio recibe los dos enteros: la cadencia del rubro decide que son.
	// Si esa logica entrara al dominio, el dominio hablaria el idioma del portal.
	//
	// OJO CON LOS NOMBRES DE PARAMETRO (regla 10). Ninguno se llama como un tipo del
	// dominio: un @rubro taparia a Rubro, un @periodo a Periodo, un @pago a Pago, un
	// @consulta a Consulta, un @finca a Finca. Por eso son @nombreDelRubro,
	// @anioDelPeriodo, @numeroDeRecibo, @fechaDeLaConsulta, @numeroDeFinca.
	public sealed class MunicipalidadAPI : IDiarioDeLaApp
	{
		private readonly PerformanceV2 app;

		public MunicipalidadAPI(PerformanceV2 app)
		{
			ArgumentNullException.ThrowIfNull(app);
			this.app = app;
		}

		// EL ADAPTADOR DEL ALFABETO: "2026.04" -> (2026, 4). Es una guarda de C# antes de
		// tocar el actor (capitulo 3 §5, paso 1): un texto que no tenga esa forma nunca
		// llega al dominio.
		private static void Partir(string periodoComoDiceElRecibo, out int anio, out int numero)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(periodoComoDiceElRecibo);
			if (!DiceUnPeriodoComoLoEscribeElRecibo(periodoComoDiceElRecibo, out anio, out numero))
				throw new ArgumentException(
					"El periodo tiene que venir como AAAA.NN, como lo escribe el recibo: '" + periodoComoDiceElRecibo + "'.",
					nameof(periodoComoDiceElRecibo));
		}

		/// <summary>
		/// Si ese texto dice un periodo en el alfabeto del recibo -- AAAA.NN --, y lo parte en anio y
		/// numero. TOTAL: contesta que no en vez de reventar, para que un interprete de la fuente pueda
		/// descartar la senal antes de que el texto llegue a un comando. Que el numero sea un mes o un
		/// trimestre lo decide la cadencia del rubro, y eso no se pregunta aca.
		/// </summary>
		internal static bool DiceUnPeriodoComoLoEscribeElRecibo(
			string periodoComoDiceElRecibo, out int anio, out int numero)
		{
			anio = 0;
			numero = 0;
			if (string.IsNullOrWhiteSpace(periodoComoDiceElRecibo)) return false;
			string[] partes = periodoComoDiceElRecibo.Trim().Split('.');
			return partes.Length == 2
				&& int.TryParse(partes[0], NumberStyles.None, CultureInfo.InvariantCulture, out anio)
				&& int.TryParse(partes[1], NumberStyles.None, CultureInfo.InvariantCulture, out numero);
		}

		// ------------------------------------------------------------------ comandos

		public string IncorporarFinca(string numeroDeFinca)
		{
			return app.Using(
				scriptForChk: @"
					Check(cuenta.PuedeIncorporarFinca(@numeroDeFinca)) Error 'esa finca ya esta, o el numero no sirve';
				",
				@"
					{
						cuenta.IncorporarFinca(@numeroDeFinca);
					}
				")
			.WithParameters(p => {
				p["numeroDeFinca", typeof(string)] = numeroDeFinca;
			})
			.PerformCheckThenCommand();
		}

		// LA PERSONA DECLARA EL VALOR DE SU FINCA. Es de ella, no de la municipalidad: por
		// eso este dominio es hibrido. Hueco 2.
		public string DeclararValorDeFinca(string numeroDeFinca, DateTime fechaDeLaDeclaracion, decimal valorDeclarado)
		{
			return app.Using(
				scriptForChk: @"
					Check(cuenta.PuedeDeclararValorDeFinca(@numeroDeFinca, @fechaDeLaDeclaracion, @valorDeclarado)) Error 'Ese valor no se pudo declarar. Tiene que ser una finca que la municipalidad ya haya mandado, un monto mayor que cero, y con fecha posterior a la última declaración que ya hiciste de esa finca: se declara una vez por día.';
				",
				@"
					{
						cuenta.DeclararValorDeFinca(@numeroDeFinca, @fechaDeLaDeclaracion, @valorDeclarado);
					}
				")
			.WithParameters(p => {
				p["numeroDeFinca", typeof(string)] = numeroDeFinca;
				p["fechaDeLaDeclaracion", typeof(DateTime)] = fechaDeLaDeclaracion;
				p["valorDeclarado", typeof(decimal)] = valorDeclarado;
			})
			.PerformCheckThenCommand();
		}

		// LA CONSULTA ES UNA FOTO, FECHADA. Se registra el dia y despues se le agregan sus
		// pendientes uno por uno, porque asi llegan.
		public string RegistrarConsulta(DateTime fechaDeLaConsulta)
		{
			return app.Using(
				scriptForChk: @"
					Check(cuenta.PuedeRegistrarConsulta(@fechaDeLaConsulta)) Error 'ya hay una consulta de ese dia';
				",
				@"
					{
						cuenta.RegistrarConsulta(@fechaDeLaConsulta);
					}
				")
			.WithParameters(p => {
				p["fechaDeLaConsulta", typeof(DateTime)] = fechaDeLaConsulta;
			})
			.PerformCheckThenCommand();
		}

		public string AgregarPendiente(DateTime fechaDeLaConsulta, string nombreDelRubro, string periodoComoDiceElRecibo,
			decimal montoPendiente, decimal interesesPendientes)
		{
			Partir(periodoComoDiceElRecibo, out int anio, out int numero);
			return app.Using(
				scriptForChk: @"
					Check(cuenta.PuedeAgregarPendiente(@fechaDeLaConsulta, @nombreDelRubro, @anioDelPeriodo, @numeroDelPeriodo, @montoPendiente, @interesesPendientes)) Error 'la consulta no esta, el rubro no existe, el periodo no se lee para ese rubro, los montos no sirven, o ya estaba';
				",
				@"
					{
						cuenta.AgregarPendiente(@fechaDeLaConsulta, @nombreDelRubro, @anioDelPeriodo, @numeroDelPeriodo, @montoPendiente, @interesesPendientes);
					}
				")
			.WithParameters(p => {
				p["fechaDeLaConsulta", typeof(DateTime)] = fechaDeLaConsulta;
				p["nombreDelRubro", typeof(string)] = nombreDelRubro;
				p["anioDelPeriodo", typeof(int)] = anio;
				p["numeroDelPeriodo", typeof(int)] = numero;
				p["montoPendiente", typeof(decimal)] = montoPendiente;
				p["interesesPendientes", typeof(decimal)] = interesesPendientes;
			})
			.PerformCheckThenCommand();
		}

		public string PonerAlCobro(string nombreDelRubro, string periodoComoDiceElRecibo, decimal montoAlCobro, DateTime puestoEl)
		{
			Partir(periodoComoDiceElRecibo, out int anio, out int numero);
			return app.Using(
				scriptForChk: @"
					Check(cuenta.PuedePonerAlCobro(@nombreDelRubro, @anioDelPeriodo, @numeroDelPeriodo, @montoAlCobro)) Error 'rubro desconocido, periodo ilegible, monto no valido, o ya estaba puesto al cobro';
				",
				@"
					{
						cuenta.PonerAlCobro(@nombreDelRubro, @anioDelPeriodo, @numeroDelPeriodo, @montoAlCobro, @puestoEl);
					}
				")
			.WithParameters(p => {
				p["nombreDelRubro", typeof(string)] = nombreDelRubro;
				p["anioDelPeriodo", typeof(int)] = anio;
				p["numeroDelPeriodo", typeof(int)] = numero;
				p["montoAlCobro", typeof(decimal)] = montoAlCobro;
				p["puestoEl", typeof(DateTime)] = puestoEl;
			})
			.PerformCheckThenCommand();
		}

		// EL COMPROBANTE. El recibo es la identidad (regla 18): el mismo numero dos veces es
		// el mismo hecho otra vez, y se rechaza.
		public string RegistrarPago(string numeroDeRecibo, DateTime fechaDelPago, decimal descuentoDelRecibo)
		{
			return app.Using(
				scriptForChk: @"
					Check(cuenta.PuedeRegistrarPago(@numeroDeRecibo, @fechaDelPago, @descuentoDelRecibo)) Error 'ese recibo ya estaba registrado, o el descuento no sirve';
				",
				@"
					{
						cuenta.RegistrarPago(@numeroDeRecibo, @fechaDelPago, @descuentoDelRecibo);
					}
				")
			.WithParameters(p => {
				p["numeroDeRecibo", typeof(string)] = numeroDeRecibo;
				p["fechaDelPago", typeof(DateTime)] = fechaDelPago;
				p["descuentoDelRecibo", typeof(decimal)] = descuentoDelRecibo;
			})
			.PerformCheckThenCommand();
		}

		// LA LINEA DEL COMPROBANTE. La mora viene tal cual la cobro la municipalidad.
		public string AgregarLineaAlPago(string numeroDeRecibo, string nombreDelRubro, string periodoComoDiceElRecibo,
			decimal montoDeLaLinea, decimal interesesDeLaLinea)
		{
			Partir(periodoComoDiceElRecibo, out int anio, out int numero);
			return app.Using(
				scriptForChk: @"
					Check(cuenta.PuedeAgregarLineaAlPago(@numeroDeRecibo, @nombreDelRubro, @anioDelPeriodo, @numeroDelPeriodo, @montoDeLaLinea, @interesesDeLaLinea)) Error 'el recibo no esta, el rubro no existe, el periodo no se lee para ese rubro, los montos no sirven, o ese periodo ya esta pagado';
				",
				@"
					{
						cuenta.AgregarLineaAlPago(@numeroDeRecibo, @nombreDelRubro, @anioDelPeriodo, @numeroDelPeriodo, @montoDeLaLinea, @interesesDeLaLinea);
					}
				")
			.WithParameters(p => {
				p["numeroDeRecibo", typeof(string)] = numeroDeRecibo;
				p["nombreDelRubro", typeof(string)] = nombreDelRubro;
				p["anioDelPeriodo", typeof(int)] = anio;
				p["numeroDelPeriodo", typeof(int)] = numero;
				p["montoDeLaLinea", typeof(decimal)] = montoDeLaLinea;
				p["interesesDeLaLinea", typeof(decimal)] = interesesDeLaLinea;
			})
			.PerformCheckThenCommand();
		}

		// LA RESERVA ES DE LA PERSONA. La app le propuso un numero (ReservaPropuesta, una
		// consulta); esto es lo que ella adopto. Un comando con fecha.
		public string AdoptarReserva(decimal montoMensual, DateTime fechaDeAdopcion)
		{
			return app.Using(
				scriptForChk: @"
					Check(cuenta.PuedeAdoptarReserva(@montoMensual, @fechaDeAdopcion)) Error 'Esa reserva no se pudo adoptar. Tiene que ser un monto mayor que cero, y con fecha posterior a la última que ya adoptaste: se adopta una vez por día, y hoy ya hay una.';
				",
				@"
					{
						cuenta.AdoptarReserva(@montoMensual, @fechaDeAdopcion);
					}
				")
			.WithParameters(p => {
				p["montoMensual", typeof(decimal)] = montoMensual;
				p["fechaDeAdopcion", typeof(DateTime)] = fechaDeAdopcion;
			})
			.PerformCheckThenCommand();
		}

		/// <summary>
		/// Registra esta instalacion ante la municipalidad. UNA sola vez: la misma clave otra vez es
		/// el mismo hecho otra vez, y otra clave dejaria huerfana la credencial ya emitida.
		/// </summary>
		public string RegistrarDispositivo(string claveDelDispositivo)
		{
			return app.Using(
				scriptForChk: @"
					Check(bitacora.PuedeRegistrarDispositivo(@claveDelDispositivo)) Error 'la clave no sirve, o esta instalacion ya esta registrada';
				",
				@"
					{
						bitacora.RegistrarDispositivo(@claveDelDispositivo);
					}
				")
			.WithParameters(p => {
				p["claveDelDispositivo", typeof(string)] = claveDelDispositivo;
			})
			.PerformCheckThenCommand();
		}

		/// <summary>
		/// Guarda la credencial con que se leen los eventos, o la rota por una nueva. El token de
		/// refresco es opcional y viaja vacio cuando el proveedor no dio ninguno.
		/// </summary>
		public string GuardarCredencial(string tokenDeAcceso, string refresco)
		{
			return app.Using(
				scriptForChk: @"
					Check(bitacora.PuedeGuardarCredencial(@tokenDeAcceso, @refresco)) Error 'no trae token de acceso, o es la credencial que ya estaba guardada';
				",
				@"
					{
						bitacora.GuardarCredencial(@tokenDeAcceso, @refresco);
					}
				")
			.WithParameters(p => {
				p["tokenDeAcceso", typeof(string)] = tokenDeAcceso;
				p["refresco", typeof(string)] = refresco;
			})
			.PerformCheckThenCommand();
		}

		/// <summary>
		/// Aplica un hecho recibido como UN comando: el cambio en la cuenta y el avance del punto de
		/// control, juntos o ninguno. Contesta cadena vacia si entro, o el motivo del Check si se
		/// rechazo -- que es el contrato que <see cref="IDiarioDeLaApp"/> pide.
		/// </summary>
		public string Aplicar(IMensajeDeEvento mensaje)
		{
			ArgumentNullException.ThrowIfNull(mensaje);

			if (mensaje is FincaIncorporada finca) return AplicarFincaIncorporada(finca);
			if (mensaje is CobroPuesto cobro) return AplicarCobroPuesto(cobro);
			if (mensaje is PagoRegistrado pago) return AplicarPagoRegistrado(pago);
			if (mensaje is LineaDePagoRegistrada linea) return AplicarLineaDePagoRegistrada(linea);
			if (mensaje is ConsultaTomada consulta) return AplicarConsultaTomada(consulta);
			if (mensaje is PendienteRegistrado pendiente) return AplicarPendienteRegistrado(pendiente);

			throw new ArgumentException(
				"Esta app no tiene comando para un mensaje de tipo '" + mensaje.Tipo
				+ "'. La ruta debio devolver null para descartarlo.", nameof(mensaje));
		}

		/// <summary>
		/// La primera mitad del Check de todo hecho recibido: el punto de control solo avanza, asi que
		/// un evento ya aplicado se rechaza sin dejar entrada. Va PRIMERO en todos, para que una
		/// re-entrega diga que ya se proceso en vez de contestar por que la cuenta lo habria refusado.
		/// </summary>
		private const string ElEventoEsNuevo =
			"Check(@evento > bitacora.UltimoEvento()) Error 'ese evento ya se proceso';";

		private string AplicarFincaIncorporada(FincaIncorporada mensaje)
		{
			return app.Using(
				scriptForChk: ElEventoEsNuevo + @"
					Check(cuenta.PuedeIncorporarFinca(@numeroDeFinca)) Error 'esa finca ya esta, o el numero no sirve';
				",
				@"
					{
						cuenta.IncorporarFinca(@numeroDeFinca);
						bitacora.Avanzar(@evento);
					}
				")
			.WithParameters(p => {
				p["evento", typeof(long)] = mensaje.NumeroDelEvento;
				p["numeroDeFinca", typeof(string)] = mensaje.NumeroDeFinca;
			})
			.PerformCheckThenCommand();
		}

		private string AplicarCobroPuesto(CobroPuesto mensaje)
		{
			Partir(mensaje.PeriodoComoDiceElRecibo, out int anio, out int numero);
			return app.Using(
				scriptForChk: ElEventoEsNuevo + @"
					Check(cuenta.PuedePonerAlCobro(@nombreDelRubro, @anioDelPeriodo, @numeroDelPeriodo, @montoAlCobro)) Error 'rubro desconocido, periodo ilegible, monto no valido, o ya estaba puesto al cobro';
				",
				@"
					{
						cuenta.PonerAlCobro(@nombreDelRubro, @anioDelPeriodo, @numeroDelPeriodo, @montoAlCobro, @puestoEl);
						bitacora.Avanzar(@evento);
					}
				")
			.WithParameters(p => {
				p["evento", typeof(long)] = mensaje.NumeroDelEvento;
				p["nombreDelRubro", typeof(string)] = mensaje.NombreDelRubro;
				p["anioDelPeriodo", typeof(int)] = anio;
				p["numeroDelPeriodo", typeof(int)] = numero;
				p["montoAlCobro", typeof(decimal)] = mensaje.MontoAlCobro;
				p["puestoEl", typeof(DateTime)] = mensaje.PuestoEl;
			})
			.PerformCheckThenCommand();
		}

		private string AplicarPagoRegistrado(PagoRegistrado mensaje)
		{
			return app.Using(
				scriptForChk: ElEventoEsNuevo + @"
					Check(cuenta.PuedeRegistrarPago(@numeroDeRecibo, @fechaDelPago, @descuentoDelRecibo)) Error 'ese recibo ya estaba registrado, o el descuento no sirve';
				",
				@"
					{
						cuenta.RegistrarPago(@numeroDeRecibo, @fechaDelPago, @descuentoDelRecibo);
						bitacora.Avanzar(@evento);
					}
				")
			.WithParameters(p => {
				p["evento", typeof(long)] = mensaje.NumeroDelEvento;
				p["numeroDeRecibo", typeof(string)] = mensaje.NumeroDeRecibo;
				p["fechaDelPago", typeof(DateTime)] = mensaje.FechaDelPago;
				p["descuentoDelRecibo", typeof(decimal)] = mensaje.DescuentoDelRecibo;
			})
			.PerformCheckThenCommand();
		}

		private string AplicarLineaDePagoRegistrada(LineaDePagoRegistrada mensaje)
		{
			Partir(mensaje.PeriodoComoDiceElRecibo, out int anio, out int numero);
			return app.Using(
				scriptForChk: ElEventoEsNuevo + @"
					Check(cuenta.PuedeAgregarLineaAlPago(@numeroDeRecibo, @nombreDelRubro, @anioDelPeriodo, @numeroDelPeriodo, @montoDeLaLinea, @interesesDeLaLinea)) Error 'el recibo no esta, el rubro no existe, el periodo no se lee para ese rubro, los montos no sirven, o ese periodo ya esta pagado';
				",
				@"
					{
						cuenta.AgregarLineaAlPago(@numeroDeRecibo, @nombreDelRubro, @anioDelPeriodo, @numeroDelPeriodo, @montoDeLaLinea, @interesesDeLaLinea);
						bitacora.Avanzar(@evento);
					}
				")
			.WithParameters(p => {
				p["evento", typeof(long)] = mensaje.NumeroDelEvento;
				p["numeroDeRecibo", typeof(string)] = mensaje.NumeroDeRecibo;
				p["nombreDelRubro", typeof(string)] = mensaje.NombreDelRubro;
				p["anioDelPeriodo", typeof(int)] = anio;
				p["numeroDelPeriodo", typeof(int)] = numero;
				p["montoDeLaLinea", typeof(decimal)] = mensaje.MontoDeLaLinea;
				p["interesesDeLaLinea", typeof(decimal)] = mensaje.InteresesDeLaLinea;
			})
			.PerformCheckThenCommand();
		}

		private string AplicarConsultaTomada(ConsultaTomada mensaje)
		{
			return app.Using(
				scriptForChk: ElEventoEsNuevo + @"
					Check(cuenta.PuedeRegistrarConsulta(@fechaDeLaConsulta)) Error 'ya hay una consulta de ese dia';
				",
				@"
					{
						cuenta.RegistrarConsulta(@fechaDeLaConsulta);
						bitacora.Avanzar(@evento);
					}
				")
			.WithParameters(p => {
				p["evento", typeof(long)] = mensaje.NumeroDelEvento;
				p["fechaDeLaConsulta", typeof(DateTime)] = mensaje.FechaDeLaConsulta;
			})
			.PerformCheckThenCommand();
		}

		private string AplicarPendienteRegistrado(PendienteRegistrado mensaje)
		{
			Partir(mensaje.PeriodoComoDiceElRecibo, out int anio, out int numero);
			return app.Using(
				scriptForChk: ElEventoEsNuevo + @"
					Check(cuenta.PuedeAgregarPendiente(@fechaDeLaConsulta, @nombreDelRubro, @anioDelPeriodo, @numeroDelPeriodo, @montoPendiente, @interesesPendientes)) Error 'la consulta no esta, el rubro no existe, el periodo no se lee para ese rubro, los montos no sirven, o ya estaba';
				",
				@"
					{
						cuenta.AgregarPendiente(@fechaDeLaConsulta, @nombreDelRubro, @anioDelPeriodo, @numeroDelPeriodo, @montoPendiente, @interesesPendientes);
						bitacora.Avanzar(@evento);
					}
				")
			.WithParameters(p => {
				p["evento", typeof(long)] = mensaje.NumeroDelEvento;
				p["fechaDeLaConsulta", typeof(DateTime)] = mensaje.FechaDeLaConsulta;
				p["nombreDelRubro", typeof(string)] = mensaje.NombreDelRubro;
				p["anioDelPeriodo", typeof(int)] = anio;
				p["numeroDelPeriodo", typeof(int)] = numero;
				p["montoPendiente", typeof(decimal)] = mensaje.MontoPendiente;
				p["interesesPendientes", typeof(decimal)] = mensaje.InteresesPendientes;
			})
			.PerformCheckThenCommand();
		}

		/// <summary>
		/// Lo que el bucle de refresh le pregunta al journal. Sale del JOURNAL y no de un campo en
		/// memoria: es lo que hace exacto el resume tras una caida.
		/// </summary>
		long IDiarioDeLaApp.UltimoEventoProcesado()
		{
			return UltimoEventoAplicado();
		}

		// ----------------------------------------------------------------- consultas

		/// <summary>
		/// Hasta donde leyo la app la narrativa de la municipalidad. Cero es "todavia no se leyo
		/// nada": los eventos se numeran desde uno.
		/// </summary>
		public long UltimoEventoAplicado()
		{
			return UnNumero("@ultimo = bitacora.UltimoEvento();", "ultimo", p => { });
		}

		/// <summary>
		/// El token con que se leen los eventos, o vacio si no hay credencial. PRE-PREGUNTA, porque
		/// el dominio niega la lectura sin credencial y su negativa no la puede nombrar una caratula.
		/// </summary>
		public string TokenGuardado()
		{
			return UnaVerdad("@tiene = bitacora.TieneCredencial();", "tiene", p => { })
				? UnTexto("@valor = bitacora.Token();", "valor", p => { })
				: "";
		}

		/// <summary>El token de refresco, o vacio si el proveedor no dio ninguno. PRE-PREGUNTA.</summary>
		public string TokenDeRefrescoGuardado()
		{
			return UnaVerdad("@puede = bitacora.PuedeRotar();", "puede", p => { })
				? UnTexto("@valor = bitacora.TokenDeRefresco();", "valor", p => { })
				: "";
		}

		/// <summary>La clave de esta instalacion, o vacio si nunca se registro. PRE-PREGUNTA.</summary>
		public string ClaveDeDispositivoGuardada()
		{
			return UnaVerdad("@tiene = bitacora.TieneDispositivo();", "tiene", p => { })
				? UnTexto("@valor = bitacora.ClaveDeDispositivo();", "valor", p => { })
				: "";
		}

		/// <summary>
		/// El estado de la cuenta en UN solo momento: cuantas fincas, cuantos cobros, cuantos pagos,
		/// cuantas consultas, si la persona adopto una reserva, hasta donde leyo la app la narrativa de
		/// la municipalidad, y si hay con que proyectar. Cruza las DOS globales de raiz en un script.
		/// </summary>
		public string EstadoDeLaCuenta()
		{
			return app.Using(@"
				{
					print cuenta.CuantasFincas fincas,
						cuenta.CuantosCobros cobros,
						cuenta.CuantosPagos pagos,
						cuenta.CuantasConsultas consultas,
						cuenta.TieneReservaDeclarada tieneReserva,
						bitacora.UltimoEvento() ultimoEventoLeido,
						cuenta.SePuedeProyectar sePuedeProyectar;
				}
			")
			.PerformQuery();
		}

		/// <summary>
		/// El nombre de cada rubro que existe, para que una caratula pueda iterarlos sin tenerlos
		/// escritos a mano. Lo contesta el CATALOGO: los cinco nombres no son un hecho de esta cuenta.
		/// </summary>
		public string[] LosRubrosQueHay()
		{
			return UnaListaDeTextos("@rubros = Rubro.LosQueHay();", "rubros", p => { });
		}

		/// <summary>
		/// El nombre de cada rubro del que la app vio pagar algo. TOTAL, y es el juego de rubros sobre
		/// el que HistorialDe y VariacionDe tienen algo que contestar.
		/// </summary>
		public string[] RubrosConHistoria()
		{
			return UnaListaDeTextos(
				"@rubros = cuenta.NombresDeLosRubrosConHistoria();", "rubros", p => { });
		}

		/// <summary>Si hay al menos una consulta registrada. TOTAL, y es la PRE-PREGUNTA de la de abajo.</summary>
		public bool HayAlgunaConsulta()
		{
			return UnaVerdad("@hay = cuenta.HayAlgunaConsulta;", "hay", p => { });
		}

		/// <summary>
		/// El dia de la consulta mas reciente, que es el argumento que PeriodosPagadosSinComprobante
		/// exige y que una caratula no tiene de donde sacar. GUARDADO: preguntar HayAlgunaConsulta
		/// antes.
		/// </summary>
		public DateTime ConsultaMasReciente()
		{
			return UnaFecha("@fecha = cuenta.ConsultaMasReciente;", "fecha", p => { });
		}

		/// <summary>
		/// El numero de cada finca incorporada, para que una caratula pueda iterarlas. TOTAL, y ninguna
		/// de las dos lecturas del valor se puede pedir sin uno de estos numeros.
		/// </summary>
		public string[] NumerosDeLasFincas()
		{
			return UnaListaDeTextos("@numeros = cuenta.NumerosDeLasFincas();", "numeros", p => { });
		}

		/// <summary>
		/// Si esa finca tenia un valor declarado vigente a esa fecha. TOTAL, y es la PRE-PREGUNTA de la
		/// de abajo: contesta que no tanto para una finca que no esta como para una sin valor.
		/// </summary>
		public bool TieneValorDeclaradoLaFinca(string numeroDeFinca, DateTime fechaDeCorte)
		{
			return UnaVerdad(
				"@tiene = cuenta.TieneValorDeFincaAl(@numeroDeFinca, @fechaDeCorte);", "tiene", p => {
					p["numeroDeFinca", typeof(string)] = numeroDeFinca;
					p["fechaDeCorte", typeof(DateTime)] = fechaDeCorte;
				});
		}

		/// <summary>
		/// El valor que esa finca tenia declarado a esa fecha. Lo aporto la PERSONA, no la
		/// municipalidad. GUARDADO: preguntar TieneValorDeclaradoLaFinca antes.
		/// </summary>
		public decimal ValorDeFincaAl(string numeroDeFinca, DateTime fechaDeCorte)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@valor = cuenta.ValorDeFincaAl(@numeroDeFinca, @fechaDeCorte);")
				.WithParameters(alquilados, p => {
					p["numeroDeFinca", typeof(string)] = numeroDeFinca;
					p["fechaDeCorte", typeof(DateTime)] = fechaDeCorte;
					p[Parameter.Out, "valor", typeof(decimal)] = default;
				})
				.PerformQuery();

				return alquilados["valor"].GetValue<decimal>();
			}
		}

		// Escalares TIPADOS por parametro Out, no parseando el print -regla 16-.
		public decimal LoPagadoEn(int anioPagado)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@pagado = cuenta.LoPagadoEn(@anioPagado);")
				.WithParameters(alquilados, p => {
					p["anioPagado", typeof(int)] = anioPagado;
					p[Parameter.Out, "pagado", typeof(decimal)] = default;
				})
				.PerformQuery();

				return alquilados["pagado"].GetValue<decimal>();
			}
		}

		/// <summary>
		/// La serie de un rubro como filas: el periodo de cada linea, cuanto cobro, la mora que la
		/// municipalidad cobro, cuantos periodos cubre, lo que costo un periodo y si eso se dedujo, en
		/// columnas paralelas y en orden de periodo. TOTAL, y contesta CERO filas donde no hay ninguna.
		/// </summary>
		public RegistroDelHistorial HistorialDe(string nombreDelRubro)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using(@"
					@estado      = cuenta.EtiquetaDelEstadoDeLaSerieDe(@nombreDelRubro);
					@estadoDicho = cuenta.FraseDelEstadoDeLaSerieDe(@nombreDelRubro);
					@periodo     = cuenta.PeriodosDelHistorialDe(@nombreDelRubro);
					@monto       = cuenta.MontosDelHistorialDe(@nombreDelRubro);
					@mora        = cuenta.MorasDelHistorialDe(@nombreDelRubro);
					@cubre       = cuenta.PeriodosCubiertosDelHistorialDe(@nombreDelRubro);
					@porPeriodo  = cuenta.MontosPorPeriodoDelHistorialDe(@nombreDelRubro);
					@inferido    = cuenta.InferenciasDelHistorialDe(@nombreDelRubro);
				")
				.WithParameters(alquilados, p => {
					p[Parameter.Out, "estado",      typeof(string)]    = default;
					p[Parameter.Out, "estadoDicho", typeof(string)]    = default;
					p[Parameter.Out, "periodo",     typeof(string[])]  = default;
					p[Parameter.Out, "monto",       typeof(decimal[])] = default;
					p[Parameter.Out, "mora",        typeof(decimal[])] = default;
					p[Parameter.Out, "cubre",       typeof(int[])]     = default;
					p[Parameter.Out, "porPeriodo",  typeof(decimal[])] = default;
					p[Parameter.Out, "inferido",    typeof(bool[])]    = default;
					p["nombreDelRubro", typeof(string)] = nombreDelRubro;
				})
				.PerformQuery();

				return RegistroDelHistorial.Of(
					alquilados["estado"].GetValue<string>(),
					alquilados["estadoDicho"].GetValue<string>(),
					alquilados["periodo"].GetValue<string[]>(),
					alquilados["monto"].GetValue<decimal[]>(),
					alquilados["mora"].GetValue<decimal[]>(),
					alquilados["cubre"].GetValue<int[]>(),
					alquilados["porPeriodo"].GetValue<decimal[]>(),
					alquilados["inferido"].GetValue<bool[]>());
			}
		}

		/// <summary>
		/// Si hay al menos dos pagos de ese rubro, que es lo que hace falta para ver una variacion.
		/// TOTAL, y es la PRE-PREGUNTA de la de abajo.
		/// </summary>
		public bool SePuedeVerLaVariacionDe(string nombreDelRubro)
		{
			return UnaVerdad(
				"@puede = cuenta.SePuedeVerLaVariacionDe(@nombreDelRubro);", "puede", p => {
					p["nombreDelRubro", typeof(string)] = nombreDelRubro;
				});
		}

		/// <summary>
		/// Por que se paga mas de ese rubro: los dos periodos, los dos montos por periodo, cuanto
		/// cambio, que lo explica, y la frase que el dominio compone. El umbral y el ruido los pone
		/// quien pregunta -- son M1 a M3, propuestos y sin firmar-, y el dominio hace la comparacion.
		/// GUARDADO: preguntar SePuedeVerLaVariacionDe antes.
		/// </summary>
		public RegistroDeLaVariacion VariacionDe(string nombreDelRubro, decimal umbralDeSalto, decimal ruido)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using(@"
					{
						laVariacion = cuenta.VariacionDe(@nombreDelRubro, @umbralDeSalto, @ruido);
						@rubro       = laVariacion.NombreDelRubro;
						@desde       = laVariacion.EtiquetaDelPeriodoAnterior;
						@hasta       = laVariacion.EtiquetaDelPeriodoActual;
						@antes       = laVariacion.MontoAnteriorPorPeriodo;
						@ahora       = laVariacion.MontoActualPorPeriodo;
						@proporcion  = laVariacion.Proporcion;
						@explicacion = laVariacion.Etiqueta;
						@esSalto     = laVariacion.EsSalto;
						@urgencia    = laVariacion.Urgencia().Etiqueta;
						@dice        = laVariacion.Dice();
					}
				")
				.WithParameters(alquilados, p => {
					p[Parameter.Out, "rubro",       typeof(string)]  = default;
					p[Parameter.Out, "desde",       typeof(string)]  = default;
					p[Parameter.Out, "hasta",       typeof(string)]  = default;
					p[Parameter.Out, "antes",       typeof(decimal)] = default;
					p[Parameter.Out, "ahora",       typeof(decimal)] = default;
					p[Parameter.Out, "proporcion",  typeof(decimal)] = default;
					p[Parameter.Out, "explicacion", typeof(string)]  = default;
					p[Parameter.Out, "esSalto",     typeof(bool)]    = default;
					p[Parameter.Out, "urgencia",    typeof(string)]  = default;
					p[Parameter.Out, "dice",        typeof(string)]  = default;
					p["nombreDelRubro", typeof(string)] = nombreDelRubro;
					p["umbralDeSalto", typeof(decimal)] = umbralDeSalto;
					p["ruido", typeof(decimal)] = ruido;
				})
				.PerformQuery();

				return RegistroDeLaVariacion.Of(
					alquilados["rubro"].GetValue<string>(),
					alquilados["desde"].GetValue<string>(),
					alquilados["hasta"].GetValue<string>(),
					alquilados["antes"].GetValue<decimal>(),
					alquilados["ahora"].GetValue<decimal>(),
					alquilados["proporcion"].GetValue<decimal>(),
					alquilados["explicacion"].GetValue<string>(),
					alquilados["esSalto"].GetValue<bool>(),
					alquilados["urgencia"].GetValue<string>(),
					alquilados["dice"].GetValue<string>());
			}
		}

		/// <summary>
		/// Lo que a esa fecha todavia no vence, como filas: de que rubro, de que periodo, el dia en
		/// que vence, cuanto, cuantos dias faltan y cuanto apura, en columnas paralelas y de lo que
		/// vence primero a lo que vence despues. TOTAL, y contesta CERO filas donde no hay ninguna.
		/// Los dias de aviso los pone quien pregunta, y la fecha de corte tambien: el dominio no lee
		/// el reloj.
		/// </summary>
		public RegistroDeVencimientos VencimientosAl(DateTime fechaDeCorte, int diasDeAviso)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using(@"
					@rubro    = cuenta.RubrosDeLosVencimientosAl(@fechaDeCorte);
					@periodo  = cuenta.PeriodosDeLosVencimientosAl(@fechaDeCorte);
					@vence    = cuenta.UltimosDiasDeLosVencimientosAl(@fechaDeCorte);
					@monto    = cuenta.MontosDeLosVencimientosAl(@fechaDeCorte);
					@faltan   = cuenta.DiasQueFaltanDeLosVencimientosAl(@fechaDeCorte);
					@urgencia = cuenta.UrgenciasDeLosVencimientosAl(@fechaDeCorte, @diasDeAviso);
					@dice     = cuenta.FrasesDeLosVencimientosAl(@fechaDeCorte, @diasDeAviso);
				")
				.WithParameters(alquilados, p => {
					p[Parameter.Out, "rubro",    typeof(string[])]  = default;
					p[Parameter.Out, "periodo",  typeof(string[])]  = default;
					p[Parameter.Out, "vence",    typeof(string[])]  = default;
					p[Parameter.Out, "monto",    typeof(decimal[])] = default;
					p[Parameter.Out, "faltan",   typeof(int[])]     = default;
					p[Parameter.Out, "urgencia", typeof(string[])]  = default;
					p[Parameter.Out, "dice",     typeof(string[])]  = default;
					p["fechaDeCorte", typeof(DateTime)] = fechaDeCorte;
					p["diasDeAviso", typeof(int)] = diasDeAviso;
				})
				.PerformQuery();

				return RegistroDeVencimientos.Of(
					alquilados["rubro"].GetValue<string[]>(),
					alquilados["periodo"].GetValue<string[]>(),
					alquilados["vence"].GetValue<string[]>(),
					alquilados["monto"].GetValue<decimal[]>(),
					alquilados["faltan"].GetValue<int[]>(),
					alquilados["urgencia"].GetValue<string[]>(),
					alquilados["dice"].GetValue<string[]>());
			}
		}

		/// <summary>
		/// Lo que a esa fecha ya vencio y nadie pago, como filas: de que rubro, de que periodo, el dia
		/// en que vencio, cuanto, cuantos dias de atraso lleva, lo que PROYECTA costar pagarlo tarde a
		/// esa tasa y cuanto apura, en columnas paralelas y de lo que vencio primero a lo que vencio
		/// despues. TOTAL, y contesta CERO filas donde no hay ninguna. La proyeccion nunca contradice
		/// lo que la municipalidad cobro: eso llega en el recibo.
		/// </summary>
		public RegistroDeAtrasos AtrasosAl(DateTime fechaDeCorte, decimal tasaAnual)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using(@"
					@rubro           = cuenta.RubrosDeLosAtrasosAl(@fechaDeCorte);
					@periodo         = cuenta.PeriodosDeLosAtrasosAl(@fechaDeCorte);
					@vencio          = cuenta.UltimosDiasDeLosAtrasosAl(@fechaDeCorte);
					@monto           = cuenta.MontosDeLosAtrasosAl(@fechaDeCorte);
					@dias            = cuenta.DiasDeAtrasoDeLosAtrasosAl(@fechaDeCorte);
					@costoProyectado = cuenta.CostosDePagarTardeDeLosAtrasosAl(@fechaDeCorte, @tasaAnual);
					@urgencia        = cuenta.UrgenciasDeLosAtrasosAl(@fechaDeCorte);
					@dice            = cuenta.FrasesDeLosAtrasosAl(@fechaDeCorte, @tasaAnual);
				")
				.WithParameters(alquilados, p => {
					p[Parameter.Out, "rubro",           typeof(string[])]  = default;
					p[Parameter.Out, "periodo",         typeof(string[])]  = default;
					p[Parameter.Out, "vencio",          typeof(string[])]  = default;
					p[Parameter.Out, "monto",           typeof(decimal[])] = default;
					p[Parameter.Out, "dias",            typeof(int[])]     = default;
					p[Parameter.Out, "costoProyectado", typeof(decimal[])] = default;
					p[Parameter.Out, "urgencia",        typeof(string[])]  = default;
					p[Parameter.Out, "dice",            typeof(string[])]  = default;
					p["fechaDeCorte", typeof(DateTime)] = fechaDeCorte;
					p["tasaAnual", typeof(decimal)] = tasaAnual;
				})
				.PerformQuery();

				return RegistroDeAtrasos.Of(
					alquilados["rubro"].GetValue<string[]>(),
					alquilados["periodo"].GetValue<string[]>(),
					alquilados["vencio"].GetValue<string[]>(),
					alquilados["monto"].GetValue<decimal[]>(),
					alquilados["dias"].GetValue<int[]>(),
					alquilados["costoProyectado"].GetValue<decimal[]>(),
					alquilados["urgencia"].GetValue<string[]>(),
					alquilados["dice"].GetValue<string[]>());
			}
		}

		/// <summary>
		/// Si hay con que proyectar el anio, que es lo que hace falta para proponer una reserva. TOTAL,
		/// y es la PRE-PREGUNTA de la de abajo.
		/// </summary>
		public bool SePuedeProyectar()
		{
			return UnaVerdad("@puede = cuenta.SePuedeProyectar;", "puede", p => { });
		}

		/// <summary>
		/// Lo que la app propone apartar por mes para ese anio, con la frase que el dominio compone.
		/// GUARDADO: preguntar SePuedeProyectar antes -- sin un pago registrado no hay con que proyectar
		/// y no se inventa un numero.
		/// </summary>
		public RegistroDeLaReservaPropuesta ReservaPropuestaPara(int anioProyectado)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using(@"
					{
						laPropuesta = cuenta.ReservaPropuestaPara(@anioProyectado);
						@anio            = laPropuesta.Anio;
						@proyeccion      = laPropuesta.ProyeccionDelAnio;
						@propuestoPorMes = laPropuesta.MontoMensual;
						@dice            = laPropuesta.Dice();
					}
				")
				.WithParameters(alquilados, p => {
					p[Parameter.Out, "anio",            typeof(int)]     = default;
					p[Parameter.Out, "proyeccion",      typeof(decimal)] = default;
					p[Parameter.Out, "propuestoPorMes", typeof(decimal)] = default;
					p[Parameter.Out, "dice",            typeof(string)]  = default;
					p["anioProyectado", typeof(int)] = anioProyectado;
				})
				.PerformQuery();

				return RegistroDeLaReservaPropuesta.Of(
					alquilados["anio"].GetValue<int>(),
					alquilados["proyeccion"].GetValue<decimal>(),
					alquilados["propuestoPorMes"].GetValue<decimal>(),
					alquilados["dice"].GetValue<string>());
			}
		}

		/// <summary>
		/// Si hay una proyeccion Y una reserva adoptada, que son las dos mitades que el faltante
		/// contrasta. TOTAL, y es la PRE-PREGUNTA de la de abajo.
		/// </summary>
		public bool SePuedeVerElFaltanteDeReserva()
		{
			return UnaVerdad("@puede = cuenta.SePuedeVerElFaltanteDeReserva;", "puede", p => { });
		}

		/// <summary>
		/// Lo adoptado contra lo proyectado a esa fecha: las dos mitades, el faltante, lo que vence en
		/// el mes, cuanto apura, y la frase que el dominio compone nombrando de quien es cada mitad. El
		/// umbral del 10 % esta firmado y entra por parametro igual, para que se pueda mover sin tocar
		/// una clase. GUARDADO: preguntar SePuedeVerElFaltanteDeReserva antes.
		/// </summary>
		public RegistroDelFaltanteDeReserva FaltanteDeReservaAl(DateTime fechaDeCorte, decimal umbralDeReserva)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using(@"
					{
						elFaltante = cuenta.FaltanteDeReservaAl(@fechaDeCorte);
						@propuesto    = elFaltante.PropuestoPorMes;
						@declarado    = elFaltante.DeclaradoPorMes;
						@faltante     = elFaltante.Faltante;
						@proporcion   = elFaltante.Proporcion;
						@venceEsteMes = elFaltante.VenceEsteMes;
						@urgencia     = elFaltante.Urgencia(@umbralDeReserva).Etiqueta;
						@dice         = elFaltante.Dice(@umbralDeReserva);
					}
				")
				.WithParameters(alquilados, p => {
					p[Parameter.Out, "propuesto",    typeof(decimal)] = default;
					p[Parameter.Out, "declarado",    typeof(decimal)] = default;
					p[Parameter.Out, "faltante",     typeof(decimal)] = default;
					p[Parameter.Out, "proporcion",   typeof(decimal)] = default;
					p[Parameter.Out, "venceEsteMes", typeof(decimal)] = default;
					p[Parameter.Out, "urgencia",     typeof(string)]  = default;
					p[Parameter.Out, "dice",         typeof(string)]  = default;
					p["fechaDeCorte", typeof(DateTime)] = fechaDeCorte;
					p["umbralDeReserva", typeof(decimal)] = umbralDeReserva;
				})
				.PerformQuery();

				return RegistroDelFaltanteDeReserva.Of(
					alquilados["propuesto"].GetValue<decimal>(),
					alquilados["declarado"].GetValue<decimal>(),
					alquilados["faltante"].GetValue<decimal>(),
					alquilados["proporcion"].GetValue<decimal>(),
					alquilados["venceEsteMes"].GetValue<decimal>(),
					alquilados["urgencia"].GetValue<string>(),
					alquilados["dice"].GetValue<string>());
			}
		}

		// M6, por parametro: cuanto me ahorro si pago hoy todo el anio.
		public decimal AhorroPorProntoPago(DateTime fechaDePago, decimal tasaAnual)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@ahorro = cuenta.AhorroPorProntoPago(@fechaDePago, @tasaAnual);")
				.WithParameters(alquilados, p => {
					p["fechaDePago", typeof(DateTime)] = fechaDePago;
					p["tasaAnual", typeof(decimal)] = tasaAnual;
					p[Parameter.Out, "ahorro", typeof(decimal)] = default;
				})
				.PerformQuery();

				return alquilados["ahorro"].GetValue<decimal>();
			}
		}

		/// <summary>
		/// Lo que la consulta de ese dia da por pagado y la app no vio pagar, como filas: de que rubro
		/// y de que periodo, en columnas paralelas, con el estado que dice POR QUE trae lo que trae.
		/// Se dice, no se tapa. TOTAL: cero filas donde no hay ninguna, y cero filas con otro estado
		/// donde no hay consulta de ese dia.
		/// </summary>
		public RegistroDePeriodosSinComprobante PeriodosPagadosSinComprobante(DateTime fechaDeLaConsulta)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using(@"
					@estado      = cuenta.EtiquetaDelEstadoDeLoSinComprobante(@fechaDeLaConsulta);
					@estadoDicho = cuenta.FraseDelEstadoDeLoSinComprobante(@fechaDeLaConsulta);
					@rubro       = cuenta.RubrosDeLoPagadoSinComprobante(@fechaDeLaConsulta);
					@periodo     = cuenta.PeriodosDeLoPagadoSinComprobante(@fechaDeLaConsulta);
					@dice        = cuenta.FrasesDeLoPagadoSinComprobante(@fechaDeLaConsulta);
				")
				.WithParameters(alquilados, p => {
					p[Parameter.Out, "estado",      typeof(string)]   = default;
					p[Parameter.Out, "estadoDicho", typeof(string)]   = default;
					p[Parameter.Out, "rubro",       typeof(string[])] = default;
					p[Parameter.Out, "periodo",     typeof(string[])] = default;
					p[Parameter.Out, "dice",        typeof(string[])] = default;
					p["fechaDeLaConsulta", typeof(DateTime)] = fechaDeLaConsulta;
				})
				.PerformQuery();

				return RegistroDePeriodosSinComprobante.Of(
					alquilados["estado"].GetValue<string>(),
					alquilados["estadoDicho"].GetValue<string>(),
					alquilados["rubro"].GetValue<string[]>(),
					alquilados["periodo"].GetValue<string[]>(),
					alquilados["dice"].GetValue<string[]>());
			}
		}

		/// <summary>
		/// Una lista de textos de vuelta desde el actor. Un arreglo de primitivos SI cruza la frontera
		/// del parametro y vuelve tipado, asi que la lista sale como lista y nadie parte una frase.
		/// </summary>
		private string[] UnaListaDeTextos(string script, string salida, Action<Parameters> ataduras)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using(script)
				.WithParameters(alquilados, p => {
					p[Parameter.Out, salida, typeof(string[])] = default;
					ataduras(p);
				})
				.PerformQuery();

				return alquilados[salida].GetValue<string[]>();
			}
		}

		/// <summary>Una fecha de vuelta desde el actor, tipada por parametro Out.</summary>
		private DateTime UnaFecha(string script, string salida, Action<Parameters> ataduras)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using(script)
				.WithParameters(alquilados, p => {
					p[Parameter.Out, salida, typeof(DateTime)] = default;
					ataduras(p);
				})
				.PerformQuery();

				return alquilados[salida].GetValue<DateTime>();
			}
		}

		/// <summary>Un numero largo de vuelta desde el actor, tipado por parametro Out.</summary>
		private long UnNumero(string script, string salida, Action<Parameters> ataduras)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using(script)
				.WithParameters(alquilados, p => {
					p[Parameter.Out, salida, typeof(long)] = default;
					ataduras(p);
				})
				.PerformQuery();

				return alquilados[salida].GetValue<long>();
			}
		}

		/// <summary>Un si o un no de vuelta desde el actor, tipado por parametro Out.</summary>
		private bool UnaVerdad(string script, string salida, Action<Parameters> ataduras)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using(script)
				.WithParameters(alquilados, p => {
					p[Parameter.Out, salida, typeof(bool)] = default;
					ataduras(p);
				})
				.PerformQuery();

				return alquilados[salida].GetValue<bool>();
			}
		}

		/// <summary>Un texto de vuelta desde el actor, tipado por parametro Out.</summary>
		private string UnTexto(string script, string salida, Action<Parameters> ataduras)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using(script)
				.WithParameters(alquilados, p => {
					p[Parameter.Out, salida, typeof(string)] = default;
					ataduras(p);
				})
				.PerformQuery();

				return alquilados[salida].GetValue<string>();
			}
		}
	}
}
