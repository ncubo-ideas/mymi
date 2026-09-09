namespace Municipalidad.Dominio
{
	/// <summary>
	/// La cadencia de los servicios: doce periodos por anio, y el numero del periodo es el MES.
	/// </summary>
	internal sealed class CobroMensual : Cadencia
	{
		internal override string Nombre { get { return "mensual"; } }
		internal override int PeriodosPorAnio { get { return 12; } }

		internal override Periodo Leer(int anio, int numero)
		{
			return new Mes(anio, numero);
		}

		internal override bool PuedeLeer(int anio, int numero)
		{
			return anio >= 2000 && anio <= 2100 && numero >= 1 && numero <= 12;
		}
	}
}
