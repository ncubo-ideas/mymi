using System;

namespace Municipalidad.Dominio
{
	// QUE COSA SE COBRA. Es el CATALOGO: cinco instancias, no cinco clases.
	//
	// Es la decision mas visible de la pasada 4, y la frontera de la guia la fija: un
	// rubro PARAMETRIZA -es mensual o trimestral, por unidad o sobre base imponible- una
	// misma operacion de cobro; no opina por si mismo. Basura no sabe nada que una fila
	// del catalogo con su nombre, su cadencia y su ley no sepa. Lo que si opina son la
	// Cadencia y la LeyDeCalculo, y por eso esas dos son clases. Diez clases de rubro
	// seria el error que la barra de activo permite solo cuando cada tipo juzga algo.
	//
	// SOLO LOS CINCO QUE LOS RECIBOS MOSTRARON. El menu del portal lista otros cinco
	// -cementerio, patentes, licores, arreglo de pago, permiso- de los que no se sabe
	// cadencia ni ley. No se inventan: un rubro desconocido revienta en el resolutor y el
	// Check lo rechaza antes. Es el hueco 9, con dueno.
	//
	// Comparado por nombre, con resolutor total desde primitivo (regla 11). Los
	// miembros son metodos estaticos y no `static readonly` porque el DSL no alcanza un
	// campo estatico y si un metodo (regla 11).
	internal sealed class Rubro
	{
		internal string Nombre { get; }
		internal Cadencia Cadencia { get; }
		internal LeyDeCalculo Ley { get; }

		private Rubro(string nombre, Cadencia cadencia, LeyDeCalculo ley)
		{
			Nombre = nombre;
			Cadencia = cadencia;
			Ley = ley;
		}

		internal static Rubro Basura()
		{
			return new Rubro("recoleccion-de-basura", Cadencia.Mensual(), LeyDeCalculo.PorUnidad());
		}

		internal static Rubro LimpiezaDeCalles()
		{
			return new Rubro("limpieza-de-calles", Cadencia.Mensual(), LeyDeCalculo.SobreBaseImponible());
		}

		internal static Rubro Policia()
		{
			return new Rubro("servicio-policia", Cadencia.Mensual(), LeyDeCalculo.PorUnidad());
		}

		internal static Rubro BienesInmuebles()
		{
			return new Rubro("bienes-inmuebles", Cadencia.Trimestral(), LeyDeCalculo.SobreBaseImponible());
		}

		internal static Rubro Parques()
		{
			return new Rubro("parques-y-zonas-verdes", Cadencia.Trimestral(), LeyDeCalculo.SobreBaseImponible());
		}

		internal static Rubro PorNombre(string nombre)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
			switch (nombre.Trim().ToLowerInvariant())
			{
				case "recoleccion-de-basura": return Basura();
				case "limpieza-de-calles": return LimpiezaDeCalles();
				case "servicio-policia": return Policia();
				case "bienes-inmuebles": return BienesInmuebles();
				case "parques-y-zonas-verdes": return Parques();
				default:
					throw new ArgumentOutOfRangeException(nameof(nombre), nombre,
						"Rubro desconocido. Los que existen son: recoleccion-de-basura, limpieza-de-calles, "
						+ "servicio-policia, bienes-inmuebles, parques-y-zonas-verdes. Ver el hueco 9.");
			}
		}

		/// <summary>
		/// El nombre de cada rubro que el catalogo tiene, uno por fila. Sale de las cinco fabricas, no
		/// de una sexta lista que pudiera quedar desalineada con ellas.
		/// </summary>
		internal static string[] LosQueHay()
		{
			return new[]
			{
				Basura().Nombre,
				LimpiezaDeCalles().Nombre,
				Policia().Nombre,
				BienesInmuebles().Nombre,
				Parques().Nombre,
			};
		}

		internal static bool EsUnNombreConocido(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return false;
			switch (nombre.Trim().ToLowerInvariant())
			{
				case "recoleccion-de-basura":
				case "limpieza-de-calles":
				case "servicio-policia":
				case "bienes-inmuebles":
				case "parques-y-zonas-verdes": return true;
				default: return false;
			}
		}

		internal bool Es(Rubro otro)
		{
			return otro != null && otro.Nombre == Nombre;
		}

		// LA LECTURA DEL PERIODO PASA POR EL RUBRO, porque es su cadencia la que sabe si el
		// numero es un mes o un trimestre. Es la puerta por la que el "2026.04" del portal
		// entra al dominio ya interpretado.
		internal Periodo LeerPeriodo(int anio, int numero)
		{
			return Cadencia.Leer(anio, numero);
		}

		internal bool PuedeLeerPeriodo(int anio, int numero)
		{
			return Cadencia.PuedeLeer(anio, numero);
		}
	}
}
