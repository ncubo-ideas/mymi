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

		// UNA TIRA DE PRUEBAS EN EL TIEMPO: estuvo, no estuvo, volvio a estar. Los
		// binarios — los nitritos, las dos sangres ocultas.
		//
		// No es una curva y no puede serlo: no hay eje vertical, porque no existe "mas
		// positivo". Lo que se dibuja son las fechas y si en cada una habia o no habia. Y
		// eso ES un dibujo, no una frase: ver tres pruebas seguidas marcadas dice algo que
		// la ultima sola no dice.
		public static readonly FormaDeLectura PresenciaEnElTiempo =
			new FormaDeLectura("presencia en el tiempo");

		// Un hecho, sin escala ni serie. Los nominales: el color de la orina, el de las
		// heces. No se dibuja: se dice, porque un color no forma una serie que se pueda
		// leer —preguntar si viene subiendo no es una pregunta—.
		public static readonly FormaDeLectura Afirmacion = new FormaDeLectura("afirmacion");

		public string Nombre { get; private set; }

		private FormaDeLectura(string nombre)
		{
			Nombre = nombre;
		}

		// Si esta forma se dibuja sobre una serie de puntos en el tiempo. La afirmacion no.
		public bool NecesitaLaSerie => !ReferenceEquals(this, Afirmacion);

		// Si los puntos que dibuja son NUMEROS. La presencia en el tiempo necesita la
		// serie y sin embargo no tiene valores: sus puntos son fechas con un si o un no.
		// Es la distincion que evita que la pantalla le pida ValorNumerico a un nitrito.
		public bool SeDibujaConNumeros =>
			ReferenceEquals(this, TrayectoriaConLineaBase)
			|| ReferenceEquals(this, ProgresoHaciaMeta)
			|| ReferenceEquals(this, PosicionEnBanda);
	}
}
