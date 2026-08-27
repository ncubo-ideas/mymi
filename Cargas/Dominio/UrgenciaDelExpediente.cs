using System;

namespace Cargas.Dominio
{
	// LO MAS URGENTE DE TODO EL EXPEDIENTE, con lo que hay que hacer al respecto.
	//
	// Antes esta respuesta era una Severidad sola, y alcanzaba porque el que-hacer era un
	// texto colgado de ella. Desde que la accion lleva plazo y la elige el analito, la
	// severidad sola ya no puede decir "mostrále esto a tu médico este mes": hay que
	// devolver tambien LA ACCION del analito que gano, que es la que la portada titula.
	//
	// Carrier por identidad: sin Equals, sin GetHashCode, no es llave de nada.
	public sealed class UrgenciaDelExpediente
	{
		public Severidad Severidad { get; private set; }

		// La accion del hallazgo mas urgente. Null cuando nada pide nada.
		public Accion Accion { get; private set; }

		public UrgenciaDelExpediente(Severidad severidad, Accion accion)
		{
			ArgumentNullException.ThrowIfNull(severidad);

			if (severidad.PideAlgo && accion == null)
				throw new ArgumentNullException(nameof(accion),
					$"Una severidad '{severidad.Nombre}' pide algo, y lo que pide "
					+ "tiene que decirse con su plazo: falta la Accion.");

			Severidad = severidad;
			Accion = accion;
		}

		// Proyecciones planas para el print del DSL.
		public string NombreDeLaSeveridad => Severidad.Nombre;
		public string AccionLegible => Accion == null ? "" : Accion.Legible;
		public bool PideAlgo => Severidad.PideAlgo;
	}
}
