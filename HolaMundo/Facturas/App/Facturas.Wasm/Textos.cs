using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Facturas.Wasm;

/// <summary>
/// Lo que esta pantalla dice con sus propias palabras, en el idioma que la persona eligio en su
/// dispositivo. Nada que haya salido del journal pasa por aqui: el numero de una factura, su
/// descripcion y sus montos se muestran tal como el proveedor los asento.
/// <para>
/// La eleccion se lee UNA vez, al arrancar, de navigator.languages. No se lee la cultura del
/// proceso: en wasm esa cultura es la del runtime y no la de la persona.
/// </para>
/// <para>
/// Un idioma que esta app no habla cae al del registro, que es el espanol del super.
/// </para>
/// </summary>
public sealed class Textos
{
	/// <summary>Las etiquetas que esta app tiene escritas, en el orden en que se prefieren.</summary>
	public static readonly string[] LasQueHay = { "es-CR", "en" };

	private const string LaDelRegistro = "es-CR";

	private const string LenguaDelRegistro = "es";

	/// <summary>
	/// EL TITULO QUE EL MODULO DECLARA, y que NO se traduce: es voz de la institucion. Vive aca y no
	/// en los catalogos para que quien traduzca no lo vea.
	/// </summary>
	public const string TituloDelModulo = "Mis compras · Súper El Triunfo";

	/// <summary>El nombre con que el super se nombra a si mismo. Voz de la institucion: no se traduce.</summary>
	public const string Institucion = "Súper El Triunfo";

	/// <summary>
	/// El lema del super, en el idioma negociado. A DIFERENCIA del nombre, este si se traduce -se
	/// pidio expresamente para la revision visual en ingles-, asi que vive en el catalogo
	/// (institucion.lema) y no como constante fija. Con el mismo respaldo que cualquier otra clave:
	/// si el catalogo no cargo, se ve "{institucion.lema}" y no una pantalla en blanco.
	/// </summary>
	public string Lema => this["institucion.lema"];

	private static readonly NumberFormatInfo comoSeEscribeEnEspanol = new NumberFormatInfo
	{
		NumberDecimalSeparator = ",",
		NumberGroupSeparator = ".",
		NumberGroupSizes = new[] { 3 },
	};

	private static readonly NumberFormatInfo comoSeEscribeEnIngles = new NumberFormatInfo
	{
		NumberDecimalSeparator = ".",
		NumberGroupSeparator = ",",
		NumberGroupSizes = new[] { 3 },
	};

	private IReadOnlyDictionary<string, string> textos =
		new Dictionary<string, string>(StringComparer.Ordinal);

	/// <summary>La etiqueta completa del catalogo que se cargo, por ejemplo es-CR.</summary>
	public string Etiqueta { get; private set; } = LaDelRegistro;

	/// <summary>La subetiqueta primaria de la etiqueta cargada.</summary>
	public string Idioma => SubetiquetaPrimaria(Etiqueta);

	/// <summary>
	/// El texto de esa clave. Una clave que falta se dice con su propio nombre entre llaves y no
	/// revienta: el hueco se ve y la pantalla sigue sirviendo.
	/// </summary>
	public string this[string clave] =>
		textos.TryGetValue(clave, out string? dicho) ? dicho : "{" + clave + "}";

	/// <summary>
	/// El texto de esa clave con sus huecos llenos. Los huecos se nombran, no se numeran: una frase
	/// traducida cambia el orden de sus partes y un hueco numerado se va al lugar equivocado.
	/// </summary>
	public string Con(string clave, params (string Hueco, string Valor)[] valores)
	{
		string dicho = this[clave];
		foreach ((string hueco, string valor) in valores)
			dicho = dicho.Replace("{" + hueco + "}", valor);
		return dicho;
	}

	/// <summary>
	/// UN MONTO, con el separador del idioma y SIEMPRE con dos decimales: la escala del decimal no
	/// sobrevive el journal, asi que un formato implicito mostraria montos distintos antes y despues
	/// de reabrir la app. Sin simbolo de moneda, porque la institucion no informa ninguna.
	/// </summary>
	public string Monto(decimal monto) =>
		monto.ToString("N2", Idioma == LenguaDelRegistro ? comoSeEscribeEnEspanol : comoSeEscribeEnIngles);

	/// <summary>Cuantas lineas o cuantos eventos: un entero con el separador de miles del idioma.</summary>
	public string Cuenta(long cuantos) =>
		cuantos.ToString("N0", Idioma == LenguaDelRegistro ? comoSeEscribeEnEspanol : comoSeEscribeEnIngles);

