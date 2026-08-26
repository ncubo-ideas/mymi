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

		// La clave de ESTA instalacion ante el proveedor. Se genera en C# y entra como
		// parametro, no dentro del DSL: asi el valor queda journalizado y el replay lo
		// reusa. Un Guid.NewGuid() en codigo de dominio daria una clave distinta en cada
		// replay y la app perderia su identidad ante el proveedor.
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

			return app.Using(
				scriptForChk: @"
					Check(@evento > bitacora.UltimoEvento) Error 'evento ya procesado';
				",
				@"
					{
						compra = Compra(@numero, @fecha, @skus, @descripciones, @cantidades, @preciosUnitarios, @categorias, @mesesDeGarantia);
						compras.Agregar(compra);
						bitacora.Avanzar(@evento);
					}
				")
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

		// La foto de un mes, sumada por perfil. Los HECHOS con los que la superficie o
		// la IA arman la lectura; el dominio no devuelve prosa.
		public string CanastaDelMes(int anio, int mes) =>
			app.Using(@"
				{
					canasta = compras.CanastaDe(@anio, @mes);
					print canasta.Anio anio,
						canasta.Mes mes,
						canasta.CantidadDeCompras cantidadDeCompras,
						canasta.GastoRecomendado gastoRecomendado,
						canasta.GastoNeutro gastoNeutro,
						canasta.GastoOcasional gastoOcasional,
						canasta.GastoNoComestible gastoNoComestible,
						canasta.GastoComestible gastoComestible,
						canasta.GastoTotal gastoTotal,
						canasta.ProporcionOcasional proporcionOcasional,
						canasta.HayComestible hayComestible,
						canasta.HayCompras hayCompras;
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
		public string ComparacionDeCanastas(int anioAntes, int mesAntes, int anioDespues, int mesDespues) =>
			app.Using(@"
				{
					comparacion = compras.CompararCanastas(@anioAntes, @mesAntes, @anioDespues, @mesDespues);
					print comparacion.HayComparacion hayComparacion,
						comparacion.ProporcionOcasionalAntes proporcionAntes,
						comparacion.ProporcionOcasionalDespues proporcionDespues,
						comparacion.CambioEnProporcion cambio,
						comparacion.CrecioLoOcasional crecioLoOcasional,
						comparacion.Antes.Anio anioAntes,
						comparacion.Antes.Mes mesAntes,
						comparacion.Antes.GastoOcasional gastoOcasionalAntes,
						comparacion.Antes.GastoComestible gastoComestibleAntes,
						comparacion.Despues.Anio anioDespues,
						comparacion.Despues.Mes mesDespues,
						comparacion.Despues.GastoOcasional gastoOcasionalDespues,
						comparacion.Despues.GastoComestible gastoComestibleDespues;
				}
			")
			.WithParameters(p => {
				p["anioAntes",   typeof(int)] = anioAntes;
				p["mesAntes",    typeof(int)] = mesAntes;
				p["anioDespues", typeof(int)] = anioDespues;
				p["mesDespues",  typeof(int)] = mesDespues;
			})
			.PerformQuery();

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
						registro.Vigente vigente;
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
							registro.VenceEl venceEl;
					}
				}
			")
			.WithParameters(p => {
				p["fecha", typeof(DateTime)] = fechaDeConsulta;
			})
			.PerformQuery();

		// Las mismas vistas, ya tipadas. Es lo que consumen la UI y las tools MCP; las
		// variantes que devuelven el string crudo quedan para diagnostico.
		public IReadOnlyList<CompraEnLista> Compras() =>
			ProyeccionDeVistas.Lista(ListaDeCompras());

		public CompraDetallada Detalle(string numero) =>
			ProyeccionDeVistas.Detalle(DetalleDeCompra(numero));

		public CanastaDelMesVista Canasta(int anio, int mes) =>
			ProyeccionDeVistas.Canasta(CanastaDelMes(anio, mes));

		public CanastasComparadas Comparacion(int anioAntes, int mesAntes, int anioDespues, int mesDespues) =>
			ProyeccionDeVistas.Comparacion(ComparacionDeCanastas(anioAntes, mesAntes, anioDespues, mesDespues));

		public GarantiaDeProducto Garantia(string texto, DateTime fechaDeConsulta) =>
			ProyeccionDeVistas.Garantia(GarantiaDe(texto, fechaDeConsulta));

		public IReadOnlyList<GarantiaDeProducto> GarantiasVigentes(DateTime fechaDeConsulta) =>
			ProyeccionDeVistas.GarantiasVigentes(GarantiasVigentesEl(fechaDeConsulta));

		// ------------------------------------------------- IDiarioDeLaApp (Portador)

		string IDiarioDeLaApp.Aplicar(IMensajeDeEvento mensaje)
		{
			ArgumentNullException.ThrowIfNull(mensaje);

			if (mensaje is CompraRegistrada registrada) return AplicarCompraRegistrada(registrada);

			throw new ArgumentException(
				$"Esta app no tiene comando para un mensaje de tipo '{mensaje.Tipo}'. La ruta debio devolver null "
				+ "para descartarlo.", nameof(mensaje));
		}
	}
}
