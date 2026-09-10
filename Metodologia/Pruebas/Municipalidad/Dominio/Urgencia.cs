using System;

namespace Municipalidad.Dominio
{
	// CUANTO APURA UN HALLAZGO. Tres niveles y nada mas: ninguna, aviso, urgente.
	//
	// Juicio M4, propuesto: LA CLASE DECIDE, NO EL MONTO. Un aumento no es urgente -ya
	// paso y no se puede evitar-; lo que vence si: aviso siete dias antes, urgente el dia
	// (M5). Los dias entran por parametro; aca solo viven los tres niveles.
	//
	// CONJUNTO CERRADO, comparado por nombre, con Etiqueta como hoja para el actor
	// (regla 13, corregida): la superficie decide que imprime, el tipo expone el nombre.
	internal sealed class Urgencia
	{
		private readonly string nombre;
		private readonly int nivel;

		private Urgencia(string nombre, int nivel)
		{
			this.nombre = nombre;
			this.nivel = nivel;
		}

		internal static Urgencia Ninguna() { return new Urgencia("ninguna", 0); }
		internal static Urgencia Aviso() { return new Urgencia("aviso", 1); }
		internal static Urgencia Urgente() { return new Urgencia("urgente", 2); }

		internal static Urgencia PorNombre(string nombre)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
			switch (nombre.Trim().ToLowerInvariant())
			{
				case "ninguna": return Ninguna();
				case "aviso": return Aviso();
				case "urgente": return Urgente();
				default:
					throw new ArgumentOutOfRangeException(nameof(nombre), nombre,
						"Urgencia desconocida. Las que existen son: ninguna, aviso, urgente.");
			}
		}

		internal bool Es(Urgencia otra) { return otra != null && otra.nombre == nombre; }
		internal bool Avisa() { return nivel > 0; }
		internal bool EsUrgente() { return nivel == 2; }

		internal string Etiqueta { get { return nombre; } }

		public override string ToString() { return nombre; }
	}
}
