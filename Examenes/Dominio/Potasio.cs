using System;

namespace Examenes.Dominio
{
	// Potasio. Banda todavia mas estrecha que la del sodio, y con una particularidad que el
	// dominio no puede ver: un potasio alto falso es de los artefactos mas frecuentes del
	// laboratorio —basta que la muestra se hemolice— y distinguirlo exige el metodo
	// analitico, que Examen no carga.
	//
	// Lo que el dominio SI puede hacer es no confundir un valor suelto con una tendencia,
	// que es exactamente para lo que sirve su serie.
	internal sealed class Potasio : ExamenDeSangre
	{
		private static readonly AnalitoContinuo Suyo = Examenes.Dominio.Analito.Potasio;

		internal Potasio(DateTime fecha, decimal valor, string unidad)
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
			return RangoDeReferencia.Entre(3.5m, 5.1m, Suyo.Unidad);
		}

		internal override decimal CambioMaximoVerosimilPorDia => 0.5m;

		internal override decimal CambioMinimoSignificativo => 0.4m;

		internal override DireccionDeseable Direccion => DireccionDeseable.Mantenerse;

		internal override decimal MultiploQueNingunIntervaloJustifica => 2m;

		// ⚠️ Orientativa. Fuera de rango se acorta: lo que importa entonces es si sigue
		// moviendose, y eso no se ve esperando el control de rutina.
		internal override int MesesHastaElProximo(Paciente paciente) =>
			EsNormalPara(paciente) ? 12 : 3;

		internal override SerieContinua NuevaSerie() => new SerieDePotasio();
	}
}
