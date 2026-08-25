using System;
using System.Collections.Generic;

namespace Examenes.Dominio
{
	// Una clase de consulta que la IA de la persona puede hacer sobre su expediente.
	//
	// Conjunto CERRADO: son estas tres y no hay una cuarta hasta que alguien la modele.
	// Se comparan por referencia, sin Equals ni GetHashCode.
	//
	// El dominio las nombra por lo que HACEN, no por el nombre de ninguna tool: que la
	// superficie de la IA se llame MCP, o mañana otra cosa, no es asunto suyo. La
	// traduccion entre este nombre y el de la tool vive en la frontera, igual que la del
	// analito.
	public sealed class ConsultaDeLaIA
	{
		public static readonly ConsultaDeLaIA Tendencia =
			new ConsultaDeLaIA("tendencia", "Para donde viene un analito, mirando toda su historia.");

		public static readonly ConsultaDeLaIA Velocidad =
			new ConsultaDeLaIA("velocidad", "A que ritmo viene cambiando un analito, por ano.");

		public static readonly ConsultaDeLaIA Revision =
			new ConsultaDeLaIA("revision", "Un repaso de todos los analitos de los que hay historia.");

		public string Nombre { get; private set; }

		// Que hace, dicho para la persona que decide si la habilita. Vive en el dominio y
		// no en la pantalla porque es lo que la consulta ES: si mañana una consulta
		// empieza a mirar otra cosa, la descripcion tiene que cambiar con ella o la
		// persona estaria decidiendo sobre una version vieja.
		public string QueHace { get; private set; }

		private ConsultaDeLaIA(string nombre, string queHace)
		{
			Nombre = nombre;
			QueHace = queHace;
		}

		public static IReadOnlyList<ConsultaDeLaIA> Todas =>
			new[] { Tendencia, Velocidad, Revision };

		// La frontera. Un nombre que no existe es un defecto de quien llama, no una
		// consulta nueva: revienta, no se inventa una instancia.
		public static ConsultaDeLaIA PorNombre(string nombre)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

			string normalizado = nombre.Trim().ToLowerInvariant();
			foreach (ConsultaDeLaIA consulta in Todas)
				if (consulta.Nombre == normalizado) return consulta;

			throw new ArgumentException(
				$"'{nombre}' no es una consulta que este expediente conozca.", nameof(nombre));
		}

		public bool EsLaMisma(ConsultaDeLaIA otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			return ReferenceEquals(this, otra);
		}
	}
}
