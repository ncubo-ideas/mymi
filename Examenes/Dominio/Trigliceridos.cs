using System;

namespace Examenes.Dominio
{
	// Trigliceridos. El segundo analito del perfil lipidico que este dominio modela, y el
	// unico de la quimica general que se lee como el colesterol: TECHO y no banda, porque
	// aca bajar siempre es mejor.
	//
	// SE MUEVEN MAS RAPIDO QUE CUALQUIER OTRO DEL EXPEDIENTE. Una sola comida grasosa los
	// duplica en horas, y de ahi salen sus dos umbrales, que son los mas altos del
	// expediente: 20 mg/dL por dia es verosimil, y una diferencia de menos de 25 no se
	// distingue de esa variacion normal. Es el mismo razonamiento del colesterol total con
	// otros numeros, y es la razon de que los umbrales sean del ANALITO y no una constante.
	//
	// ⚠️ Orientativos, como todos los de este dominio.
	internal sealed class Trigliceridos : ExamenDeSangre
	{
		private static readonly AnalitoContinuo Suyo = Examenes.Dominio.Analito.Trigliceridos;

		internal Trigliceridos(DateTime fecha, decimal valor, string unidad)
			: base(fecha, Suyo, Medida(valor, unidad, Suyo))
		{
		}

		internal override string Nombre => Suyo.ParaMostrar;

		// TECHO SOLO, y no depende del sexo ni de la edad. El corte de 150 mg/dL es el mismo
		// para adultos, a diferencia del colesterol total, cuyo techo baja a los 50: no todo
		// umbral de un perfil lipidico se mueve con la edad, y suponerlo seria inventar una
		// regla que el laboratorio no publica.
		internal override RangoDeReferencia RangoPara(Paciente paciente)
		{
			ArgumentNullException.ThrowIfNull(paciente);
			return RangoDeReferencia.HastaUnMaximoDe(150m, Suyo.Unidad);
		}

		internal override decimal CambioMaximoVerosimilPorDia => 20m;

		internal override decimal CambioMinimoSignificativo => 25m;

		internal override DireccionDeseable Direccion => DireccionDeseable.Bajar;

		// El corte mas alto del expediente, por la misma razon que los otros dos umbrales:
		// unos trigliceridos de 90 que pasan a 500 son perfectamente posibles en un ano.
		internal override decimal MultiploQueNingunIntervaloJustifica => 6m;

		// En rango, con el perfil lipidico cada cinco anos; fuera, todos los anos. Es el
		// mismo criterio del colesterol total, con el que se piden juntos.
		internal override int MesesHastaElProximo(Paciente paciente) =>
			EsNormalPara(paciente) ? 60 : 12;

		internal override SerieContinua NuevaSerie() => new SerieDeTrigliceridos();
	}
}
