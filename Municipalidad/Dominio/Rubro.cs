using System;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// Que cosa se cobra. Es el CATALOGO: cinco instancias y no cinco clases, porque un rubro
	/// PARAMETRIZA una misma operacion de cobro — es mensual o trimestral, por unidad o sobre base
	/// imponible — y no opina por si mismo. Ninguno sabe nada que una fila del catalogo con su nombre,
	/// su cadencia y su ley no sepa. Lo que si opina son la cadencia y la ley de calculo, y por eso esas
	/// dos son clases.
	/// <para>
	/// SOLO LOS CINCO QUE LOS RECIBOS MOSTRARON. El menu del portal lista otros de los que no se sabe
	/// cadencia ni ley, y no se inventan: un rubro desconocido revienta en el resolutor, y el Check lo
	/// rechaza antes de que llegue a un comando.
	/// </para>
	/// <para>
	/// Comparado por nombre, con resolutor total desde el primitivo. Los miembros del catalogo son
	/// metodos estaticos y no campos, porque el DSL alcanza un metodo y no un campo estatico, y cada
	/// uno entrega UNA instancia compartida en vez de volver a armarla.
	/// </para>
	/// </summary>
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

		private static readonly Rubro basura =
			new Rubro("recoleccion-de-basura", Cadencia.Mensual(), LeyDeCalculo.PorUnidad());
		private static readonly Rubro limpiezaDeCalles =
			new Rubro("limpieza-de-calles", Cadencia.Mensual(), LeyDeCalculo.SobreBaseImponible());
		private static readonly Rubro policia =
			new Rubro("servicio-policia", Cadencia.Mensual(), LeyDeCalculo.PorUnidad());
		private static readonly Rubro bienesInmuebles =
			new Rubro("bienes-inmuebles", Cadencia.Trimestral(), LeyDeCalculo.SobreBaseImponible());
		private static readonly Rubro parques =
			new Rubro("parques-y-zonas-verdes", Cadencia.Trimestral(), LeyDeCalculo.SobreBaseImponible());

		internal static Rubro Basura()
		{
			return basura;
		}

		internal static Rubro LimpiezaDeCalles()
		{
			return limpiezaDeCalles;
		}

		internal static Rubro Policia()
		{
			return policia;
		}

		internal static Rubro BienesInmuebles()
		{
			return bienesInmuebles;
		}

		internal static Rubro Parques()
		{
			return parques;
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
						+ "servicio-policia, bienes-inmuebles, parques-y-zonas-verdes. El portal cobra otros de los "
						+ "que no se sabe cadencia ni ley, y no se inventan.");
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

		/// <summary>
		/// La lectura del periodo pasa por el rubro, porque es su cadencia la que sabe si el numero es un
		/// mes o un trimestre. Es por donde el periodo del portal entra al dominio ya interpretado.
		/// </summary>
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
