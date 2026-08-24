using System;

namespace Examenes.Dominio
{
	/// <summary>
	/// Nitritos en orina: la prueba que sugiere infeccion bacteriana. Forma 5 — binaria.
	/// </summary>
	///
	/// Es el segundo hallazgo del urianalisis, al lado del color, y esta puesto justamente
	/// ahi: UN examen que informa DOS formas de magnitud distintas —una nominal y una
	/// binaria— es la demostracion mas corta de que "el resultado de un examen" no es un
	/// numero con una etiqueta.
	///
	/// Conjunto CERRADO de tres inhabitantes, y el tercero es el que justifica la clase:
	///
	///   Positivo     hay nitritos. En ESTE analito, positivo es un hallazgo.
	///   Negativo     no hay. Es lo normal.
	///   NoRealizado  el laboratorio no corrio la prueba. NO es lo mismo que negativo.
	///
	/// Con un <c>bool</c> el tercer estado cae en <c>null</c> y ahi se perdio el
	/// significado: "no sabemos" quedaria indistinguible de "no hay", que es exactamente el
	/// error que hace que una app le diga a alguien que esta bien cuando nadie lo midio.
	///
	/// Se comparan por referencia: hay una sola instancia de cada uno y no hay Equals.
	public sealed class Nitritos : MagnitudBinaria
	{
		public static readonly Nitritos Positivo = new Nitritos("positivo", realizada: true, positivo: true);
		public static readonly Nitritos Negativo = new Nitritos("negativo", realizada: true, positivo: false);
		public static readonly Nitritos NoRealizado = new Nitritos("no realizado", realizada: false, positivo: false);

		/// <summary>El vocabulario de ESTE analito. Los nitritos son positivo/negativo — una serologia seria reactivo/no reactivo.</summary>
		public string Etiqueta { get; }

		private readonly bool realizada;
		private readonly bool positivo;

		private Nitritos(string etiqueta, bool realizada, bool positivo)
		{
			Etiqueta = etiqueta;
			this.realizada = realizada;
			this.positivo = positivo;
		}

		public override string Expresado => Etiqueta;

		public override bool FueRealizada => realizada;

		public override bool EsPositivo
		{
			get
			{
				// Preguntar el resultado de una prueba que no se corrio es un defecto de
				// quien pregunta, no un "no". Devolver false aca haria que "no se midio"
				// y "salio negativo" fueran la misma respuesta, que es precisamente lo que
				// esta clase existe para impedir.
				if (!realizada)
					throw new InvalidOperationException(
						"Los nitritos no se midieron en este examen: preguntar por el resultado "
						+ "exige consultar antes FueRealizada.");

				return positivo;
			}
		}

		/// <summary>
		/// En nitritos, positivo ES anormal. Que lo conteste el analito y no el llamador es
		/// lo que permite que otro binario —un factor Rh, donde positivo es lo comun— viva
		/// en la misma jerarquia sin un if afuera.
		/// </summary>
		public override bool EsHallazgo => realizada && positivo;

		/// <summary>
		/// La frontera. La tira llega como texto en el evento del proveedor y se resuelve UNA
		/// vez, aca. Un evento sin el campo significa que la prueba no se corrio, que es un
		/// estado legitimo; un texto que no es ninguno de los tres es un evento mal formado y
		/// revienta, no se inventa un cuarto inhabitante.
		/// </summary>
		public static Nitritos PorNombre(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return NoRealizado;

			string normalizado = nombre.Trim().ToLowerInvariant();
			if (normalizado == Positivo.Etiqueta) return Positivo;
			if (normalizado == Negativo.Etiqueta) return Negativo;
			if (normalizado == NoRealizado.Etiqueta) return NoRealizado;

			throw new ArgumentException(
				$"'{nombre}' no es un resultado de nitritos. Los validos son: "
				+ $"{Positivo.Etiqueta}, {Negativo.Etiqueta}, {NoRealizado.Etiqueta}.", nameof(nombre));
		}
	}
}
