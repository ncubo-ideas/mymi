using System;

namespace Examenes.Dominio
{
	// Cloro. El tercero de los electrolitos, y el que casi nunca se lee solo: acompana al
	// sodio, y lo que un medico mira es la relacion entre los dos.
	//
	// ESA RELACION NO ESTA MODELADA, y es honesto decir donde vive el limite: un derivado
	// que combina dos analitos —el anion gap— es de la familia de la TFG, que el doc de
	// hallazgos deja para el alcance donde entran los derivados. Aca cada uno se lee contra
	// su propia banda, que es lo que este dominio sabe hacer bien.
	internal sealed class Cloro : ExamenDeSangre
	{
		private static readonly AnalitoContinuo Suyo = Examenes.Dominio.Analito.Cloro;

		internal Cloro(DateTime fecha, decimal valor, string unidad)
			: base(fecha, Suyo, Medida(valor, unidad, Suyo))
		{
		}

		internal override string Clave => Suyo.Clave;

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
			return RangoDeReferencia.Entre(98m, 107m, Suyo.Unidad);
		}

		internal override decimal CambioMaximoVerosimilPorDia => 3m;

		internal override decimal CambioMinimoSignificativo => 3m;

		internal override DireccionDeseable Direccion => DireccionDeseable.Mantenerse;

		internal override decimal MultiploQueNingunIntervaloJustifica => 1.5m;

		// ⚠️ Orientativa. Fuera de rango se acorta: lo que importa entonces es si sigue
		// moviendose, y eso no se ve esperando el control de rutina.
		internal override int MesesHastaElProximo(Paciente paciente) =>
			EsNormalPara(paciente) ? 12 : 6;

		internal override SerieContinua NuevaSerie() => new SerieDeCloro();
	}
}
