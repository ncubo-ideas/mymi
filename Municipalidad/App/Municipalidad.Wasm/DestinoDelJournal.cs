namespace Municipalidad.Wasm
{
	/// <summary>
	/// Donde bajan los bytes del journal, y lo unico que cambia entre los dos hospedajes de esta app:
	/// suelta, el almacenamiento del navegador -- una carpeta de la persona, o la del propio navegador;
	/// hospedada, los archivos de Gnothi, por el puente nativo.
	/// <para>
	/// El movimiento entre MEMFS y el destino NO cambia: lo hace el puente del journal con System.IO, que
	/// es la misma superficie que usa el motor, y el orden precarga-sincrona / persistencia-asincrona
	/// queda igual. Por eso el destino es tan chico: listar, leer y escribir. El selector, el permiso y
	/// el almacenamiento del navegador son plomeria del navegador y del otro lado no existen.
	/// </para>
	/// </summary>
	public interface IDestinoDelJournal
	{
		/// <summary>
		/// Como se llama el destino, para los mensajes de error. Suelta es el nombre de la carpeta;
		/// hospedada, quien escribe. Un fallo de escritura que no dice DONDE no se puede accionar.
		/// </summary>
		string Nombre { get; }

		/// <summary>
		/// Si hay donde escribir AHORA. Suelta puede ser false con carpeta elegida, porque el navegador
		/// degrada el permiso al reabrir; hospedada es siempre true, porque los archivos son de Gnothi y
		/// nadie le revoca a nadie el permiso sobre lo suyo.
		/// </summary>
		bool Disponible { get; }

		/// <summary>Las rutas relativas de lo que hay en el destino, siempre con barra.</summary>
		ValueTask<ResultadoDeListado> ListarAsync();

		ValueTask<ResultadoDeLectura> LeerAsync(string ruta);

		ValueTask<ResultadoDeEscritura> EscribirAsync(string ruta, string contenidoBase64);
	}

	/// <summary>
	/// El destino cuando la app corre HOSPEDADA dentro de Gnothi: los bytes cruzan el puente nativo y
	/// aterrizan en los archivos de Gnothi, un directorio por modulo.
	/// <para>
	/// Entra como delegados y no como interop, por la misma razon que la fuente de eventos: esta clase no
	/// sabe de JS ni de WebViews, asi que se puede sostener con funciones en memoria para probarla. Quien
	/// la construye -- la caratula, cuando detecta que esta hospedada -- es quien sabe llamar al puente.
	/// </para>
	/// <para>
	/// GNOTHI NO COMPONE NADA: recibe una ruta y unos bytes que el modulo ya escribio y los deja donde
	/// van. No es un segundo escritor del journal.
	/// </para>
	/// </summary>
	public sealed class DestinoPorPuente : IDestinoDelJournal
	{
		private readonly Func<ValueTask<ResultadoDeListado>> listar;
		private readonly Func<string, ValueTask<ResultadoDeLectura>> leer;
		private readonly Func<string, string, ValueTask<ResultadoDeEscritura>> escribir;

		public DestinoPorPuente(
			Func<ValueTask<ResultadoDeListado>> listar,
			Func<string, ValueTask<ResultadoDeLectura>> leer,
			Func<string, string, ValueTask<ResultadoDeEscritura>> escribir,
			string nombre = "Gnothi")
		{
			ArgumentNullException.ThrowIfNull(listar);
			ArgumentNullException.ThrowIfNull(leer);
			ArgumentNullException.ThrowIfNull(escribir);
			ArgumentException.ThrowIfNullOrWhiteSpace(nombre);

			this.listar = listar;
			this.leer = leer;
			this.escribir = escribir;
			Nombre = nombre;
		}

		public string Nombre { get; }

		/// <summary>Hospedada siempre hay donde escribir: el destino es de Gnothi.</summary>
		public bool Disponible => true;

		public ValueTask<ResultadoDeListado> ListarAsync() => listar();

		public ValueTask<ResultadoDeLectura> LeerAsync(string ruta) => leer(ruta);

		public ValueTask<ResultadoDeEscritura> EscribirAsync(string ruta, string contenidoBase64) =>
			escribir(ruta, contenidoBase64);
	}
}
