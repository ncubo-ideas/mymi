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
	public sealed class ColorDeHeces : MagnitudNominal
	{
		public static readonly ColorDeHeces Chocolate = new ColorDeHeces("chocolate", true);
		public static readonly ColorDeHeces Marron = new ColorDeHeces("marron", true);
		public static readonly ColorDeHeces Verdoso = new ColorDeHeces("verdoso", true);

		// Los dos que no son esperables, y lo son por razones distintas que el dominio no
		// necesita explicar para saber que hay que mirarlos.
		public static readonly ColorDeHeces Negro = new ColorDeHeces("negro", false);
		public static readonly ColorDeHeces Palido = new ColorDeHeces("palido", false);

		public override string Nombre { get; }

		public override string Expresado => Nombre;

		public bool EsEsperable { get; private set; }

		private ColorDeHeces(string nombre, bool esEsperable)
		{
			Nombre = nombre;
			EsEsperable = esEsperable;
		}

		public static ColorDeHeces PorNombre(string nombre)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

			string normalizado = nombre.Trim().ToLowerInvariant();
			if (normalizado == Chocolate.Nombre) return Chocolate;
			if (normalizado == Marron.Nombre) return Marron;
			if (normalizado == Verdoso.Nombre) return Verdoso;
			if (normalizado == Negro.Nombre) return Negro;
			if (normalizado == Palido.Nombre) return Palido;

			throw new ArgumentException(
				$"'{nombre}' no es un color de heces que el laboratorio reporte.", nameof(nombre));
		}
	}
}
