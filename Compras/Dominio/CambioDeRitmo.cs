using System;
using System.Collections.Generic;

namespace Compras.Dominio
{
	/// <summary>
	/// CÓMO CAMBIÓ EL RITMO de un producto: cada cuánto lo compraba antes y cada cuánto lo
	/// compra ahora. "El helado pasó de una vez al mes a todas las semanas."
	/// </summary>
	// ES LA LECTURA QUE LA WIKI PIDE POR SU NOMBRE. La respuesta esperada de S10 dice que
	// el helado "paso de 1 vez al mes a casi todas las semanas", y hasta ahora el modulo no
	// podia decirlo: RitmoDeCompra mide por DEPARTAMENTO -sirve para leer un hueco- y esto
	// es por PRODUCTO y en dos epocas.
	//
	// La diferencia no es de grano nomas. Un departamento se lee para saber si FALTA algo;
	// un producto se lee para saber si el habito CAMBIO, que es otra pregunta. Al helado
	// nadie le lee un hueco -este modulo no le dice a nadie que hace un mes que no compra
	// helado- pero que pasara de mensual a semanal si es un dato.
	//
	// Y NO JUZGA. Dice de que a que, no si esta mejor. "Que el helado pasara de mensual a
	// semanal es un dato, no una falta": la app lo señala y se calla.
	internal abstract class CambioDeRitmo
	{
		/// <summary>Cuántas compras del producto hacen falta en un lado para medirle un ritmo.</summary>
		// Dos compras dan UN intervalo, y un intervalo solo no es un habito: puede ser la
		// semana que se acabo antes. Con tres hay dos intervalos y ya se puede tomar una
		// mediana que aguante una casualidad.
		internal const int ComprasQueHacenFaltaDeCadaLado = 3;

		internal static CambioDeRitmo Ninguno(string sku, string descripcion) =>
			new SinRitmo(sku, descripcion);

		internal static CambioDeRitmo De(string sku, string descripcion,
			IReadOnlyList<int> antes, IReadOnlyList<int> despues) =>
			new Medido(sku, descripcion, antes, despues);

		internal string Sku { get; private set; }
		internal string Descripcion { get; private set; }

		private CambioDeRitmo(string sku, string descripcion)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(sku);
			ArgumentNullException.ThrowIfNull(descripcion);

			Sku = sku;
			Descripcion = descripcion;
		}

		/// <summary>
		/// Si los DOS lados tienen suficientes compras para medirles un ritmo. Consulta de
		/// presencia, total: sin esto no se afirma nada.
		/// </summary>
		internal abstract bool HayConQueComparar { get; }

		/// <summary>La cadencia de antes del corte. Preguntar HayConQueComparar primero.</summary>
		internal abstract Cadencia Antes { get; }

		/// <summary>La cadencia de después del corte. Preguntar HayConQueComparar primero.</summary>
		internal abstract Cadencia Ahora { get; }

		/// <summary>Si la cadencia cambió de nombre. Total: en false, viene igual.</summary>
		internal abstract bool Cambio { get; }

		/// <summary>Si ahora lo compra más seguido que antes. Preguntar Cambio primero.</summary>
		internal abstract bool EsMasSeguido { get; }

		internal abstract string ParaElAsistente { get; }

		// LAS DOS CADENCIAS, en la forma corta y SIN GUARDA: vacias cuando no hay ritmo
		// medido. Existen para la frontera, que proyecta plano y no puede preguntar antes:
		// un lector guardado no revienta el campo, revienta la consulta entera.
		//
		// Es el mismo par que RegistroDeGarantia -la lectura tipada para el codigo, la
		// total para el wire-, y la razon de que no sea una sola: quien tiene el objeto en
		// la mano debe recibir un Cadencia, no un string, o el conjunto cerrado no sirve
		// de nada.
		internal abstract string AntesCompacto { get; }
		internal abstract string AhoraCompacto { get; }

		private sealed class SinRitmo : CambioDeRitmo
		{
			internal SinRitmo(string sku, string descripcion) : base(sku, descripcion) { }

			internal override bool HayConQueComparar => false;

			internal override Cadencia Antes =>
				throw new InvalidOperationException(
					$"No hay {ComprasQueHacenFaltaDeCadaLado} compras de '{Descripcion}' de cada lado "
					+ "del corte con las que medir un ritmo. Preguntar HayConQueComparar antes.");

			internal override Cadencia Ahora => Antes;
			internal override bool Cambio => false;

			internal override bool EsMasSeguido =>
				throw new InvalidOperationException(
					$"No hay ritmo medido para '{Descripcion}', asi que no hay nada que sea "
					+ "mas seguido. Preguntar HayConQueComparar antes.");

			internal override string AntesCompacto => "";
			internal override string AhoraCompacto => "";

			internal override string ParaElAsistente =>
				$"todavía no hay suficientes compras de {Descripcion} de los dos lados "
				+ "como para decir si cambió el ritmo";
		}

		private sealed class Medido : CambioDeRitmo
		{
			private readonly Cadencia antes;
			private readonly Cadencia ahora;

			internal Medido(string sku, string descripcion,
				IReadOnlyList<int> intervalosDeAntes, IReadOnlyList<int> intervalosDeAhora)
				: base(sku, descripcion)
			{
				ArgumentNullException.ThrowIfNull(intervalosDeAntes);
				ArgumentNullException.ThrowIfNull(intervalosDeAhora);
				if (intervalosDeAntes.Count == 0 || intervalosDeAhora.Count == 0)
					throw new ArgumentException(
						"Un cambio de ritmo sin intervalos de los dos lados no es un cambio; eso es Ninguno.");

				antes = Cadencia.Para(Mediana(intervalosDeAntes));
				ahora = Cadencia.Para(Mediana(intervalosDeAhora));
			}

			internal override bool HayConQueComparar => true;
			internal override Cadencia Antes => antes;
			internal override Cadencia Ahora => ahora;

			// Se comparan las CADENCIAS y no los dias: de 7 a 6 dias no es un cambio de
			// habito, es la semana que se corrio. Las dos siguen siendo "todas las semanas",
			// y decir que cambio seria inventar una noticia.
			internal override bool Cambio => !ReferenceEquals(antes, ahora);

			internal override bool EsMasSeguido => ahora.EsMasSeguidaQue(antes);

			internal override string AntesCompacto => antes.Compacta;
			internal override string AhoraCompacto => ahora.Compacta;

			internal override string ParaElAsistente => Cambio
				? $"{Descripcion} pasó de {antes.ParaMostrar} a {ahora.ParaMostrar}"
				: $"{Descripcion} sigue {ahora.ParaMostrar}";

			// La MEDIANA, igual que RitmoDeCompra y por la misma razon: una compra de
			// reserva corre el promedio y no mueve la mediana. Un habito se describe mejor
			// por su intervalo tipico que por su media.
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
}
