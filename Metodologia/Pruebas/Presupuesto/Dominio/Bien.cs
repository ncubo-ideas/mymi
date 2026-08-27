using System;
using System.Collections.Generic;

namespace Presupuesto.Dominio
{
	// UNA COSA A LA QUE SE LE ATRIBUYEN GASTOS Y QUE PUEDE PRODUCIR INGRESOS. La propiedad
	// alquilada, el carro.
	//
	// EXISTE PORQUE EL BANCO NO PUEDE SABERLO. El banco ve un pago a una ferreteria y nada
	// mas; que ese pago sea de la propiedad alquilada y no de la casa propia lo sabe solo la
	// persona. Es la dimension mas clara de Preguntas.md que no viene en ningun estado de
	// cuenta, y sin ella la pregunta 2 -"vale la pena el alquiler contra los gastos de esa
	// propiedad?"- no tiene por donde empezar.
	//
	// La atribucion se MARCA cargo por cargo, que es lo que dijo la entrevista, con la
	// esperanza de inferirla con el tiempo. Cuarta aparicion del mismo patron: marcar
	// primero, inferir despues.
	//
	// HUECO 6 - Preguntas.md no dice cuanto VALE el bien. Y sin valor no hay rendimiento
	// porcentual: se puede decir "esta propiedad te dejo 200 mil netos este ano" pero no "te
	// rindio un 4%", y por lo tanto NO SE PUEDE comparar contra un certificado de deposito,
	// que es la mitad del juicio 7. La entrevista pidio esa comparacion y el archivo no
	// alcanzo a darle con que. Aca el valor es opcional: cuando falta, el rendimiento se
	// contesta en plata y se dice que la comparacion no esta disponible.
	public sealed class Bien
	{
		private readonly List<Cargo> gastos = new List<Cargo>();
		private readonly List<Cargo> ingresos = new List<Cargo>();

		public Bien(string nombre, decimal valorEstimado)
		{
			if (string.IsNullOrWhiteSpace(nombre))
				throw new ArgumentException("El bien necesita un nombre que la persona reconozca.", nameof(nombre));
			if (valorEstimado < 0)
				throw new ArgumentOutOfRangeException(nameof(valorEstimado),
					"Un valor negativo no es una estimacion, es un error de captura.");

			Nombre = nombre;
			ValorEstimado = valorEstimado;
		}

		public string Nombre { get; }

		// Cero significa "no se sabe", no "no vale nada". De eso depende si el rendimiento se
		// puede expresar en porcentaje.
		public decimal ValorEstimado { get; }

		public bool SeSabeCuantoVale => ValorEstimado > 0;

		// La persona marca. El dominio no adivina.
		public void SeLeAtribuye(Cargo gasto)
		{
			if (gasto == null) throw new ArgumentNullException(nameof(gasto));
			gastos.Add(gasto);
		}

		public void Produce(Cargo ingreso)
		{
			if (ingreso == null) throw new ArgumentNullException(nameof(ingreso));
			ingresos.Add(ingreso);
		}

		public decimal GastosEntre(DateTime desde, DateTime hasta) => Sumar(gastos, desde, hasta);

		public decimal IngresosEntre(DateTime desde, DateTime hasta) => Sumar(ingresos, desde, hasta);

		public decimal NetoEntre(DateTime desde, DateTime hasta) =>
			IngresosEntre(desde, hasta) - GastosEntre(desde, hasta);

		private static decimal Sumar(List<Cargo> cargos, DateTime desde, DateTime hasta)
		{
			decimal total = 0m;
			foreach (Cargo c in cargos)
				if (c.Fecha >= desde.Date && c.Fecha <= hasta.Date) total += c.Monto;
			return total;
		}

		public override string ToString() => Nombre;
	}
}
