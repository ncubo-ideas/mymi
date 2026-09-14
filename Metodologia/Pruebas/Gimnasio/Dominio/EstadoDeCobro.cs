using System;

namespace Gimnasio.Dominio
{
	// EL ESTADO DE UN COBRO, con las palabras del gimnasio: "Abierto" -el papel lo
	// mostro- y "Pagado" -Jorge lo dio de palabra el 2026-09-08, hueco 18-. "Vencido" es
	// plausible y no se vio: no se inventa.
	//
	// ES UNA FASE, NO UN TIPO: un cobro abierto pasa a pagado sin dejar de ser el mismo
	// cobro. Por eso es un campo y no una subclase (E36; glosario, rigidez), y la
	// transicion vive en Cobro.CambiarEstado con la regla 18: el mismo estado otra vez es
	// el mismo hecho otra vez.
	//
	// Y LO QUE ESTE ESTADO NO HACE: un cobro abierto despues de su vencimiento NO se
	// convierte en mora. La mora la dice el gimnasio con la bandera de la membresia (N1).
	internal sealed class EstadoDeCobro
	{
		private readonly string nombre;

		private EstadoDeCobro(string nombre) { this.nombre = nombre; }

		internal static EstadoDeCobro Abierto() { return new EstadoDeCobro("abierto"); }
		internal static EstadoDeCobro Pagado() { return new EstadoDeCobro("pagado"); }

		internal static EstadoDeCobro PorNombre(string nombre)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
			switch (nombre.Trim().ToLowerInvariant())
			{
				case "abierto": return Abierto();
				case "pagado": return Pagado();
				default:
					throw new ArgumentOutOfRangeException(nameof(nombre), nombre,
						"Estado de cobro desconocido. Los que existen son: abierto, pagado. Si el gimnasio dijo "
						+ "otro, hay que verlo.");
			}
		}

		internal static bool EsUnNombreConocido(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return false;
			string n = nombre.Trim().ToLowerInvariant();
			return n == "abierto" || n == "pagado";
		}

		internal bool Es(EstadoDeCobro otro)
		{
			return otro != null && otro.nombre == nombre;
		}

		internal bool EstaAbierto() { return nombre == "abierto"; }
		internal bool EstaPagado() { return nombre == "pagado"; }

		internal string Etiqueta { get { return nombre; } }

		public override string ToString() { return nombre; }
	}
}
