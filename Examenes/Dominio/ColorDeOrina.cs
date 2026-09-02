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
	internal sealed class ColorDeOrina : MagnitudNominal
	{
		internal static readonly ColorDeOrina Transparente = new ColorDeOrina("transparente", "transparente", true);
		internal static readonly ColorDeOrina AmarilloPalido = new ColorDeOrina("amarillo palido", "amarillo pálido", true);
		internal static readonly ColorDeOrina Ambar = new ColorDeOrina("ambar", "ámbar", true);
		internal static readonly ColorDeOrina Oscuro = new ColorDeOrina("oscuro", "oscuro", false);
		internal static readonly ColorDeOrina Rojizo = new ColorDeOrina("rojizo", "rojizo", false);

		internal override string Nombre { get; }

		// El nombre CON ACENTO, que es el que ve la persona. El canonico -sin acento- es
		// la llave con la que llega del laboratorio y con la que se compara.
		internal string ParaMostrar { get; private set; }

		internal override string Expresado => ParaMostrar;

		// El color es lo que un examen de orina informa, y algunos colores son un
		// hallazgo. Que el propio color sepa si es esperable evita un switch afuera.
		internal override bool EsEsperable { get; }

		// Un color inesperado se mira, no se corre: el color solo no distingue una causa
		// banal de una que no lo es. Que lo conteste el color y no el examen es lo que
		// permite que su lectura viaje sola, sin la de los binarios que van al lado.
		//
		// ⚠️ Orientativo, igual que todos los umbrales de este dominio.
		internal override Severidad SeveridadDelHallazgo =>
			EsEsperable ? Severidad.Informativo : Severidad.Vigilar;

		internal override Accion AccionDelHallazgo =>
			EsEsperable ? null : Accion.ConversarloConTuMedico(Plazo.EnLaProximaCita);

		internal override string ParaSonia => EsEsperable
			? $"el color de la orina salió {ParaMostrar}, que es de lo esperable"
			: $"el color de la orina salió {ParaMostrar}";

		private ColorDeOrina(string nombre, string paraMostrar, bool esEsperable)
		{
			Nombre = nombre;
			ParaMostrar = paraMostrar;
			EsEsperable = esEsperable;
		}

		// La frontera: el color llega como texto en el evento y se resuelve una vez.
		internal static ColorDeOrina PorNombre(string nombre)
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
