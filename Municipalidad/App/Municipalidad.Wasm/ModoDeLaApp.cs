namespace Municipalidad.Wasm
{
	/// <summary>
	/// En que hospedaje corre esta app, decidido UNA vez al arrancar y no vuelto a preguntar.
	/// <para>
	/// Es el interruptor del que cuelgan las dos unicas diferencias entre los dos modos: de donde salen
	/// los eventos y a donde bajan los bytes. El dominio, el actor y el journal son identicos, y por eso
	/// las respuestas tambien.
	/// </para>
	/// <para>
	/// Se decide antes de que arranque el render, porque el contenedor construye el puente del journal
	/// con el destino ya elegido y despues seria tarde.
	/// </para>
	/// </summary>
	public sealed class ModoDeLaApp
	{
		public bool Hospedada { get; private set; }

		/// <summary>El puente hacia Gnothi. Vacio cuando la app corre suelta.</summary>
		public PuenteDeGnothi Puente { get; private set; }

		/// <summary>
		/// El destino del journal cuando esta hospedada. Vacio cuando corre suelta, y ahi el puente del
		/// journal se ocupa el mismo del almacenamiento del navegador.
		/// </summary>
		public IDestinoDelJournal Destino { get; private set; }

		public void Hospedar(PuenteDeGnothi puente)
		{
			ArgumentNullException.ThrowIfNull(puente);

			Hospedada = true;
			Puente = puente;
			Destino = puente.ComoDestino();
		}
	}
}
