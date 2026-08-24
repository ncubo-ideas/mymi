using System;

namespace Puppets.Portador
{
	// El proveedor rechazo la credencial. Tiene tipo propio porque es el UNICO fallo de
	// lectura del que el bucle puede recuperarse solo: rota con el refresh token y
	// reintenta. Un 500 o un timeout no tienen esa salida y suben tal cual.
	public sealed class CredencialRechazadaException : Exception
	{
		public CredencialRechazadaException(string mensaje) : base(mensaje) { }

		public CredencialRechazadaException(string mensaje, Exception interna) : base(mensaje, interna) { }
	}
}
