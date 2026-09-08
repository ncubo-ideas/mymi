using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// Como quedo el tanque: lleno o parcial. Cerrado de dos, porque "no me dijiste" no
	/// es un tercer estado del tanque sino la AUSENCIA de declaracion, y esa ausencia es
	/// su propio tipo (ver DeclaracionDeTanque.Ninguna).
	/// </summary>
	internal sealed class EstadoDelTanque
	{
		internal static readonly EstadoDelTanque Lleno = new EstadoDelTanque("lleno");
		internal static readonly EstadoDelTanque Parcial = new EstadoDelTanque("parcial");

		internal string Nombre { get; private set; }

		private EstadoDelTanque(string nombre)
		{
			Nombre = nombre;
		}

		internal static EstadoDelTanque PorNombre(string nombre)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

			string normalizado = nombre.Trim().ToLowerInvariant();
			if (normalizado == Lleno.Nombre) return Lleno;
			if (normalizado == Parcial.Nombre) return Parcial;

			throw new ErrorDeDominio(
				$"'{nombre}' no es un estado del tanque conocido. Los validos son: "
				+ $"{Lleno.Nombre}, {Parcial.Nombre}.");
		}

		internal bool EsLleno => ReferenceEquals(this, Lleno);
	}
}
