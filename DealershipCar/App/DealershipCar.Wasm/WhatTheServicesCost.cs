using System;
using System.Collections.Generic;
using System.Linq;
using DealershipCar.App;

namespace DealershipCar.Wasm
{
	/// <summary>
	/// Lo que el fabricante publico que cuestan los servicios de este modelo, enumerado entero y por
	/// la unica puerta. Va aparte de la ficha, que no toma ningun instante: la vigencia de un precio
	/// se juzga contra un momento, y sellarle un reloj a la ficha entera la marcaria con una lectura
	/// que el resto de ella no usa.
	/// </summary>
	public sealed class WhatTheServicesCost
	{
		/// <summary>
		/// Si alguna revision sellada publico algun precio. Es lo que separa un registro al que nunca
		/// le dijeron ningun precio de uno en que la revision que rige no publica ninguno.
		/// </summary>
		public bool AnyPriceOnRecord { get; init; }

		/// <summary>Cuantos precios publicados hay, contados por el dominio.</summary>
		public int PriceCountOnRecord { get; init; }

		/// <summary>
		/// Los precios publicados como filas, de la revision mas nueva a la mas vieja. Ninguna fila
		/// pide un hito a nadie: los hitos son del fabricante y esta respuesta los enumera.
		/// </summary>
		public IReadOnlyList<APublishedPriceOnTheSheet> Rows { get; init; } =
			new List<APublishedPriceOnTheSheet>();

		/// <summary>
		/// Lee las tres respuestas por la puerta. DOS de las tres no toman instante y la de las filas
		/// si, y esa asimetria es la afirmacion: sellada es un ESTADO y no un instante, y el instante
		/// mueve la FRASE de cada fila y nunca el juego de filas.
		/// </summary>
		public static WhatTheServicesCost ReadThrough(DealershipCarAPI api, DateTime asOf)
		{
			ArgumentNullException.ThrowIfNull(api);

			PublishedPriceRecord record = api.PublishedServicePricesAsOf(asOf);

			var rows = new List<APublishedPriceOnTheSheet>();

			foreach (int row in Enumerable.Range(0, record.Operation.Length))
				rows.Add(new APublishedPriceOnTheSheet
				{
					Operation = record.Operation[row],
					Milestone = record.Milestone[row],
					Says = record.Says[row],
					Revision = record.Revision[row],
				});

			return new WhatTheServicesCost
			{
				AnyPriceOnRecord = api.HasAnyPublishedServicePrice(),
				PriceCountOnRecord = api.PublishedServicePriceCount(),
				Rows = rows,
			};
		}
	}

	/// <summary>
	/// UN precio publicado: de que operacion, a que hito, la frase con que el dominio lo dice al
	/// instante preguntado, y de que revision salio. Un precio sin su procedencia no se puede
	/// distinguir de uno viejo.
	/// </summary>
	public sealed class APublishedPriceOnTheSheet
	{
		/// <summary>
		/// La operacion, por el nombre del miembro del canon, que es la mitad direccionable: con ella
		/// se puede volver a preguntar por esa operacion.
		/// </summary>
		public string Operation { get; init; } = "";

		/// <summary>
		/// El hito DICHO con su unidad, como lo lee una persona, y deliberadamente no en una forma
		/// direccionable: devolverlo como un numero mas un codigo de unidad es el mapeo al reves.
		/// </summary>
		public string Milestone { get; init; } = "";

		/// <summary>
		/// El precio en la frase que compuso el dominio: el monto, su moneda, su vigencia, si esa
		/// vigencia rige al instante preguntado, y la revision.
		/// </summary>
		public string Says { get; init; } = "";

		/// <summary>La revision de la que salio, que es la que rige su clave.</summary>
		public string Revision { get; init; } = "";
	}
}
