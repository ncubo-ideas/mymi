using System;

namespace Examenes.Dominio
{
	// GLUCOSA EN LA ORINA. El rinon no la deja pasar hasta que la de la sangre pasa cierto
	// umbral, asi que una glucosuria positiva es casi siempre una glucemia que estuvo alta.
	//
	// POR ESO SU SEVERIDAD NO ESCALA CON LA SERIE, al contrario de la proteina y las cetonas:
	// una vez ya amerita mostrarlo, porque una sola vez ya significa lo que significa. Que
	// tres binarios de la MISMA tira tengan tres escaleras distintas es el argumento de
	// Alvaro puesto donde se puede comprobar.
	//
	// SU PAR CONTINUO ESTA EN EL EXPEDIENTE —Analito.GlucosaEnAyunas— y son dos analitos y no
	// uno: se miden en muestras distintas, con formas distintas, y una tira no informa
	// mg/dL. Que compartan el sinonimo "glucosa" seria hacerlos el mismo, asi que ese nombre
	// resuelve SOLO al de sangre; este responde a "glucosuria".
	internal sealed class GlucosaEnOrina : MagnitudBinaria
	{
		internal static readonly GlucosaEnOrina Positivo =
			new GlucosaEnOrina("positivo", realizada: true, positivo: true);

		internal static readonly GlucosaEnOrina Negativo =
			new GlucosaEnOrina("negativo", realizada: true, positivo: false);

		internal static readonly GlucosaEnOrina NoRealizado =
			new GlucosaEnOrina("no realizado", realizada: false, positivo: false);

		internal string Etiqueta { get; }

		private readonly bool realizada;
		private readonly bool positivo;

		private GlucosaEnOrina(string etiqueta, bool realizada, bool positivo)
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
						"La glucosa en orina no se midio en este examen: preguntar por el resultado exige "
						+ "consultar antes FueRealizada.");

				return positivo;
			}
		}

		internal override bool EsHallazgo => realizada && positivo;

		// ⚠️ Orientativo.
		internal override Severidad SeveridadDelHallazgo =>
			EsHallazgo ? Severidad.ConsultePronto : Severidad.Informativo;

		internal override Accion AccionDelHallazgo =>
			EsHallazgo ? Accion.MostrarleEstoATuMedico(Plazo.EstaSemana) : null;

		internal override string ParaSonia => !realizada
			? "el laboratorio no corrió esta prueba"
			: positivo
				? "salió glucosa en la orina"
				: "no salió glucosa en la orina";

		// La frontera: el resultado llega como texto en el evento y se resuelve una vez.
		// VACIO ES "NO REALIZADO" y no un negativo: la tira reactiva no siempre corre todas
		// sus casillas, y un examen viejo no midio ninguna.
		internal static GlucosaEnOrina PorNombre(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return NoRealizado;

			string normalizado = nombre.Trim().ToLowerInvariant();
			if (normalizado == Positivo.Etiqueta) return Positivo;
			if (normalizado == Negativo.Etiqueta) return Negativo;
			if (normalizado == NoRealizado.Etiqueta) return NoRealizado;

			throw new ArgumentException(
				$"'{nombre}' no es un resultado de glucosa en orina. Los validos son: "
				+ $"{Positivo.Etiqueta}, {Negativo.Etiqueta}, {NoRealizado.Etiqueta}.",
				nameof(nombre));
		}

		internal override SerieBinaria NuevaSerie() => new SerieDeGlucosaEnOrina();
	}
}
