using System;

namespace Presupuesto.Dominio
{
	// UN MOVIMIENTO. El grano de todo este dominio.
	//
	// Preguntas.md, parte 1: monto, fecha, medio de pago. Y la PROCEDENCIA, que en este
	// dominio no es metadato: es lo que distingue lo que dijo el banco de lo que dijo la
	// persona, y esa distincion queda pegada para siempre.
	//
	// HUECO 3 - LA MONEDA NO APARECE EN Preguntas.md. Ni una vez. Aca se toma la decision
	// minima: un solo monto, sin moneda, en la moneda de la persona. Es la unica que no
	// inventa nada. Pero es fragil: la entrevista hablo de comparar contra el rendimiento de
	// CRIPTO -juicio 7- y esas comparaciones no suelen ser en colones. Si entra una segunda
	// moneda, esta clase cambia y arrastra a todas las que suman.
	//
	// HUECO 4 - EL MEDIO DE PAGO esta listado como atributo y ninguna de las cinco preguntas
	// lo usa. Se guarda porque el archivo lo pide, pero sin saber si es capacidad o ruido: no
	// hay juicio que lo mencione. Vale preguntarlo antes de que alguien construya una pantalla
	// encima.
	internal sealed class Cargo
	{
		internal Cargo(DateTime fecha, decimal monto, string descripcionDelBanco,
			string medioDePago, Procedencia procedencia)
		{
			if (monto <= 0)
				throw new ArgumentOutOfRangeException(nameof(monto),
					"Un cargo mueve plata. Cero o negativo no es un cargo: es otra cosa que este dominio no modela.");

			Fecha = fecha.Date;
			Monto = monto;
			DescripcionDelBanco = descripcionDelBanco ?? "";
			MedioDePago = medioDePago ?? "";
			Procedencia = procedencia ?? throw new ArgumentNullException(nameof(procedencia));
		}

		internal DateTime Fecha { get; }

		internal decimal Monto { get; }

		// El texto tal como lo mando el emisor. NO es la identidad del gasto: es la evidencia
		// con la que se resuelve. Se guarda crudo porque cuando una marca falle, esto es lo
		// que hay que mirar.
		internal string DescripcionDelBanco { get; }

		internal string MedioDePago { get; }

		internal Procedencia Procedencia { get; }

		internal bool EstaEn(int ano, int mes) => Fecha.Year == ano && Fecha.Month == mes;

		public override string ToString() => $"{Fecha:yyyy-MM-dd} {Monto}";
	}
}
