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
	// Y es tambien el examen que informa DOS FORMAS DE MAGNITUD a la vez —seis magnitudes
	// entre las dos formas, desde que la tira informa sus otras tres casillas—:
	//
	//   el color     -> MagnitudNominal   (forma 6: no se ordena, y SI tiene serie)
	//   los nitritos -> MagnitudBinaria   (forma 5: presencia, sin escala, con un
	//                                      tercer estado que no es negativo)
	//   la sangre    -> MagnitudBinaria   (forma 5 tambien, y NO se lee igual que los
	//                                      nitritos: ver SangreEnOrina)
	//
	// Un solo examen, seis magnitudes, y ninguna es un numero. Que DOS de ellas sean la
	// misma forma y pidan acciones distintas es el argumento de Alvaro en una sola clase:
	// si la severidad se dedujera de "es positivo", estos dos darian lo mismo.
	//
	// Y las TRES aportan una serie, cada una con la suya. Ver Expediente.Agregar: el examen
	// entero no es el punto de ninguna, y sin embargo un urianalisis suma tres analitos al
	// conteo de la portada.
	internal sealed class ExamenDeOrina : Examen
	{
		internal ColorDeOrina Color { get; private set; }

		internal Nitritos Nitritos { get; private set; }

		internal SangreEnOrina Sangre { get; private set; }

		// LAS TRES CASILLAS QUE LA TIRA YA TRAIA. Un urianalisis real informa siete cosas y
		// esta clase leia tres; estas son las que el reporte da como positivo o negativo, o
		// sea las que entran sin abrir la forma ordinal —la de las cruces—.
		internal ProteinaEnOrina Proteina { get; private set; }

		internal Cetonas Cetonas { get; private set; }

		internal GlucosaEnOrina Glucosa { get; private set; }

		// El constructor de DOS argumentos existe y se queda: los journals que ya se
		// escribieron tienen literalmente "ExamenDeOrina(@fecha, @color)" adentro, y el
		// replay los reconstruye llamando a ESA firma. Borrarla no seria una refactor,
		// seria romper expedientes ya escritos. Un examen viejo simplemente no midio
		// nitritos, que es exactamente lo que NoRealizado significa.
		internal ExamenDeOrina(DateTime fecha, string color)
			: this(fecha, color, null, null)
		{
		}

		// Misma razon que la de dos argumentos: hay journals escritos con
		// "ExamenDeOrina(@fecha, @color, @nitritos)" y el replay llama a ESA firma. Cada
		// vez que el examen aprende a informar algo nuevo se agrega una sobrecarga; no se
		// cambia una que ya se escribio en el registro de alguien.
		internal ExamenDeOrina(DateTime fecha, string color, string nitritos)
			: this(fecha, color, nitritos, null)
		{
		}

		// Y la de CUATRO, por la misma razon que las dos de arriba: es la firma con la que
		// se escribieron los journals desde que la tira informo la sangre oculta. Ya son
		// tres sobrecargas historicas, y ese numero es la cuenta de las veces que este
		// examen aprendio a informar algo nuevo.
		internal ExamenDeOrina(DateTime fecha, string color, string nitritos, string sangre)
			: this(fecha, color, nitritos, sangre, null, null, null)
		{
		}

		// LA TIRA ENTERA. Los que no se pasen quedan en NoRealizado, que es lo que
		// corresponde: la tira no siempre corre todas sus casillas, y eso es distinto de un
		// negativo.
		internal ExamenDeOrina(DateTime fecha, string color, string nitritos, string sangre,
			string proteina, string cetonas, string glucosa)
			: base(fecha)
		{
			Color = ColorDeOrina.PorNombre(color);
			Nitritos = Nitritos.PorNombre(nitritos);
			Sangre = SangreEnOrina.PorNombre(sangre);
			Proteina = ProteinaEnOrina.PorNombre(proteina);
			Cetonas = Cetonas.PorNombre(cetonas);
			Glucosa = GlucosaEnOrina.PorNombre(glucosa);
		}

		internal override string Clave => "examen-de-orina";

		// El examen entero no es un punto de ninguna serie continua: ni un color ni un
		// binario se evaluan con las reglas de velocidad de cambio de los cuantitativos, y
		// no es que "todavia no este implementado" — es que la pregunta no existe.
		//
		// Lo que si aporta son TRES series, una por lectura. Ver Expediente.Agregar.
		internal override bool AportaSerieContinua => false;



		// TRES COSAS MEDIDAS, y cada una es un analito. Las que el laboratorio no corrio no
		// aparecen: una prueba no realizada no es una lectura vacia, es una lectura que no
		// existe. Es el mismo criterio que la celda vacia del historial.
		internal override IReadOnlyList<LecturaDeExamen> Lecturas
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

				if (Proteina.FueRealizada)
					lecturas.Add(new LecturaDeExamen(
						Examenes.Dominio.Analito.ProteinaEnLaOrina, Proteina));

				if (Cetonas.FueRealizada)
					lecturas.Add(new LecturaDeExamen(
						Examenes.Dominio.Analito.CetonasEnOrina, Cetonas));

				if (Glucosa.FueRealizada)
					lecturas.Add(new LecturaDeExamen(
						Examenes.Dominio.Analito.GlucosaEnLaOrina, Glucosa));

				return lecturas;
			}
		}

		internal bool ColorEsEsperable => Color.EsEsperable;

		// Si este examen encontro algo que merece mirarse. Lo contesta preguntandole a
		// cada magnitud por SU propia anormalidad, no comparando strings afuera: un color
		// oscuro es un hallazgo, y unos nitritos positivos tambien, por razones distintas
		// que cada clase conoce.
		internal bool TieneHallazgo => !Color.EsEsperable
			|| Nitritos.EsHallazgo || Sangre.EsHallazgo
			|| Proteina.EsHallazgo || Cetonas.EsHallazgo || Glucosa.EsHallazgo;

		// QUE HACER con este examen: lo mas urgente que digan sus magnitudes. Cada una
		// aporta su propia severidad, y la del examen es la mayor.
		//
		// Aca se ve por que la severidad no puede deducirse de "hay hallazgo": los
		// nitritos positivos y la sangre positiva son los dos hallazgos binarios de ESTA
		// misma tira, y no piden lo mismo.
		internal Severidad SeveridadDelExamen
		{
			get
			{
				Severidad mayor = Color.EsEsperable ? Severidad.Informativo : Severidad.Vigilar;
				mayor = Severidad.LaMayor(mayor, Nitritos.SeveridadDelHallazgo);
				mayor = Severidad.LaMayor(mayor, Sangre.SeveridadDelHallazgo);
				mayor = Severidad.LaMayor(mayor, Proteina.SeveridadDelHallazgo);
				mayor = Severidad.LaMayor(mayor, Cetonas.SeveridadDelHallazgo);
				return Severidad.LaMayor(mayor, Glucosa.SeveridadDelHallazgo);
			}
		}

		// La lectura, elegida con lo que dice cada magnitud de si misma. La pantalla no
		// compone nada: pide esto.
		/// <summary>La frase que este examen elige, por su nombre.</summary>
		internal string ClaveDeLaFrase
		{
			get
			{
				if (Sangre.EsHallazgo) return Sangre.ClaveDeLaFrase;
				if (Nitritos.EsHallazgo) return Nitritos.ClaveDeLaFrase;
				if (Glucosa.EsHallazgo) return Glucosa.ClaveDeLaFrase;
				if (Proteina.EsHallazgo) return Proteina.ClaveDeLaFrase;
				if (Cetonas.EsHallazgo) return Cetonas.ClaveDeLaFrase;
				if (!Color.EsEsperable) return Color.ClaveDeLaFrase;
				return "resultado.sin-hallazgos";
			}
		}
	}
}
