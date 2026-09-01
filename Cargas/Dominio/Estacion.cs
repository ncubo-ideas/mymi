using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// La sucursal donde vive un surtidor. Minima a proposito: mapas, direcciones y
	/// precios de referencia estan fuera de alcance; lo unico que el dominio necesita
	/// es poder decir DONDE cargo Sonia.
	/// </summary>
	internal sealed class Estacion
	{
		internal string Nombre { get; private set; }

		internal Estacion(string nombre)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);
			Nombre = nombre.Trim();
		}

		internal bool EsLaMisma(Estacion otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			return string.Equals(Nombre, otra.Nombre, StringComparison.OrdinalIgnoreCase);
		}
	}
}
