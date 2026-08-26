using System;

namespace Examenes.Dominio
{
	// EL VALOR DE UN ANO, para poder comparar este contra el pasado.
	//
	// Alvaro, 2026-08-26: "usted a eso le tiene que hacer un histograma, que tiene que
	// hacer tal vez un COMPARATIVO CON EL ANO PASADO, deme la tendencia."
	//
	// Y la aclaracion de que es esto y que no es: "no son prints de la tabla, SON
	// COMPUTOS". Un comparativo interanual no es la lista de examenes agrupada: es una
	// serie nueva, con un punto por ano, que puede tener menos puntos que mediciones -si
	// hubo dos en el mismo ano- o huecos -si hubo un ano sin medir-.
	//
	// Los huecos NO se rellenan, por la misma razon que las celdas del cuadro: interpolar
	// un ano que nadie midio es inventar un dato.
	public sealed class MedicionDelAno
	{
		public int Ano { get; private set; }

		// El ULTIMO valor del ano, no el promedio. Es el que el medico mira: el estado en
		// que quedo la persona al cerrar ese ano, no una mezcla de los que hubo.
		public Medicion Valor { get; private set; }

		public int Cuantas { get; private set; }

		// Con signo, contra el ano anterior CON MEDICIONES. Null en el primero, y tambien
		// cuando el anterior no existe: sin punto de comparacion no hay comparativo.
		public decimal? CambioContraElAnterior { get; private set; }

		public MedicionDelAno(int ano, Medicion valor, int cuantas, decimal? cambio)
		{
			ArgumentNullException.ThrowIfNull(valor);

			Ano = ano;
			Valor = valor;
			Cuantas = cuantas;
			CambioContraElAnterior = cambio;
		}

		public bool HayConQueComparar => CambioContraElAnterior.HasValue;

		public bool Subio => CambioContraElAnterior.HasValue && CambioContraElAnterior.Value > 0m;

		public string Expresado => HayConQueComparar
			? $"{Ano}: {Valor.Expresado} ({(Subio ? "+" : "")}{CambioContraElAnterior.Value} contra {Ano - 1})"
			: $"{Ano}: {Valor.Expresado}";
	}
}
