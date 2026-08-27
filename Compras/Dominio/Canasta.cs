using System;
using System.Collections.Generic;

namespace Compras.Dominio
{
	/// <summary>
	/// La foto de UN MES de compras, sumada POR DEPARTAMENTO: cuanto entro de cada
	/// pasillo, con lo comestible y lo no comestible separados. Devuelve montos y
	/// proporciones — hechos, no prosa.
	/// </summary>
	// Suma por el departamento del super y no por una etiqueta de valor. Antes esta clase
	// repartia el gasto en recomendado / neutro / ocasional, y eso era una lectura de
	// salud que este modulo no tiene con que sostener: el super no sabe si la persona esta
	// en una dieta, si son cinco en la casa, ni si el helado era de un cumpleanos. El
	// departamento es un hecho de gondola y aguanta cualquier pregunta que venga despues.
	public class Canasta
	{
		private readonly Dictionary<string, decimal> porDepartamento =
			new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

		public int Anio { get; private set; }
		public int Mes { get; private set; }
		public int CantidadDeCompras { get; private set; }

		public decimal GastoComestible { get; private set; }
		public decimal GastoNoComestible { get; private set; }

		public decimal GastoTotal => GastoComestible + GastoNoComestible;

		public bool HayComestible => GastoComestible > 0m;

		public bool HayCompras => CantidadDeCompras > 0;

		/// <summary>El mes, como se escribe: 2026-03.</summary>
		public string MesExpresado => $"{Anio:0000}-{Mes:00}";

		internal Canasta(int anio, int mes)
		{
			if (anio < 1900 || anio > 3000)
				throw new ArgumentOutOfRangeException(nameof(anio), anio, "El anio de la canasta no es razonable.");
			if (mes < 1 || mes > 12)
				throw new ArgumentOutOfRangeException(nameof(mes), mes, "El mes tiene que estar entre 1 y 12.");

			Anio = anio;
			Mes = mes;
		}

		internal void Sumar(Compra compra)
		{
			ArgumentNullException.ThrowIfNull(compra);

			foreach (Item item in compra.Items)
			{
				string llave = item.Categoria.Nombre;
				porDepartamento.TryGetValue(llave, out decimal acumulado);
				porDepartamento[llave] = acumulado + item.Total;

				if (item.Categoria.EsComestible) GastoComestible = GastoComestible + item.Total;
				else GastoNoComestible = GastoNoComestible + item.Total;
			}

			CantidadDeCompras = CantidadDeCompras + 1;
		}

		/// <summary>Cuanto entro de un departamento. Total: uno que no aparecio es cero, no una ausencia.</summary>
		public decimal GastoDe(Categoria categoria)
		{
			ArgumentNullException.ThrowIfNull(categoria);

			porDepartamento.TryGetValue(categoria.Nombre, out decimal monto);
			return monto;
		}

		/// <summary>Consulta de presencia, total: si este departamento entro en el mes.</summary>
		public bool Tiene(Categoria categoria) => GastoDe(categoria) > 0m;

		/// <summary>
		/// Los departamentos que entraron, del que mas pesa al que menos. Total: un mes sin
		/// compras devuelve la lista vacia, que ya es una respuesta.
		/// </summary>
		public IReadOnlyList<GastoDeDepartamento> Departamentos
		{
			get
			{
				var gastos = new List<GastoDeDepartamento>();

				foreach (Categoria categoria in Categoria.Todas)
				{
					decimal monto = GastoDe(categoria);
					if (monto > 0m) gastos.Add(new GastoDeDepartamento(categoria, monto, GastoTotal));
				}

				gastos.Sort((uno, otro) => otro.Monto.CompareTo(uno.Monto));
				return gastos;
			}
		}
	}

	/// <summary>Cuanto entro de un departamento en un mes, y que parte del total fue.</summary>
	public class GastoDeDepartamento
	{
		public Categoria Categoria { get; private set; }
		public decimal Monto { get; private set; }

		private readonly decimal total;

		internal GastoDeDepartamento(Categoria categoria, decimal monto, decimal total)
		{
			ArgumentNullException.ThrowIfNull(categoria);

			Categoria = categoria;
			Monto = monto;
			this.total = total;
		}

		public string Titulo => Categoria.Titulo;

		/// <summary>Que parte del gasto del mes fue. 1 es todo el mes.</summary>
		public decimal ProporcionDelTotal => total == 0m ? 0m : Math.Round(Monto / total, 4);
	}
}
