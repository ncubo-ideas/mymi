using System;

namespace Examenes.Dominio
{
	// QUE LE PASO A UNA CATEGORIA A LO LARGO DEL TIEMPO. Es el tercer conjunto de lectura,
	// al lado de Tendencia y de Presencia, y son tres y no uno porque no son la misma
	// pregunta:
	//
	//   Tendencia   "para donde viene"        ascenso, descenso, estable
	//   Presencia   "aparecio o no"           una prueba esta o no esta
	//   Constancia  "salio de lo esperable"   hay N categorias, y algunas son esperables
	//
	// POR QUE NO SE REUSA Presencia. La forma de CONTAR es la misma —cuantas y cuantas
	// seguidas— y el vocabulario no. Un binario tiene UNA cosa que puede aparecer; un
	// nominal tiene varias categorias, y dos de ellas inesperadas no son la misma cosa: el
	// negro y el palido de ColorDeHeces son inesperados por razones distintas. Ademas
	// Presencia esta escrita entera en terminos de pruebas y de positivos, y decir
	// "positivas: 2" de un color es la misma aplanadora que decir "en ascenso" de un
	// nitrito, un piso mas abajo.
	//
	// POR QUE EL NOMINAL NECESITA SERIE, que es lo que todo esto habilita. ColorDeOrina
	// contesta Vigilar siempre, y esta bien: una magnitud solo se ve a si misma. La
	// historia distingue tres cosas que esa respuesta no puede distinguir —un oscuro
	// suelto, dos oscuros seguidos, y un oscuro que despues volvio a ambar— y las tres
	// piden conductas distintas. Es el argumento con el que existe SerieBinaria, aplicado a
	// la ultima forma que no lo tenia.
	internal sealed class Constancia
	{
		// Menos de dos observaciones: no hay serie. Con una sola categoria SI se puede
		// leer el resultado —eso lo hace la hoja—, pero no se puede afirmar que se
		// repitio ni que volvio.
		internal static readonly Constancia Indeterminada = new Constancia("indeterminada");

		// Todas las observaciones cayeron dentro de lo esperable. NO exige que sea la
		// MISMA categoria: pasar de amarillo palido a ambar es cambiar de categoria sin
		// salirse de lo esperable, y es literalmente lo que trae el guion de la demo.
		// Presencia.NuncaAparecio no sabe nombrar ese caso, porque en un binario no hay
		// dos maneras distintas de estar bien.
		internal static readonly Constancia SiempreEsperable =
			new Constancia("siempre-esperable");

		// La ultima salio fuera de lo esperable y la anterior no. Todavia nadie lo vio dos
		// veces.
		internal static readonly Constancia Aparecio = new Constancia("aparecio");

		// Dos o mas inesperadas SEGUIDAS al final. Dejo de ser un resultado suelto, y es
		// el inhabitante que justifica que los nominales tengan serie.
		internal static readonly Constancia Persiste = new Constancia("persiste");

		// Hubo UNA inesperada aislada y la observacion siguiente volvio a lo esperable. NO
		// ERA, y decirlo es la buena noticia que ninguna medicion sola puede dar.
		internal static readonly Constancia NoConfirmado = new Constancia("no-confirmado");

		// Estuvo fuera de lo esperable de verdad —dos o mas veces— y despues volvio. No es
		// lo mismo que NoConfirmado: ahi nunca se confirmo nada, aca se confirmo y volvio.
		internal static readonly Constancia Volvio = new Constancia("volvio");

		/// <summary>
		/// EL CODIGO de esta constancia: identidad, y lo unico que cruza el wire. Como se dice lo busca con este
		/// codigo la capa que redacta, en el idioma que le pidieron.
		/// </summary>
		internal string Nombre { get; private set; }

		private Constancia(string nombre)
		{
			Nombre = nombre;
		}

		// Si HOY la categoria esta fuera de lo esperable. Es lo unico que se puede
		// preguntar sin mirar la historia, y por eso es lo unico que la magnitud sola sabe
		// contestar.
		internal bool EstaFueraDeLoEsperable =>
			ReferenceEquals(this, Aparecio) || ReferenceEquals(this, Persiste);

		// Si se repitio. Es lo que separa un dato de un hecho, y lo que escala el plazo.
		internal bool EstaConfirmado => ReferenceEquals(this, Persiste);

		// Se movio hacia donde conviene: volvio a lo esperable.
		internal bool EsMejora =>
			ReferenceEquals(this, NoConfirmado) || ReferenceEquals(this, Volvio);
	}
}
