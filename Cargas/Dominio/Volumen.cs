using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// Una cantidad de combustible con su unidad. El valor solo no viaja por el dominio:
	/// 40 litros y 40 galones son cargas muy distintas.
	/// </summary>
	internal sealed class Volumen
	{
		internal decimal Valor { get; private set; }
		internal UnidadDeVolumen Unidad { get; private set; }

		internal Volumen(decimal valor, UnidadDeVolumen unidad)
		{
			ArgumentNullException.ThrowIfNull(unidad);
			if (valor <= 0m)
				throw new ErrorDeDominio($"Un volumen de carga debe ser positivo, no {valor}.");

			Valor = valor;
			Unidad = unidad;
		}

		/// <summary>El valor en el canonico del dominio. Toda suma y todo rendimiento se
		/// calculan en litros, sin importar en que unidad vendio el surtidor.</summary>
		internal decimal EnLitros => Unidad.EnLitros(Valor);

		// La suma cruza unidades porque la conversion es fisica (ver UnidadDeVolumen).
		// El resultado queda en litros: es el canonico, no una preferencia de pantalla.
		internal Volumen Mas(Volumen otro)
		{
			ArgumentNullException.ThrowIfNull(otro);
			return new Volumen(EnLitros + otro.EnLitros, UnidadDeVolumen.Litro);
		}

		internal string Expresado => $"{Valor:0.###} {Unidad.Simbolo}";
	}
}
