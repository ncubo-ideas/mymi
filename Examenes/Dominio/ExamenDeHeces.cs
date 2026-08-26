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
	// Igual que la orina, NO tiene numero ni unidad ni rango, y no aporta serie. Y trae
	// dos magnitudes de formas distintas:
	//
	//   el color   -> MagnitudNominal   (forma 6)
	//   la sangre  -> MagnitudBinaria   (forma 5)
	public sealed class ExamenDeHeces : Examen
	{
		public ColorDeHeces Color { get; private set; }

		public SangreEnHeces Sangre { get; private set; }

		public ExamenDeHeces(DateTime fecha, string color)
			: this(fecha, color, null)
		{
		}

		public ExamenDeHeces(DateTime fecha, string color, string sangre) : base(fecha)
		{
			Color = ColorDeHeces.PorNombre(color);
			Sangre = SangreEnHeces.PorNombre(sangre);
		}

		public override string Nombre => "Examen general de heces";

		public override bool AportaSerie => false;

		public override string ResultadoExpresado => Sangre.FueRealizada
			? $"{Color.Expresado}, sangre {Sangre.Expresado}"
			: Color.Expresado;

		public override IReadOnlyList<LecturaDeExamen> Lecturas
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

		public bool TieneHallazgo => !Color.EsEsperable || Sangre.EsHallazgo;

		// LA COMPARACION QUE JUSTIFICA ESTA CLASE. Con una sangre positiva esto devuelve
		// Seguimiento; el mismo positivo en ExamenDeOrina devuelve AtencionInmediata. Dos
		// binarios, dos severidades, y ni la pantalla ni la IA tienen que saber por que.
		public Severidad SeveridadDelExamen
		{
			get
			{
				Severidad mayor = Color.EsEsperable ? Severidad.Informativo : Severidad.Seguimiento;
				return Severidad.LaMayor(mayor, Sangre.SeveridadDelHallazgo);
			}
		}

		public string ParaSonia
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
