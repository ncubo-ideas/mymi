namespace Compras.Wasm;

// DONDE BAJAN LOS BYTES del journal, y lo unico que cambia entre los dos modos de la app
// (especificacion 11 §2):
//
//   suelta      el storage del browser: una carpeta real de la persona, u OPFS
//   hospedada   los archivos del contenedor de Gnothi, por el puente nativo
//
// El movimiento MEMFS <-> destino NO cambia: lo hace PuenteDeJournal con System.IO, que es
// la misma superficie que usa el motor, y el orden precarga-sincrona / persistencia-
// asincrona queda igual. Por eso el destino es tan chico: listar, leer y escribir. Todo lo
// demas -picker, permisos, OPFS, exportar- es plomeria del browser y del otro lado no
// existe: Gnothi no le pregunta a nadie donde escribir.
public interface IDestinoDelJournal
{
	// Para los mensajes de error. En el browser es el nombre de la carpeta; hospedado, de
	// donde escribe Gnothi. Un error de escritura sin decir DONDE no se puede accionar.
	string Nombre { get; }

	// Si hay donde escribir AHORA. En el browser puede ser false con carpeta elegida: el
	// permiso caduca al reabrir. Hospedado es siempre true, porque los archivos son de
	// Gnothi y nadie le revoca el permiso sobre lo suyo.
	bool Disponible { get; }

	// Las rutas relativas de lo que hay en el destino, con '/' siempre.
	ValueTask<ResultadoDeListado> Listar();

	ValueTask<ResultadoDeLectura> Leer(string ruta);

	ValueTask<ResultadoDeEscritura> Escribir(string ruta, string contenidoBase64);
}

// EL DESTINO CUANDO LA APP CORRE HOSPEDADA dentro de Gnothi. Los bytes cruzan el puente
// nativo y aterrizan en los archivos del contenedor de Gnothi, un directorio por modulo
// (especificacion 11 §5.1).
//
// Entra como delegados y no como interop, por la misma razon que la fuente de eventos:
// esta clase no sabe de JS, de WebViews ni de Android, asi que se puede sostener con
// funciones en memoria para probarla. Quien la construye -la caratula, cuando detecta que
// esta hospedada- es quien sabe como se llama al puente.
//
// GNOTHI NO COMPONE NADA. Recibe una ruta y unos bytes que el modulo ya escribio, y los
// deja donde van. No es un segundo autor del journal: es el brazo del modulo.
public sealed class DestinoPorPuente : IDestinoDelJournal
{
	private readonly Func<ValueTask<ResultadoDeListado>> listar;
	private readonly Func<string, ValueTask<ResultadoDeLectura>> leer;
	private readonly Func<string, string, ValueTask<ResultadoDeEscritura>> escribir;

	public string Nombre { get; }

	// Hospedado siempre hay donde escribir: el destino es de Gnothi.
	public bool Disponible => true;

	public DestinoPorPuente(
		Func<ValueTask<ResultadoDeListado>> listar,
		Func<string, ValueTask<ResultadoDeLectura>> leer,
		Func<string, string, ValueTask<ResultadoDeEscritura>> escribir,
		string nombre = "Mymi")
	{
		ArgumentNullException.ThrowIfNull(listar);
		ArgumentNullException.ThrowIfNull(leer);
		ArgumentNullException.ThrowIfNull(escribir);
		ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

		this.listar = listar;
		this.leer = leer;
		this.escribir = escribir;
		Nombre = nombre;
	}

	public ValueTask<ResultadoDeListado> Listar() => listar();

	public ValueTask<ResultadoDeLectura> Leer(string ruta) => leer(ruta);

	public ValueTask<ResultadoDeEscritura> Escribir(string ruta, string contenidoBase64) =>
		escribir(ruta, contenidoBase64);
}
