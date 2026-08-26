using System;

namespace Examenes.Dominio
{
	// COMO SE LEE este analito, y por lo tanto como se dibuja.
	//
	// POR QUE ESTO ES DOMINIO Y NO ESTILO. La forma no es una decision de diseno: es lo
	// que el analito ES. Un antigeno se lee como una trayectoria contra la linea base de
	// la persona; un colesterol como un progreso hacia una meta; una hemoglobina como una
	// posicion dentro de una banda con piso y techo. Esas son tres preguntas distintas, y
	// por eso son tres dibujos distintos.
	//
	// La pantalla venia mostrandolos a los tres iguales -una fila de tabla cada uno-, que
	// es la misma generalizacion que Alvaro rechazo en el dominio, un piso mas arriba.
	// Poner la forma aca es lo que impide que vuelva a pasar: agregar un analito obliga a
	// decir como se lee, y la pantalla no puede inventarlo.
	//
	// Conjunto cerrado. La superficie hace un switch sobre esto, y ese switch SI es
	// presentacion legitima: traduce una forma del dominio a pixeles.
	public sealed class FormaDeLectura
	{
		// Una trayectoria en el tiempo contra la linea base de la persona. El antigeno.
		public static readonly FormaDeLectura TrayectoriaConLineaBase =
			new FormaDeLectura("trayectoria con linea base");

		// Un progreso desde donde arranco el episodio hasta una meta. El colesterol.
		public static readonly FormaDeLectura ProgresoHaciaMeta =
			new FormaDeLectura("progreso hacia meta");

		// Una posicion dentro de una banda con piso Y techo. La hemoglobina.
		public static readonly FormaDeLectura PosicionEnBanda =
			new FormaDeLectura("posicion en banda");

		// Un hecho, sin escala ni serie. Los binarios y los nominales: la orina, las heces.
		// No se dibuja: se dice.
		public static readonly FormaDeLectura Afirmacion = new FormaDeLectura("afirmacion");

		public string Nombre { get; private set; }

		private FormaDeLectura(string nombre)
		{
			Nombre = nombre;
		}

		// Si esta forma se dibuja sobre una serie de puntos en el tiempo. La afirmacion no.
		public bool NecesitaLaSerie => !ReferenceEquals(this, Afirmacion);
	}
}
