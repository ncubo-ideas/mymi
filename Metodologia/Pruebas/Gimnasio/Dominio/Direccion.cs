using System;

namespace Gimnasio.Dominio
{
	// HACIA DONDE ES DESEABLE QUE SE MUEVA UNA MEDIDA.
	//
	// Es un conjunto cerrado y NO es propiedad de la medida: sale del objetivo de la
	// persona -juicio 1 del Preguntas.md-. Subir de peso es buena noticia para quien
	// necesita subir y mala para quien necesita bajar.
	//
	// La excepcion esta en MasaMuscular, que la fija ella misma. Por eso esto es un
	// tipo y no un bool: hacen falta tres estados y dos origenes distintos.
	internal sealed class Direccion
	{
		private readonly string nombre;
		private readonly int signoDeseado;   // +1 subir, -1 bajar, 0 mantener

		private Direccion(string nombre, int signoDeseado)
		{
			this.nombre = nombre;
			this.signoDeseado = signoDeseado;
		}

		internal static Direccion Subir() { return new Direccion("subir", 1); }
		internal static Direccion Bajar() { return new Direccion("bajar", -1); }
		internal static Direccion Mantener() { return new Direccion("mantener", 0); }

		internal static Direccion PorNombre(string nombre)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
			switch (nombre.Trim().ToLowerInvariant())
			{
				case "subir": return Subir();
				case "bajar": return Bajar();
				case "mantener": return Mantener();
				default:
					throw new ArgumentOutOfRangeException(nameof(nombre), nombre,
						"Direccion desconocida. Las que existen son: subir, bajar, mantener.");
			}
		}

		internal bool Es(Direccion otra)
		{
			return otra != null && otra.nombre == nombre;
		}

		internal bool EsSubir() { return signoDeseado > 0; }
		internal bool EsBajar() { return signoDeseado < 0; }
		internal bool EsMantener() { return signoDeseado == 0; }

		// El movimiento va hacia donde se queria? Con "mantener" nada acerca ni aleja:
		// lo que importa ahi es el ruido, y eso lo juzga Tendencia.
		internal bool Favorece(decimal cambio)
		{
			if (signoDeseado == 0) return cambio == 0m;
			return signoDeseado > 0 ? cambio > 0m : cambio < 0m;
		}

		public override string ToString() { return nombre; }
	}
}
