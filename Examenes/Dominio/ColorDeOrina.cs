using System;

namespace Examenes.Dominio
{
	// El color de una muestra de orina. Conjunto cerrado: son los colores que el
	// laboratorio reporta, no un texto libre.
	//
	// Existe porque el color es una caracteristica de la ORINA. Un examen de sangre no
	// tiene color, y por eso esto no vive en Examen sino en ExamenDeOrina: es el caso
	// que obliga a que la herencia sea real y no una jerarquia decorativa.
	// Es la FORMA 6 de la taxonomia de Magnitud: nominal. No se ordena y no tiene serie,
	// y eso no es una carencia sino lo que un color ES.
	public sealed class ColorDeOrina : MagnitudNominal
	{
		public static readonly ColorDeOrina Transparente = new ColorDeOrina("transparente", true);
		public static readonly ColorDeOrina AmarilloPalido = new ColorDeOrina("amarillo palido", true);
		public static readonly ColorDeOrina Ambar = new ColorDeOrina("ambar", true);
		public static readonly ColorDeOrina Oscuro = new ColorDeOrina("oscuro", false);
		public static readonly ColorDeOrina Rojizo = new ColorDeOrina("rojizo", false);

		public override string Nombre { get; }

		public override string Expresado => Nombre;

		// El color es lo que un examen de orina informa, y algunos colores son un
		// hallazgo. Que el propio color sepa si es esperable evita un switch afuera.
		public bool EsEsperable { get; private set; }

		private ColorDeOrina(string nombre, bool esEsperable)
		{
			Nombre = nombre;
			EsEsperable = esEsperable;
		}

		// La frontera: el color llega como texto en el evento y se resuelve una vez.
		public static ColorDeOrina PorNombre(string nombre)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

			string normalizado = nombre.Trim().ToLowerInvariant();
			if (normalizado == Transparente.Nombre) return Transparente;
			if (normalizado == AmarilloPalido.Nombre) return AmarilloPalido;
			if (normalizado == Ambar.Nombre) return Ambar;
			if (normalizado == Oscuro.Nombre) return Oscuro;
			if (normalizado == Rojizo.Nombre) return Rojizo;

			throw new ArgumentException(
				$"'{nombre}' no es un color de orina que el laboratorio reporte.", nameof(nombre));
		}
	}
}
