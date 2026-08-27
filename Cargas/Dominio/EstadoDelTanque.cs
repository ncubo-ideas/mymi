using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// Como quedo el tanque: lleno o parcial. Cerrado de dos, porque "no me dijiste" no
	/// es un tercer estado del tanque sino la AUSENCIA de declaracion, y esa ausencia es
	/// su propio tipo (ver DeclaracionDeTanque.Ninguna).
	/// </summary>
	public sealed class EstadoDelTanque
	{
		public static readonly EstadoDelTanque Lleno = new EstadoDelTanque("lleno");
		public static readonly EstadoDelTanque Parcial = new EstadoDelTanque("parcial");

		public string Nombre { get; private set; }

		private EstadoDelTanque(string nombre)
		{
			Nombre = nombre;
		}

		public static EstadoDelTanque PorNombre(string nombre)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

			string normalizado = nombre.Trim().ToLowerInvariant();
			if (normalizado == Lleno.Nombre) return Lleno;
			if (normalizado == Parcial.Nombre) return Parcial;

			throw new ErrorDeDominio(
				$"'{nombre}' no es un estado del tanque conocido. Los validos son: "
				+ $"{Lleno.Nombre}, {Parcial.Nombre}.");
		}

		public bool EsLleno => ReferenceEquals(this, Lleno);
	}
}
