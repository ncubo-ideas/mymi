using System;

namespace Gimnasio.Dominio
{
	// CONJUNTO CERRADO. Dos miembros y nada mas.
	//
	// El resolutor total NO es decoracion: es la unica puerta de entrada desde un
	// comando. Un `static readonly` no se alcanza desde el DSL -regla 11 del
	// substrato-, asi que sin `PorNombre` este tipo seria inalcanzable.
	//
	// Y revienta ante lo desconocido, a proposito: un genero mal escrito tiene que
	// fallar al entrar, no producir una lectura equivocada tres pantallas despues.
	internal sealed class Genero
	{
		private readonly string nombre;

		private Genero(string nombre) { this.nombre = nombre; }

		internal static Genero Femenino() { return new Genero("femenino"); }
		internal static Genero Masculino() { return new Genero("masculino"); }

		internal static Genero PorNombre(string nombre)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
			switch (nombre.Trim().ToLowerInvariant())
			{
				case "femenino": return Femenino();
				case "masculino": return Masculino();
				default:
					throw new ArgumentOutOfRangeException(nameof(nombre), nombre,
						"Genero desconocido. Los que existen son: femenino, masculino.");
			}
		}

		internal bool Es(Genero otro)
		{
			return otro != null && otro.nombre == nombre;
		}

		public override string ToString() { return nombre; }
	}
}
