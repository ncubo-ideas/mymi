using System;

namespace Gimnasio.Dominio
{
	// SI UN HALLAZGO MERECE AVISO, Y CON QUE SIGNO. Es el juicio 6.
	//
	// Lo que lo hace distinto de una severidad comun: EXISTE LA URGENCIA POSITIVA. El
	// Preguntas.md lo dice con su ejemplo -"un cambio de 2 cm en cintura podria tomarse
	// como una urgencia positiva; un aumento de 2 cm, una urgencia negativa"-. La app
	// avisa cuando algo va bien, no solo cuando va mal.
	//
	// Por eso son tres estados y no un bool: "no hay noticia" no es lo mismo que "hay
	// una mala noticia", y "hay una buena" tampoco se deduce negando.
	internal sealed class Urgencia
	{
		private readonly string nombre;

		private Urgencia(string nombre) { this.nombre = nombre; }

		internal static Urgencia Ninguna() { return new Urgencia("ninguna"); }
		internal static Urgencia Positiva() { return new Urgencia("positiva"); }
		internal static Urgencia Negativa() { return new Urgencia("negativa"); }

		internal static Urgencia PorNombre(string nombre)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
			switch (nombre.Trim().ToLowerInvariant())
			{
				case "ninguna": return Ninguna();
				case "positiva": return Positiva();
				case "negativa": return Negativa();
				default:
					throw new ArgumentOutOfRangeException(nameof(nombre), nombre,
						"Urgencia desconocida. Las que existen son: ninguna, positiva, negativa.");
			}
		}

		internal bool Avisa() { return nombre != "ninguna"; }
		internal bool EsPositiva() { return nombre == "positiva"; }
		internal bool EsNegativa() { return nombre == "negativa"; }

		public override string ToString() { return nombre; }
	}
}
