namespace Cargas.Dominio
{
	/// <summary>
	/// Lo que el dominio concluye de la serie de tramos. Cerrado y por referencia. Es
	/// SEMANTICO a proposito: dice mejoro o empeoro, nunca subio o bajo, porque cambiar
	/// la unidad de presentacion invierte la flecha y el signo pero jamas el juicio
	/// (§5.3 del brief).
	/// </summary>
	// Los cuatro de abajo -SinTramo, NoComparable, DatoSospechoso, TotalNoCuadra- son
	// los que no existen en un modelo de tabla, y son el argumento del dominio hecho
	// pantalla: una tabla plana no puede decir "todavia no puedo calcular tu
	// rendimiento, y esto es exactamente lo que falta".
	public sealed class VeredictoDeRendimiento
	{
		public static readonly VeredictoDeRendimiento Estable =
			new VeredictoDeRendimiento("estable");

		public static readonly VeredictoDeRendimiento Mejoro =
			new VeredictoDeRendimiento("mejoro");

		public static readonly VeredictoDeRendimiento Empeoro =
			new VeredictoDeRendimiento("empeoro");

		/// <summary>Hay un solo tramo: todavia no hay serie contra la cual juzgarlo.</summary>
		public static readonly VeredictoDeRendimiento Indeterminado =
			new VeredictoDeRendimiento("indeterminado");

		/// <summary>Falta un segundo tanque lleno, o falta un kilometraje. El veredicto
		/// que motiva el ingreso: nombra lo que falta y lo que se desbloquea.</summary>
		public static readonly VeredictoDeRendimiento SinTramo =
			new VeredictoDeRendimiento("sin tramo");

		/// <summary>El tramo mas reciente se midio con otro instrumento (u otro
		/// combustible): no se puede comparar contra la linea base sin reservas.</summary>
		public static readonly VeredictoDeRendimiento NoComparable =
			new VeredictoDeRendimiento("no comparable");

		/// <summary>Rendimiento implausible: casi seguro una carga sin registrar. Un
		/// problema de datos, no de la realidad, y lo que corresponde es sugerir
		/// registrar la carga que falto, no alarmar.</summary>
		public static readonly VeredictoDeRendimiento DatoSospechoso =
			new VeredictoDeRendimiento("dato sospechoso");

		/// <summary>Precio por volumen no da el total, mas alla del redondeo.</summary>
		public static readonly VeredictoDeRendimiento TotalNoCuadra =
			new VeredictoDeRendimiento("total no cuadra");

		public string Nombre { get; private set; }

		private VeredictoDeRendimiento(string nombre)
		{
			Nombre = nombre;
		}

		public bool EsMejora => ReferenceEquals(this, Mejoro);
		public bool EsDeterioro => ReferenceEquals(this, Empeoro);

		/// <summary>Si hubo calculo de verdad detras: los primeros cuatro. Los otros
		/// cuatro hablan de por que NO se pudo (o no se debe) leer el numero.</summary>
		public bool HayLectura =>
			ReferenceEquals(this, Estable) || ReferenceEquals(this, Mejoro)
			|| ReferenceEquals(this, Empeoro) || ReferenceEquals(this, Indeterminado);

		public bool SugiereRegistrarLaCargaQueFalto => ReferenceEquals(this, DatoSospechoso);
	}
}
