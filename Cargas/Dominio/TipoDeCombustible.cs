using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// El grado de combustible que despacho el surtidor. Cerrado y por referencia.
	/// Importa para la COMPARABILIDAD: cambiar de regular a super, o de gasolina a
	/// diesel, hace que los tramos no sean del todo comparables entre si.
	/// </summary>
	internal sealed class TipoDeCombustible
	{
		internal static readonly TipoDeCombustible Regular = new TipoDeCombustible("regular");
		internal static readonly TipoDeCombustible Super = new TipoDeCombustible("super");
		internal static readonly TipoDeCombustible Diesel = new TipoDeCombustible("diesel");

		internal string Nombre { get; private set; }

		private TipoDeCombustible(string nombre)
		{
			Nombre = nombre;
		}

		// PorNombre revienta y EsConocido decide: reventar es lo correcto cuando alguien
		// PREGUNTA por un tipo -es un error de quien pregunta- y es lo incorrecto cuando
		// una estacion PUBLICA uno que esta app no modela, que la frontera descarta.
		internal static bool EsConocido(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return false;
			string normalizado = nombre.Trim().ToLowerInvariant();
			return normalizado == Regular.Nombre || normalizado == Super.Nombre
				|| normalizado == Diesel.Nombre;
		}

		internal static TipoDeCombustible PorNombre(string nombre)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

			string normalizado = nombre.Trim().ToLowerInvariant();
			if (normalizado == Regular.Nombre) return Regular;
			if (normalizado == Super.Nombre) return Super;
			if (normalizado == Diesel.Nombre) return Diesel;

			throw new ErrorDeDominio(
				$"'{nombre}' no es un tipo de combustible conocido. Los validos son: "
				+ $"{Regular.Nombre}, {Super.Nombre}, {Diesel.Nombre}.");
		}

		internal bool EsElMismo(TipoDeCombustible otro)
		{
			ArgumentNullException.ThrowIfNull(otro);
			return ReferenceEquals(this, otro);
		}
	}
}
