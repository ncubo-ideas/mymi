namespace Cargas.Wasm;

// EN QUE HOSPEDAJE CORRE LA APP, decidido UNA VEZ al arrancar y no vuelto a preguntar.
//
// Es el interruptor del que cuelgan las DOS unicas diferencias entre los dos modos
// (especificacion 11 §2): de donde salen los eventos y a donde bajan los bytes. El dominio,
// el actor y el journal son identicos.
//
// Se decide en Program.cs ANTES de que arranque el render, porque el contenedor construye
// el PuenteDeJournal con el destino ya elegido y despues seria tarde.
public sealed class ModoDeLaApp
{
	public bool Hospedada { get; private set; }

	// El puente hacia Gnothi. Null cuando corre suelta.
	public PuenteDeGnothi? Puente { get; private set; }

	// El destino del journal cuando esta hospedada. Null cuando corre suelta, y ahi el
	// PuenteDeJournal se ocupa el mismo del storage del browser.
	public IDestinoDelJournal? Destino { get; private set; }

	public void Hospedar(PuenteDeGnothi puente)
	{
		ArgumentNullException.ThrowIfNull(puente);

		Hospedada = true;
		Puente = puente;
		Destino = puente.ComoDestino();
	}
}
