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
	internal sealed class FormaDeLectura
	{
		// Una trayectoria en el tiempo contra la linea base de la persona. El antigeno.
		internal static readonly FormaDeLectura TrayectoriaConLineaBase =
			new FormaDeLectura("trayectoria-con-linea-base");

		// Un progreso desde donde arranco el episodio hasta una meta. El colesterol.
		internal static readonly FormaDeLectura ProgresoHaciaMeta =
			new FormaDeLectura("progreso-hacia-meta");

		// Una posicion dentro de una banda con piso Y techo. La hemoglobina.
		internal static readonly FormaDeLectura PosicionEnBanda =
			new FormaDeLectura("posicion-en-banda");

		// UNA TIRA DE PRUEBAS EN EL TIEMPO: estuvo, no estuvo, volvio a estar. Los
		// binarios — los nitritos, las dos sangres ocultas.
		//
		// No es una curva y no puede serlo: no hay eje vertical, porque no existe "mas
		// positivo". Lo que se dibuja son las fechas y si en cada una habia o no habia. Y
		// eso ES un dibujo, no una frase: ver tres pruebas seguidas marcadas dice algo que
		// la ultima sola no dice.
		internal static readonly FormaDeLectura PresenciaEnElTiempo =
			new FormaDeLectura("presencia-en-el-tiempo");

		// UNA TIRA DE CATEGORIAS EN EL TIEMPO: ambar, ambar, oscuro. Los nominales: el
		// color de la orina, el de las heces.
		//
		// No es una curva y no puede serlo: no hay eje vertical, porque las categorias no se
		// ordenan. Y no es la Afirmacion que usaban antes, por la MISMA razon por la que los
		// binarios dejaron de usarla -ver SerieBinaria.Forma-: ver tres observaciones en fila
		// dice algo que la ultima sola no dice, que el color inesperado se repitio o que
		// volvio a lo esperable. Sigue siendo cierto que preguntarle a un color si viene
		// subiendo no es una pregunta; lo que hay es si salio y si volvio.
		internal static readonly FormaDeLectura CategoriaEnElTiempo =
			new FormaDeLectura("categoria-en-el-tiempo");

		// Un hecho, sin escala ni SERIE DETRAS. Es lo que contesta InterpretarSerieDe cuando
		// no hay serie de ese analito o cuando no le corresponde a la persona: no queda
		// ninguna forma que dibujar, y lo unico que se puede hacer es decir lo que hay.
		//
		// Antes era la forma de los nominales. Dejo de serlo cuando los colores formaron
		// serie: una lectura sin serie y una serie que no se dibuja con numeros son dos cosas
		// distintas, y si comparten forma la pantalla no puede separarlas.
		internal static readonly FormaDeLectura Afirmacion = new FormaDeLectura("afirmacion");

		internal string Nombre { get; private set; }

		private FormaDeLectura(string nombre)
		{
			Nombre = nombre;
		}

		// Si esta forma se dibuja sobre una serie de puntos en el tiempo. La afirmacion no.
		internal bool NecesitaLaSerie => !ReferenceEquals(this, Afirmacion);

		// Si los puntos que dibuja son NUMEROS. La presencia en el tiempo y la categoria en
		// el tiempo necesitan la serie y sin embargo no tienen valores: sus puntos son fechas
		// con un si o un no, o fechas con un nombre. Es la distincion que evita que la
		// pantalla le pida ValorNumerico a un nitrito o a un color.
		internal bool SeDibujaConNumeros =>
			ReferenceEquals(this, TrayectoriaConLineaBase)
			|| ReferenceEquals(this, ProgresoHaciaMeta)
			|| ReferenceEquals(this, PosicionEnBanda);
	}
}
