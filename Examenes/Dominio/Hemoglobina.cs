using System;

namespace Examenes.Dominio
{
	// Hemoglobina. Es el analito que existe para que el dominio tenga un caso donde lo
	// que preocupa es el valor BAJO, no el alto: sin el, "clasificar" podria confundirse
	// con "pasarse del techo" y el rango con dos extremos no se estaria usando.
	public sealed class Hemoglobina : ExamenDeSangre
	{
		public const string NombreDelAnalito = "Hemoglobina";

		public Hemoglobina(DateTime fecha, decimal valor, string unidad)
			: base(fecha, Medida(valor, unidad, UnidadDeMedida.GramosPorDecilitro, NombreDelAnalito))
		{
		}

		public override string Nombre => NombreDelAnalito;

		public override string Analito => NombreDelAnalito;

		// Rango con piso Y techo, a diferencia del colesterol y el antigeno.
		public override RangoDeReferencia RangoPara(int edad) =>
			edad >= 60
				? RangoDeReferencia.Entre(12.5m, 16.5m, UnidadDeMedida.GramosPorDecilitro)
				: RangoDeReferencia.Entre(13.0m, 17.0m, UnidadDeMedida.GramosPorDecilitro);

		public override decimal CambioMaximoVerosimilPorDia => 0.1m;

		public override decimal CambioQueExigeAtencion => 2.0m;

		public override bool DireccionPreocupanteEsAscenso => false;

		// La hemoglobina se lee por nivel contra su rango, no por pendiente.
		public override decimal VelocidadQueExigeAtencionPorAno => 0m;
	}
}
