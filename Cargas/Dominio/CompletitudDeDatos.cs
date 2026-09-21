namespace Cargas.Dominio
{
	/// <summary>
	/// Que tan completo esta el aporte de Sonia: cuantas cargas tienen kilometraje y
	/// cuantos tramos se pueden calcular. Es una linea discreta -"8 de 9 cargas con
	/// kilometraje"-, nunca un reproche: alimenta la motivacion del §10 mostrando el
	/// trabajo a medio hacer, no un formulario en blanco.
	/// </summary>
	internal sealed class CompletitudDeDatos
	{
		internal int CargasConKilometraje { get; private set; }
		internal int TotalDeCargas { get; private set; }
		internal int TramosCalculados { get; private set; }
		internal int TramosBloqueados { get; private set; }

		internal CompletitudDeDatos(int conKilometraje, int total, int calculados, int bloqueados)
		{
			CargasConKilometraje = conKilometraje;
			TotalDeCargas = total;
			TramosCalculados = calculados;
			TramosBloqueados = bloqueados;
		}

	}
}
