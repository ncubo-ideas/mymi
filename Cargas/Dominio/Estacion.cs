using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// La sucursal donde vive un surtidor. Minima a proposito: mapas, direcciones y
	/// precios de referencia estan fuera de alcance; lo unico que el dominio necesita
	/// es poder decir DONDE cargo Sonia.
	/// </summary>
	public sealed class Estacion
	{
		public string Nombre { get; private set; }

		public Estacion(string nombre)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);
			Nombre = nombre.Trim();
		}

		public bool EsLaMisma(Estacion otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			return string.Equals(Nombre, otra.Nombre, StringComparison.OrdinalIgnoreCase);
		}
	}
}
