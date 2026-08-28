using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// La excepcion propia del dominio: se lanza cuando un llamador viola un contrato
	/// (un argumento imposible, una lectura de algo que no existe, una suma de monedas
	/// distintas). Un desenlace ESPERADO del negocio -un tramo que todavia no se puede
	/// calcular- nunca es una excepcion: es un veredicto o un estado.
	/// </summary>
	public sealed class ErrorDeDominio : Exception
	{
		public ErrorDeDominio(string mensaje) : base(mensaje)
		{
		}
	}
}