	/// <summary>
	/// UN INSTANTE, en ano-mes-dia y hora de reloj. NO cambia con el idioma: el orden ano-mes-dia no
	/// se puede leer al reves, y dd/MM se lee MM/dd en la otra mitad del mundo.
	/// </summary>
	public string Instante(DateTime instante) =>
		instante.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

	private static readonly string[] mesesCortosEs =
		{ "ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "set", "oct", "nov", "dic" };

	private static readonly string[] mesesCortosEn =
		{ "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

	/// <summary>
	/// LA FECHA DE UNA FACTURA, corta, para la etiqueta debajo de una barra del grafico de
	/// evolucion. Deriva SOLO de Fecha -el mismo dato que ya muestra Instante()-, nada nuevo. Va con
	/// nombre de mes -no numeros- porque ahi no hay ambiguedad de dia/mes que evitar, y a diferencia
	/// de Instante() un rotulo de grafico si tiene sentido que hable el idioma de la persona.
	/// </summary>
	public string FechaCorta(DateTime fecha) => Idioma == LenguaDelRegistro
		? $"{fecha.Day} {mesesCortosEs[fecha.Month - 1]}"
		: $"{mesesCortosEn[fecha.Month - 1]} {fecha.Day}";

	/// <summary>
	/// SOLO el mes, abreviado, para la fichita de fecha de "Recent purchases" (dia grande arriba,
	/// mes chico al medio, año chico abajo -tres piezas separadas, no una frase-). Mismos arreglos
	/// que FechaCorta(), en el idioma negociado: "Aug" en ingles, nunca "Ago" a la fuerza.
	/// </summary>
	public string MesCorto(DateTime fecha) =>
		Idioma == LenguaDelRegistro ? mesesCortosEs[fecha.Month - 1] : mesesCortosEn[fecha.Month - 1];

	private static readonly string[] mesesLargosEs =
	{
		"enero", "febrero", "marzo", "abril", "mayo", "junio",
		"julio", "agosto", "setiembre", "octubre", "noviembre", "diciembre",
	};

	private static readonly string[] mesesLargosEn =
	{
		"January", "February", "March", "April", "May", "June",
		"July", "August", "September", "October", "November", "December",
	};

	/// <summary>
	/// "August 2026" / "agosto 2026": la etiqueta de una opcion del selector de periodo. Mes y año
	/// son los mismos que ya trae cada Fecha real -esto solo los nombra-, nunca un periodo que la
	/// app se inventa.
	/// </summary>
	public string PeriodoLargo(int anio, int mes) => Idioma == LenguaDelRegistro
		? $"{mesesLargosEs[mes - 1]} {anio}"
		: $"{mesesLargosEn[mes - 1]} {anio}";

	/// <summary>
	/// UN PORCENTAJE, con un decimal y el separador del idioma, SIEMPRE con el valor absoluto: el
	/// signo (+/-) es de quien llama, porque "sube" y "baja" a veces se dicen con una palabra y no
	/// con un signo delante.
	/// </summary>
	public string Porcentaje(decimal valor) =>
		Math.Abs(valor).ToString("N1", Idioma == LenguaDelRegistro ? comoSeEscribeEnEspanol : comoSeEscribeEnIngles) + "%";

	/// <summary>
	/// FORZADO A INGLES: pedido explicito y repetido de que la version visible de TODA esta app se
	/// muestre en ingles sin importar lo que pida el dispositivo -asi navigator.languages niegue
	/// "en" (el caso real: el dispositivo de la persona que revisa pide espanol primero), la
	/// revision tiene que verse en ingles. El mecanismo de negociacion real por
	/// navigator.languages (LaPrimeraQueHablamos/LoQuePidioElDispositivoAsync, mas abajo) sigue
	/// intacto y sin tocar -no se borro nada, no se hardcodeo ningun texto-, simplemente esta
	/// pantalla no lo consulta todavia: alcanza con volver a llamarlo aca para reactivar el
	/// bilingue el dia que se vuelva a pedir.
	/// </summary>
	public async Task NegociarAsync(IJSRuntime js, HttpClient http)
	{
		ArgumentNullException.ThrowIfNull(js);
		ArgumentNullException.ThrowIfNull(http);

		const string etiquetaForzada = "en";

		Etiqueta = etiquetaForzada;
		textos = await CargarAsync(http, Etiqueta);

		if (textos.Count == 0 && Etiqueta != LaDelRegistro)
		{
			Etiqueta = LaDelRegistro;
			textos = await CargarAsync(http, Etiqueta);
		}

		await FijarElIdiomaDelDocumentoAsync(js);
	}

	/// <summary>
	/// Le dice al documento en que idioma quedo, que es lo que leen el lector de pantalla y el
	/// guionado. Se fija DESPUES de negociar y nunca en el HTML: el documento se sirve igual para los
	/// dos idiomas.
	/// <para>
	/// De la misma pasada se reemplazan los textos de la cascara -- el aviso de error con su
	/// enlace -- que estan escritos a mano en el HTML. Se ven cuando la app NO arranco, que es justo
	/// cuando no hay catalogo con que decirlos: por eso nacen en el idioma del registro y esto los
	/// reemplaza, y quien nunca llega hasta aca ve la caida de siempre.
	/// </para>
	/// </summary>
	private async Task FijarElIdiomaDelDocumentoAsync(IJSRuntime js)
	{
		try
		{
			await using IJSObjectReference modulo =
				await js.InvokeAsync<IJSObjectReference>("import", "./idioma-interop.js");

			await modulo.InvokeVoidAsync("fijarIdiomaDelDocumento", Etiqueta);

			await modulo.InvokeVoidAsync("fijarTextosDeLaCascara",
				this["comun.errorInesperado"], this["comun.recargar"]);
		}
		catch (JSException)
		{
		}
		catch (InvalidOperationException)
		{
		}
	}

	/// <summary>
	/// Lo que la persona pidio en su dispositivo, en su orden. La lectura entra por un modulo propio
	/// y nunca por eval: la politica de contenido con que se sirve un modulo hospedado admite
	/// WebAssembly y no admite eval.
	/// </summary>
	private static async Task<string[]> LoQuePidioElDispositivoAsync(IJSRuntime js)
	{
		try
		{
			await using IJSObjectReference modulo =
				await js.InvokeAsync<IJSObjectReference>("import", "./idioma-interop.js");

			return await modulo.InvokeAsync<string[]>("idiomas");
		}
		catch (JSException)
		{
			return [];
		}
		catch (InvalidOperationException)
		{
			return [];
		}
	}

	/// <summary>
	/// La primera etiqueta pedida cuya lengua esta app habla, respetando el ORDEN de la persona.
	/// Quien pide en-GB habla ingles, y esta app no tiene un ingles por region que ofrecerle.
	/// </summary>
	private static string LaPrimeraQueHablamos(string[] pedidas)
	{
		foreach (string pedida in pedidas)
		{
			string lengua = SubetiquetaPrimaria(pedida);
			foreach (string nuestra in LasQueHay)
				if (SubetiquetaPrimaria(nuestra) == lengua) return nuestra;
		}
		return LaDelRegistro;
	}

	private static async Task<IReadOnlyDictionary<string, string>> CargarAsync(HttpClient http, string etiqueta)
	{
		try
		{
			JsonElement raiz = await http.GetFromJsonAsync<JsonElement>("texto/" + etiqueta + ".json");
			var plano = new Dictionary<string, string>(StringComparer.Ordinal);
			Aplanar(raiz, "", plano);
			return plano;
		}
		catch (HttpRequestException)
		{
			return new Dictionary<string, string>(StringComparer.Ordinal);
		}
		catch (JsonException)
		{
			return new Dictionary<string, string>(StringComparer.Ordinal);
		}
	}

	/// <summary>
	/// El catalogo viene anidado por pantalla, para que se lea, y se guarda plano con la clave
	/// separada por puntos, para que se busque.
	/// </summary>
	private static void Aplanar(JsonElement nodo, string prefijo, Dictionary<string, string> plano)
	{
		if (nodo.ValueKind != JsonValueKind.Object)
		{
			if (prefijo.Length > 0 && nodo.ValueKind == JsonValueKind.String)
				plano[prefijo] = nodo.GetString() ?? "";
			return;
		}

		foreach (JsonProperty hoja in nodo.EnumerateObject())
		{
			if (hoja.Name == "//") continue;
			Aplanar(hoja.Value, prefijo.Length == 0 ? hoja.Name : prefijo + "." + hoja.Name, plano);
		}
	}

	private static string SubetiquetaPrimaria(string etiqueta)
	{
		if (string.IsNullOrWhiteSpace(etiqueta)) return "";
		string limpia = etiqueta.Trim().ToLowerInvariant();
		int guion = limpia.IndexOf('-');
		return guion < 0 ? limpia : limpia.Substring(0, guion);
	}
}
