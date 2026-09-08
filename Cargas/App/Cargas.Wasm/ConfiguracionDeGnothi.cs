namespace Cargas.Wasm;

// COMO SE LLEGA A GNOTHI desde la app suelta. Sale de appsettings.json y no de una constante
// compilada, por la misma razon que el juego de URLs del proveedor: otra institucion, u otro
// despliegue, cambia estos dos valores y nada mas. Es el mismo par que usa Examenes.
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
