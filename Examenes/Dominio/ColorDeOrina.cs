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
		public static readonly ColorDeOrina Transparente = new ColorDeOrina("transparente", "transparente", true);
		public static readonly ColorDeOrina AmarilloPalido = new ColorDeOrina("amarillo palido", "amarillo pálido", true);
		public static readonly ColorDeOrina Ambar = new ColorDeOrina("ambar", "ámbar", true);
		public static readonly ColorDeOrina Oscuro = new ColorDeOrina("oscuro", "oscuro", false);
		public static readonly ColorDeOrina Rojizo = new ColorDeOrina("rojizo", "rojizo", false);

		public override string Nombre { get; }

		// El nombre CON ACENTO, que es el que ve la persona. El canonico -sin acento- es
		// la llave con la que llega del laboratorio y con la que se compara.
		public string ParaMostrar { get; private set; }

		public override string Expresado => ParaMostrar;

		// El color es lo que un examen de orina informa, y algunos colores son un
		// hallazgo. Que el propio color sepa si es esperable evita un switch afuera.
		public bool EsEsperable { get; private set; }

		private ColorDeOrina(string nombre, string paraMostrar, bool esEsperable)
		{
			Nombre = nombre;
			ParaMostrar = paraMostrar;
			EsEsperable = esEsperable;
		}

		// La frontera: el color llega como texto en el evento y se resuelve una vez.
		public static ColorDeOrina PorNombre(string nombre)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

			// Tolerante a acentos y mayusculas, igual que Analito: el laboratorio escribe
			// "MARRON", "Marrón" o "marron" y las tres son el mismo color.
			string normalizado = NombreNormalizado.De(nombre);
			if (NombreNormalizado.EsElMismo(normalizado, Transparente.Nombre)) return Transparente;
			if (NombreNormalizado.EsElMismo(normalizado, AmarilloPalido.Nombre)) return AmarilloPalido;
			if (NombreNormalizado.EsElMismo(normalizado, Ambar.Nombre)) return Ambar;
			if (NombreNormalizado.EsElMismo(normalizado, Oscuro.Nombre)) return Oscuro;
			if (NombreNormalizado.EsElMismo(normalizado, Rojizo.Nombre)) return Rojizo;

			throw new ArgumentException(
				$"'{nombre}' no es un color de orina que el laboratorio reporte.", nameof(nombre));
		}
	}
}
