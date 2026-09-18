using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// Una distancia con su unidad: la lectura de un odometro, o el largo de un tramo.
	/// </summary>
	internal sealed class Distancia
	{
		internal decimal Valor { get; private set; }
		internal UnidadDeDistancia Unidad { get; private set; }

		internal Distancia(decimal valor, UnidadDeDistancia unidad)
		{
			ArgumentNullException.ThrowIfNull(unidad);
			if (valor < 0m)
				throw new ErrorDeDominio($"Una distancia no puede ser negativa: {valor}.");

			Valor = valor;
			Unidad = unidad;
		}

		/// <summary>El canonico del dominio: todo tramo y todo rendimiento se calculan
		/// en kilometros.</summary>
		internal decimal EnKilometros => Unidad.EnKilometros(Valor);

		// Cuanto se avanzo desde otra lectura. La monotonia del odometro no se valida
		// aca sino donde hay contexto para saber cual lectura es anterior (el vehiculo):
		// esta operacion solo resta en canonico.
		internal decimal KilometrosDesde(Distancia anterior)
		{
			ArgumentNullException.ThrowIfNull(anterior);
			return EnKilometros - anterior.EnKilometros;
		}

		internal bool EsMayorQue(Distancia otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			return EnKilometros > otra.EnKilometros;
		}

		/// <summary>
		/// EL SIMBOLO DE LA UNIDAD, aplanado. La cifra NO se escribe aca: como se separan los
		/// miles y los decimales es una decision del idioma, y el dominio no tiene uno.
		/// </summary>
		internal string SimboloDeLaUnidad => Unidad.Simbolo;
	}
}
