namespace Municipalidad.Dominio
{
	/// <summary>
	/// La cadencia de bienes inmuebles y parques: cuatro periodos por anio, y el numero del periodo es
	/// el TRIMESTRE y no el mes.
	/// </summary>
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
