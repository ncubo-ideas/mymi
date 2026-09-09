using System;

namespace Examenes.Dominio
{
	// PROTEINA EN LA ORINA, la casilla de la tira reactiva con la que se mira el rinon sin
	// sacar sangre.
	//
	// LO SUYO ES QUE UNA VEZ NO ES NADA Y DOS SI. Una proteinuria aislada aparece con
	// fiebre, con ejercicio o de pie mucho tiempo, y se va sola; la que se repite es la que
	// significa algo. Es el argumento de SerieBinaria en su forma mas limpia: la MAGNITUD
	// pide repetir el examen y la SERIE, cuando ya se repitio, pide otra cosa.
	//
	// SE UNE CON "ALBUMINA" por la tabla de sinonimos del analito: los reportes viejos
	// informan albumina en la tira y los nuevos proteina, y son la misma casilla.
	internal sealed class ProteinaEnOrina : MagnitudBinaria
	{
		internal static readonly ProteinaEnOrina Positivo =
			new ProteinaEnOrina("positivo", realizada: true, positivo: true);

		internal static readonly ProteinaEnOrina Negativo =
			new ProteinaEnOrina("negativo", realizada: true, positivo: false);

		internal static readonly ProteinaEnOrina NoRealizado =
			new ProteinaEnOrina("no realizado", realizada: false, positivo: false);

		internal string Etiqueta { get; }

		private readonly bool realizada;
		private readonly bool positivo;

		private ProteinaEnOrina(string etiqueta, bool realizada, bool positivo)
		{
			Etiqueta = etiqueta;
			this.realizada = realizada;
			this.positivo = positivo;
		}

		internal override string Expresado => Etiqueta;

		internal override bool FueRealizada => realizada;

		// Preguntar el resultado de lo que no se corrio REVIENTA, y no devuelve false. Un
		// negativo es una afirmacion del laboratorio; la ausencia de la prueba no lo es, y
		// confundirlos es como una app termina diciendo que alguien esta bien cuando nadie lo
		// midio. Ver FueRealizada.
		internal override bool EsPositivo
		{
			get
			{
				if (!realizada)
					throw new InvalidOperationException(
						"La proteina en orina no se midio en este examen: preguntar por el resultado exige "
						+ "consultar antes FueRealizada.");

				return positivo;
			}
		}

		internal override bool EsHallazgo => realizada && positivo;

		// ⚠️ Orientativo.
		internal override Severidad SeveridadDelHallazgo =>
			EsHallazgo ? Severidad.Vigilar : Severidad.Informativo;

		internal override Accion AccionDelHallazgo =>
			EsHallazgo ? Accion.RepetirElExamen(Plazo.EsteMes) : null;

		internal override string ParaSonia => !realizada
			? "el laboratorio no corrió esta prueba"
			: positivo
				? "salió proteína en la orina"
				: "no salió proteína en la orina";

		// La frontera: el resultado llega como texto en el evento y se resuelve una vez.
		// VACIO ES "NO REALIZADO" y no un negativo: la tira reactiva no siempre corre todas
		// sus casillas, y un examen viejo no midio ninguna.
		internal static ProteinaEnOrina PorNombre(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return NoRealizado;

			string normalizado = nombre.Trim().ToLowerInvariant();
			if (normalizado == Positivo.Etiqueta) return Positivo;
			if (normalizado == Negativo.Etiqueta) return Negativo;
			if (normalizado == NoRealizado.Etiqueta) return NoRealizado;

			throw new ArgumentException(
				$"'{nombre}' no es un resultado de proteina en orina. Los validos son: "
				+ $"{Positivo.Etiqueta}, {Negativo.Etiqueta}, {NoRealizado.Etiqueta}.",
				nameof(nombre));
		}

		internal override SerieBinaria NuevaSerie() => new SerieDeProteinaEnOrina();
	}
}
