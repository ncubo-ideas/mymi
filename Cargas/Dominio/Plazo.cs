using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// CUANDO. Es la mitad de la accion: "anota el kilometraje" sin un cuando es el
	/// consejo de plantilla que este producto no da. Cerrado y ordenado, comparado por
	/// referencia como Severidad.
	/// </summary>
	// ⚠️ Los plazos concretos son criterio propio para el demo, como los umbrales.
	internal sealed class Plazo
	{
		/// <summary>El momento natural de este negocio: la proxima vez que cargue.</summary>
		internal static readonly Plazo EnTuProximaCarga =
			new Plazo("en tu proxima carga", "en tu próxima carga", 1);

		internal static readonly Plazo EsteMes = new Plazo("este mes", "este mes", 2);

		internal static readonly Plazo EstaSemana = new Plazo("esta semana", "esta semana", 3);

		internal static readonly Plazo Pronto = new Plazo("pronto", "pronto", 4);

		/// <summary>El plazo que no apura nada: pesa cero.</summary>
		internal static readonly Plazo CuandoQueras =
			new Plazo("cuando queras", "cuando querás", 0);

		// La etiqueta interna, sin acentos: para comparar y serializar.
		internal string Nombre { get; private set; }

		// El plazo como se le dice a la persona, con acentos, listo para encadenar
		// despues del que: "anotá el kilometraje" + "en tu próxima carga".
		internal string ParaMostrar { get; private set; }

		private readonly int peso;

		private Plazo(string nombre, string paraMostrar, int peso)
		{
			Nombre = nombre;
			ParaMostrar = paraMostrar;
			this.peso = peso;
		}

		internal bool EsMasApuradoQue(Plazo otro)
		{
			ArgumentNullException.ThrowIfNull(otro);
			return peso > otro.peso;
		}
	}
}
