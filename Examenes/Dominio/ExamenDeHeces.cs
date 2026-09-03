using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// Examen general de heces. La TERCERA muestra del dominio, despues de sangre y orina.
	//
	// POR QUE EXISTE, y no es por completitud. El feedback de Alvaro pide que dos hallazgos
	// de la misma forma produzcan acciones distintas, y su ejemplo son las dos sangres:
	//
	//   "Sangre en las heces ⟹ quizas estrenimiento. Sangre en la orina ⟹ quizas CORRA
	//    donde el medico, rapido."
	//
	// Ese contraste no se puede montar dentro del urianalisis, porque la sangre en heces
	// NO es un campo mas de la orina: es otra muestra. Meterla ahi habria sido exactamente
	// la aplanadora que el feedback rechaza —dos cosas distintas en la misma fila porque
	// entran en las mismas columnas.
	//
	// Con dos examenes separados, cada uno con su binario, la comparacion es real: mismo
	// tipo de magnitud, misma pregunta, distinta conducta, y la diferencia vive en el
	// analito. Ver SangreEnOrina y SangreEnHeces, que estan escritas para leerse juntas.
	//
	// Igual que la orina, NO tiene numero ni unidad ni rango, asi que el examen entero no es
	// el punto de ninguna serie continua. Sus DOS magnitudes si tienen serie, cada una la
	// suya, y son de formas distintas:
	//
	//   el color   -> MagnitudNominal   (forma 6)
	//   la sangre  -> MagnitudBinaria   (forma 5)
	internal sealed class ExamenDeHeces : Examen
	{
		internal ColorDeHeces Color { get; private set; }

		internal SangreEnHeces Sangre { get; private set; }

		internal ExamenDeHeces(DateTime fecha, string color)
			: this(fecha, color, null)
		{
		}

		internal ExamenDeHeces(DateTime fecha, string color, string sangre) : base(fecha)
		{
			Color = ColorDeHeces.PorNombre(color);
			Sangre = SangreEnHeces.PorNombre(sangre);
		}

		internal override string Nombre => "Examen general de heces";

		internal override bool AportaSerieContinua => false;

		internal override string ResultadoExpresado => Sangre.FueRealizada
			? $"{Color.Expresado}, sangre {Sangre.Expresado}"
			: Color.Expresado;

		internal override IReadOnlyList<LecturaDeExamen> Lecturas
		{
			get
			{
				var lecturas = new List<LecturaDeExamen>
				{
					new LecturaDeExamen(Examenes.Dominio.Analito.ColorDeLasHeces, Color),
				};

				if (Sangre.FueRealizada)
					lecturas.Add(new LecturaDeExamen(
						Examenes.Dominio.Analito.SangreEnLasHeces, Sangre));

				return lecturas;
			}
		}

		internal bool TieneHallazgo => !Color.EsEsperable || Sangre.EsHallazgo;

		// LA COMPARACION QUE JUSTIFICA ESTA CLASE. Con una sangre positiva esto devuelve
		// Seguimiento; el mismo positivo en ExamenDeOrina devuelve AtencionInmediata. Dos
		// binarios, dos severidades, y ni la pantalla ni la IA tienen que saber por que.
		internal Severidad SeveridadDelExamen
		{
			get
			{
				Severidad mayor = Color.EsEsperable ? Severidad.Informativo : Severidad.Vigilar;
				return Severidad.LaMayor(mayor, Sangre.SeveridadDelHallazgo);
			}
		}

		internal string ParaSonia
		{
			get
			{
				if (Sangre.EsHallazgo) return Sangre.ParaSonia;
				if (!Color.EsEsperable) return $"el color de las heces salió {Color.Expresado}";
				return "sin hallazgos";
			}
		}
	}
}
