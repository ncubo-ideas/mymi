namespace Municipalidad.Dominio
{
	// LA CADENCIA DE BIENES INMUEBLES Y PARQUES. Cuatro periodos por anio; el numero del
	// periodo es el TRIMESTRE, no el mes -el "4" de 2026.04 es el cuarto trimestre-.
	internal sealed class CobroTrimestral : Cadencia
	{
		internal override string Nombre { get { return "trimestral"; } }
		internal override int PeriodosPorAnio { get { return 4; } }

		internal override Periodo Leer(int anio, int numero)
		{
			return new Trimestre(anio, numero);
		}

		internal override bool PuedeLeer(int anio, int numero)
		{
			return anio >= 2000 && anio <= 2100 && numero >= 1 && numero <= 4;
		}
	}
}
