using System;
using System.Collections.Generic;

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
	//   la sangre    -> MagnitudBinaria   (forma 5 tambien, y NO se lee igual que los
	//                                      nitritos: ver SangreEnOrina)
	//
	// Un solo examen, tres magnitudes, y ninguna es un numero. Que DOS de ellas sean la
	// misma forma y pidan acciones distintas es el argumento de Alvaro en una sola clase:
	// si la severidad se dedujera de "es positivo", estos dos darian lo mismo.
	public sealed class ExamenDeOrina : Examen
	{
		public ColorDeOrina Color { get; private set; }

		public Nitritos Nitritos { get; private set; }

		public SangreEnOrina Sangre { get; private set; }

		// El constructor de DOS argumentos existe y se queda: los journals que ya se
		// escribieron tienen literalmente "ExamenDeOrina(@fecha, @color)" adentro, y el
		// replay los reconstruye llamando a ESA firma. Borrarla no seria una refactor,
		// seria romper expedientes ya escritos. Un examen viejo simplemente no midio
		// nitritos, que es exactamente lo que NoRealizado significa.
		public ExamenDeOrina(DateTime fecha, string color)
			: this(fecha, color, null, null)
		{
		}

		// Misma razon que la de dos argumentos: hay journals escritos con
		// "ExamenDeOrina(@fecha, @color, @nitritos)" y el replay llama a ESA firma. Cada
		// vez que el examen aprende a informar algo nuevo se agrega una sobrecarga; no se
		// cambia una que ya se escribio en el registro de alguien.
		public ExamenDeOrina(DateTime fecha, string color, string nitritos)
			: this(fecha, color, nitritos, null)
		{
		}

		public ExamenDeOrina(DateTime fecha, string color, string nitritos, string sangre)
			: base(fecha)
		{
			Color = ColorDeOrina.PorNombre(color);
			Nitritos = Nitritos.PorNombre(nitritos);
			Sangre = SangreEnOrina.PorNombre(sangre);
		}

		public override string Nombre => "Examen general de orina";

		// No aporta serie: ni un color ni un binario se evaluan con las reglas de
		// velocidad de cambio que usan los analitos cuantitativos. No es que "todavia no
		// este implementado" - es que la pregunta no existe.
		public override bool AportaSerie => false;

		public override string ResultadoExpresado
		{
			get
			{
				string texto = Color.Expresado;
				if (Nitritos.FueRealizada) texto += $", nitritos {Nitritos.Expresado}";
				if (Sangre.FueRealizada) texto += $", sangre {Sangre.Expresado}";
				return texto;
			}
		}

		// TRES COSAS MEDIDAS, y cada una es un analito. Las que el laboratorio no corrio no
		// aparecen: una prueba no realizada no es una lectura vacia, es una lectura que no
		// existe. Es el mismo criterio que la celda vacia del historial.
		public override IReadOnlyList<LecturaDeExamen> Lecturas
		{
			get
			{
				var lecturas = new List<LecturaDeExamen>
				{
					new LecturaDeExamen(Examenes.Dominio.Analito.ColorDeLaOrina, Color),
				};

				if (Nitritos.FueRealizada)
					lecturas.Add(new LecturaDeExamen(
						Examenes.Dominio.Analito.NitritosEnOrina, Nitritos));

				if (Sangre.FueRealizada)
					lecturas.Add(new LecturaDeExamen(
						Examenes.Dominio.Analito.SangreEnLaOrina, Sangre));

				return lecturas;
			}
		}

		public bool ColorEsEsperable => Color.EsEsperable;

		// Si este examen encontro algo que merece mirarse. Lo contesta preguntandole a
		// cada magnitud por SU propia anormalidad, no comparando strings afuera: un color
		// oscuro es un hallazgo, y unos nitritos positivos tambien, por razones distintas
		// que cada clase conoce.
		public bool TieneHallazgo => !Color.EsEsperable || Nitritos.EsHallazgo || Sangre.EsHallazgo;

		// QUE HACER con este examen: lo mas urgente que digan sus magnitudes. Cada una
		// aporta su propia severidad, y la del examen es la mayor.
		//
		// Aca se ve por que la severidad no puede deducirse de "hay hallazgo": los
		// nitritos positivos y la sangre positiva son los dos hallazgos binarios de ESTA
		// misma tira, y no piden lo mismo.
		public Severidad SeveridadDelExamen
		{
			get
			{
				Severidad mayor = Color.EsEsperable ? Severidad.Informativo : Severidad.Vigilar;
				mayor = Severidad.LaMayor(mayor, Nitritos.SeveridadDelHallazgo);
				return Severidad.LaMayor(mayor, Sangre.SeveridadDelHallazgo);
			}
		}

		// La lectura, armada con lo que dice cada magnitud de si misma. La pantalla no
		// compone nada: pide esto.
		public string ParaSonia
		{
			get
			{
				if (Sangre.EsHallazgo) return Sangre.ParaSonia;
				if (Nitritos.EsHallazgo) return Nitritos.ParaSonia;
				if (!Color.EsEsperable) return $"el color de la orina salió {Color.Expresado}";
				return "sin hallazgos";
			}
		}
	}
}
