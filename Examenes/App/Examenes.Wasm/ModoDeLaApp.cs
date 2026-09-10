namespace Examenes.Wasm;

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

// COMO SE LLEGA A GNOTHI desde la app suelta. Sale de appsettings.json y no de una constante
// compilada, por la misma razon que el juego de URLs del proveedor: otra institucion, u otro
// despliegue, cambia estos dos valores y nada mas.
//
// El dominio es DE GNOTHI y no de la institucion, a proposito (especificacion 13 §7):
// Chromium mantiene en el browser los enlaces al mismo host de la pagina actual, y la nota
// tecnica TN3155 de Apple hace lo mismo con los universal links del mismo dominio que la
// navegacion previa. Un enlace al propio dominio no abriria la app nunca.
public sealed class ConfiguracionDeGnothi
{
	public string Dominio { get; set; } = "go.gnothi.app";

	public string Paquete { get; set; } = "com.ncubo.gnothi.host";
}

// QUE PASO CON EL TRASPASO. Se dice en numeros porque "omitidas" no es un fallo: significa
// que esa asercion ya estaba, y que abrir el enlace dos veces no ensucio el journal.
public sealed class ResultadoDeTraspaso
{
	public int Aplicadas { get; set; }

	public int Omitidas { get; set; }

	public override string ToString() => $"aplicadas={Aplicadas} omitidas={Omitidas}";
}
