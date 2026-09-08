using System;
using System.Globalization;

namespace Compras.Dominio
{
	/// <summary>
	/// CÓMO SE VENDE un producto. Es la raíz de toda línea de ticket, y la razón de que
	/// una cantidad signifique algo.
	/// </summary>
	///
	/// Existe porque "3" no es un dato. Un supermercado vende de formas que no se leen
	/// igual, y este demo distingue cinco:
	///
	///   1. Por unidad         brócoli, olla arrocera        se cuenta, entero
	///   2. Por contenido      leche 1 L, manteca 500 g      se cuenta, y cada pieza mide
	///   3. Por peso variable  pollo 1,340 kg                NO se cuenta: se pesa
	///   4. Por paquete        soda lata 12-pack             se cuenta, y trae piezas dentro
	///   5. A granel           arroz servido en caja         identificada, sin construir
	///
	/// ALCANCE. Están IMPLEMENTADAS la 1, la 2 y la 4. La 3 y la 5 quedan IDENTIFICADAS y
	/// sin construir a propósito: exigen que la cantidad de una línea sea decimal, y los
	/// journals ya escritos la tienen entera. Que estén nombradas acá y no construidas es
	/// lo que hace que el modelo se lea rico sin inflar el alcance ni romper el replay.
	///
	/// Lo que la raíz declara es deliberadamente POCO: cómo se dice una cantidad y cuánto
	/// da en la unidad base. No declara "cuántas piezas", porque un peso variable no tiene
	/// piezas; no declara "contenido", porque una unidad suelta no lo tiene. Cada capacidad
	/// aparece en la rama que de verdad la tiene.
	///
	/// No es sealed: existe para que se herede (las hojas sí se sellan).
	internal abstract class Presentacion
	{
		/// <summary>La forma en que se vende, ya dicha: "unidad", "1 L", "12 u por paquete".</summary>
		internal abstract string Expresada { get; }

		/// <summary>La unidad en la que se expresa el total: u, kg o L.</summary>
		internal abstract UnidadDeVenta UnidadBase { get; }

		/// <summary>
		/// Cuánto entró a la canasta, en la unidad base. Seis leches de 1 L son 6 L; dos
		/// mantecas de 500 g son 1 kg; dos 12-packs son 24 u.
		/// </summary>
		internal abstract decimal TotalEnBase(int cantidad);

		/// <summary>El resolver de la frontera. Formas: "u", "1 L", "500 g", "12x u".</summary>
		// El wire manda texto porque una presentacion no es un numero: se resuelve UNA vez
		// aca y de este punto para adentro el dominio trabaja con la instancia.
		internal static Presentacion Desde(string texto)
		{
			ArgumentNullException.ThrowIfNull(texto);

			string limpio = texto.Trim();
			if (limpio.Length == 0 || string.Equals(limpio, "u", StringComparison.OrdinalIgnoreCase))
				return PorUnidad;

			// "12x u" es un paquete: doce piezas adentro de un empaque.
			int equis = limpio.IndexOf('x');
			if (equis > 0)
			{
				string cuantas = limpio.Substring(0, equis).Trim();
				if (int.TryParse(cuantas, NumberStyles.Integer, CultureInfo.InvariantCulture, out int piezas))
					return DePaquete(piezas);
			}

			// "1 L", "500 g": un contenido y su unidad.
			int espacio = limpio.IndexOf(' ');
			if (espacio > 0)
			{
				string valor = limpio.Substring(0, espacio).Trim();
				string simbolo = limpio.Substring(espacio + 1).Trim();
				if (decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal contenido)
					&& UnidadDeVenta.Existe(simbolo))
					return DeContenido(contenido, UnidadDeVenta.PorSimbolo(simbolo));
			}

			throw new ArgumentException(
				$"'{texto}' no es una presentacion conocida. Las formas validas son 'u', "
				+ "'1 L', '500 g' o '12x u'.", nameof(texto));
		}

		/// <summary>Reconocimiento total: la pareja de Desde.</summary>
		internal static bool Existe(string texto)
		{
			if (texto is null) return false;
			try { Desde(texto); return true; } catch (ArgumentException) { return false; }
		}

		// --------------------------------------------------------------- las tres formas

		/// <summary>Forma 1 — se cuenta y nada más. Es la que porta lo que no declara otra.</summary>
		internal static readonly Presentacion PorUnidad = new Suelta();

		/// <summary>Forma 2 — cada pieza trae un contenido medible.</summary>
		internal static Presentacion DeContenido(decimal contenido, UnidadDeVenta unidad) =>
			new ConContenido(contenido, unidad);

		/// <summary>Forma 4 — cada empaque trae varias piezas contables.</summary>
		internal static Presentacion DePaquete(int piezasPorEmpaque) => new EnPaquete(piezasPorEmpaque);

		private sealed class Suelta : Presentacion
		{
			internal override string Expresada => "unidad";
			internal override UnidadDeVenta UnidadBase => UnidadDeVenta.Unidad;

			internal override decimal TotalEnBase(int cantidad) => Positiva(cantidad);
		}

		private sealed class ConContenido : Presentacion
		{
			private readonly decimal contenido;
			private readonly UnidadDeVenta unidad;

			internal ConContenido(decimal contenido, UnidadDeVenta unidad)
			{
				ArgumentNullException.ThrowIfNull(unidad);
				if (contenido <= 0m)
					throw new ArgumentOutOfRangeException(nameof(contenido), contenido,
						"Una pieza con contenido cero o negativo no se vende.");

				this.contenido = contenido;
				this.unidad = unidad;
			}

			internal override string Expresada =>
				$"{contenido.ToString("0.###", CultureInfo.InvariantCulture)} {unidad.Simbolo}";

			internal override UnidadDeVenta UnidadBase => unidad.Base;

			internal override decimal TotalEnBase(int cantidad) =>
				unidad.ALaBase(contenido * Positiva(cantidad));
		}

		private sealed class EnPaquete : Presentacion
		{
			private readonly int piezas;

			internal EnPaquete(int piezasPorEmpaque)
			{
				if (piezasPorEmpaque <= 1)
					throw new ArgumentOutOfRangeException(nameof(piezasPorEmpaque), piezasPorEmpaque,
						"Un paquete de una pieza o menos no es un paquete; eso es Presentacion.PorUnidad.");

				piezas = piezasPorEmpaque;
			}

			internal override string Expresada => $"{piezas} u por paquete";
			internal override UnidadDeVenta UnidadBase => UnidadDeVenta.Unidad;

			internal override decimal TotalEnBase(int cantidad) => piezas * Positiva(cantidad);
		}

		private static int Positiva(int cantidad)
		{
			if (cantidad <= 0)
				throw new ArgumentOutOfRangeException(nameof(cantidad), cantidad,
					"Una linea con cantidad cero o negativa no es una linea.");
			return cantidad;
		}
	}
}
