using System;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// Cada cuanto se cobra un rubro, y como se lee el numero de su periodo. Es una clase y no un numero
	/// de meses porque las dos cadencias leen ese numero distinto, vencen distinto y cuentan distinto
	/// cuantos periodos cubre un pago: tres reglas, no tres constantes.
	/// <para>
	/// El recibo no dice la cadencia de un rubro: no viene en ningun campo.
	/// </para>
	/// <para>
	/// Conjunto cerrado de dos, comparado por nombre, con resolutor total desde el primitivo. Cada
	/// miembro es UNA instancia compartida que su metodo entrega sin volver a armarla.
	/// </para>
	/// </summary>
	internal abstract class Cadencia
	{
		internal abstract string Nombre { get; }
		internal abstract int PeriodosPorAnio { get; }

		/// <summary>
		/// La lectura del periodo, que es lo propio de cada cadencia: recibe el anio y el numero ya
		/// separados, y dice que periodo son.
		/// </summary>
		internal abstract Periodo Leer(int anio, int numero);

		/// <summary>
		/// El predicado total que acompana a Leer: es lo que un Check puede preguntar antes de escribir,
		/// en vez de reventar a mitad del comando.
		/// </summary>
		internal abstract bool PuedeLeer(int anio, int numero);

		internal bool Es(Cadencia otra)
		{
			return otra != null && otra.Nombre == Nombre;
		}

		internal bool EsMensual() { return Nombre == "mensual"; }
		internal bool EsTrimestral() { return Nombre == "trimestral"; }

		private static readonly Cadencia mensual = new CobroMensual();
		private static readonly Cadencia trimestral = new CobroTrimestral();

		internal static Cadencia Mensual() { return mensual; }
		internal static Cadencia Trimestral() { return trimestral; }

		internal static Cadencia PorNombre(string nombre)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
			switch (nombre.Trim().ToLowerInvariant())
			{
				case "mensual": return Mensual();
				case "trimestral": return Trimestral();
				default:
					throw new ArgumentOutOfRangeException(nameof(nombre), nombre,
						"Cadencia desconocida. Las que existen son: mensual, trimestral.");
			}
		}

		internal static bool EsUnNombreConocido(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return false;
			switch (nombre.Trim().ToLowerInvariant())
			{
				case "mensual":
				case "trimestral": return true;
				default: return false;
			}
		}
	}
}
