using System;

namespace Examenes.Dominio
{
	/// <summary>
	/// Sangre oculta en orina (hematuria). Forma 5 — binaria.
	/// </summary>
	///
	/// ES LA MITAD DEL EJEMPLO CON EL QUE ALVARO EXPLICO EL PEDIDO, y su par vive en
	/// <see cref="SangreEnHeces"/>. Las dos clases son deliberadamente casi identicas en
	/// forma y opuestas en conducta, porque ESE es el punto:
	///
	///   "Sangre en las heces ⟹ quizas estrenimiento. Sangre en la orina ⟹ quizas CORRA
	///    donde el medico, rapido. No es asunto de pantalla, es que el dominio deberia
	///    tener mas funciones o metodos."
	///
	/// Los dos son un binario positivo. Los dos son "fuera de lo normal". Un modelo
	/// generico —una tabla con una columna `anormal`— les pone la misma etiqueta y la misma
	/// frase. Y la accion que exigen no se parece en nada.
	///
	/// Si estas dos clases devolvieran la misma severidad, el modelo colapso a la misma
	/// tabla y no hacia falta el dominio. Hay una comprobacion que lo verifica.
	///
	/// Vive en el urianalisis, al lado del color y de los nitritos: TRES formas de magnitud
	/// en un solo examen, y ahora dos binarios distintos que no se leen igual.
	public sealed class SangreEnOrina : MagnitudBinaria
	{
		public static readonly SangreEnOrina Positivo =
			new SangreEnOrina("positivo", realizada: true, positivo: true);
		public static readonly SangreEnOrina Negativo =
			new SangreEnOrina("negativo", realizada: true, positivo: false);
		public static readonly SangreEnOrina NoRealizado =
			new SangreEnOrina("no realizado", realizada: false, positivo: false);

		/// <summary>El vocabulario de ESTE analito. La tira reporta positivo/negativo.</summary>
		public string Etiqueta { get; }

		private readonly bool realizada;
		private readonly bool positivo;

		private SangreEnOrina(string etiqueta, bool realizada, bool positivo)
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
				if (!realizada)
					throw new InvalidOperationException(
						"La sangre en orina no se midio en este examen: preguntar por el "
						+ "resultado exige consultar antes FueRealizada.");

				return positivo;
			}
		}

		public override bool EsHallazgo => realizada && positivo;

		/// <summary>
		/// NO ESPERA A LA PROXIMA CITA. Sangre en la orina puede venir de causas banales,
		/// pero tambien de cosas que se tratan mejor temprano, y el examen solo no
		/// distingue cual. Ante esa incertidumbre lo que corresponde es que la vea alguien
		/// pronto.
		///
		/// Es la severidad mas alta que produce cualquier analito del demo, y esta puesta a
		/// proposito en el hallazgo mas parecido en FORMA al mas leve de todos.
		///
		/// ⚠️ El criterio clinico concreto refleja el ejemplo del feedback y sirve para
		/// modelar la estructura. Antes de mostrarse fuera del equipo conviene que alguien
		/// con criterio medico revise el umbral.
		/// </summary>
		public override Severidad SeveridadDelHallazgo =>
			EsHallazgo ? Severidad.AtencionInmediata : Severidad.Informativo;

		public override string ParaSonia =>
			!realizada ? "el laboratorio no corrió esta prueba"
			: positivo ? "apareció sangre en la orina, y eso conviene que lo revise un médico pronto"
			: "no apareció sangre en la orina, que es lo esperable";

		/// <summary>
		/// La frontera. Un evento sin el campo significa que la prueba no se corrio —un
		/// estado legitimo—; un texto que no es ninguno de los tres es un evento mal formado
		/// y revienta, no se inventa un cuarto inhabitante.
		/// </summary>
		public static SangreEnOrina PorNombre(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return NoRealizado;

			string normalizado = nombre.Trim().ToLowerInvariant();
			if (normalizado == Positivo.Etiqueta) return Positivo;
			if (normalizado == Negativo.Etiqueta) return Negativo;
			if (normalizado == NoRealizado.Etiqueta) return NoRealizado;

			throw new ArgumentException(
				$"'{nombre}' no es un resultado de sangre en orina. Los validos son: "
				+ $"{Positivo.Etiqueta}, {Negativo.Etiqueta}, {NoRealizado.Etiqueta}.", nameof(nombre));
		}
	}
}
