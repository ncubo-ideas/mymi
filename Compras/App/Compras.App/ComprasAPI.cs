using System;
using System.Collections.Generic;
using Choreography.Theater;
using Compras.App.Eventos;
using Compras.App.Vistas;
using Puppeteer;
using Puppets.Portador;

namespace Compras.App
{
	// LA SUPERFICIE INTENCIONAL de la app. Todo el mundo entra por aca: la UI, el bucle
	// de refresh y el servidor MCP. Nadie habla con el actor crudo.
	//
	// Es tambien el IDiarioDeLaApp que el Portador necesita: el bucle no sabe hacer
	// performs, sabe pedirle a esto que aplique un mensaje.
	public sealed class ComprasAPI : IDiarioDeLaApp
	{
		private readonly PerformanceV2 app;

		public ComprasAPI(PerformanceV2 app)
		{
			ArgumentNullException.ThrowIfNull(app);
			this.app = app;
		}

		// ---------------------------------------------------------------- comandos

		// Aplica UN evento como UN comando. Devuelve "" si entro, o el mensaje del Check
		// si se rechazo.
		//
		// El checkpoint (bitacora.Avanzar) va DENTRO del mismo comando que inserta la
		// compra: eso es lo que hace exacto el resume tras una caida. O entraron los dos
		// o no entro ninguno; no existe el estado "guarde la compra pero no avance el
		// checkpoint", que seria el que duplica al reintentar.
		//
		// El Check es la idempotencia. Un evento redelivered se rechaza bajo el read lock
		// y NO deja entrada en el journal - por eso es un Check y no un if en el cuerpo,
		// que si persistiria una entrada no-op por cada reintento.
		//
		// Las categorias viajan como string[] y las resuelve el constructor de Compra
		// con Categoria.Desde: de aca para adentro no hay strings de categoria sueltos.
		public string AplicarCompraRegistrada(CompraRegistrada evento)
		{
			ArgumentNullException.ThrowIfNull(evento);
			if (evento.Evento <= 0)
				throw new ArgumentOutOfRangeException(nameof(evento), evento.Evento,
					"El numero de evento debe ser positivo.");

			// DOS SCRIPTS Y NO UNO CON GUARDA: el evento sin sucursal journalea EXACTAMENTE
			// el comando de siempre -los journals nuevos y los viejos quedan iguales linea a
			// linea-, y el que si la trae journalea la forma situada. La sucursal entra por
			// el wither EnSucursal en una sentencia aparte, no por otro constructor: Compra
			// ya tiene dos formas y el DSL resuelve por cantidad de argumentos.
			string script = evento.Sucursal.Length == 0
				? @"
					{
						compra = Compra(@numero, @fecha, @skus, @descripciones, @cantidades, @preciosUnitarios, @categorias, @mesesDeGarantia);
						compras.Agregar(compra);
						bitacora.Avanzar(@evento);
					}
				"
				: @"
					{
						compra = Compra(@numero, @fecha, @skus, @descripciones, @cantidades, @preciosUnitarios, @categorias, @mesesDeGarantia);
						situada = compra.EnSucursal(@sucursal);
						compras.Agregar(situada);
						bitacora.Avanzar(@evento);
					}
				";

			// EL RECONOCIMIENTO DEL DEPARTAMENTO VIVE ACA, no en la frontera. El nombre del
			// tipo va DENTRO del script: el motor lo resuelve por reflexion -que no tiene
			// frontera de assembly- asi que el dominio se queda entero `internal` y este
			// proyecto no nombra un solo tipo suyo en C#. Rechazar por Check no deja entrada
			// en el journal, igual que la idempotencia.
			return app.Using(
				scriptForChk: @"
					Check(@evento > bitacora.UltimoEvento) Error 'evento ya procesado';
					Check(Categoria.TodasExisten(@categorias)) Error 'departamento no modelado';
				",
				script)
			.WithParameters(p => {
				p["evento",           typeof(long)]      = evento.Evento;
				p["numero",           typeof(string)]    = evento.Numero;
				p["fecha",            typeof(DateTime)]  = evento.Fecha;
				p["skus",             typeof(string[])]  = evento.Skus;
				p["descripciones",    typeof(string[])]  = evento.Descripciones;
				p["cantidades",       typeof(int[])]     = evento.Cantidades;
				p["preciosUnitarios", typeof(decimal[])] = evento.PreciosUnitarios;
				p["categorias",       typeof(string[])]  = evento.Categorias;
				p["mesesDeGarantia",  typeof(int[])]     = evento.MesesDeGarantia;
				if (evento.Sucursal.Length > 0)
					p["sucursal", typeof(string)] = evento.Sucursal;
			})
			.PerformCheckThenCommand();
		}

		// El segundo evento: una promocion del super. MISMO esquema de idempotencia que la
		// compra -Check contra la bitacora, checkpoint DENTRO del mismo comando- porque
		// viajan por el mismo stream numerado: o entran los dos o no entra ninguno.
		public string AplicarPromocionPublicada(PromocionPublicada evento)
		{
			ArgumentNullException.ThrowIfNull(evento);
			if (evento.Evento <= 0)
				throw new ArgumentOutOfRangeException(nameof(evento), evento.Evento,
					"El numero de evento debe ser positivo.");

			return app.Using(
				scriptForChk: @"
					Check(@evento > bitacora.UltimoEvento) Error 'evento ya procesado';
				",
				@"
					{
						promocion = Promocion(@gancho, @detalle, @sku, @validaHasta);
						cartelera.Colgar(promocion);
						bitacora.Avanzar(@evento);
					}
				")
			.WithParameters(p => {
				p["evento",      typeof(long)]     = evento.Evento;
				p["gancho",      typeof(string)]   = evento.Gancho;
				p["detalle",     typeof(string)]   = evento.Detalle;
				p["sku",         typeof(string)]   = evento.Sku ?? "";
				p["validaHasta", typeof(DateTime)] = evento.ValidaHasta;
			})
			.PerformCheckThenCommand();
		}

		// ---------------------------------------------------------------- consultas

		// Valores TIPADOS, no texto parseado: es lo que le permite a las tools MCP
		// devolver structuredContent con numeros de verdad, y a la UI hacer aritmetica
		// sin adivinar el formato del print.
		public decimal TotalDeCompras()
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@total = compras.Total;")
				.WithParameters(alquilados, p => {
					p[Parameter.Out, "total", typeof(decimal)] = default;
				})
				.PerformQuery();

				return alquilados["total"].GetValue<decimal>();
			}
		}

