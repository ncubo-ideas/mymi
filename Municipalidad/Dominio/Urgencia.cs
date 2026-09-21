using System;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// Cuanto apura un hallazgo: tres niveles y nada mas, ninguna, aviso y urgente.
	/// <para>
	/// LA CLASE DECIDE, NO EL MONTO. Un aumento no es urgente, porque ya paso y no se puede evitar; lo
	/// que vence si. Los dias de aviso entran por parametro y aca solo viven los tres niveles.
	/// </para>
	/// <para>
	/// Conjunto cerrado, comparado por nombre, con Etiqueta como hoja para el actor: la superficie
	/// decide que imprime y el tipo expone el nombre. Cada miembro es UNA instancia compartida que su
	/// metodo entrega sin volver a armarla.
	/// </para>
	/// </summary>
	internal sealed class Urgencia
	{
		private readonly string nombre;
		private readonly int nivel;

		private Urgencia(string nombre, int nivel)
		{
			this.nombre = nombre;
			this.nivel = nivel;
		}

		private static readonly Urgencia ninguna = new Urgencia("ninguna", 0);
		private static readonly Urgencia aviso = new Urgencia("aviso", 1);
		private static readonly Urgencia urgente = new Urgencia("urgente", 2);

		internal static Urgencia Ninguna() { return ninguna; }
		internal static Urgencia Aviso() { return aviso; }
		internal static Urgencia Urgente() { return urgente; }

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
