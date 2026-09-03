using System;

namespace Examenes.Dominio
{
	// Glucosa en ayunas. Es el analito con el que se detecta la diabetes antes de que
	// duela, y por eso su banda tiene piso Y techo: 60 mg/dL no es "mejor que 88".
	//
	// EL AYUNO ES PARTE DE LA IDENTIDAD y no un detalle del metodo: una glucosa tomada dos
	// horas despues de comer se lee contra OTROS umbrales, asi que no es el mismo analito.
	// El dominio no sabe todavia si la muestra fue en ayunas —la condicion preanalitica es
	// uno de los cambios estructurales que este alcance no toca— y por eso el analito se
	// llama por el ayuno: lo que entra por aca se lee como si lo fuera.
	internal sealed class GlucosaEnAyunas : ExamenDeSangre
	{
		private static readonly AnalitoContinuo Suyo = Examenes.Dominio.Analito.GlucosaEnAyunas;

		internal GlucosaEnAyunas(DateTime fecha, decimal valor, string unidad)
			: base(fecha, Suyo, Medida(valor, unidad, Suyo))
		{
		}

		internal override string Nombre => Suyo.ParaMostrar;

		// Piso Y TECHO: alejarse es peor en las dos direcciones, igual que en la hemoglobina
		// y al reves del colesterol.
		//
		// NO DEPENDE DEL SEXO NI DE LA EDAD, y decirlo importa: no se devuelve null nunca, asi
		// que este analito se puede leer con lo que el expediente ya tiene. Es la razon por la
		// que esta entre los siete de la quimica general —ver Analito— y no la creatinina.
		//
		// ⚠️ Los cortes son los de uso corriente en adultos y son ORIENTATIVOS: cada
		// laboratorio publica los suyos, y el rango con procedencia es uno de los cambios
		// estructurales que este alcance no toca.
		internal override RangoDeReferencia RangoPara(Paciente paciente)
		{
			ArgumentNullException.ThrowIfNull(paciente);
			return RangoDeReferencia.Entre(70m, 100m, Suyo.Unidad);
		}

		internal override decimal CambioMaximoVerosimilPorDia => 5m;

		internal override decimal CambioMinimoSignificativo => 8m;

		internal override DireccionDeseable Direccion => DireccionDeseable.Mantenerse;

		internal override decimal MultiploQueNingunIntervaloJustifica => 4m;

		// ⚠️ Orientativa. Fuera de rango se acorta: lo que importa entonces es si sigue
		// moviendose, y eso no se ve esperando el control de rutina.
		internal override int MesesHastaElProximo(Paciente paciente) =>
			EsNormalPara(paciente) ? 36 : 6;

		internal override SerieContinua NuevaSerie() => new SerieDeGlucosaEnAyunas();
	}
}
