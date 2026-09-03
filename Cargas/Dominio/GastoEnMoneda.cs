using System;
using System.Collections.Generic;

namespace Cargas.Dominio
{
	/// <summary>
	/// El gasto de un periodo EN UNA MONEDA. Toda agregacion de dinero de este dominio
	/// agrupa por moneda y devuelve una fila por cada una: sumar cruzado exigiria un
	/// tipo de cambio con fecha, que es presentacion, no dominio (ver Moneda).
	/// </summary>
	internal sealed class GastoEnMoneda
	{
		internal Moneda Moneda { get; private set; }
		internal decimal Monto { get; private set; }
		internal int CantidadDeCargas { get; private set; }
		internal decimal Litros { get; private set; }

		internal GastoEnMoneda(Moneda moneda, decimal monto, int cantidadDeCargas, decimal litros)
		{
			ArgumentNullException.ThrowIfNull(moneda);

			Moneda = moneda;
			Monto = monto;
			CantidadDeCargas = cantidadDeCargas;
			Litros = litros;
		}

		/// <summary>El gasto de un periodo sobre las cargas que se le den, agrupado por
		/// moneda. Recibe las cargas y no las busca: asi la MISMA acumulacion sirve para
		/// el gasto de una persona (todas sus cargas) y para el de un carro (las suyas),
		/// que es lo que evita que una de las dos se calcule distinto que la otra.</summary>
		internal static IReadOnlyList<GastoEnMoneda> DelPeriodo(
			IEnumerable<Carga> cargas, int anio, int mes)
		{
			ArgumentNullException.ThrowIfNull(cargas);

			var montos = new Dictionary<Moneda, GastoAcumulado>();
			foreach (Carga carga in cargas)
			{
				if (carga.Fecha.Year != anio || carga.Fecha.Month != mes) continue;

				if (!montos.TryGetValue(carga.Total.Moneda, out GastoAcumulado acumulado))
				{
					acumulado = new GastoAcumulado();
					montos[carga.Total.Moneda] = acumulado;
				}
				acumulado.Monto += carga.Total.Monto;
				acumulado.Cargas++;
				acumulado.Litros += carga.Volumen.EnLitros;
			}

			var filas = new List<GastoEnMoneda>();
			foreach (KeyValuePair<Moneda, GastoAcumulado> par in montos)
				filas.Add(new GastoEnMoneda(par.Key, par.Value.Monto, par.Value.Cargas,
					par.Value.Litros));
			return filas;
		}

		private sealed class GastoAcumulado
		{
			internal decimal Monto;
			internal int Cargas;
			internal decimal Litros;
		}

		internal string CodigoDeMoneda => Moneda.Codigo;
		internal string Expresado =>
			$"{Moneda.Simbolo}{Cifras.Plata(Monto)} en {CantidadDeCargas} cargas";
	}
}
