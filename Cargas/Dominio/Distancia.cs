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

		// CON SEPARADOR DE MILES E INDEPENDIENTE DE LA CULTURA, como toda cifra que el
		// dominio pone en una frase (ver Cifras). Con "{Valor:0.#}" pelado la parada decia
		// "43200 km" tres centimetros debajo de un "43 624 km" de la caratula: la misma
		// magnitud con dos convenciones se lee como dos medidas.
		internal string Expresada => $"{Valor.ToString("#,0.#", Cifras.Agrupadas)} {Unidad.Simbolo}";
	}
}
