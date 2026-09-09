namespace Municipalidad.Wasm
{
	/// <summary>
	/// Por donde se le habla a la institucion, leido de wwwroot/appsettings.json y nunca de una
	/// constante compilada: el mismo build tiene que poder apuntar a otro host.
	/// </summary>
	public sealed class AjustesDeLaMunicipalidad
	{
		/// <summary>La raiz del feed. Relativa en produccion, para llegar por el mismo origen.</summary>
		public string BaseUrl { get; set; } = "eventos/";

		/// <summary>Que clase de sujeto pide sus eventos.</summary>
		public string Recurso { get; set; } = "contribuyente";

		/// <summary>Con que identidad se le piden a la institucion los eventos de esta cuenta.</summary>
		public string Id { get; set; } = "CEDULA-DEMO-1";
	}
}
