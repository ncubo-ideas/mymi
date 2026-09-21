namespace Concesionario.Wasm
{
	// DONDE BAJAN LOS BYTES del journal, y lo unico que cambia entre los dos modos de la app
	// (especificacion 11 §2):
	//
	//   suelta      el storage del browser: una carpeta real de la persona, u OPFS
	//   hospedada   los archivos del contenedor de Gnothi, por el puente nativo
	//
	// El movimiento MEMFS <-> destino NO cambia: lo hace JournalBridge con System.IO, que es
	// la misma superficie que usa el motor, y el orden precarga-sincrona / persistencia-
	// asincrona queda igual. Por eso el destino es tan chico: listar, leer y escribir. Todo lo
	// demas -picker, permisos, OPFS, exportar- es plomeria del browser y del otro lado no
	// existe: Gnothi no le pregunta a nadie donde escribir.
	public interface IJournalDestination
	{
		// Para los mensajes de error. En el browser es el nombre de la carpeta; hospedado, de
		// donde escribe Gnothi. Un error de escritura sin decir DONDE no se puede accionar.
		string Name { get; }

		// Si hay donde escribir AHORA. En el browser puede ser false con carpeta elegida: el
		// permiso caduca al reabrir. Hospedado es siempre true, porque los archivos son de
		// Gnothi y nadie le revoca el permiso sobre lo suyo.
		bool Available { get; }

		// Las rutas relativas de lo que hay en el destino, con '/' siempre.
		ValueTask<ListResult> ListAsync();

		ValueTask<ReadResult> ReadAsync(string path);

		ValueTask<WriteResult> WriteAsync(string path, string contentBase64);
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
	public sealed class DestinationOverBridge : IJournalDestination
	{
		private readonly Func<ValueTask<ListResult>> list;
		private readonly Func<string, ValueTask<ReadResult>> read;
		private readonly Func<string, string, ValueTask<WriteResult>> write;

		public string Name { get; }

		// Hospedado siempre hay donde escribir: el destino es de Gnothi.
		public bool Available => true;

		public DestinationOverBridge(
			Func<ValueTask<ListResult>> list,
			Func<string, ValueTask<ReadResult>> read,
			Func<string, string, ValueTask<WriteResult>> write,
			string name = "Mymi")
		{
			ArgumentNullException.ThrowIfNull(list);
			ArgumentNullException.ThrowIfNull(read);
			ArgumentNullException.ThrowIfNull(write);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(name);

			this.list = list;
			this.read = read;
			this.write = write;
			Name = name;
		}

		public ValueTask<ListResult> ListAsync() => list();

		public ValueTask<ReadResult> ReadAsync(string path) => read(path);

		public ValueTask<WriteResult> WriteAsync(string path, string contentBase64) =>
			write(path, contentBase64);
	}
}
