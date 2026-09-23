using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// CUANDO. Es la mitad de la accion: "anota el kilometraje" sin un cuando es el
	/// consejo de plantilla que este producto no da. Cerrado y ordenado, comparado por
	/// referencia como Severidad.
	/// <para>
	/// Los plazos concretos son criterio propio para el demo, como los umbrales.
	/// </para>
	/// </summary>
	internal sealed class Plazo
	{
		/// <summary>El momento natural de este negocio: la proxima vez que cargue.</summary>
		internal static readonly Plazo EnTuProximaCarga = new Plazo("proxima-carga", 1);

		internal static readonly Plazo EsteMes = new Plazo("este-mes", 2);

		internal static readonly Plazo EstaSemana = new Plazo("esta-semana", 3);

		internal static readonly Plazo Pronto = new Plazo("pronto", 4);

		/// <summary>El plazo que no apura nada: pesa cero.</summary>
		internal static readonly Plazo CuandoQueras = new Plazo("cuando-queras", 0);

		/// <summary>
		/// EL CODIGO de este plazo: identidad, y lo unico que cruza el wire. Como se le dice a la
		/// persona lo busca con este codigo la capa que redacta, en el idioma que le pidieron.
		/// </summary>
		internal string Clave { get; private set; }

		private readonly int peso;

		private Plazo(string clave, int peso)
		{
			Clave = clave;
			this.peso = peso;
		}

		internal bool EsMasApuradoQue(Plazo otro)
		{
			ArgumentNullException.ThrowIfNull(otro);
			return peso > otro.peso;
		}
	}
}
