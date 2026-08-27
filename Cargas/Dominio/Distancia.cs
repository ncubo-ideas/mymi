using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// Una distancia con su unidad: la lectura de un odometro, o el largo de un tramo.
	/// </summary>
	public sealed class Distancia
	{
		public decimal Valor { get; private set; }
		public UnidadDeDistancia Unidad { get; private set; }

		public Distancia(decimal valor, UnidadDeDistancia unidad)
		{
			ArgumentNullException.ThrowIfNull(unidad);
			if (valor < 0m)
				throw new ErrorDeDominio($"Una distancia no puede ser negativa: {valor}.");

			Valor = valor;
			Unidad = unidad;
		}

		/// <summary>El canonico del dominio: todo tramo y todo rendimiento se calculan
		/// en kilometros.</summary>
		public decimal EnKilometros => Unidad.EnKilometros(Valor);

		// Cuanto se avanzo desde otra lectura. La monotonia del odometro no se valida
		// aca sino donde hay contexto para saber cual lectura es anterior (el vehiculo):
		// esta operacion solo resta en canonico.
		public decimal KilometrosDesde(Distancia anterior)
		{
			ArgumentNullException.ThrowIfNull(anterior);
			return EnKilometros - anterior.EnKilometros;
		}

		public bool EsMayorQue(Distancia otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			return EnKilometros > otra.EnKilometros;
		}

		public string Expresada => $"{Valor:0.#} {Unidad.Simbolo}";
	}
}
