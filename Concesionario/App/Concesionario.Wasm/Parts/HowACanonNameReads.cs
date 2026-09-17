using Concesionario.App.Wording;

namespace Concesionario.Wasm.Parts
{
	/// <summary>
	/// Como se lee en la pantalla cada nombre del canon. El nombre lo dice el dominio y cruza la puerta
	/// tal cual; aca solo se elige la etiqueta, y un nombre que ninguna lista nombre se separa en cada
	/// mayuscula en vez de desaparecer.
	/// <para>
	/// LA ETIQUETA SALE DEL CATALOGO, uno por idioma, y no de una tabla escrita aqui: un conjunto
	/// cerrado no es dato del registro -- lo elige el dominio de una lista suya -- asi que se traduce.
	/// El CODIGO es identidad y no se toca.
	/// </para>
	/// </summary>
	public static class HowACanonNameReads
	{
		/// <summary>Una de las nueve clases de operacion de mantenimiento.</summary>
		public static string AnOperation(Phrases phrases, string kind) =>
			phrases.HowItIsSaid("canon.operation", kind);

		/// <summary>Donde esta parada una operacion respecto de su vencimiento.</summary>
		public static string ADueState(Phrases phrases, string dueState) =>
			phrases.HowItIsSaid("canon.dueState", dueState);

		/// <summary>Una de las seis clases de fluido.</summary>
		public static string AFluid(Phrases phrases, string kind) =>
			phrases.HowItIsSaid("canon.fluid", kind);

		/// <summary>Una de las tres posiciones de llanta.</summary>
		public static string ATyre(Phrases phrases, string position) =>
			phrases.HowItIsSaid("canon.tyre", position);

		/// <summary>
		/// Donde esta parado el vehiculo frente a su sobre o frente a un sistema del alcance. Las dos
		/// posturas se leen aca porque comparten <c>NotPublished</c>, que se dice igual en las dos.
		/// </summary>
		public static string AStanding(Phrases phrases, string standing) =>
			phrases.HowItIsSaid("canon.standing", standing);
	}
}
