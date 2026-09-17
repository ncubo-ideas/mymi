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
	internal abstract class RitmoDelSuper
	{
		/// <summary>Cuántas compras hacen falta para poder hablar de un ritmo.</summary>
		// Con dos compras hay UN intervalo, y un intervalo no es un ritmo: puede ser la
		// semana que se hizo mercado dos veces. Con tres hay dos intervalos y una mediana
		// que aguanta una casualidad.
		internal const int ComprasQueHacenFalta = 3;

		internal static RitmoDelSuper Ninguno(int compras) => new SinRitmo(compras);

		internal static RitmoDelSuper De(IReadOnlyList<DateTime> fechas) => new Medido(fechas);

		/// <summary>Cuántas compras se miraron. Total: siempre contesta, incluso cero.</summary>
		internal abstract int ComprasMiradas { get; }

		/// <summary>
		/// Si el ritmo se pudo medir. Consulta de presencia, total: sin esto no se afirma
		/// nada.
		/// </summary>
		internal abstract bool EsObservado { get; }

		/// <summary>Cada cuántos días vuelve al súper, típicamente. Preguntar EsObservado primero.</summary>
		internal abstract int DiasTipicos { get; }

		/// <summary>La cadencia con la que va, dicha en palabras. Preguntar EsObservado primero.</summary>
		internal abstract Cadencia Cadencia { get; }

		/// <summary>El día de la semana en que más compra. Preguntar EsObservado primero.</summary>
		internal abstract DayOfWeek DiaFavorito { get; }

		/// <summary>
		/// EL CODIGO del dia favorito, sin acentos: `lunes`, `martes`, `miercoles`... Vacio cuando no
		/// hay ritmo. Como se dice lo busca con este codigo la capa que redacta, en su idioma.
		/// </summary>
		internal abstract string ClaveDelDiaFavorito { get; }

		/// <summary>Cuántas compras cayeron en el día favorito. Total: cero cuando no hay ritmo.</summary>
		internal abstract int ComprasEnElDiaFavorito { get; }

		/// <summary>El momento del día en que más compra. Preguntar EsObservado primero.</summary>
		internal abstract MomentoDelDia MomentoFavorito { get; }

		/// <summary>Cuántas compras cayeron en cada momento del día. Total: siempre trae los tres.</summary>
		internal abstract IReadOnlyList<ComprasEnUnMomento> PorMomento { get; }

		/// <summary>
		/// QUE FRASE dice este ritmo, por su nombre: `sin-compras`, `pocas-compras` o `medido`. El dia
		/// y el momento van en sus huecos por codigo, no por dicho.
		/// </summary>
		internal abstract string Clave { get; }

		// LAS LECTURAS PARA LA FRONTERA, sin guarda: cero y vacio cuando no hay ritmo. Es el
		// mismo par que RegistroDeGarantia y CambioDeRitmo -la tipada para el codigo, la
		// total para el wire- porque una proyeccion plana no puede preguntar antes: un
		// lector guardado no revienta el campo, revienta la consulta entera.
		//
		// Y no son un duplicado disfrazado: quien tiene el objeto en la mano debe recibir un
		// Cadencia y un MomentoDelDia, no dos strings, o el conjunto cerrado no sirve.
		internal abstract int DiasEntreCompras { get; }

		/// <summary>El codigo de la cadencia con que va. Vacio cuando no hay ritmo: lectura total.</summary>
		internal abstract string ClaveDeLaCadencia { get; }

		/// <summary>El codigo del momento favorito. Vacio cuando no hay ritmo: lectura total.</summary>
		internal abstract string ClaveDelMomentoFavorito { get; }

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

			internal override int ComprasMiradas => compras;
			internal override bool EsObservado => false;

			internal override int DiasTipicos =>
				throw new InvalidOperationException(
					$"Hacen falta {ComprasQueHacenFalta} compras para medir cada cuanto va al "
					+ $"super, y hay {compras}. Preguntar EsObservado antes.");

			internal override Cadencia Cadencia =>
				throw new InvalidOperationException(
					"No hay ritmo medido, asi que no hay cadencia. Preguntar EsObservado antes.");

			internal override DayOfWeek DiaFavorito =>
				throw new InvalidOperationException(
					"No hay ritmo medido, asi que no hay dia favorito. Preguntar EsObservado antes.");

			internal override string ClaveDelDiaFavorito => "";
			internal override int ComprasEnElDiaFavorito => 0;

			internal override MomentoDelDia MomentoFavorito =>
				throw new InvalidOperationException(
					"No hay ritmo medido, asi que no hay momento favorito. Preguntar EsObservado antes.");

			// Los tres momentos en cero: la pantalla puede dibujar la fila vacia sin
			// preguntar, que es de lo que sirve una lectura total.
			internal override IReadOnlyList<ComprasEnUnMomento> PorMomento
			{
				get
				{
					var vacios = new List<ComprasEnUnMomento>();
					foreach (MomentoDelDia momento in MomentoDelDia.Todos)
						vacios.Add(new ComprasEnUnMomento(momento, 0, 0));

					return vacios;
				}
			}

			internal override int DiasEntreCompras => 0;
			internal override string ClaveDeLaCadencia => "";
			internal override string ClaveDelMomentoFavorito => "";

			internal override string Clave => compras == 0 ? "sin-compras" : "pocas-compras";
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

			internal override int ComprasMiradas => cuantasMiradas;
			internal override bool EsObservado => true;
			internal override int DiasTipicos => diasTipicos;
			internal override Cadencia Cadencia => cadencia;
			internal override DayOfWeek DiaFavorito => diaFavorito;
			internal override string ClaveDelDiaFavorito => CodigoDelDia(diaFavorito);
			internal override int ComprasEnElDiaFavorito => comprasEnElDia;
			internal override MomentoDelDia MomentoFavorito => momentoFavorito;
			internal override IReadOnlyList<ComprasEnUnMomento> PorMomento => porMomento;

			internal override int DiasEntreCompras => diasTipicos;
			internal override string ClaveDeLaCadencia => cadencia.Clave;
			internal override string ClaveDelMomentoFavorito => momentoFavorito.Nombre;

			internal override string Clave => "medido";

			/// <summary>
			/// EL CODIGO del dia de la semana, sin acentos y en el alfabeto del calendario, no en el de
			/// un idioma. Vive aca porque el dominio es el dueno del conjunto; como se dice cada uno lo
			/// resuelve quien redacta.
			/// </summary>
			private static string CodigoDelDia(DayOfWeek dia) => dia switch
			{
				DayOfWeek.Monday => "lunes",
				DayOfWeek.Tuesday => "martes",
				DayOfWeek.Wednesday => "miercoles",
				DayOfWeek.Thursday => "jueves",
				DayOfWeek.Friday => "viernes",
				DayOfWeek.Saturday => "sabado",
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
	internal sealed class ComprasEnUnMomento
	{
		internal MomentoDelDia Momento { get; private set; }
		internal int Cuantas { get; private set; }

		internal ComprasEnUnMomento(MomentoDelDia momento, int cuantas, int deCuantas)
		{
			ArgumentNullException.ThrowIfNull(momento);

			Momento = momento;
			Cuantas = cuantas;
			DeCuantas = deCuantas;
		}

		internal int DeCuantas { get; private set; }

		/// <summary>Qué parte de las compras cayó en este momento. Cero cuando no hay compras.</summary>
		internal decimal Proporcion => DeCuantas == 0 ? 0m : Math.Round((decimal)Cuantas / DeCuantas, 4);

		/// <summary>El codigo del momento, aplanado para que el print lo alcance en un solo nivel.</summary>
		internal string ClaveDelMomento => Momento.Nombre;
	}
}
