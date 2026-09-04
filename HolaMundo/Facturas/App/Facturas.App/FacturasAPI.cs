using System;
using System.Collections.Generic;
using Choreography.Theater;
using Facturas.App.Eventos;
using Facturas.App.Vistas;
using Puppeteer;
using Puppets.Portador;

namespace Facturas.App
{
	// LA SUPERFICIE INTENCIONAL de la app. Todo el mundo entra por aca: la UI, el
	// bucle de refresh y el servidor MCP. Nadie habla con el actor crudo.
	//
	// Es tambien el IDiarioDeLaApp que el Portador necesita: el bucle no sabe hacer
	// performs, sabe pedirle a esto que aplique un mensaje.
	public sealed class FacturasAPI : IDiarioDeLaApp
	{
		private readonly PerformanceV2 app;

		public FacturasAPI(PerformanceV2 app)
		{
			ArgumentNullException.ThrowIfNull(app);
			this.app = app;
		}

		// ---------------------------------------------------------------- comandos

		// Aplica UN evento como UN comando. Devuelve "" si entro, o el mensaje del
		// Check si se rechazo.
		//
		// El checkpoint (bitacora.Avanzar) va DENTRO del mismo comando que inserta la
		// factura: eso es lo que hace exacto el resume tras una caida. O entraron los
		// dos o no entro ninguno; no existe el estado "guarde la factura pero no
		// avance el checkpoint", que seria el que duplica al reintentar.
		//
		// El Check es la idempotencia. Un evento redelivered se rechaza bajo el read
		// lock y NO deja entrada en el journal - por eso es un Check y no un if en el
		// cuerpo, que si persistiria una entrada no-op por cada reintento.
		public string AplicarFacturaAgregada(FacturaAgregada evento)
		{
			ArgumentNullException.ThrowIfNull(evento);
			if (evento.Evento <= 0)
				throw new ArgumentOutOfRangeException(nameof(evento), evento.Evento, "El numero de evento debe ser positivo.");

			return app.Using(
				scriptForChk: @"
					Check(@evento > bitacora.UltimoEvento) Error 'evento ya procesado';
				",
				@"
					{
						factura = Factura(@numero, @fecha, @skus, @descripciones, @cantidades, @preciosUnitarios);
						facturas.Agregar(factura);
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
			})
			.PerformCheckThenCommand();
		}

		// ---------------------------------------------------------------- consultas

		// Valor TIPADO, no texto parseado: es lo que le permite a la tool MCP
		// devolver structuredContent con un decimal de verdad, y a la UI hacer
		// aritmetica sin adivinar el formato del print.
		public decimal TotalDeFacturas()
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@total = facturas.Total;")
				.WithParameters(alquilados, p => {
					p[Parameter.Out, "total", typeof(decimal)] = default;
				})
				.PerformQuery();

				return alquilados["total"].GetValue<decimal>();
			}
		}

		public int CantidadDeFacturas()
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@cantidad = facturas.Cantidad;")
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

		// Las dos vistas de la app: la lista y el detalle. Devuelven la emision del
		// print tal cual - con el formatter JSON eso YA es el JSON serializado, asi
		// que no hay DTO ni re-serializacion en el medio.
		//
		// La proyeccion se AUTORA aca, hoja por hoja. No se puede hacer
		// `print facturas.Lista` : el motor lo rechaza porque una coleccion de objetos
		// de dominio es material, no un valor, y decidir que se ve de ella es del
		// actor, no del material. Medido - el mensaje es explicito:
		//   "emits a value of type 'IReadOnlyList`1', which is material, not a value.
		//    The projection is the actor's to author, not the material's".
		// Que la vista sea explicita es lo correcto tambien de este lado: la app
		// decide que expone, no lo hereda de la forma del dominio.
		public string ListaDeFacturas() =>
			app.Using(@"
				foreach (factura in facturas.Lista) {
					print factura.Numero numero,
						factura.Fecha fecha,
						factura.CantidadDeLineas lineas,
						factura.Total total;
				}
			")
			.PerformQuery();

		// El indexador con @param es DSL valido y es la razon por la que Facturario
		// expone this[numero]. El bloque mantiene `factura` local: en una consulta una
		// asignacion de primer nivel seria escritura de global, prohibida en contexto
		// read-only.
		public string DetalleDeFactura(string numero)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(numero);

			return app.Using(@"
				{
					factura = facturas[@numero];
					print factura.Numero numero,
						factura.Fecha fecha,
						factura.Total total;
					foreach (item in factura.Items) {
						print item.Sku sku,
							item.Descripcion descripcion,
							item.Cantidad cantidad,
							item.CostoUnitario costoUnitario,
							item.Total totalDeLinea;
					}
				}
			")
			.WithParameters(p => {
				p["numero", typeof(string)] = numero;
			})
			.PerformQuery();
		}

		// Las mismas dos vistas, ya tipadas. Es lo que consumen la UI y las tools MCP;
		// las variantes que devuelven el string crudo quedan para diagnostico.
		public IReadOnlyList<FacturaEnLista> Facturas() =>
			ProyeccionDeVistas.Lista(ListaDeFacturas());

		public FacturaDetallada Detalle(string numero) =>
			ProyeccionDeVistas.Detalle(DetalleDeFactura(numero));

		// ------------------------------------------------- IDiarioDeLaApp (Portador)

		string IDiarioDeLaApp.Aplicar(IMensajeDeEvento mensaje)
		{
			ArgumentNullException.ThrowIfNull(mensaje);

			if (mensaje is FacturaAgregada agregada) return AplicarFacturaAgregada(agregada);

			throw new ArgumentException(
				$"APP1 no tiene comando para un mensaje de tipo '{mensaje.Tipo}'. La ruta debio devolver null para descartarlo.",
				nameof(mensaje));
		}
	}
}
