using System;

namespace Examenes.Dominio
{
	// CETONAS EN LA ORINA. Es el caso literal del §2 del doc de hallazgos: el reporte de 2022
	// dice ACETONA y el de 2026 dice CETONAS, y son el MISMO analito con dos nombres. Con la
	// tabla de sinonimos las dos hojas caen en una serie de dos puntos, y esa es la primera
	// vez que dos laboratorios distintos se unen con datos de la demo.
	//
	// LO SUYO: UNAS CETONAS SUELTAS NO SON UN HALLAZGO CLINICO. Aparecen con un ayuno largo
	// o una dieta baja en carbohidratos, que es informacion de la persona y no del
	// laboratorio. Las que se repiten son otra cosa, y esa diferencia solo la ve la serie.
	internal sealed class Cetonas : MagnitudBinaria
	{
		internal static readonly Cetonas Positivo =
			new Cetonas("positivo", realizada: true, positivo: true);

		internal static readonly Cetonas Negativo =
			new Cetonas("negativo", realizada: true, positivo: false);

		internal static readonly Cetonas NoRealizado =
			new Cetonas("no realizado", realizada: false, positivo: false);

		internal string Etiqueta { get; }

		private readonly bool realizada;
		private readonly bool positivo;

		private Cetonas(string etiqueta, bool realizada, bool positivo)
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
						"Las cetonas no se midio en este examen: preguntar por el resultado exige "
						+ "consultar antes FueRealizada.");

				return positivo;
			}
		}

		internal override bool EsHallazgo => realizada && positivo;

		// ⚠️ Orientativo.
		internal override Severidad SeveridadDelHallazgo =>
			EsHallazgo ? Severidad.Vigilar : Severidad.Informativo;

		internal override Accion AccionDelHallazgo =>
			EsHallazgo ? Accion.ConversarloConTuMedico(Plazo.EnLaProximaCita) : null;

		internal override string ParaSonia => !realizada
			? "el laboratorio no corrió esta prueba"
			: positivo
				? "salieron cetonas en la orina"
				: "no salieron cetonas en la orina";

		// La frontera: el resultado llega como texto en el evento y se resuelve una vez.
		// VACIO ES "NO REALIZADO" y no un negativo: la tira reactiva no siempre corre todas
		// sus casillas, y un examen viejo no midio ninguna.
		internal static Cetonas PorNombre(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return NoRealizado;

			string normalizado = nombre.Trim().ToLowerInvariant();
			if (normalizado == Positivo.Etiqueta) return Positivo;
			if (normalizado == Negativo.Etiqueta) return Negativo;
			if (normalizado == NoRealizado.Etiqueta) return NoRealizado;

			throw new ArgumentException(
				$"'{nombre}' no es un resultado de cetonas. Los validos son: "
				+ $"{Positivo.Etiqueta}, {Negativo.Etiqueta}, {NoRealizado.Etiqueta}.",
				nameof(nombre));
		}

		internal override SerieBinaria NuevaSerie() => new SerieDeCetonas();
	}
}
