using System;
using Choreography.Theater;
using Puppeteer;

namespace Gimnasio.App
{
	// LA SUPERFICIE INTENCIONAL. Un metodo por intencion, con nombre de negocio.
	// La consola, el browser y el asistente entran todos por aca; nadie escribe scripts.
	//
	// OJO CON LOS NOMBRES DE PARAMETRO. Ninguno se llama como un tipo del dominio: un
	// @medida taparia a la clase Medida y el script dejaria de resolver (regla 10 del
	// substrato). Por eso son @nombreDeLaMedida, @diaQueFue, @inicioDelPlan.
	public sealed class GimnasioAPI
	{
		private readonly PerformanceV2 app;

		public GimnasioAPI(PerformanceV2 app)
		{
			ArgumentNullException.ThrowIfNull(app);
			this.app = app;
		}

		// ------------------------------------------------------------------ comandos

		public string DeclararPerfil(string genero, int edad, decimal alturaEnCentimetros)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.PuedeDeclararPerfil(@generoDeclarado, @edad, @altura)) Error 'el perfil ya estaba declarado, o los datos no son validos';
				",
				@"
					{
						trayectoria.DeclararPerfil(Perfil(Genero.PorNombre(@generoDeclarado), @edad, @altura));
					}
				")
			.WithParameters(p => {
				p["generoDeclarado", typeof(string)] = genero;
				p["edad", typeof(int)] = edad;
				p["altura", typeof(decimal)] = alturaEnCentimetros;
			})
			.PerformCheckThenCommand();
		}

		// El objetivo se crea vacio y despues se le agregan metas. Dos comandos y no uno
		// porque una meta puede llegar meses despues de la otra.
		public string DeclararObjetivo()
		{
			return app.Using(
				scriptForChk: @"
					Check(!trayectoria.TieneObjetivo) Error 'el objetivo ya estaba declarado';
				",
				@"
					{
						trayectoria.DeclararObjetivo(Objetivo());
					}
				")
			.PerformCheckThenCommand();
		}

		public string AgregarMetaConValor(string nombreDeLaMedida, string haciaDonde, decimal valorDeseado)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.SePuedeAgregarMetaPara(@nombreDeLaMedida)) Error 'no hay objetivo, o esa medida ya tiene meta';
				",
				@"
					{
						trayectoria.Objetivo.Agregar(
							Meta.ConValor(@nombreDeLaMedida, Direccion.PorNombre(@haciaDonde), @valorDeseado));
					}
				")
			.WithParameters(p => {
				p["nombreDeLaMedida", typeof(string)] = nombreDeLaMedida;
				p["haciaDonde", typeof(string)] = haciaDonde;
				p["valorDeseado", typeof(decimal)] = valorDeseado;
			})
			.PerformCheckThenCommand();
		}

		public string AgregarMetaSoloDireccion(string nombreDeLaMedida, string haciaDonde)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.SePuedeAgregarMetaPara(@nombreDeLaMedida)) Error 'no hay objetivo, o esa medida ya tiene meta';
				",
				@"
					{
						trayectoria.Objetivo.Agregar(
							Meta.SoloDireccion(@nombreDeLaMedida, Direccion.PorNombre(@haciaDonde)));
					}
				")
			.WithParameters(p => {
				p["nombreDeLaMedida", typeof(string)] = nombreDeLaMedida;
				p["haciaDonde", typeof(string)] = haciaDonde;
			})
			.PerformCheckThenCommand();
		}

		// EL PLAN SE IDENTIFICA POR SU FECHA DE INICIO. En C# alcanzaba con la referencia
		// al objeto; desde un script hace falta un NOMBRE, y el gimnasio no le pone
		// ninguno al papel. Ver el hueco 10.
		public string RegistrarPlan(DateTime inicio, int diasPorSemana)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.PuedeRegistrarPlan(@inicioDelPlan, @diasPorSemana)) Error 'ya hay un plan que empieza ese dia, o la frecuencia no es valida';
				",
				@"
					{
						trayectoria.Agregar(Plan(@inicioDelPlan, @diasPorSemana));
					}
				")
			.WithParameters(p => {
				p["inicioDelPlan", typeof(DateTime)] = inicio;
				p["diasPorSemana", typeof(int)] = diasPorSemana;
			})
			.PerformCheckThenCommand();
		}

		// LAS DOS FORMAS DE PRESCRIBIR DEL PAPEL (v3): "3x 12 a 15 · 00:30" y "15 minutos".
		// Dos comandos y no uno con un tipo adentro: un ternario no unifica dos subclases
		// (regla 12), y cada forma tiene su propia puerta total (regla 11). El peso YA NO
		// viene aca: el papel no lo trae; si algun dia viene, entra por IndicarPeso.
		public string PrescribirFuerza(DateTime inicioDelPlan, string nombreDelEjercicio,
			int series, int repeticionesDesde, int repeticionesHasta, int descansoEnSegundos)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.PuedePrescribirFuerza(@inicioDelPlan, @nombreDelEjercicio, @series, @repeticionesDesde, @repeticionesHasta, @descansoEnSegundos)) Error 'no hay plan ese dia, ya prescribe ese ejercicio, o los numeros no sirven';
				",
				@"
					{
						trayectoria.PlanQueEmpezo(@inicioDelPlan)
							.Prescribir(EjercicioPrescrito.DeFuerza(@nombreDelEjercicio, @series, @repeticionesDesde, @repeticionesHasta, @descansoEnSegundos));
					}
				")
			.WithParameters(p => {
				p["inicioDelPlan", typeof(DateTime)] = inicioDelPlan;
				p["nombreDelEjercicio", typeof(string)] = nombreDelEjercicio;
				p["series", typeof(int)] = series;
				p["repeticionesDesde", typeof(int)] = repeticionesDesde;
				p["repeticionesHasta", typeof(int)] = repeticionesHasta;
				p["descansoEnSegundos", typeof(int)] = descansoEnSegundos;
			})
			.PerformCheckThenCommand();
		}

		public string PrescribirCardio(DateTime inicioDelPlan, string nombreDelEjercicio, int minutos)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.PuedePrescribirCardio(@inicioDelPlan, @nombreDelEjercicio, @minutos)) Error 'no hay plan ese dia, ya prescribe ese ejercicio, o los minutos no sirven';
				",
				@"
					{
						trayectoria.PlanQueEmpezo(@inicioDelPlan)
							.Prescribir(EjercicioPrescrito.DeCardio(@nombreDelEjercicio, @minutos));
					}
				")
			.WithParameters(p => {
				p["inicioDelPlan", typeof(DateTime)] = inicioDelPlan;
				p["nombreDelEjercicio", typeof(string)] = nombreDelEjercicio;
				p["minutos", typeof(int)] = minutos;
			})
			.PerformCheckThenCommand();
		}

		// EL PESO, cuando la maquina lo requiere (hueco 17, Jorge). Un hecho aparte sobre un
		// ejercicio ya prescrito.
		public string IndicarPeso(DateTime inicioDelPlan, string nombreDelEjercicio, decimal pesoIndicado)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.PuedeIndicarPeso(@inicioDelPlan, @nombreDelEjercicio, @pesoIndicado)) Error 'no hay ese ejercicio en ese plan, ya tiene peso, o el peso no sirve';
				",
				@"
					{
						trayectoria.PlanQueEmpezo(@inicioDelPlan).Prescrito(@nombreDelEjercicio).IndicarPeso(@pesoIndicado);
					}
				")
			.WithParameters(p => {
				p["inicioDelPlan", typeof(DateTime)] = inicioDelPlan;
				p["nombreDelEjercicio", typeof(string)] = nombreDelEjercicio;
				p["pesoIndicado", typeof(decimal)] = pesoIndicado;
			})
			.PerformCheckThenCommand();
		}

		public string TerminarPlan(DateTime inicioDelPlan, DateTime fin)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.PuedeTerminarPlan(@inicioDelPlan, @finDelPlan)) Error 'ese plan no existe, ya termino, o terminaria antes de empezar';
				",
				@"
					{
						trayectoria.PlanQueEmpezo(@inicioDelPlan).Terminar(@finDelPlan);
					}
				")
			.WithParameters(p => {
				p["inicioDelPlan", typeof(DateTime)] = inicioDelPlan;
				p["finDelPlan", typeof(DateTime)] = fin;
			})
			.PerformCheckThenCommand();
		}

		public string Asistio(DateTime dia)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.PuedeRegistrarAsistencia(@diaQueFue)) Error 'ese dia ya estaba registrado';
				",
				@"
					{
						trayectoria.Asistio(@diaQueFue);
					}
				")
			.WithParameters(p => {
				p["diaQueFue", typeof(DateTime)] = dia;
			})
			.PerformCheckThenCommand();
		}

		// LA MEDIDA SE CREA POR NOMBRE. Las cuatro son un conjunto cerrado, y desde un
		// script no se puede elegir una subclase con un ternario -regla 12- ni nombrarla
		// sin un resolutor total -regla 11-. Ver el hueco 11.
		public string RegistrarMedida(DateTime fechaDeLaMedicion, string queSeMidio, decimal valor)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.PuedeRegistrarMedida(@fechaDeLaMedicion, @queSeMidio, @valor)) Error 'esa medida no existe, el valor no sirve, o ya estaba en esa hoja';
				",
				@"
					{
						trayectoria.AbrirMedicionDe(@fechaDeLaMedicion).Registrar(Medida.Crear(@queSeMidio, @valor));
					}
				")
			.WithParameters(p => {
				p["fechaDeLaMedicion", typeof(DateTime)] = fechaDeLaMedicion;
				p["queSeMidio", typeof(string)] = queSeMidio;
				p["valor", typeof(decimal)] = valor;
			})
			.PerformCheckThenCommand();
		}

		// LA MEDIDA CON LA PALABRA DEL APARATO AL LADO (v3, N7). "Increased", "High":
		// llega como hecho y el dominio la registra. El parametro no se llama @evaluacion
		// a secas por costumbre de la regla 10, aunque el tipo se llame distinto.
		public string RegistrarMedidaEvaluada(DateTime fechaDeLaMedicion, string queSeMidio, decimal valor,
			string loQueDijoElAparato)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.PuedeRegistrarMedidaEvaluada(@fechaDeLaMedicion, @queSeMidio, @valor, @loQueDijoElAparato)) Error 'esa medida no existe, el valor no sirve, ya estaba en esa hoja, o el aparato nunca dijo esa palabra';
				",
				@"
					{
						trayectoria.AbrirMedicionDe(@fechaDeLaMedicion)
							.Registrar(Medida.Crear(@queSeMidio, @valor).ConEvaluacion(EvaluacionDelAparato.PorNombre(@loQueDijoElAparato)));
					}
				")
			.WithParameters(p => {
				p["fechaDeLaMedicion", typeof(DateTime)] = fechaDeLaMedicion;
				p["queSeMidio", typeof(string)] = queSeMidio;
				p["valor", typeof(decimal)] = valor;
				p["loQueDijoElAparato", typeof(string)] = loQueDijoElAparato;
			})
			.PerformCheckThenCommand();
		}

		// ------------------------------------------------------ la membresia (v2, N1)

		// EL PRODUCTO Y EL DIA DE VENCIMIENTO (v3, N11): "Plan Black", "Dia 10". El
		// parametro no se llama @producto ni @plan: el segundo taparia a la clase Plan
		// (regla 10), y "plan" ya son tres cosas en el papel.
		public string Contratar(string nombreDelProducto, int diaDeVencimiento, DateTime fechaDeContratacion)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.PuedeContratar(@nombreDelProducto, @diaDeVencimiento, @fechaDeContratacion)) Error 'es el mismo producto que ya tiene, el dia no es del mes, o es anterior a la ultima contratacion';
				",
				@"
					{
						trayectoria.Membresia.Contratar(Contratacion(@fechaDeContratacion, @nombreDelProducto, @diaDeVencimiento));
					}
				")
			.WithParameters(p => {
				p["nombreDelProducto", typeof(string)] = nombreDelProducto;
				p["diaDeVencimiento", typeof(int)] = diaDeVencimiento;
				p["fechaDeContratacion", typeof(DateTime)] = fechaDeContratacion;
			})
			.PerformCheckThenCommand();
		}

		// EL COBRO PASO A PAGADO (hueco 18, Jorge). Se nombra por su vencimiento; el mismo
		// estado otra vez se rechaza (regla 18).
		public string CambiarEstadoDeCobro(DateTime venceElCobro, string comoEstaElCobro)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.PuedeCambiarEstadoDeCobro(@venceElCobro, @comoEstaElCobro)) Error 'no hay un cobro con ese vencimiento, el estado no existe, o ya estaba asi';
				",
				@"
					{
						trayectoria.Membresia.CambiarEstadoDeCobro(@venceElCobro, EstadoDeCobro.PorNombre(@comoEstaElCobro));
					}
				")
			.WithParameters(p => {
				p["venceElCobro", typeof(DateTime)] = venceElCobro;
				p["comoEstaElCobro", typeof(string)] = comoEstaElCobro;
			})
			.PerformCheckThenCommand();
		}

		// LA BANDERA DEL GIMNASIO. Manda "no activa" y despues "activo"; el dominio registra
		// el cambio con su fecha y deriva el periodo. El parametro no se llama @estado ni
		// @estadoDeMembresia porque taparia al tipo (regla 10). El Check rechaza el mismo
		// estado dos veces: es el mismo hecho otra vez (regla 18).
		public string CambiarEstadoDeMembresia(string nuevoEstado, DateTime fechaDelCambio)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.PuedeCambiarEstadoDeMembresia(@nuevoEstado, @fechaDelCambio)) Error 'ese estado no existe, es el mismo que ya tiene, o es anterior al ultimo cambio';
				",
				@"
					{
						trayectoria.Membresia.CambiarEstado(EstadoDeMembresia.PorNombre(@nuevoEstado), @fechaDelCambio);
					}
				")
			.WithParameters(p => {
				p["nuevoEstado", typeof(string)] = nuevoEstado;
				p["fechaDelCambio", typeof(DateTime)] = fechaDelCambio;
			})
			.PerformCheckThenCommand();
		}

		public string RegistrarPago(DateTime fechaDelPago, decimal montoPagado)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.PuedeRegistrarPago(@fechaDelPago, @montoPagado)) Error 'el monto no sirve, o ese pago ya estaba registrado';
				",
				@"
					{
						trayectoria.Membresia.RegistrarPago(Pago(@fechaDelPago, @montoPagado));
					}
				")
			.WithParameters(p => {
				p["fechaDelPago", typeof(DateTime)] = fechaDelPago;
				p["montoPagado", typeof(decimal)] = montoPagado;
			})
			.PerformCheckThenCommand();
		}

		// EL COBRO TAL COMO EL PAPEL LO TRAE (v3, N10): etiqueta, monto, vencimiento, el
		// periodo que cubre y su estado. El estado no se llama @estado: taparia a los
		// tipos de estado (regla 10).
		public string AnunciarCobro(DateTime venceElCobro, decimal montoDelCobro, string etiquetaDelCobro,
			DateTime cubreDesde, DateTime cubreHasta, string comoEstaElCobro)
		{
			return app.Using(
				scriptForChk: @"
					Check(trayectoria.PuedeAnunciarCobro(@venceElCobro, @montoDelCobro, @etiquetaDelCobro, @cubreDesde, @cubreHasta, @comoEstaElCobro)) Error 'el monto o el periodo no sirven, el estado no existe, ya hay un cobro con ese vencimiento, o se solapa con otro';
				",
				@"
					{
						trayectoria.Membresia.AnunciarCobro(
							Cobro(@venceElCobro, @montoDelCobro, @etiquetaDelCobro, @cubreDesde, @cubreHasta, EstadoDeCobro.PorNombre(@comoEstaElCobro)));
					}
				")
			.WithParameters(p => {
				p["venceElCobro", typeof(DateTime)] = venceElCobro;
				p["montoDelCobro", typeof(decimal)] = montoDelCobro;
				p["etiquetaDelCobro", typeof(string)] = etiquetaDelCobro;
				p["cubreDesde", typeof(DateTime)] = cubreDesde;
				p["cubreHasta", typeof(DateTime)] = cubreHasta;
				p["comoEstaElCobro", typeof(string)] = comoEstaElCobro;
			})
			.PerformCheckThenCommand();
		}

		// ----------------------------------------------------------------- consultas

		// Escalares TIPADOS por parametro Out, no parseando el print -regla 16-.
		public int CuantasVecesSeMidio(string nombreDeLaMedida)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@cuantas = trayectoria.CuantasVecesSeMidio(@nombreDeLaMedida);")
				.WithParameters(alquilados, p => {
					p["nombreDeLaMedida", typeof(string)] = nombreDeLaMedida;
					p[Parameter.Out, "cuantas", typeof(int)] = default;
				})
				.PerformQuery();

				return alquilados["cuantas"].GetValue<int>();
			}
		}

		// COMO CAMBIO LO QUE LE PRESCRIBEN (v3, N8): la pregunta 2 reformulada sobre lo
		// que el papel trae. Carrier plano: antes, despues, cambio, en que se mide.
		public string ComoCambioLoPrescrito(string nombreDelEjercicio)
		{
			return app.Using(@"
				{
					progresion = trayectoria.ComoCambioLoPrescrito(@nombreDelEjercicio);
					print progresion.NombreDelEjercicio ejercicio,
						progresion.EnQueSeMide enQueSeMide,
						progresion.Antes antes,
						progresion.Despues despues,
						progresion.Cambio cambio,
						progresion.Subio subio;
				}
			")
			.WithParameters(p => {
				p["nombreDelEjercicio", typeof(string)] = nombreDelEjercicio;
			})
			.PerformQuery();
		}

		// LO QUE EL APARATO DIJO DE UNA MEDIDA (v3, N7), al lado de lo que la app dice del
		// movimiento: el aparato lee el punto; la app lee la serie. Los dos juicios, y el
		// primero ya no lo calcula la app.
		public string LoQueDijoElAparato(string nombreDeLaMedida, decimal ruido, decimal noCreible)
		{
			return app.Using(@"
				{
					referencia = Referencia(@sinRango, @sinRango, @ruido, @noCreible);
					tendencia = trayectoria.ComoVengo(@nombreDeLaMedida, referencia);
					print trayectoria.LoQueDijoElAparato(@nombreDeLaMedida).Etiqueta elAparatoDijo,
						tendencia.ToString() movimiento,
						tendencia.Cambio cambio,
						tendencia.Urgencia().ToString() urgencia;
				}
			")
			.WithParameters(p => {
				p["nombreDeLaMedida", typeof(string)] = nombreDeLaMedida;
				p["sinRango", typeof(decimal)] = 0m;
				p["ruido", typeof(decimal)] = ruido;
				p["noCreible", typeof(decimal)] = noCreible;
			})
			.PerformQuery();
		}

		// QUE COBRO CUBRE UN DIA (v3, N10). El gimnasio manda el periodo de cada cobro;
		// que cobro cubre el dia que entrene lo contesta la app.
		public string CobroQueCubre(DateTime elDia)
		{
			return app.Using(@"
				{
					cobro = trayectoria.Membresia.CobroQueCubre(@elDia);
					print cobro.Etiqueta etiqueta,
						cobro.Monto monto,
						cobro.Vence vence,
						cobro.CubreDesde cubreDesde,
						cobro.CubreHasta cubreHasta,
						cobro.DiasQueCubre dias,
						cobro.VenceDentroDelPeriodo venceDentroDelPeriodo,
						cobro.Estado.Etiqueta comoEsta;
				}
			")
			.WithParameters(p => {
				p["elDia", typeof(DateTime)] = elDia;
			})
			.PerformQuery();
		}

		// EL PRODUCTO Y EL DIA DE VENCIMIENTO (v3, N11). Si el gimnasio nunca lo mando,
		// falla diciendo que no se sabe.
		public string ElProducto()
		{
			return app.Using(@"
				{
					print trayectoria.Membresia.Producto producto,
						trayectoria.Membresia.DiaDeVencimiento venceElDia;
				}
			")
			.PerformQuery();
		}

		// La pregunta del v2, guardada desde el v3: solo contesta si los dos planes traen
		// peso. Hoy ninguno lo trae.
		public decimal CuantoSubioLoIndicado(string nombreDelEjercicio)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@cuanto = trayectoria.CuantoSubioLoIndicado(@nombreDelEjercicio);")
				.WithParameters(alquilados, p => {
					p["nombreDelEjercicio", typeof(string)] = nombreDelEjercicio;
					p[Parameter.Out, "cuanto", typeof(decimal)] = default;
				})
				.PerformQuery();

				return alquilados["cuanto"].GetValue<decimal>();
			}
		}

		public bool SePuedeVerLaProgresionDelPeso(string nombreDelEjercicio)
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@sePuede = trayectoria.SePuedeVerLaProgresionDelPeso(@nombreDelEjercicio);")
				.WithParameters(alquilados, p => {
					p["nombreDelEjercicio", typeof(string)] = nombreDelEjercicio;
					p[Parameter.Out, "sePuede", typeof(bool)] = default;
				})
				.PerformQuery();

				return alquilados["sePuede"].GetValue<bool>();
			}
		}

		// LOS DOS JUICIOS EN UNA SOLA VISTA, y ese es el punto: la lectura y la tendencia
		// no se deducen entre si, asi que la app tiene que poder decir las dos.
		//
		// La Referencia se construye DENTRO del bloque: en una consulta una asignacion de
		// primer nivel seria escritura de global, prohibida en contexto de solo lectura.
		public string ComoVoy(string nombreDeLaMedida,
			decimal bajoHasta, decimal altoDesde, decimal ruido, decimal noCreible)
		{
			return app.Using(@"
				{
					referencia = Referencia(@bajoHasta, @altoDesde, @ruido, @noCreible);
					lectura = trayectoria.ComoEstoy(@nombreDeLaMedida, referencia);
					tendencia = trayectoria.ComoVengo(@nombreDeLaMedida, referencia);
					print lectura.ValorMedido medido,
						lectura.QueSeComparo seCompara,
						lectura.ValorComparado comparado,
						lectura.ToString() estado,
						tendencia.ToString() movimiento,
						tendencia.Cambio cambio,
						tendencia.Urgencia().ToString() urgencia;
				}
			")
			.WithParameters(p => {
				p["nombreDeLaMedida", typeof(string)] = nombreDeLaMedida;
				p["bajoHasta", typeof(decimal)] = bajoHasta;
				p["altoDesde", typeof(decimal)] = altoDesde;
				p["ruido", typeof(decimal)] = ruido;
				p["noCreible", typeof(decimal)] = noCreible;
			})
			.PerformQuery();
		}

		// Desde el v2 dice cuantos dias no se esperaron por mora (N2): el numero viaja al
		// lado de la palabra, para que "esperaba 33" se pueda explicar con "desconto 6".
		public string IncumplimientoDelPlan(DateTime inicioDelPlan)
		{
			return app.Using(@"
				{
					incumplimiento = trayectoria.IncumplimientoDe(trayectoria.PlanQueEmpezo(@inicioDelPlan));
					print incumplimiento.DiasEsperados esperados,
						incumplimiento.DiasDescontadosPorInactividad descontadosPorInactividad,
						incumplimiento.DiasQueFue fue,
						incumplimiento.DiasQueFaltaron faltaron,
						incumplimiento.Cumplio cumplio;
				}
			")
			.WithParameters(p => {
				p["inicioDelPlan", typeof(DateTime)] = inicioDelPlan;
			})
			.PerformQuery();
		}

		// EL ESTADO DE LA MEMBRESIA. Si el gimnasio nunca mando la bandera, la consulta
		// falla diciendo que no se sabe -igual que ComoVoy sin mediciones-: no hay "activo"
		// por defecto. La etiqueta es una hoja del tipo, no un ToString (regla 13).
		public string EstadoDeLaMembresia()
		{
			return app.Using(@"
				{
					print trayectoria.Membresia.EstadoActual estado,
						trayectoria.Membresia.CuantosCambiosDeEstado cambios,
						trayectoria.Membresia.CuantosPagos pagos,
						trayectoria.Membresia.CuantosCobros cobros;
				}
			")
			.PerformQuery();
		}

		// LOS PERIODOS EN MORA, hoja por hoja, con los dias que caen dentro de la ventana
		// pedida. NO se imprime el fin: el de un periodo abierto no existe, y un nullable
		// viaja como bool mas valor (regla 13). Va si cerro, y cuantos dias de la ventana
		// estuvo en mora -que es lo que el plan descuenta-.
		public string PeriodosInactivos(DateTime desde, DateTime hasta)
		{
			return app.Using(@"
				foreach (elPeriodo in trayectoria.Membresia.PeriodosInactivos()) {
					print elPeriodo.Desde desde,
						elPeriodo.TieneFin cerrado,
						elPeriodo.DiasDentroDe(@desdeLaVentana, @hastaLaVentana) diasEnLaVentana;
				}
			")
			.WithParameters(p => {
				p["desdeLaVentana", typeof(DateTime)] = desde;
				p["hastaLaVentana", typeof(DateTime)] = hasta;
			})
			.PerformQuery();
		}

		// LA AUSENCIA. Proyectada hoja por hoja: una coleccion de objetos de dominio es
		// material, no un valor, y decidir que se ve de ella es del actor -regla 13-.
		public string PlanesSinMedicion(int diasDeGracia)
		{
			return app.Using(@"
				foreach (elPlan in trayectoria.PlanesQueTerminaronSinMedicion(@diasDeGracia)) {
					print elPlan.Inicio empezo,
						elPlan.Fin termino,
						elPlan.SemanasQueDuro() semanas;
				}
			")
			.WithParameters(p => {
				p["diasDeGracia", typeof(int)] = diasDeGracia;
			})
			.PerformQuery();
		}
	}
}