		public int CantidadDeCompras()
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@cantidad = compras.Cantidad;")
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

		// El rango del historial. Es lo que le permite a quien pregunta -sobre todo a la
		// IA- saber que meses puede comparar, en vez de adivinar uno vacio.
		public string RangoDelHistorial() =>
			app.Using(@"
				{
					rango = compras.Rango;
					print rango.MesesConCompras mesesConCompras,
						rango.PrimerMesExpresado primerMes,
						rango.UltimoMesExpresado ultimoMes;
				}
			")
			.PerformQuery();

		// Las vistas de la app. Devuelven la emision del print tal cual - con el
		// formatter JSON eso YA es el JSON serializado, asi que no hay DTO ni
		// re-serializacion en el medio.
		//
		// La proyeccion se AUTORA aca, hoja por hoja. No se puede hacer
		// `print compras.Lista`: el motor lo rechaza porque una coleccion de objetos de
		// dominio es material, no un valor, y decidir que se ve de ella es del actor,
		// no del material.
		public string ListaDeCompras() =>
			app.Using(@"
				foreach (compra in compras.Lista) {
					print compra.Numero numero,
						compra.Fecha fecha,
						compra.Sucursal sucursal,
						compra.CantidadDeLineas lineas,
						compra.Total total;
				}
			")
			.PerformQuery();

		// El indexador con @param es DSL valido y es la razon por la que Historial
		// expone this[numero]. El bloque mantiene `compra` local: en una consulta una
		// asignacion de primer nivel seria escritura de global, prohibida en contexto
		// read-only.
		public string DetalleDeCompra(string numero)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(numero);

			return app.Using(@"
				{
					compra = compras[@numero];
					print compra.Numero numero,
						compra.Fecha fecha,
						compra.Sucursal sucursal,
						compra.Total total;
					foreach (item in compra.Items) {
						print item.Sku sku,
							item.Descripcion descripcion,
							item.Cantidad cantidad,
							item.PrecioUnitario precioUnitario,
							item.Total totalDeLinea,
							item.Categoria.Nombre categoria,
							item.TieneGarantia tieneGarantia;
					}
				}
			")
			.WithParameters(p => {
				p["numero", typeof(string)] = numero;
			})
			.PerformQuery();
		}

		// La foto de un mes, sumada POR DEPARTAMENTO. Los HECHOS con los que la superficie
		// o la IA arman la lectura; el dominio no devuelve prosa. La cabecera sale como
		// "canasta" y cada pasillo como "departamento".
		public string CanastaDelMes(int anio, int mes) =>
			app.Using(@"
				{
					canasta = compras.CanastaDe(@anio, @mes);
					print canasta.Anio anio,
						canasta.Mes mes,
						canasta.CantidadDeCompras cantidadDeCompras,
						canasta.GastoNoComestible gastoNoComestible,
						canasta.GastoComestible gastoComestible,
						canasta.GastoTotal gastoTotal,
						canasta.HayComestible hayComestible,
						canasta.HayCompras hayCompras;
					foreach (departamento in canasta.Departamentos) {
						print departamento.Categoria.Nombre categoria,
							departamento.Monto monto,
							departamento.ProporcionDelTotal proporcionDelTotal;
					}
				}
			")
			.WithParameters(p => {
				p["anio", typeof(int)] = anio;
				p["mes",  typeof(int)] = mes;
			})
			.PerformQuery();

		// LA consulta que justifica el dominio: como cambio la canasta entre dos meses.
		// Es la pregunta que ningun ticket contesta, y la mitad "compras" del cruce con
		// el colesterol que hace el asistente.
		//
		// La linea la produce el DOMINIO, y lo que se proyecta son hechos: los dos totales
		// y de que pasillos salio la diferencia. Sin veredicto — el super senala y se
		// calla; el cruce con cualquier otra cosa de la persona es del asistente.
		//
		// La direccion va en el SIGNO del cambio y no en un booleano: Subio es una lectura
		// guardada del dominio y en una proyeccion plana reventaria cuando no hay serie.
		public string ComparacionDeCanastas(int anioAntes, int mesAntes, int anioDespues, int mesDespues) =>
			app.Using(@"
				{
					comparacion = compras.CompararCanastas(@anioAntes, @mesAntes, @anioDespues, @mesDespues);
					print comparacion.HayComparacion hayComparacion,
						comparacion.GastoAntes gastoAntes,
						comparacion.GastoDespues gastoDespues,
						comparacion.CambioEnElTotal cambioEnElTotal,
						comparacion.CambioEnProporcion cambio,
						comparacion.HayAlgoQueLoExplique hayAlgoQueLoExplique,
						comparacion.Clave clave,
						comparacion.Antes.Anio anioAntes,
						comparacion.Antes.Mes mesAntes,
						comparacion.Antes.GastoComestible gastoComestibleAntes,
						comparacion.Despues.Anio anioDespues,
						comparacion.Despues.Mes mesDespues,
						comparacion.Despues.GastoComestible gastoComestibleDespues;
					foreach (diferencia in comparacion.Diferencias) {
						print diferencia.Categoria.Nombre categoria,
							diferencia.MontoAntes montoAntes,
							diferencia.MontoDespues montoDespues,
							diferencia.Cambio cambioDelPasillo,
							diferencia.Subio subio,
							diferencia.EsNuevo esNuevo,
							diferencia.Desaparecio desaparecio,
							diferencia.CuentaEnElMovimiento explicaElCambio;
					}
				}
			")
			.WithParameters(p => {
				p["anioAntes",   typeof(int)] = anioAntes;
				p["mesAntes",    typeof(int)] = mesAntes;
				p["anioDespues", typeof(int)] = anioDespues;
				p["mesDespues",  typeof(int)] = mesDespues;
			})
			.PerformQuery();

		// La comparacion por defecto: el ultimo mes con compras contra el mismo mes del
		// anio anterior. Que dos meses se comparan lo elige el DOMINIO, no la superficie.
		public string ComparacionConElAnioPasado() =>
			app.Using(@"
				{
					comparacion = compras.CompararConElAnioPasado();
					print comparacion.HayComparacion hayComparacion,
						comparacion.GastoAntes gastoAntes,
						comparacion.GastoDespues gastoDespues,
						comparacion.CambioEnElTotal cambioEnElTotal,
						comparacion.CambioEnProporcion cambio,
						comparacion.HayAlgoQueLoExplique hayAlgoQueLoExplique,
						comparacion.Clave clave,
						comparacion.Antes.Anio anioAntes,
						comparacion.Antes.Mes mesAntes,
						comparacion.Antes.GastoComestible gastoComestibleAntes,
						comparacion.Despues.Anio anioDespues,
						comparacion.Despues.Mes mesDespues,
						comparacion.Despues.GastoComestible gastoComestibleDespues;
					foreach (diferencia in comparacion.Diferencias) {
						print diferencia.Categoria.Nombre categoria,
							diferencia.MontoAntes montoAntes,
							diferencia.MontoDespues montoDespues,
							diferencia.Cambio cambioDelPasillo,
							diferencia.Subio subio,
							diferencia.EsNuevo esNuevo,
							diferencia.Desaparecio desaparecio,
							diferencia.CuentaEnElMovimiento explicaElCambio;
					}
				}
			")
			.PerformQuery();

		// EL ULTIMO MES CONTRA EL MES ANTERIOR, que es otra pregunta: contra el año pasado se
		// ve si la canasta cambio de fondo -las estaciones y los habitos ya dieron la
		// vuelta-, y contra el mes anterior se ve si ESTE mes viene distinto, que es lo que
		// alguien pregunta cuando abre la app un martes.
		//
		// La aritmetica del mes anterior la hace el DOMINIO: una superficie que la resuelva
		// sola la va a resolver mal en enero.
		public string ComparacionConElMesPasado() =>
			app.Using(@"
				{
					comparacion = compras.CompararConElMesAnterior();
					print comparacion.HayComparacion hayComparacion,
						comparacion.GastoAntes gastoAntes,
						comparacion.GastoDespues gastoDespues,
						comparacion.CambioEnElTotal cambioEnElTotal,
						comparacion.CambioEnProporcion cambio,
						comparacion.HayAlgoQueLoExplique hayAlgoQueLoExplique,
						comparacion.Clave clave,
						comparacion.Antes.Anio anioAntes,
						comparacion.Antes.Mes mesAntes,
						comparacion.Antes.GastoComestible gastoComestibleAntes,
						comparacion.Despues.Anio anioDespues,
						comparacion.Despues.Mes mesDespues,
						comparacion.Despues.GastoComestible gastoComestibleDespues;
					foreach (diferencia in comparacion.Diferencias) {
						print diferencia.Categoria.Nombre categoria,
							diferencia.MontoAntes montoAntes,
							diferencia.MontoDespues montoDespues,
							diferencia.Cambio cambioDelPasillo,
							diferencia.Subio subio,
							diferencia.EsNuevo esNuevo,
							diferencia.Desaparecio desaparecio,
							diferencia.CuentaEnElMovimiento explicaElCambio;
					}
				}
			")
			.PerformQuery();

		// LO QUE NO FALLA: los productos que vuelven compra tras compra. Es la lectura con
		// la que la especificacion de marca pide que la app ABRA -"la pantalla abre con la
		// firma de su semana... la lista de recibos existe, pero no es la portada"-.
		//
		// La cabecera sale como "firma" y cada producto como "habitual".
		public string FirmaDeLaSemana(int cuantasCompras) =>
			app.Using(@"
				{
					firma = compras.FirmaDeLasUltimas(@cuantas);
					print firma.ComprasMiradas comprasMiradas,
						firma.CuantosHabituales cuantosHabituales,
						firma.Clave clave;
					foreach (habitual in firma.Habituales) {
						print habitual.Sku sku,
							habitual.Descripcion descripcion,
							habitual.ComprasEnQueAparece comprasEnQueAparece,
							habitual.ComprasMiradas deCuantas,
							habitual.Proporcion proporcion,
							habitual.EstaEnTodas estaEnTodas,
							habitual.Constancia.Clave constancia;
					}
				}
			")
			.WithParameters(p => {
				p["cuantas", typeof(int)] = cuantasCompras;
			})
			.PerformQuery();

		// EL QUIEBRE, con fecha: que se volvio habitual y desde cuando, y que dejo de
		// aparecer. La otra lectura de la portada.
		//
		// El MES del cambio no se proyecta: es una lectura guardada del dominio y en una
		// proyeccion plana reventaria cuando no hay cambio. Sale del primer producto que
		// entro, que ya viene ordenado por fecha.
		public string CambioEnLaCanasta(DateTime corte, int cuantasCompras) =>
			app.Using(@"
				{
					cambio = compras.CambioDesde(@corte, @cuantas);
					print cambio.HayConQueComparar hayConQueComparar,
						cambio.HayCambio hayCambio,
						cambio.Clave clave;
					foreach (entro in cambio.Entraron) {
						print entro.Sku sku,
							entro.Descripcion descripcion,
							entro.DesdeExpresado desde,
							entro.ComprasEnQueAparece comprasEnQueAparece,
							entro.Proporcion proporcion;
					}
					foreach (salio in cambio.Salieron) {
						print salio.Sku sku,
							salio.Descripcion descripcion,
							salio.ComprasEnQueAparece comprasEnQueAparece,
							salio.Proporcion proporcion;
					}
				}
			")
			.WithParameters(p => {
				p["corte",   typeof(DateTime)] = corte;
				p["cuantas", typeof(int)] = cuantasCompras;
			})
			.PerformQuery();

		// EL MISMO CAMBIO, PERO EL QUIEBRE LO ENCUENTRA EL DOMINIO. Es la que usa la portada.
		//
		// SE PROYECTA CorteExpresado Y NO Corte: la lectura tipada es GUARDADA -revienta
		// cuando no hay dos lados que comparar- y en un print plano eso no blanquea la
		// columna, mata la consulta entera. CorteExpresado es su pareja total y devuelve
		// vacio, que es justo lo que la pantalla necesita para no dibujar nada.
		public string CambioEncontrado(int cuantasCompras) =>
			app.Using(@"
				{
					cambio = compras.CambioEncontrado(@cuantas);
					print cambio.HayConQueComparar hayConQueComparar,
						cambio.HayCambio hayCambio,
						cambio.CorteExpresado corte,
						cambio.Clave clave;
					foreach (entro in cambio.Entraron) {
						print entro.Sku sku,
							entro.Descripcion descripcion,
							entro.DesdeExpresado desde,
							entro.ComprasEnQueAparece comprasEnQueAparece,
							entro.Proporcion proporcion;
					}
					foreach (salio in cambio.Salieron) {
						print salio.Sku sku,
							salio.Descripcion descripcion,
							salio.ComprasEnQueAparece comprasEnQueAparece,
							salio.Proporcion proporcion;
					}
				}
			")
			.WithParameters(p => {
				p["cuantas", typeof(int)] = cuantasCompras;
			})
			.PerformQuery();

		// CADA CUANTO COMPRA UN PRODUCTO, antes y despues del corte. Es la lectura que la
		// wiki pide por su nombre -"el helado paso de 1 vez al mes a casi todas las
		// semanas"- y la tarjeta "un cambio en tu rutina" del diseño de la app.
		//
		// Se proyectan las lecturas TOTALES: aca el producto puede no tener ritmo medido, y
		// Antes/Ahora son guardadas. En una proyeccion plana un lector guardado no revienta
		// el campo, revienta la consulta entera.
		public string RitmoDeUnProducto(string producto, DateTime corte)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(producto);

			return app.Using(@"
				{
					ritmo = compras.RitmoDeUnProducto(@producto, @corte);
					print ritmo.Sku sku,
						ritmo.Descripcion descripcion,
						ritmo.HayConQueComparar hayConQueComparar,
						ritmo.Cambio cambio,
						ritmo.ClaveDeAntes antes,
						ritmo.ClaveDeAhora ahora,
						ritmo.Clave clave;
				}
			")
			.WithParameters(p => {
				p["producto", typeof(string)]   = producto;
				p["corte",    typeof(DateTime)] = corte;
			})
			.PerformQuery();
		}

		// COMO VA AL SUPER: cada cuantos dias vuelve, que dia y en que momento. Es la
		// pantalla "tu ritmo de super" del diseño de la app, con las tres facetas juntas.
		//
		// Se proyectan las lecturas TOTALES -DiasEntreCompras, CadenciaLegible,
		// MomentoFavoritoExpresado- porque sin suficientes compras las tipadas son
		// guardadas, y en una proyeccion plana eso revienta la consulta entera. Los tres
		// momentos SI vienen siempre, asi que su foreach no necesita guarda.
		//
		// DE CADA MOMENTO SE PROYECTA ParaMostrar Y NO Nombre. Nombre es la etiqueta interna
		// sin acentos, que existe para comparar y serializar: en pantalla se leia "Manana",
		// y peor, el favorito se compara contra MomentoFavoritoExpresado -que SI trae
		// acento-, asi que la mañana nunca se marcaba como favorita. Tarde y noche
		// funcionaban de casualidad, por no tener tilde.
		public string RitmoDelSuper() =>
			app.Using(@"
				{
					ritmo = compras.RitmoDelSuper();
					print ritmo.ComprasMiradas comprasMiradas,
						ritmo.EsObservado esObservado,
						ritmo.DiasEntreCompras diasEntreCompras,
						ritmo.ClaveDeLaCadencia cadencia,
						ritmo.ClaveDelDiaFavorito diaFavorito,
						ritmo.ComprasEnElDiaFavorito comprasEnElDiaFavorito,
						ritmo.ClaveDelMomentoFavorito momentoFavorito,
						ritmo.Clave clave;
					foreach (momento in ritmo.PorMomento) {
						print momento.ClaveDelMomento nombre,
							momento.Cuantas cuantas,
							momento.DeCuantas deCuantas,
							momento.Proporcion proporcion;
					}
				}
			")
			.PerformQuery();

		// LOS PRODUCTOS A LOS QUE LES CAMBIO EL RITMO, el mas notorio primero.
		//
		// ACA SI se proyecta EsMasSeguido, que es guardada, y no es una inconsistencia: el
		// dominio devuelve esta lista YA FILTRADA -solo los que tienen ritmo medido y
		// cambiaron-, asi que dentro de la lista ninguna guarda puede saltar. La diferencia
		// con la consulta de arriba es de quien garantiza la precondicion.
		public string CambiosDeRitmo(DateTime corte) =>
			app.Using(@"
				{
					cambios = compras.CambiosDeRitmo(@corte);
					foreach (ritmo in cambios) {
						print ritmo.Sku sku,
							ritmo.Descripcion descripcion,
							ritmo.ClaveDeAntes antes,
							ritmo.ClaveDeAhora ahora,
							ritmo.EsMasSeguido esMasSeguido,
							ritmo.Clave clave;
					}
				}
			")
			.WithParameters(p => {
				p["corte", typeof(DateTime)] = corte;
			})
			.PerformQuery();

		// LOS MISMOS RITMOS, contra el quiebre que encuentra el dominio. Las dos tarjetas de
		// la portada cuentan EL MISMO quiebre desde angulos distintos -una dice QUE entro, la
		// otra CADA CUANTO-, asi que tienen que partir por la misma fecha, y quien garantiza
		// eso es el dominio y no la coincidencia de que la pantalla les pase lo mismo.
		public string CambiosDeRitmoDelQuiebre(int cuantasCompras) =>
			app.Using(@"
				{
					cambios = compras.CambiosDeRitmoDelQuiebre(@cuantas);
					foreach (ritmo in cambios) {
						print ritmo.Sku sku,
							ritmo.Descripcion descripcion,
							ritmo.ClaveDeAntes antes,
							ritmo.ClaveDeAhora ahora,
							ritmo.EsMasSeguido esMasSeguido,
							ritmo.Clave clave;
					}
				}
			")
			.WithParameters(p => {
				p["cuantas", typeof(int)] = cuantasCompras;
			})
			.PerformQuery();

		// COMO SE MOVIERON LOS PRECIOS de los productos que si lleva. Es "lo que cambio de
		// precio - de los productos que si llevas" del diseño de la app, y es SU propia
		// inflacion: el dato que ninguna app de super da hoy.
		//
		// El dominio devuelve la lista YA FILTRADA -solo los que tienen serie y cambio
		// real-, asi que dentro del foreach ninguna guarda puede saltar.
		public string PreciosDeLosHabituales(int cuantasCompras) =>
			app.Using(@"
				{
					precios = compras.PreciosDeLosHabituales(@cuantas);
					foreach (precio in precios) {
						print precio.Sku sku,
							precio.Descripcion descripcion,
							precio.PrimerPrecioPorUnidad antes,
							precio.UltimoPrecioPorUnidad ahora,
							precio.UnidadDeReferencia unidad,
							precio.CambioEnProporcion cambioEnProporcion,
							precio.Subio subio,
							precio.VecesQueLoCompro vecesQueLoCompro,
							precio.PrimerMesExpresado desde,
							precio.UltimoMesExpresado hasta,
							precio.Clave clave;
					}
				}
			")
			.WithParameters(p => {
				p["cuantas", typeof(int)] = cuantasCompras;
			})
			.PerformQuery();

		// EN QUE SUCURSALES COMPRA, y desde cuando se mueve. LaDeSiempre NO se imprime:
		// es una lectura guardada (revienta sin datos) y en un print plano eso mata la
		// consulta; la primera del arreglo ES la de siempre.
		public string SucursalesQueFrecuenta() =>
			app.Using(@"
				{
					recorrido = compras.Sucursales();
					print recorrido.HayDatos hayDatos,
						recorrido.CuantasDistintas cuantasDistintas,
						recorrido.ComprasSinSucursal comprasSinSucursal,
						recorrido.ComprasConSucursal comprasConSucursal,
						recorrido.NombreDeLaDeSiempre laDeSiempre,
						recorrido.ComprasEnLaDeSiempre comprasEnLaDeSiempre,
						recorrido.SeMueve seMueve,
						recorrido.SeMueveDesdeExpresado seMueveDesde,
						recorrido.Clave clave;
					foreach (sucursal in recorrido.Sucursales) {
						print sucursal.Nombre nombre,
							sucursal.Compras compras,
							sucursal.PrimeraVez primeraVez,
							sucursal.UltimaVez ultimaVez;
					}
				}
			")
			.PerformQuery();

		// LA SERIE DE PRECIOS de UN producto, con cada observacion fechada: es la que
		// contesta "el mismo helado te costaba 3.680 en enero de 2025 y hoy 4.214".
		// Geannina la pidio publicada: estaba construida y solo la leian las
		// comprobaciones.
		//
		// OJO: Subio NO se imprime a proposito. Es una lectura GUARDADA (revienta sin
		// cambio real) y en un print plano eso mata la consulta entera; el signo ya
		// viaja en cambio.
		public string SerieDePrecios(string termino)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(termino);

			return app.Using(@"
				{
					serie = compras.PrecioBuscado(@termino);
					print serie.Sku sku,
						serie.Descripcion descripcion,
						serie.VecesQueLoCompro vecesQueLoCompro,
						serie.HaySerie haySerie,
						serie.PrimerPrecioPorUnidad primero,
						serie.UltimoPrecioPorUnidad ultimo,
						serie.UnidadDeReferencia unidad,
						serie.Cambio cambio,
						serie.CambioEnProporcion cambioEnProporcion,
						serie.HayCambioReal hayCambioReal,
						serie.PrimerMesExpresado desde,
						serie.UltimoMesExpresado hasta,
						serie.Clave clave;
					foreach (observado in serie.Observados) {
						print observado.Fecha fecha,
							observado.MesExpresado mes,
							observado.PrecioPorUnidad precioPorUnidad,
							observado.PrecioDeLinea precioDeLinea,
							observado.ClaveDeLaPresentacion presentacion,
							observado.ContenidoDeLaPresentacion contenido,
							observado.PiezasDeLaPresentacion piezas,
							observado.SimboloDeLaPresentacion simbolo,
							observado.NumeroDeCompra numeroDeCompra;
					}
				}
			")
			.WithParameters(p => {
				p["termino", typeof(string)] = termino;
			})
			.PerformQuery();
		}

		// LA CANASTA FRENTE A SU PASADO: cuanto cuesta hoy una pieza de cada habitual
		// contra lo que costaba hace tantos meses, sumado en colones. Es la cifra que
		// encabeza la tarjeta de precios ("+₡1.150 vs. hace 3 meses", del mock de
		// Fernanda), con el detalle por producto para quien quiera saber de donde sale.
		public string CanastaFrenteA(DateTime hoy, int mesesAtras, int cuantasCompras) =>
			app.Using(@"
				{
					frente = compras.CanastaFrenteA(@hoy, @meses, @cuantas);
					print frente.HayConQueComparar hayConQueComparar,
						frente.MesesDeVentana mesesDeVentana,
						frente.Cambio cambio,
						frente.Clave clave;
					foreach (producto in frente.Productos) {
						print producto.Sku sku,
							producto.Descripcion descripcion,
							producto.ClaveDeLaPresentacion presentacion,
							producto.ContenidoDeLaPresentacion contenido,
							producto.PiezasDeLaPresentacion piezas,
							producto.SimboloDeLaPresentacion simbolo,
							producto.Antes antes,
							producto.Ahora ahora,
							producto.Cambio cambio;
					}
				}
			")
			.WithParameters(p => {
				p["hoy", typeof(DateTime)] = hoy;
				p["meses", typeof(int)] = mesesAtras;
				p["cuantas", typeof(int)] = cuantasCompras;
			})
			.PerformQuery();

		// LA BUENA NOTICIA DE UN RECIBO: que, de esa compra, esta hoy mas barato. Todas
		// las lecturas son totales -Hay en false trae el resto vacio o en cero-, asi que
		// el print plano no corre peligro.
		public string RebajaDesde(string numeroDeCompra)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(numeroDeCompra);

			return app.Using(@"
				{
					rebaja = compras.RebajaDesde(@numero);
					print rebaja.Hay hay,
						rebaja.Sku sku,
						rebaja.Descripcion descripcion,
						rebaja.NumeroDeCompra numeroDeCompra,
						rebaja.CuantoMenos cuantoMenos,
						rebaja.Clave clave;
				}
			")
			.WithParameters(p => {
				p["numero", typeof(string)] = numeroDeCompra;
			})
			.PerformQuery();
		}

		// La garantia de un producto, con su respaldo: en que compra fue, cuando, y si
		// sigue vigente. La FECHA DE CONSULTA entra como parametro porque el dominio no
		// lee el reloj: quien pregunta pone su "hoy".
		public string GarantiaDe(string texto, DateTime fechaDeConsulta)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(texto);

			return app.Using(@"
				{
					registro = compras.BuscarGarantia(@texto, @fecha);
					print registro.Encontrada encontrada,
						registro.TextoBuscado textoBuscado,
						registro.Sku sku,
						registro.Descripcion descripcion,
						registro.NumeroDeCompra numeroDeCompra,
						registro.FechaDeCompra fechaDeCompra,
						registro.MesesDeCobertura mesesDeCobertura,
						registro.VenceEl venceEl,
						registro.Vigente vigente,
						registro.DiasParaVencer diasParaVencer,
						registro.NombreDeLaSeveridad severidad,
						registro.Clave clave;
				}
			")
			.WithParameters(p => {
				p["texto", typeof(string)]   = texto;
				p["fecha", typeof(DateTime)] = fechaDeConsulta;
			})
			.PerformQuery();
		}

		// Todas las coberturas que siguen corriendo en la fecha de consulta.
		public string GarantiasVigentesEl(DateTime fechaDeConsulta) =>
			app.Using(@"
				{
					vigentes = compras.GarantiasVigentes(@fecha);
					foreach (registro in vigentes) {
						print registro.Sku sku,
							registro.Descripcion descripcion,
							registro.NumeroDeCompra numeroDeCompra,
							registro.FechaDeCompra fechaDeCompra,
							registro.MesesDeCobertura mesesDeCobertura,
							registro.VenceEl venceEl,
							registro.DiasParaVencer diasParaVencer,
							registro.NombreDeLaSeveridad severidad,
							registro.Clave clave;
					}
				}
			")
			.WithParameters(p => {
				p["fecha", typeof(DateTime)] = fechaDeConsulta;
			})
			.PerformQuery();

		// ---------------------------------------------------- lo que NO esta (el hueco)

		// Las coberturas cuya ventana se esta cerrando. No es la lista de garantias que
		// la app ya muestra: son las que piden algo, y eso sale del plazo de cada una
		// contra la fecha de consulta.
		public string GarantiasPorVencerEl(DateTime fechaDeConsulta) =>
			app.Using(@"
				{
					porVencer = compras.GarantiasPorVencer(@fecha);
					foreach (registro in porVencer) {
						print registro.Sku sku,
							registro.Descripcion descripcion,
							registro.NumeroDeCompra numeroDeCompra,
							registro.FechaDeCompra fechaDeCompra,
							registro.VenceEl venceEl,
							registro.DiasParaVencer diasParaVencer,
							registro.NombreDeLaSeveridad severidad,
							registro.Clave clave;
					}
				}
			")
			.WithParameters(p => {
				p["fecha", typeof(DateTime)] = fechaDeConsulta;
			})
			.PerformQuery();

		// CUANTO Y CUANDO entro algo, mes a mes. La cabecera y los meses se imprimen en
		// el mismo bloque: la cabecera sale como "consumo" y cada mes como "mes".
		public string ConsumoDe(string termino)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(termino);

			return app.Using(@"
				{
					consumo = compras.ConsumoDe(@termino);
					print consumo.Termino termino,
						consumo.EsCategoria esCategoria,
						consumo.MesesConConsumo mesesConConsumo,
						consumo.TotalGastado totalGastado,
						consumo.TotalUnidades totalUnidades,
						consumo.PrimerMesExpresado primerMes,
						consumo.UltimoMesExpresado ultimoMes,
						consumo.MesDelPicoExpresado mesDelPico,
						consumo.GastoEnElPrimerMes gastoEnElPrimerMes,
						consumo.GastoEnElUltimoMes gastoEnElUltimoMes,
						consumo.CambioDeGasto cambioDeGasto,
						consumo.TotalCompras totalCompras,
						consumo.UltimaVezExpresada ultimaVez,
						consumo.Constancia.Clave constancia,
						consumo.HaySerie haySerie,
						consumo.HayCambioReal hayCambioReal,
						consumo.Clave clave;
					foreach (mes in consumo.Meses) {
						print mes.MesExpresado mes,
							mes.Unidades unidades,
							mes.Gasto gasto,
							mes.Compras compras;
					}
					foreach (anio in consumo.Anios) {
						print anio.AnioExpresado anio,
							anio.Unidades unidades,
							anio.Gasto gasto,
							anio.Compras compras,
							anio.MesesConCompras mesesConCompras;
					}
				}
			")
			.WithParameters(p => {
				p["termino", typeof(string)] = termino;
			})
			.PerformQuery();
		}

		// EN CUALES COMPRAS APARECIO ESTO. La cabecera y las compras salen en el mismo
		// bloque, igual que el consumo: la cabecera como "busqueda" y cada compra como
		// "compra".
		//
		// TODOS LOS CAMPOS DE LA CABECERA SON TOTALES, y eso no es suerte: un print plano
		// no puede preguntar Existe() antes de imprimir, y un lector guardado no le
		// blanquea la columna — le mata la consulta entera. Buscar algo que no existe tiene
		// que devolver la cabecera en cero, no un error.
		public string ComprasCon(string termino)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(termino);

			return app.Using(@"
				{
					busqueda = compras.ComprasCon(@termino);
					print busqueda.Buscado buscado,
						busqueda.Existe() existe,
						busqueda.CuantasCompras cuantasCompras,
						busqueda.TotalUnidades totalUnidades,
						busqueda.TotalGastado totalGastado,
						busqueda.Clave clave;
					foreach (producto in busqueda.Productos) {
						print producto nombre;
					}
					foreach (compra in busqueda.Compras) {
						print compra.Numero numero,
							compra.FechaExpresada fecha,
							compra.Sucursal sucursal,
							compra.Unidades unidades,
							compra.Gasto gasto,
							compra.LineasDeLaCompra lineasDeLaCompra,
							compra.TotalDeLaCompra totalDeLaCompra;
					}
				}
			")
			.WithParameters(p => {
				p["termino", typeof(string)] = termino;
			})
			.PerformQuery();
		}

		// LO QUE VA A FALTAR EN LA PROXIMA COMPRA. La cabecera y los productos salen en el
		// mismo bloque: la cabecera como "lista" y cada producto como "producto".
		//
		// SE PROYECTAN TODOS y no solo los que tocan: el filtro es de la pantalla -que muestra
		// los atrasados- y de la IA, que a veces quiere saber que esta por caer. Recortar aca
		// obligaria a dos consultas para dos lecturas de la misma lista.
		public string LoQueVaAFaltar(DateTime hoy) =>
			app.Using(@"
				{
					lista = compras.LoQueVaAFaltar(@hoy);
					print lista.Existe() existe,
						lista.CuantosTocan cuantosTocan,
						lista.Clave clave;
					foreach (producto in lista.Productos) {
						print producto.Sku sku,
							producto.Descripcion descripcion,
							producto.ClaveDeLaCadencia cadencia,
							producto.DiasTipicos diasTipicos,
							producto.UltimaVezExpresada ultimaVez,
							producto.DiasDesdeLaUltima diasDesdeLaUltima,
							producto.DiasDeMas diasDeMas,
							producto.DiasQueFaltan diasQueFaltan,
							producto.Toca toca,
							producto.Clave clave;
					}
				}
			")
			.WithParameters(p => {
				p["hoy", typeof(DateTime)] = hoy;
			})
			.PerformQuery();

		// LAS PROMOCIONES VIGENTES a una fecha. El "hoy" viene recibido: el dominio no lee
		// el reloj, y una promocion vigente-o-no depende de cuando se pregunta.
		public string PromocionesVigentesEl(DateTime fecha) =>
			app.Using(@"
				{
					foreach (promocion in cartelera.VigentesEl(@fecha)) {
						print promocion.Gancho gancho,
							promocion.Detalle detalle,
							promocion.Sku sku,
							promocion.ValidaHastaExpresada validaHasta,
							promocion.DiasQueQuedanEl(@fecha) diasQueQuedan,
							promocion.Clave clave;
					}
				}
			")
			.WithParameters(p => {
				p["fecha", typeof(DateTime)] = fecha;
			})
			.PerformQuery();

		// LA consulta que mira el hueco: que dejo de entrar a la canasta. Una pantalla no
		// puede renderizar una ausencia que nadie le paso.
		public string AbastecimientosPendientesEl(DateTime fechaDeConsulta) =>
			app.Using(@"
				{
					pendientes = compras.AbastecimientosPendientes(@fecha);
					foreach (pendiente in pendientes) {
						print pendiente.NombreDeLaCategoria categoria,
							pendiente.UltimaCompra ultimaCompra,
							pendiente.DiasDesdeLaUltima diasDesdeLaUltima,
							pendiente.DiasEsperados diasEsperados,
							pendiente.EsRitmoObservado esRitmoObservado,
							pendiente.EsComestible esComestible,
							pendiente.NombreDeLaSeveridad severidad,
							pendiente.Clave clave;
					}
				}
			")
			.WithParameters(p => {
				p["fecha", typeof(DateTime)] = fechaDeConsulta;
			})
			.PerformQuery();

		/// <summary>
		/// EL IDIOMA DEL REGISTRO del supermercado, que es el que contestan las caras que le hablan al
		/// modelo y la consola. Su dueno es la redaccion; esto es el nombre con que las cascaras lo
		/// piden, para que no tengan que nombrarla.
		/// </summary>
		public const string IdiomaDelRegistro = Frases.Frases.IdiomaDelRegistro;

		// Las mismas vistas, ya tipadas. Es lo que consumen la UI y las tools MCP; las
		// variantes que devuelven el string crudo quedan para diagnostico.
		//
		// CADA UNA PIDE UN IDIOMA, y no lo adivina: el dominio entrega una clave y sus valores, y
		// quien pregunta es el unico que sabe en que idioma quiere leerlos. La caratula pasa el de la
		// persona; el servidor del asistente y la consola pasan el del registro.
		public IReadOnlyList<CompraEnLista> Compras() =>
			ProyeccionDeVistas.Lista(ListaDeCompras());

		public CompraDetallada Detalle(string numero, string idioma) =>
			ProyeccionDeVistas.Detalle(DetalleDeCompra(numero), Frases.Frases.De(idioma));

		public CanastaDelMesVista Canasta(int anio, int mes, string idioma) =>
			ProyeccionDeVistas.Canasta(CanastaDelMes(anio, mes), Frases.Frases.De(idioma));

		/// <summary>Cuántas compras se miran para reconocer la firma, si nadie dice otra cosa.</summary>
		// Ocho son unas dos meses de compras semanales: alcanza para que "lo de siempre"
		// se repita y es poco para que un cambio de habitos quede sepultado.
		public const int ComprasQueSeMiranParaLaFirma = 8;

		public FirmaDeLaSemana Firma(string idioma, int cuantasCompras = ComprasQueSeMiranParaLaFirma) =>
			ProyeccionDeVistas.Firma(FirmaDeLaSemana(cuantasCompras), Frases.Frases.De(idioma));

		public CambioDeLaCanasta Cambio(DateTime corte, string idioma,
			int cuantasCompras = ComprasQueSeMiranParaLaFirma) =>
			ProyeccionDeVistas.Cambio(CambioEnLaCanasta(corte, cuantasCompras), Frases.Frases.De(idioma));

		// LAS DOS DEL QUIEBRE QUE ENCUENTRA EL DOMINIO. Son las que usa la portada; las
		// que reciben el corte se quedan para quien quiera preguntar por una fecha suya.
		public CambioDeLaCanasta CambioDelQuiebre(string idioma,
			int cuantasCompras = ComprasQueSeMiranParaLaFirma) =>
			ProyeccionDeVistas.Cambio(CambioEncontrado(cuantasCompras), Frases.Frases.De(idioma));

		public IReadOnlyList<RitmoQueCambio> RitmosDelQuiebre(string idioma,
			int cuantasCompras = ComprasQueSeMiranParaLaFirma) =>
			ProyeccionDeVistas.RitmosQueCambiaron(CambiosDeRitmoDelQuiebre(cuantasCompras),
				Frases.Frases.De(idioma));

		public Vistas.RitmoDeUnProducto Ritmo(string producto, DateTime corte, string idioma) =>
			ProyeccionDeVistas.Ritmo(RitmoDeUnProducto(producto, corte), Frases.Frases.De(idioma));

		public IReadOnlyList<RitmoQueCambio> RitmosQueCambiaron(DateTime corte, string idioma) =>
			ProyeccionDeVistas.RitmosQueCambiaron(CambiosDeRitmo(corte), Frases.Frases.De(idioma));

		public RitmoDelSuperVista Ritmo(string idioma) =>
			ProyeccionDeVistas.RitmoDelSuper(RitmoDelSuper(), Frases.Frases.De(idioma));

		public IReadOnlyList<PrecioQueCambio> Precios(string idioma,
			int cuantasCompras = ComprasQueSeMiranParaLaFirma) =>
			ProyeccionDeVistas.Precios(PreciosDeLosHabituales(cuantasCompras), Frases.Frases.De(idioma));

		/// <summary>La ventana contra la que se mira el pasado, si nadie pide otra.</summary>
		// ESPEJO DEL JUICIO DEL DOMINIO, y la copia esta AMARRADA. El juicio es suyo
		// -CanastaFrenteAlPasado.VentanaPorDefectoEnMeses, "esta aca para que ninguna
		// superficie invente el suyo"- pero un valor por defecto de un parametro tiene que
		// ser una constante de compilacion, y desde que el dominio es enteramente `internal`
		// esta Superficie no puede nombrarlo. Comprobaciones compara las dos: si el dominio
		// cambia su juicio y esto no, el fixture lo dice, en vez de que la portada empiece a
		// mirar una ventana distinta de la que el dominio documenta.
		public const int VentanaPorDefectoEnMeses = 3;

		public CanastaFrenteAlPasadoVista FrenteAlPasado(DateTime hoy, string idioma,
			int mesesAtras = VentanaPorDefectoEnMeses,
			int cuantasCompras = ComprasQueSeMiranParaLaFirma) =>
			ProyeccionDeVistas.FrenteAlPasado(CanastaFrenteA(hoy, mesesAtras, cuantasCompras),
				Frases.Frases.De(idioma));

		public RebajaVista Rebaja(string numeroDeCompra, string idioma) =>
			ProyeccionDeVistas.Rebaja(RebajaDesde(numeroDeCompra), Frases.Frases.De(idioma));

		public SerieDePreciosVista Serie(string termino, string idioma) =>
			ProyeccionDeVistas.Serie(SerieDePrecios(termino), Frases.Frases.De(idioma));

		public RecorridoDeSucursalesVista Recorrido(string idioma) =>
			ProyeccionDeVistas.Recorrido(SucursalesQueFrecuenta(), Frases.Frases.De(idioma));

		public CanastasComparadas Comparacion(int anioAntes, int mesAntes, int anioDespues, int mesDespues,
			string idioma) =>
			ProyeccionDeVistas.Comparacion(ComparacionDeCanastas(anioAntes, mesAntes, anioDespues, mesDespues),
				Frases.Frases.De(idioma));

		public CanastasComparadas ComparacionConElMesAnterior(string idioma) =>
			ProyeccionDeVistas.Comparacion(ComparacionConElMesPasado(), Frases.Frases.De(idioma));

		public CanastasComparadas ComparacionConElAnioAnterior(string idioma) =>
			ProyeccionDeVistas.Comparacion(ComparacionConElAnioPasado(), Frases.Frases.De(idioma));

		public RangoDelHistorialVista Rango() =>
			ProyeccionDeVistas.Rango(RangoDelHistorial());

		public ConsumoEnElTiempo Consumo(string termino, string idioma) =>
			ProyeccionDeVistas.Consumo(ConsumoDe(termino), Frases.Frases.De(idioma));

		public ComprasHalladas Halladas(string termino, string idioma) =>
			ProyeccionDeVistas.Halladas(ComprasCon(termino), Frases.Frases.De(idioma));

		public ListaDeLaProximaCompra Lista(DateTime hoy, string idioma) =>
			ProyeccionDeVistas.LaProximaCompra(LoQueVaAFaltar(hoy), Frases.Frases.De(idioma));

		public IReadOnlyList<PromocionVista> Promociones(DateTime fecha, string idioma) =>
			ProyeccionDeVistas.Promociones(PromocionesVigentesEl(fecha), Frases.Frases.De(idioma));

		public GarantiaDeProducto Garantia(string texto, DateTime fechaDeConsulta, string idioma) =>
			ProyeccionDeVistas.Garantia(GarantiaDe(texto, fechaDeConsulta), Frases.Frases.De(idioma));

		public IReadOnlyList<GarantiaDeProducto> GarantiasVigentes(DateTime fechaDeConsulta, string idioma) =>
			ProyeccionDeVistas.GarantiasVigentes(GarantiasVigentesEl(fechaDeConsulta), Frases.Frases.De(idioma));

		public IReadOnlyList<GarantiaDeProducto> GarantiasPorVencer(DateTime fechaDeConsulta, string idioma) =>
			ProyeccionDeVistas.GarantiasVigentes(GarantiasPorVencerEl(fechaDeConsulta), Frases.Frases.De(idioma));

		public IReadOnlyList<AbastecimientoEnEspera> AbastecimientosPendientes(DateTime fechaDeConsulta,
			string idioma) =>
			ProyeccionDeVistas.Abastecimientos(AbastecimientosPendientesEl(fechaDeConsulta),
				Frases.Frases.De(idioma));

		// ------------------------------------------------- IDiarioDeLaApp (Portador)

		string IDiarioDeLaApp.Aplicar(IMensajeDeEvento mensaje)
		{
			ArgumentNullException.ThrowIfNull(mensaje);

			if (mensaje is CompraRegistrada registrada) return AplicarCompraRegistrada(registrada);
			if (mensaje is PromocionPublicada promocion) return AplicarPromocionPublicada(promocion);

			throw new ArgumentException(
				$"Esta app no tiene comando para un mensaje de tipo '{mensaje.Tipo}'. La ruta debio devolver null "
				+ "para descartarlo.", nameof(mensaje));
		}
	}
}
