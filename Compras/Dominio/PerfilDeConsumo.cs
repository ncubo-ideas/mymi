using System;

namespace Compras.Dominio
{
	/// <summary>
	/// Que papel juega una categoria COMESTIBLE en la canasta de la persona. Es el eje
	/// sobre el que la canasta se puede analizar despues: cuanto de lo que come es
	/// recomendado, cuanto es neutro y cuanto es un antojo ocasional.
	/// </summary>
	// Conjunto cerrado, comparado por referencia. Solo lo comestible lo porta (ver
	// Categoria.NoComestible).
	public sealed class PerfilDeConsumo
	{
		public static readonly PerfilDeConsumo Recomendado = new PerfilDeConsumo("recomendado");
		public static readonly PerfilDeConsumo Neutro = new PerfilDeConsumo("neutro");
		public static readonly PerfilDeConsumo Ocasional = new PerfilDeConsumo("ocasional");

		public string Nombre { get; private set; }

		private PerfilDeConsumo(string nombre)
		{
			Nombre = nombre;
		}

		public bool EsOcasional => ReferenceEquals(this, Ocasional);
	}
}
