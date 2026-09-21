using System;

namespace Examenes.Dominio
{
	// Proteinas totales. Se informa en g/dL, la misma unidad que la hemoglobina, y eso no es
	// casualidad ni un problema: son dos analitos distintos que se miden en lo mismo, y la
	// unidad la dice el analito.
	//
	// SON LA SUMA DE DOS FRACCIONES —albumina y globulinas— y el reporte real las trae por
	// separado. Aca entra el total: la particion que suma cien es uno de los cambios
	// estructurales del doc de hallazgos, y este alcance no lo toca. La analogia exacta es
	// el colesterol fraccionado, que si esta modelado, y ver ColesterolDetallado alcanza para
	// entender lo que costaria.
	internal sealed class ProteinasTotales : ExamenDeSangre
	{
		private static readonly AnalitoContinuo Suyo = Examenes.Dominio.Analito.ProteinasTotales;

		internal ProteinasTotales(DateTime fecha, decimal valor, string unidad)
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
			return RangoDeReferencia.Entre(6.0m, 8.3m, Suyo.Unidad);
		}

		internal override decimal CambioMaximoVerosimilPorDia => 0.2m;

		internal override decimal CambioMinimoSignificativo => 0.5m;

		internal override DireccionDeseable Direccion => DireccionDeseable.Mantenerse;

		internal override decimal MultiploQueNingunIntervaloJustifica => 2m;

		// ⚠️ Orientativa. Fuera de rango se acorta: lo que importa entonces es si sigue
		// moviendose, y eso no se ve esperando el control de rutina.
		internal override int MesesHastaElProximo(Paciente paciente) =>
			EsNormalPara(paciente) ? 12 : 6;

		internal override SerieContinua NuevaSerie() => new SerieDeProteinasTotales();
	}
}
