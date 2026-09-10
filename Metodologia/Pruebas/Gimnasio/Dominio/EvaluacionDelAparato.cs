using System;

namespace Gimnasio.Dominio
{
	// LA PALABRA QUE EL APARATO PONE AL LADO DE CADA VALOR: Increased, Insufficient, High.
	// Es SU lectura del numero -contra su rango- y aca llega como HECHO: la app la
	// registra, no la recalcula (juicio N7 del v3, firmado por Jorge el 2026-09-08; la
	// regla "la columna de contraste" de la guia; el laboratorio con sus H y L).
	//
	// CONJUNTO CERRADO DE LAS PALABRAS QUE EL PAPEL MOSTRO Y JORGE QUISO. El papel traia
	// una cuarta -"Didn't reach goals"- que no comparaba contra un rango sino contra una
	// meta fijada en el aparato; Jorge la saco (hueco 20): "solo vamos a tener la
	// medida". Las otras plausibles -"Normal", "Decreased", "Low"- no se vieron y no se
	// inventan: una palabra desconocida revienta en el resolutor y lo dice, igual que un
	// rubro desconocido en la municipalidad. Cuando aparezca en una pantalla, entra.
	internal sealed class EvaluacionDelAparato
	{
		private readonly string nombre;

		private EvaluacionDelAparato(string nombre) { this.nombre = nombre; }

		internal static EvaluacionDelAparato Increased() { return new EvaluacionDelAparato("increased"); }
		internal static EvaluacionDelAparato Insufficient() { return new EvaluacionDelAparato("insufficient"); }
		internal static EvaluacionDelAparato High() { return new EvaluacionDelAparato("high"); }

		// RESOLUTOR TOTAL (regla 11). Las palabras se dejan en el idioma del aparato, que
		// es el del papel: traducirlas seria inventar una equivalencia que nadie firmo.
		internal static EvaluacionDelAparato PorNombre(string nombre)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
			switch (nombre.Trim().ToLowerInvariant())
			{
				case "increased": return Increased();
				case "insufficient": return Insufficient();
				case "high": return High();
				default:
					throw new ArgumentOutOfRangeException(nameof(nombre), nombre,
						"Evaluacion del aparato desconocida. Las que entran son: increased, insufficient, high. "
						+ "Si el aparato dijo otra, hay que verla.");
			}
		}

		internal static bool EsUnNombreConocido(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return false;
			switch (nombre.Trim().ToLowerInvariant())
			{
				case "increased":
				case "insufficient":
				case "high": return true;
				default: return false;
			}
		}

		internal bool Es(EvaluacionDelAparato otra)
		{
			return otra != null && otra.nombre == nombre;
		}

		// La hoja para el actor (regla 13).
		internal string Etiqueta { get { return nombre; } }

		public override string ToString() { return nombre; }
	}
}
