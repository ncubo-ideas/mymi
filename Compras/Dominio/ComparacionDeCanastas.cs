using System;
using System.Collections.Generic;
using System.Globalization;

namespace Compras.Dominio
{
	/// <summary>
	/// Como cambio la canasta entre dos meses, y — lo que ninguna pantalla puede armar —
	/// QUE DEPARTAMENTOS EXPLICAN la diferencia. Total: comparar contra un mes sin compras
	/// no revienta.
	/// </summary>
	// "Gasto 20.000 mas que el ano pasado" lo suma cualquiera. Lo que nadie tiene es de
	// DONDE salieron esos 20.000, y eso solo se sabe cruzando los dos meses departamento
	// por departamento.
	//
	// No hay veredicto: se dice cuanto y de donde, no si esta bien. La direccion deseable
	// de una canasta de comida depende de cosas que el super no sabe -un resultado de
	// laboratorio, una dieta, cuantos comen en la casa-, y quien las cruza es el asistente
	// con permiso de la persona.
	internal class ComparacionDeCanastas
	{
		/// <summary>
		/// Que parte del movimiento total tiene que mover un departamento para que valga
		/// nombrarlo. Menos de un decimo es ruido de despensa y ensucia la lectura.
		/// </summary>
		internal const decimal ParteDelMovimientoQueCuenta = 0.1m;

		internal Canasta Antes { get; private set; }
		internal Canasta Despues { get; private set; }

		internal ComparacionDeCanastas(Canasta antes, Canasta despues)
		{
			ArgumentNullException.ThrowIfNull(antes);
			ArgumentNullException.ThrowIfNull(despues);

			Antes = antes;
			Despues = despues;
		}

		/// <summary>Solo hay comparacion si LOS DOS meses tienen compras; si no, se inventaria una lectura.</summary>
		internal bool HayComparacion => Antes.HayCompras && Despues.HayCompras;

		internal decimal GastoAntes => Antes.GastoTotal;

		internal decimal GastoDespues => Despues.GastoTotal;

		/// <summary>El HECHO, con signo: cuanto mas -o menos- se gasto en el mes de despues.</summary>
		internal decimal CambioEnElTotal => HayComparacion ? GastoDespues - GastoAntes : 0m;

		/// <summary>El cambio del total en proporcion. 0,11 es un 11% mas que el mes de antes.</summary>
		internal decimal CambioEnProporcion =>
			HayComparacion && GastoAntes != 0m
				? Math.Round(CambioEnElTotal / GastoAntes, 4)
				: 0m;

		/// <summary>
		/// Departamento por departamento, del que mas movio la aguja al que menos. Total:
		/// sin comparacion la lista viene vacia.
		/// </summary>
		internal IReadOnlyList<DiferenciaDeDepartamento> Diferencias
		{
			get
			{
				var diferencias = new List<DiferenciaDeDepartamento>();
				if (!HayComparacion) return diferencias;

				decimal movimiento = 0m;
				foreach (Categoria categoria in Categoria.Todas)
					movimiento = movimiento + Math.Abs(Despues.GastoDe(categoria) - Antes.GastoDe(categoria));

				foreach (Categoria categoria in Categoria.Todas)
				{
					decimal antes = Antes.GastoDe(categoria);
					decimal despues = Despues.GastoDe(categoria);
					if (antes == 0m && despues == 0m) continue;

					diferencias.Add(new DiferenciaDeDepartamento(categoria, antes, despues, movimiento));
				}

				diferencias.Sort((uno, otro) => Math.Abs(otro.Cambio).CompareTo(Math.Abs(uno.Cambio)));
				return diferencias;
			}
		}

		/// <summary>Los departamentos que EXPLICAN la diferencia: los que movieron lo suficiente.</summary>
		internal IReadOnlyList<DiferenciaDeDepartamento> LasQueExplican
		{
			get
			{
				var explican = new List<DiferenciaDeDepartamento>();
				foreach (DiferenciaDeDepartamento diferencia in Diferencias)
					if (diferencia.CuentaEnElMovimiento) explican.Add(diferencia);

				return explican;
			}
		}

		/// <summary>Consulta de presencia, total: si hay algun departamento que explique el cambio.</summary>
		internal bool HayAlgoQueLoExplique => LasQueExplican.Count > 0;

		/// <summary>
		/// QUE FRASE dice esta comparacion, por su nombre: `sin-comparacion`, `repartida` o
		/// `explicada`. Los pasillos que la explican ya se publican en Diferencias, cada uno con su
		/// Subio y su EsNuevo, asi que esta lectura no los repite.
		/// </summary>
		internal string Clave =>
			!HayComparacion ? "sin-comparacion" : !HayAlgoQueLoExplique ? "repartida" : "explicada";
	}

	/// <summary>
	/// Cuanto movio UN departamento entre los dos meses: los dos montos, el cambio con
	/// signo, y si es un pasillo que antes no aparecia.
	/// </summary>
	internal class DiferenciaDeDepartamento
	{
		internal Categoria Categoria { get; private set; }
		internal decimal MontoAntes { get; private set; }
		internal decimal MontoDespues { get; private set; }

		private readonly decimal movimientoTotal;

		internal DiferenciaDeDepartamento(Categoria categoria, decimal montoAntes, decimal montoDespues,
			decimal movimientoTotal)
		{
			ArgumentNullException.ThrowIfNull(categoria);

			Categoria = categoria;
			MontoAntes = montoAntes;
			MontoDespues = montoDespues;
			this.movimientoTotal = movimientoTotal;
		}

		/// <summary>Con signo: positivo si en el mes de despues entro mas de este pasillo.</summary>
		internal decimal Cambio => MontoDespues - MontoAntes;

		internal bool Subio => Cambio > 0m;

		/// <summary>Si el pasillo ENTRO: no aparecia en el mes de antes y ahora si.</summary>
		internal bool EsNuevo => MontoAntes == 0m && MontoDespues > 0m;

		/// <summary>Si el pasillo SE FUE: aparecia en el mes de antes y ahora no.</summary>
		internal bool Desaparecio => MontoAntes > 0m && MontoDespues == 0m;

		/// <summary>Si movio lo suficiente como para que valga nombrarlo al explicar el cambio.</summary>
		internal bool CuentaEnElMovimiento =>
			movimientoTotal > 0m
			&& Math.Abs(Cambio) / movimientoTotal >= ComparacionDeCanastas.ParteDelMovimientoQueCuenta;
	}
}
