using System;
using System.Collections.Generic;

namespace Compras.Dominio
{
	/// <summary>
	/// EN QUÉ SUCURSALES compra la persona, y desde cuándo se mueve: la de siempre, las
	/// demás, y el mes en que apareció la primera distinta.
	/// </summary>
	// LA PROPUESTA DE GEANNINA, entera: "antes compraba siempre en la misma; desde enero
	// aparece en tres". Sonia supervisa tiendas desde enero, y el super puede corroborar
	// ese arco por si solo — desde la fuente que nadie esperaria.
	//
	// LAS COMPRAS SIN SUCURSAL QUEDAN AFUERA del conteo y se dicen aparte: los journals
	// escritos antes de que el campo existiera no dicen donde fueron, y contarlas como
	// "otra sucursal" o como "la de siempre" seria inventar un dato que el papel no trae.
	internal class RecorridoDeSucursales
	{
		internal static RecorridoDeSucursales SinDatos(int comprasSinSucursal) =>
			new RecorridoDeSucursales(Array.Empty<SucursalFrecuentada>(), comprasSinSucursal);

		internal static RecorridoDeSucursales De(IReadOnlyList<SucursalFrecuentada> sucursales,
			int comprasSinSucursal) =>
			new RecorridoDeSucursales(sucursales, comprasSinSucursal);

		private RecorridoDeSucursales(IReadOnlyList<SucursalFrecuentada> sucursales, int comprasSinSucursal)
		{
			ArgumentNullException.ThrowIfNull(sucursales);
			Sucursales = sucursales;
			ComprasSinSucursal = comprasSinSucursal;
		}

		/// <summary>Consulta total: si alguna compra dice en qué sucursal fue.</summary>
		internal bool HayDatos => Sucursales.Count > 0;

		/// <summary>La más frecuentada primero; a igual cantidad, la conocida primero.</summary>
		internal IReadOnlyList<SucursalFrecuentada> Sucursales { get; private set; }

		internal int CuantasDistintas => Sucursales.Count;

		/// <summary>Cuántas compras no dicen sucursal (las de antes de que el campo viajara).</summary>
		internal int ComprasSinSucursal { get; private set; }

		/// <summary>La más frecuentada. Preguntar HayDatos primero.</summary>
		internal SucursalFrecuentada LaDeSiempre =>
			HayDatos
				? Sucursales[0]
				: throw new InvalidOperationException(
					"Ninguna compra dice sucursal: no hay una 'de siempre'. Preguntar HayDatos antes.");

		/// <summary>
		/// Desde cuándo se mueve: el mes de la primera compra en una sucursal DISTINTA a
		/// la de siempre, como aaaa-mm. Vacío si nunca salió de una — pareja total de
		/// SeMueve.
		/// </summary>
		internal string SeMueveDesdeExpresado
		{
			get
			{
				if (!SeMueve) return "";

				DateTime primera = DateTime.MaxValue;
				for (int i = 1; i < Sucursales.Count; i++)
					if (Sucursales[i].PrimeraVez < primera) primera = Sucursales[i].PrimeraVez;

				return $"{primera.Year:0000}-{primera.Month:00}";
			}
		}

		internal bool SeMueve => Sucursales.Count > 1;

		/// <summary>
		/// QUE FRASE dice este recorrido, por su nombre: `sin-datos`, `una-sola` o `se-mueve`. Los
		/// nombres de las sucursales son del registro del super y ya se publican en Sucursales, asi que
		/// esta lectura no los repite.
		/// </summary>
		internal string Clave => !HayDatos ? "sin-datos" : !SeMueve ? "una-sola" : "se-mueve";

		/// <summary>Cuantas compras SI dicen sucursal. Cero cuando ninguna lo dice: lectura total.</summary>
		internal int ComprasConSucursal
		{
			get
			{
				int total = 0;
				foreach (SucursalFrecuentada sucursal in Sucursales) total += sucursal.Compras;
				return total;
			}
		}

		/// <summary>El nombre de la de siempre. Vacio cuando ninguna compra dice sucursal: lectura total.</summary>
		internal string NombreDeLaDeSiempre => HayDatos ? Sucursales[0].Nombre : "";

		/// <summary>Cuantas compras fueron en la de siempre. Cero cuando no hay ninguna: lectura total.</summary>
		internal int ComprasEnLaDeSiempre => HayDatos ? Sucursales[0].Compras : 0;
	}

	/// <summary>Una sucursal y su rastro: cuántas compras, la primera y la última vez.</summary>
	internal class SucursalFrecuentada
	{
		internal string Nombre { get; private set; }
		internal int Compras { get; private set; }
		internal DateTime PrimeraVez { get; private set; }
		internal DateTime UltimaVez { get; private set; }

		internal SucursalFrecuentada(string nombre, int compras, DateTime primeraVez, DateTime ultimaVez)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);
			if (compras <= 0)
				throw new ArgumentOutOfRangeException(nameof(compras), compras,
					"Una sucursal sin compras no se frecuenta.");

			Nombre = nombre;
			Compras = compras;
			PrimeraVez = primeraVez;
			UltimaVez = ultimaVez;
		}
	}
}
