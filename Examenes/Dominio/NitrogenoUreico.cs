using System;

namespace Examenes.Dominio
{
	// Nitrogeno ureico en sangre —el BUN de los reportes—. Junto con la creatinina es como
	// se mira el rinon, y ESTA APP NO TRAE LA CREATININA a proposito: su rango se publica
	// por sexo, asi que un expediente sin sexo declarado no podria leerla. Ver el comentario
	// de la quimica general en Analito.
	//
	// Sube por deshidratacion y por una dieta muy proteica, no solo por el rinon, y por eso
	// su severidad fuera de rango es "vigilar" y no mas: un BUN alto suelto es una
	// conversacion, no una urgencia.
	internal sealed class NitrogenoUreico : ExamenDeSangre
	{
		private static readonly AnalitoContinuo Suyo = Examenes.Dominio.Analito.NitrogenoUreico;

		internal NitrogenoUreico(DateTime fecha, decimal valor, string unidad)
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
			return RangoDeReferencia.Entre(7m, 20m, Suyo.Unidad);
		}

		internal override decimal CambioMaximoVerosimilPorDia => 2m;

		internal override decimal CambioMinimoSignificativo => 4m;

		internal override DireccionDeseable Direccion => DireccionDeseable.Mantenerse;

		internal override decimal MultiploQueNingunIntervaloJustifica => 4m;

		// ⚠️ Orientativa. Fuera de rango se acorta: lo que importa entonces es si sigue
		// moviendose, y eso no se ve esperando el control de rutina.
		internal override int MesesHastaElProximo(Paciente paciente) =>
			EsNormalPara(paciente) ? 12 : 6;

		internal override SerieContinua NuevaSerie() => new SerieDeNitrogenoUreico();
	}
}
