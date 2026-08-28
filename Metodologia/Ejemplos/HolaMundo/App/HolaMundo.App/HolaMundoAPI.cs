using System;
using Choreography.Theater;
using Puppeteer;

namespace HolaMundo.App
{
	// LA SUPERFICIE INTENCIONAL. Un método por intención, con nombre de negocio.
	// Nadie de afuera escribe scripts: la consola, el browser y el asistente entran
	// todos por acá. Que la lógica sea UNA es lo que hace que la persona y la IA vean
	// lo mismo.
	public sealed class HolaMundoAPI
	{
		private readonly PerformanceV2 app;

		public HolaMundoAPI(PerformanceV2 app)
		{
			ArgumentNullException.ThrowIfNull(app);
			this.app = app;
		}

		// ------------------------------------------------------------------ comando

		// AGREGA UNA FACTURA. Devuelve "" si entró, o el motivo si se rechazó.
		//
		// ACÁ ESTÁ LA REGLA MÁS IMPORTANTE DEL SUBSTRATO, y conviene entenderla antes
		// de escribir tu propia app.
		//
		// El `Check` corre bajo read lock, ANTES del write lock. Si falla, **no se
		// escribe nada**. Si en cambio dejaras que el dominio tirara la excepción a
		// mitad del comando, la entrada YA se habría escrito — y desde ese momento cada
		// arranque la vuelve a replayar y vuelve a fallar. Un número repetido quedaría
		// envenenando el journal para siempre.
		//
		// Por eso es un Check y no un `if` adentro del cuerpo: un `if` también evitaría
		// el error, pero dejaría una entrada no-op en el journal por cada reintento.
		//
		// Ver la regla 1 de Metodologia/Guias/reglas-del-substrato.md
		public string AgregarFactura(string numero, DateTime fecha,
			string[] descripciones, int[] cantidades, decimal[] precios)
		{
			return app.Using(
				scriptForChk: @"
					Check(libro.PuedeAgregar(@numero)) Error 'esa factura ya estaba en el libro';
				",
				@"
					{
						factura = Factura(@numero, @fecha, @descripciones,
						                  @cantidades, @precios);
						libro.Agregar(factura);
					}
				")
			.WithParameters(p => {
				p["numero",        typeof(string)]    = numero;
				p["fecha",         typeof(DateTime)]  = fecha;
				p["descripciones", typeof(string[])]  = descripciones;
				p["cantidades",    typeof(int[])]     = cantidades;
				p["precios",       typeof(decimal[])] = precios;
			})
			.PerformCheckThenCommand();
		}

		// ----------------------------------------------------------------- consultas

		// VALOR TIPADO, no texto parseado.
		//
		// Fijate en la forma: se declara un parámetro de SALIDA con su tipo, el script
		// le asigna, y se lo lee tipado. No se imprime y se parsea. Eso es lo que le
		// permite a una UI hacer aritmética sin adivinar el formato, y a una tool del
		// asistente devolver un decimal de verdad y no una cadena.
		public decimal TotalDeFacturas()
		{
			using (RentedParameter alquilados = app.Actor.RentedParameters())
			{
				app.Actor.Using("@total = libro.Total;")
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
				app.Actor.Using("@cantidad = libro.Cantidad;")
				.WithParameters(alquilados, p => {
					p[Parameter.Out, "cantidad", typeof(int)] = default;
				})
				.PerformQuery();

				return alquilados["cantidad"].GetValue<int>();
			}
		}

		// LA LISTA, proyectada HOJA POR HOJA.
		//
		// No se puede hacer `print facturas.Lista`: el motor lo rechaza, porque una
		// colección de objetos de dominio es MATERIAL, no un valor — y decidir qué se
		// ve de ella es del actor, no del material. Ver la regla 13 del substrato.
		//
		// Y fijate en la sintaxis del print: cada valor lleva su ALIAS detrás —sin comillas—
		// y los pares se separan con COMA. Sin alias, el motor no sabe cómo nombrar el campo.
		public string ListaDeFacturas()
		{
			return app.Using(@"
				foreach (factura in libro.Facturas) {
					print factura.Numero numero,
						factura.Fecha fecha,
						factura.CantidadDeLineas lineas,
						factura.Total total;
				}
			")
			.PerformQuery();
		}
	}
}
