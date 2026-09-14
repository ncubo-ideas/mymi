namespace Municipalidad.Wasm.Vistas
{
	/// <summary>
	/// Lo que pasó cuando la persona aportó algo a su cuenta, en TRES estados y nunca en dos: entró,
	/// el dominio lo rechazó y dijo por que, o entró y no se pudo bajar a la carpeta.
	/// <para>
	/// El tercero es el que no se puede callar: el hecho quedó en el journal en memoria y NO en la
	/// carpeta, asi que la pantalla y el respaldo dejaron de decir lo mismo.
	/// </para>
	/// </summary>
	public sealed class LoQueDijoElAporte
	{
		private LoQueDijoElAporte() { }

		/// <summary>Si el aporte quedó registrado Y guardado.</summary>
		public bool QuedoRegistrado { get; private init; }

		/// <summary>Si quedó registrado pero la carpeta rechazó la escritura.</summary>
		public bool QuedoSinRespaldo { get; private init; }

		/// <summary>
		/// La frase con que el DOMINIO rechazó el aporte, y nada más: es suya, viaja como la compuso y
		/// esta capa no la traduce.
		/// </summary>
		public string Dice { get; private init; } = "";

		/// <summary>
		/// Lo que dice ESTA APP cuando la falla fue suya y no del dominio. Es lo contrario de
		/// <see cref="Dice"/> y por eso son dos campos: sólo éste se dice en el idioma de la persona.
		/// </summary>
		public Apunte Apunte { get; private init; } = Apunte.Ninguno;

		internal static LoQueDijoElAporte Entro() =>
			new LoQueDijoElAporte { QuedoRegistrado = true };

		internal static LoQueDijoElAporte NoSePudo(string porque) =>
			new LoQueDijoElAporte { Dice = porque };

		internal static LoQueDijoElAporte NoSePudoPorLaApp(Apunte porque) =>
			new LoQueDijoElAporte { Apunte = porque };

		internal static LoQueDijoElAporte QuedoSinGuardar(Apunte porque) =>
			new LoQueDijoElAporte { QuedoRegistrado = true, QuedoSinRespaldo = true, Apunte = porque };
	}
}
