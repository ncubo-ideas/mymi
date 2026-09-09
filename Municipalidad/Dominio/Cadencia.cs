using System;

namespace Municipalidad.Dominio
{
	// CADA CUANTO SE COBRA UN RUBRO, Y COMO SE LEE SU PERIODO.
	//
	// Es una clase y no un `int meses` porque OPINA -pasada 4 de la guia-: la mensual y
	// la trimestral leen el numero del periodo distinto (el 4 es abril o es el cuarto
	// trimestre), vencen distinto, y cuentan distinto cuantos periodos cubre un pago.
	// Tres reglas, no tres constantes. Es la fila de la pasada 4 que mas claramente
	// merece clase, y la que un modelo espejo del recibo jamas tendria: el recibo no
	// dice la cadencia, se deduce de seis documentos.
	//
	// CONJUNTO CERRADO de dos, con resolutor total desde primitivo (regla 11), y
	// comparado por nombre.
	internal abstract class Cadencia
	{
		internal abstract string Nombre { get; }
		internal abstract int PeriodosPorAnio { get; }

		// LA LECTURA DEL PERIODO es lo propio de cada cadencia. El texto "2026.04" se parte
		// en la API; aca llega (2026, 4) y la cadencia dice que es.
		internal abstract Periodo Leer(int anio, int numero);

		// El predicado total que acompana a Leer: es lo que un Check puede preguntar antes
		// de escribir, en vez de reventar a mitad del comando (regla 17).
		internal abstract bool PuedeLeer(int anio, int numero);

		internal bool Es(Cadencia otra)
		{
			return otra != null && otra.Nombre == Nombre;
		}

		internal bool EsMensual() { return Nombre == "mensual"; }
		internal bool EsTrimestral() { return Nombre == "trimestral"; }

		// ---------------------------------------------------------------- el resolutor

		internal static Cadencia Mensual() { return new CobroMensual(); }
		internal static Cadencia Trimestral() { return new CobroTrimestral(); }

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
