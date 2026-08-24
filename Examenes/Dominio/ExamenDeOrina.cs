using System;

namespace Examenes.Dominio
{
	// Examen general de orina. NO tiene Medicion ni unidad ni rango de referencia.
	//
	// Es la prueba de que la jerarquia carga verdad del dominio. Esta clase no puede
	// contestar "cuanto medis" ni "veni subiendo", porque un examen de orina no tiene
	// esas preguntas; y ExamenDeSangre no puede contestar "de que color", porque la
	// sangre no tiene color en este sentido. Si Examen hubiera declarado un decimal
	// Valor, uno de los dos habria tenido que devolver cero y mentir.
	//
	// Y es tambien el examen que informa DOS FORMAS DE MAGNITUD a la vez:
	//
	//   el color     -> MagnitudNominal   (forma 6: no se ordena, no tiene serie)
	//   los nitritos -> MagnitudBinaria   (forma 5: presencia, sin escala, con un
	//                                      tercer estado que no es negativo)
	//
	// Un solo examen, dos formas, y ninguna de las dos es un numero. Es el argumento de
	// la taxonomia de Magnitud en la clase mas corta del dominio.
	public sealed class ExamenDeOrina : Examen
	{
		public ColorDeOrina Color { get; private set; }

		public Nitritos Nitritos { get; private set; }

		// El constructor de DOS argumentos existe y se queda: los journals que ya se
		// escribieron tienen literalmente "ExamenDeOrina(@fecha, @color)" adentro, y el
		// replay los reconstruye llamando a ESA firma. Borrarla no seria una refactor,
		// seria romper expedientes ya escritos. Un examen viejo simplemente no midio
		// nitritos, que es exactamente lo que NoRealizado significa.
		public ExamenDeOrina(DateTime fecha, string color)
			: this(fecha, color, null)
		{
		}

		public ExamenDeOrina(DateTime fecha, string color, string nitritos) : base(fecha)
		{
			Color = ColorDeOrina.PorNombre(color);
			Nitritos = Nitritos.PorNombre(nitritos);
		}

		public override string Nombre => "Examen general de orina";

		// No aporta serie: ni un color ni un binario se evaluan con las reglas de
		// velocidad de cambio que usan los analitos cuantitativos. No es que "todavia no
		// este implementado" - es que la pregunta no existe.
		public override bool AportaSerie => false;

		public override string ResultadoExpresado => Nitritos.FueRealizada
			? $"{Color.Expresado}, nitritos {Nitritos.Expresado}"
			: Color.Expresado;

		public bool ColorEsEsperable => Color.EsEsperable;

		// Si este examen encontro algo que merece mirarse. Lo contesta preguntandole a
		// cada magnitud por SU propia anormalidad, no comparando strings afuera: un color
		// oscuro es un hallazgo, y unos nitritos positivos tambien, por razones distintas
		// que cada clase conoce.
		public bool TieneHallazgo => !Color.EsEsperable || Nitritos.EsHallazgo;
	}
}
