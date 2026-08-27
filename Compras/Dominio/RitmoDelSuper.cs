using System;
using System.Collections.Generic;

namespace Compras.Dominio
{
	/// <summary>
	/// CÓMO VA AL SÚPER: cada cuántos días vuelve, qué día de la semana suele ir, y en qué
	/// momento del día. "Cada 6 días, y casi siempre un viernes por la tarde."
	/// </summary>
	// ES OTRA PREGUNTA QUE RitmoDeCompra, y el nombre parecido puede confundir: aquel mide
	// cada cuanto entra UN DEPARTAMENTO -sirve para leer un hueco-, y esto mide cada cuanto
	// va AL SUPER, sin importar que compre. Una persona puede ir todas las semanas y comprar
	// pollo cada tres: son dos ritmos y no se derivan uno del otro.
	//
	// El diseño de la app le da pantalla propia -"Tu ritmo de super: hay dias y momentos que
	// se repiten mas de lo que parece"- con las tres facetas juntas, y por eso van juntas
	// aca: son una lectura, no tres.
	//
	// Y NO JUZGA. Que alguien vaya al super los viernes por la noche no es mejor ni peor que
	// ir los martes en la mañana. La app se lo cuenta porque probablemente no lo habia
	// notado, que es literalmente lo que el diseño pregunta: "¿lo habias notado?".
	public abstract class RitmoDelSuper
	{
		/// <summary>Cuántas compras hacen falta para poder hablar de un ritmo.</summary>
		// Con dos compras hay UN intervalo, y un intervalo no es un ritmo: puede ser la
		// semana que se hizo mercado dos veces. Con tres hay dos intervalos y una mediana
		// que aguanta una casualidad.
		public const int ComprasQueHacenFalta = 3;

		internal static RitmoDelSuper Ninguno(int compras) => new SinRitmo(compras);

		internal static RitmoDelSuper De(IReadOnlyList<DateTime> fechas) => new Medido(fechas);

		/// <summary>Cuántas compras se miraron. Total: siempre contesta, incluso cero.</summary>
		public abstract int ComprasMiradas { get; }

		/// <summary>
		/// Si el ritmo se pudo medir. Consulta de presencia, total: sin esto no se afirma
		/// nada.
		/// </summary>
		public abstract bool EsObservado { get; }

		/// <summary>Cada cuántos días vuelve al súper, típicamente. Preguntar EsObservado primero.</summary>
		public abstract int DiasTipicos { get; }

		/// <summary>La cadencia con la que va, dicha en palabras. Preguntar EsObservado primero.</summary>
		public abstract Cadencia Cadencia { get; }

		/// <summary>El día de la semana en que más compra. Preguntar EsObservado primero.</summary>
		public abstract DayOfWeek DiaFavorito { get; }

		/// <summary>El día favorito en español: "viernes". Vacío cuando no hay ritmo.</summary>
		public abstract string DiaFavoritoExpresado { get; }

		/// <summary>Cuántas compras cayeron en el día favorito. Total: cero cuando no hay ritmo.</summary>
		public abstract int ComprasEnElDiaFavorito { get; }

		/// <summary>El momento del día en que más compra. Preguntar EsObservado primero.</summary>
		public abstract MomentoDelDia MomentoFavorito { get; }

		/// <summary>Cuántas compras cayeron en cada momento del día. Total: siempre trae los tres.</summary>
		public abstract IReadOnlyList<ComprasEnUnMomento> PorMomento { get; }

		public abstract string ParaSonia { get; }

		private sealed class SinRitmo : RitmoDelSuper
		{
			private readonly int compras;

			internal SinRitmo(int compras)
			{
				if (compras < 0)
					throw new ArgumentOutOfRangeException(nameof(compras), compras,
						"No se pueden mirar menos de cero compras.");

				this.compras = compras;
			}

			public override int ComprasMiradas => compras;
			public override bool EsObservado => false;

			public override int DiasTipicos =>
				throw new InvalidOperationException(
					$"Hacen falta {ComprasQueHacenFalta} compras para medir cada cuanto va al "
					+ $"super, y hay {compras}. Preguntar EsObservado antes.");

			public override Cadencia Cadencia =>
				throw new InvalidOperationException(
					"No hay ritmo medido, asi que no hay cadencia. Preguntar EsObservado antes.");

			public override DayOfWeek DiaFavorito =>
				throw new InvalidOperationException(
					"No hay ritmo medido, asi que no hay dia favorito. Preguntar EsObservado antes.");

			public override string DiaFavoritoExpresado => "";
			public override int ComprasEnElDiaFavorito => 0;

			public override MomentoDelDia MomentoFavorito =>
				throw new InvalidOperationException(
					"No hay ritmo medido, asi que no hay momento favorito. Preguntar EsObservado antes.");

			// Los tres momentos en cero: la pantalla puede dibujar la fila vacia sin
			// preguntar, que es de lo que sirve una lectura total.
			public override IReadOnlyList<ComprasEnUnMomento> PorMomento
			{
				get
				{
					var vacios = new List<ComprasEnUnMomento>();
					foreach (MomentoDelDia momento in MomentoDelDia.Todos)
						vacios.Add(new ComprasEnUnMomento(momento, 0, 0));

					return vacios;
				}
			}

			public override string ParaSonia => compras == 0
				? "todavía no hay compras suyas con las que ver cada cuánto va al súper"
				: $"con {compras} compras todavía no alcanza para decir cada cuánto va al súper";
		}

