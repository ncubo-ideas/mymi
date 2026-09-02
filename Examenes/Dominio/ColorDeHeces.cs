using System;

namespace Examenes.Dominio
{
	// El color de una muestra de heces. Conjunto cerrado, forma 6 — nominal.
	//
	// LOS INHABITANTES SALEN DE REPORTES REALES: un examen de heces general informa
	// "Color: CHOCOLATE" y "Aspecto: PASTOSA" como dos campos de texto, y chocolate es lo
	// normal. Que el conjunto no sea el mismo que el de la orina no es un detalle: son dos
	// vocabularios distintos porque son dos muestras distintas, y por eso son dos clases.
	//
	// Un solo tipo "Color" compartido entre orina y heces habria obligado a que un color
	// esperable en una fuera un hallazgo en la otra segun quien pregunte, que es la clase
	// de porosidad que este dominio evita.
	internal sealed class ColorDeHeces : MagnitudNominal
	{
		internal static readonly ColorDeHeces Chocolate = new ColorDeHeces("chocolate", "chocolate", true);
		internal static readonly ColorDeHeces Marron = new ColorDeHeces("marron", "marrón", true);
		internal static readonly ColorDeHeces Verdoso = new ColorDeHeces("verdoso", "verdoso", true);

		// Los dos que no son esperables, y lo son por razones distintas que el dominio no
		// necesita explicar para saber que hay que mirarlos.
		internal static readonly ColorDeHeces Negro = new ColorDeHeces("negro", "negro", false);
		internal static readonly ColorDeHeces Palido = new ColorDeHeces("palido", "pálido", false);

		internal override string Nombre { get; }

		// El nombre CON ACENTO, que es el que ve la persona. El canonico -sin acento- es
		// la llave con la que llega del laboratorio y con la que se compara.
		internal string ParaMostrar { get; private set; }

		internal override string Expresado => ParaMostrar;

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
			? $"el color de las heces salió {ParaMostrar}, que es de lo esperable"
			: $"el color de las heces salió {ParaMostrar}";

		private ColorDeHeces(string nombre, string paraMostrar, bool esEsperable)
		{
			Nombre = nombre;
			ParaMostrar = paraMostrar;
			EsEsperable = esEsperable;
		}

		internal static ColorDeHeces PorNombre(string nombre)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

			// Tolerante a acentos y mayusculas, igual que Analito: el laboratorio escribe
			// "MARRON", "Marrón" o "marron" y las tres son el mismo color.
			string normalizado = NombreNormalizado.De(nombre);
			if (NombreNormalizado.EsElMismo(normalizado, Chocolate.Nombre)) return Chocolate;
			if (NombreNormalizado.EsElMismo(normalizado, Marron.Nombre)) return Marron;
			if (NombreNormalizado.EsElMismo(normalizado, Verdoso.Nombre)) return Verdoso;
			if (NombreNormalizado.EsElMismo(normalizado, Negro.Nombre)) return Negro;
			if (NombreNormalizado.EsElMismo(normalizado, Palido.Nombre)) return Palido;

			throw new ArgumentException(
				$"'{nombre}' no es un color de heces que el laboratorio reporte.", nameof(nombre));
		}
	}
}
