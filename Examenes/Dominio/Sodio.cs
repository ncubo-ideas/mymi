using System;

namespace Examenes.Dominio
{
	// Sodio. Es el analito que DireccionDeseable.Mantenerse ya nombraba en su comentario
	// antes de que existiera esta clase: lo bueno es quedarse adentro, y alejarse es peor en
	// las dos direcciones.
	//
	// SU BANDA ES ESTRECHISIMA —diez unidades sobre 140— y esa estrechez es la razon de que
	// su multiplo inverosimil sea el mas bajo del expediente: un sodio no se duplica nunca,
	// asi que un factor de 1.4 ya delata un error de laboratorio o una unidad equivocada.
	internal sealed class Sodio : ExamenDeSangre
	{
		private static readonly AnalitoContinuo Suyo = Examenes.Dominio.Analito.Sodio;

		internal Sodio(DateTime fecha, decimal valor, string unidad)
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
			return RangoDeReferencia.Entre(135m, 145m, Suyo.Unidad);
		}

		internal override decimal CambioMaximoVerosimilPorDia => 3m;

		internal override decimal CambioMinimoSignificativo => 3m;

		internal override DireccionDeseable Direccion => DireccionDeseable.Mantenerse;

		internal override decimal MultiploQueNingunIntervaloJustifica => 1.4m;

		// ⚠️ Orientativa. Fuera de rango se acorta: lo que importa entonces es si sigue
		// moviendose, y eso no se ve esperando el control de rutina.
		internal override int MesesHastaElProximo(Paciente paciente) =>
			EsNormalPara(paciente) ? 12 : 3;

		internal override SerieContinua NuevaSerie() => new SerieDeSodio();
	}
}