		private sealed class Medido : RitmoDelSuper
		{
			private readonly int diasTipicos;
			private readonly Cadencia cadencia;
			private readonly DayOfWeek diaFavorito;
			private readonly int comprasEnElDia;
			private readonly int cuantasMiradas;
			private readonly List<ComprasEnUnMomento> porMomento = new List<ComprasEnUnMomento>();
			private readonly MomentoDelDia momentoFavorito;

			internal Medido(IReadOnlyList<DateTime> fechas)
			{
				ArgumentNullException.ThrowIfNull(fechas);
				if (fechas.Count < ComprasQueHacenFalta)
					throw new ArgumentException(
						$"Un ritmo medido necesita al menos {ComprasQueHacenFalta} compras; eso es Ninguno.",
						nameof(fechas));

				cuantasMiradas = fechas.Count;

				var ordenadas = new List<DateTime>(fechas);
				ordenadas.Sort();

				var intervalos = new List<int>();
				for (int i = 1; i < ordenadas.Count; i++)
				{
					int dias = (int)(ordenadas[i].Date - ordenadas[i - 1].Date).TotalDays;
					if (dias > 0) intervalos.Add(dias);
				}

				if (intervalos.Count == 0)
					throw new ArgumentException(
						"Todas las compras cayeron el mismo dia: no hay intervalo que medir.",
						nameof(fechas));

				diasTipicos = Mediana(intervalos);
				cadencia = Cadencia.Para(diasTipicos);

				// EL DIA FAVORITO. Con empate gana el que va primero en la semana, y es
				// arbitrario a proposito: cualquier criterio lo es, y elegir uno estable
				// evita que la misma pregunta conteste distinto en dos corridas.
				var porDia = new int[7];
				foreach (DateTime fecha in ordenadas) porDia[(int)fecha.DayOfWeek]++;

				int mejorDia = 0;
				for (int i = 1; i < 7; i++)
					if (porDia[i] > porDia[mejorDia]) mejorDia = i;

				diaFavorito = (DayOfWeek)mejorDia;
				comprasEnElDia = porDia[mejorDia];

				// LOS TRES MOMENTOS, siempre los tres y en orden del dia: una fila que se
				// dibuja igual tenga o no compras cada momento.
				MomentoDelDia mejorMomento = MomentoDelDia.Manana;
				int masCompras = -1;
				foreach (MomentoDelDia momento in MomentoDelDia.Todos)
				{
					int cuantas = 0;
					foreach (DateTime fecha in ordenadas)
						if (ReferenceEquals(MomentoDelDia.De(fecha), momento)) cuantas++;

					porMomento.Add(new ComprasEnUnMomento(momento, cuantas, cuantasMiradas));
					if (cuantas > masCompras) { masCompras = cuantas; mejorMomento = momento; }
				}

				momentoFavorito = mejorMomento;
			}

			public override int ComprasMiradas => cuantasMiradas;
			public override bool EsObservado => true;
			public override int DiasTipicos => diasTipicos;
			public override Cadencia Cadencia => cadencia;
			public override DayOfWeek DiaFavorito => diaFavorito;
			public override string DiaFavoritoExpresado => EnEspanol(diaFavorito);
			public override int ComprasEnElDiaFavorito => comprasEnElDia;
			public override MomentoDelDia MomentoFavorito => momentoFavorito;
			public override IReadOnlyList<ComprasEnUnMomento> PorMomento => porMomento;

			public override string ParaSonia =>
				$"suele ir al súper cada {diasTipicos} días, y casi siempre un "
				+ $"{DiaFavoritoExpresado} por la {momentoFavorito.ParaMostrar}";

			// El nombre del dia en español. Va aca y no en una pantalla porque la frase de
			// arriba lo necesita, y la frase la escribe el dominio.
			private static string EnEspanol(DayOfWeek dia) => dia switch
			{
				DayOfWeek.Monday => "lunes",
				DayOfWeek.Tuesday => "martes",
				DayOfWeek.Wednesday => "miércoles",
				DayOfWeek.Thursday => "jueves",
				DayOfWeek.Friday => "viernes",
				DayOfWeek.Saturday => "sábado",
				_ => "domingo",
			};

			// La MEDIANA, igual que los otros ritmos: una ida extra de imprevisto corre el
			// promedio y no mueve la mediana.
			private static int Mediana(IReadOnlyList<int> intervalos)
			{
				var ordenados = new List<int>(intervalos);
				ordenados.Sort();

				int medio = ordenados.Count / 2;
				return ordenados.Count % 2 == 1
					? ordenados[medio]
					: (int)Math.Round((ordenados[medio - 1] + ordenados[medio]) / 2.0);
			}
		}
	}

	/// <summary>Cuántas compras cayeron en un momento del día, y qué parte del total fue.</summary>
	public sealed class ComprasEnUnMomento
	{
		public MomentoDelDia Momento { get; private set; }
		public int Cuantas { get; private set; }

		internal ComprasEnUnMomento(MomentoDelDia momento, int cuantas, int deCuantas)
		{
			ArgumentNullException.ThrowIfNull(momento);

			Momento = momento;
			Cuantas = cuantas;
			DeCuantas = deCuantas;
		}

		public int DeCuantas { get; private set; }

		/// <summary>Qué parte de las compras cayó en este momento. Cero cuando no hay compras.</summary>
		public decimal Proporcion => DeCuantas == 0 ? 0m : Math.Round((decimal)Cuantas / DeCuantas, 4);

		/// <summary>La línea de la fila: "tarde · 7 compras".</summary>
		public string ParaMostrar =>
			$"{Momento.ParaMostrar} · {Cuantas} {(Cuantas == 1 ? "compra" : "compras")}";
	}
}
