using System;
using System.Globalization;
using Choreography.Theater;
using Puppeteer;

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
	public sealed class MunicipalidadAPI
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
			string[] partes = periodoComoDiceElRecibo.Trim().Split('.');
			if (partes.Length != 2
				|| !int.TryParse(partes[0], NumberStyles.None, CultureInfo.InvariantCulture, out anio)
				|| !int.TryParse(partes[1], NumberStyles.None, CultureInfo.InvariantCulture, out numero))
				throw new ArgumentException(
					"El periodo tiene que venir como AAAA.NN, como lo escribe el recibo: '" + periodoComoDiceElRecibo + "'.",
					nameof(periodoComoDiceElRecibo));
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
					Check(cuenta.PuedeDeclararValorDeFinca(@numeroDeFinca, @fechaDeLaDeclaracion, @valorDeclarado)) Error 'la finca no esta, el valor no sirve, o hay una declaracion posterior';
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
					Check(cuenta.PuedeAdoptarReserva(@montoMensual, @fechaDeAdopcion)) Error 'una reserva tiene que ser positiva y posterior a la ultima adoptada';
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

		// ----------------------------------------------------------------- consultas

		public string EstadoDeLaCuenta()
		{
			return app.Using(@"
				{
					print cuenta.CuantasFincas fincas,
						cuenta.CuantosCobros cobros,
						cuenta.CuantosPagos pagos,
						cuenta.CuantasConsultas consultas,
						cuenta.TieneReservaDeclarada tieneReserva;
				}
			")
			.PerformQuery();
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

		// LA SERIE DE UN RUBRO, hoja por hoja. Los objetos adentro se aplanaron con
		// accesores -regla 13-; PeriodosQueCubre y EsInferido viajan porque son la marca
		// del hueco 6 y la superficie tiene que poder decirlo.
		public string HistorialDe(string nombreDelRubro)
		{
			return app.Using(@"
				foreach (laLinea in cuenta.HistorialDe(@nombreDelRubro)) {
					print laLinea.EtiquetaDelPeriodo periodo,
						laLinea.Monto monto,
						laLinea.Intereses mora,
						laLinea.PeriodosQueCubre cubre,
						laLinea.MontoPorPeriodo porPeriodo,
						laLinea.EsInferido inferido;
				}
			")
			.WithParameters(p => {
				p["nombreDelRubro", typeof(string)] = nombreDelRubro;
			})
			.PerformQuery();
		}

		// "POR QUE ESTE TRIMESTRE PAGO MAS?" Umbral y ruido entran por parametro: son M1 a
		// M3, propuestos y sin firmar. El dominio hace la comparacion; los numeros vienen
		// de afuera.
		public string VariacionDe(string nombreDelRubro, decimal umbralDeSalto, decimal ruido)
		{
			return app.Using(@"
				{
					laVariacion = cuenta.VariacionDe(@nombreDelRubro, @umbralDeSalto, @ruido);
					print laVariacion.NombreDelRubro rubro,
						laVariacion.EtiquetaDelPeriodoAnterior desde,
						laVariacion.EtiquetaDelPeriodoActual hasta,
						laVariacion.MontoAnteriorPorPeriodo antes,
						laVariacion.MontoActualPorPeriodo ahora,
						laVariacion.Proporcion proporcion,
						laVariacion.Etiqueta explicacion,
						laVariacion.EsSalto esSalto,
						laVariacion.Urgencia().Etiqueta urgencia;
				}
			")
			.WithParameters(p => {
				p["nombreDelRubro", typeof(string)] = nombreDelRubro;
				p["umbralDeSalto", typeof(decimal)] = umbralDeSalto;
				p["ruido", typeof(decimal)] = ruido;
			})
			.PerformQuery();
		}

		// LA AUSENCIA: que se me vence. Los dias de aviso son M5, por parametro. La fecha
		// de corte entra: el dominio no lee el reloj.
		public string VencimientosAl(DateTime fechaDeCorte, int diasDeAviso)
		{
			return app.Using(@"
				foreach (elVencimiento in cuenta.VencimientosAl(@fechaDeCorte)) {
					print elVencimiento.NombreDelRubro rubro,
						elVencimiento.EtiquetaDelPeriodo periodo,
						elVencimiento.Vence vence,
						elVencimiento.Monto monto,
						elVencimiento.DiasQueFaltan(@fechaDeCorte) faltan,
						elVencimiento.Urgencia(@fechaDeCorte, @diasDeAviso).Etiqueta urgencia;
				}
			")
			.WithParameters(p => {
				p["fechaDeCorte", typeof(DateTime)] = fechaDeCorte;
				p["diasDeAviso", typeof(int)] = diasDeAviso;
			})
			.PerformQuery();
		}

		// Lo vencido, con la PROYECCION de lo que costara pagarlo tarde (M7, por parametro).
		// Nunca contradice lo que la municipalidad cobro: eso llega en el recibo.
		public string AtrasosAl(DateTime fechaDeCorte, decimal tasaAnual)
		{
			return app.Using(@"
				foreach (elAtraso in cuenta.AtrasosAl(@fechaDeCorte)) {
					print elAtraso.NombreDelRubro rubro,
						elAtraso.EtiquetaDelPeriodo periodo,
						elAtraso.Vencio vencio,
						elAtraso.Monto monto,
						elAtraso.DiasDeAtraso(@fechaDeCorte) dias,
						elAtraso.CostoDePagarTarde(@fechaDeCorte, @tasaAnual) costoProyectado,
						elAtraso.Urgencia().Etiqueta urgencia;
				}
			")
			.WithParameters(p => {
				p["fechaDeCorte", typeof(DateTime)] = fechaDeCorte;
				p["tasaAnual", typeof(decimal)] = tasaAnual;
			})
			.PerformQuery();
		}

		// LO QUE LA APP PROPONE apartar. Si no hay historia, la consulta falla diciendo que
		// no hay con que proyectar: no hay numero por defecto.
		public string ReservaPropuestaPara(int anioProyectado)
		{
			return app.Using(@"
				{
					laPropuesta = cuenta.ReservaPropuestaPara(@anioProyectado);
					print laPropuesta.Anio anio,
						laPropuesta.ProyeccionDelAnio proyeccion,
						laPropuesta.MontoMensual propuestoPorMes;
				}
			")
			.WithParameters(p => {
				p["anioProyectado", typeof(int)] = anioProyectado;
			})
			.PerformQuery();
		}

		// LO ADOPTADO CONTRA LO PROYECTADO. El umbral del 10 % esta firmado, e igual entra
		// por parametro para que se pueda mover sin tocar una clase.
		public string FaltanteDeReservaAl(DateTime fechaDeCorte, decimal umbralDeReserva)
		{
			return app.Using(@"
				{
					elFaltante = cuenta.FaltanteDeReservaAl(@fechaDeCorte);
					print elFaltante.PropuestoPorMes propuesto,
						elFaltante.DeclaradoPorMes declarado,
						elFaltante.Faltante faltante,
						elFaltante.Proporcion proporcion,
						elFaltante.VenceEsteMes venceEsteMes,
						elFaltante.Urgencia(@umbralDeReserva).Etiqueta urgencia;
				}
			")
			.WithParameters(p => {
				p["fechaDeCorte", typeof(DateTime)] = fechaDeCorte;
				p["umbralDeReserva", typeof(decimal)] = umbralDeReserva;
			})
			.PerformQuery();
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

		// M8: lo que la consulta dice pagado y la app no vio pagar. Se dice, no se tapa.
		public string PeriodosPagadosSinComprobante(DateTime fechaDeLaConsulta)
		{
			return app.Using(@"
				foreach (elPeriodo in cuenta.PeriodosPagadosSinComprobante(@fechaDeLaConsulta)) {
					print elPeriodo.NombreDelRubro rubro,
						elPeriodo.EtiquetaDelPeriodo periodo;
				}
			")
			.WithParameters(p => {
				p["fechaDeLaConsulta", typeof(DateTime)] = fechaDeLaConsulta;
			})
			.PerformQuery();
		}
	}
}
