using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Cargas.Wasm
{
	/// <summary>
	/// Lo que esta pantalla dice con sus propias palabras, en el idioma que la persona eligio en su
	/// dispositivo. Las frases que compone el dominio NO pasan por aqui: esas las trae la puerta ya
	/// dichas, y lo unico que esta capa le manda es la etiqueta del idioma.
	/// <para>
	/// La eleccion se lee UNA vez, al arrancar, de navigator.languages, que es la lista ordenada que
	/// el sistema operativo le da al navegador. No se lee la cultura del proceso: en wasm esa cultura
	/// es la del runtime y no la de la persona.
	/// </para>
	/// <para>
	/// Un idioma que esta app no habla cae al del registro, que es el espanol: la estacion lleva la
	/// bitacora en espanol y una pantalla a medias en un idioma que nadie eligio es peor que una
	/// entera en el idioma de la institucion.
	/// </para>
	/// </summary>
	public sealed class Textos
	{
		/// <summary>Las etiquetas que esta app tiene escritas, en el orden en que se prefieren.</summary>
		public static readonly string[] LasQueHay = { "es-CR", "en" };

		private const string LaDelRegistro = "es-CR";

		private IReadOnlyDictionary<string, string> textos =
			new Dictionary<string, string>(StringComparer.Ordinal);

		/// <summary>
		/// EL NOMBRE Y EL CLAIM QUE LA INSTITUCION DECLARA, y que NO se traducen: son voz de la
		/// estacion, no texto de esta carátula. Viven aca y no en los catalogos justamente para que
		/// quien traduzca no los vea: lo que no esta en el archivo no se puede traducir por descuido.
		/// </summary>
		public const string NombreDeLaMarca = "Estaciones Faro";

		/// <summary>El claim de la estacion, en su idioma. Tampoco se traduce.</summary>
		public const string ClaimDeLaMarca = "Tu camino, claro";

		/// <summary>La etiqueta completa del catalogo que se cargo, por ejemplo es-CR.</summary>
		public string Etiqueta { get; private set; } = LaDelRegistro;

		/// <summary>
		/// La subetiqueta primaria, que es lo unico que la puerta necesita para pedirle al dominio
		/// sus frases en este idioma.
		/// </summary>
		public string Idioma => SubetiquetaPrimaria(Etiqueta);

		/// <summary>
		/// El texto de esa clave. Una clave que falta se dice con su propio nombre entre llaves y no
		/// revienta: una pantalla a la que le falta una palabra sigue sirviendo, y el hueco se ve.
		/// </summary>
		public string this[string clave] => textos.TryGetValue(clave, out string dicho) ? dicho : "{" + clave + "}";

		/// <summary>
		/// Si este catalogo tiene escrita esa clave. Existe para el canal de fallos, que es el
		/// unico que recibe claves de afuera -- el browser y el motor nombran sus errores como
		/// quieren -- y tiene que poder caer a otra frase en vez de mostrar un {hueco} crudo.
		/// </summary>
		public bool Conoce(string clave) => textos.ContainsKey(clave);

		/// <summary>
		/// El texto de esa clave con sus huecos llenos. Los huecos se nombran, no se numeran: una
		/// frase traducida cambia el orden de sus partes y un hueco numerado se va al lugar
		/// equivocado sin que nada avise.
		/// </summary>
		public string Con(string clave, params (string Hueco, string Valor)[] valores)
		{
			string dicho = this[clave];
			foreach ((string hueco, string valor) in valores)
				dicho = dicho.Replace("{" + hueco + "}", valor);
			return dicho;
		}

		/// <summary>
		/// COMO SE DICE EL NOMBRE DE UN ESTADO que el dominio eligio de una lista cerrada. NO es dato
		/// del registro: `dato-sospechoso` y `sin-lecturas` los eligio el dominio, no la estacion, y
		/// en pantalla son indistinguibles a ojo de un surtidor que si vino del registro. El codigo
		/// sigue cruzando intacto -- es identidad, y con el se decide que ofrecer -- y esto es lo que
		/// se lee.
		/// </summary>
		public string ComoSeDice(string codigo) =>
			Cargas.App.Frases.LaRedaccion.Etiqueta(LasFrases, codigo);

		/// <summary>
		/// EL MISMO RENDERIZADOR con que la puerta compone las frases del dominio. Una cifra se
		/// escribe en UN solo lugar: la de un instrumento y la de la frase de al lado son la misma
		/// pantalla, y dos implementaciones de la coma decimal son la frase rival en pequeno.
		/// </summary>
		public Cargas.App.Frases.Frases LasFrases => Cargas.App.Frases.Frases.De(Idioma);

		/// <summary>Una cifra con un decimal: rendimiento, litros, ritmos.</summary>
		public string Cifra(decimal medida) => LasFrases.Cifra(medida);

		/// <summary>Una cifra entera: kilometros, cuentas.</summary>
		public string Entera(decimal medida) => LasFrases.Entera(medida);

		/// <summary>Dinero con el simbolo de su moneda.</summary>
		public string Monto(decimal monto, string moneda) => LasFrases.Monto(monto, moneda);

		/// <summary>El simbolo de una moneda, que no se traduce: es con lo que la estacion cobra.</summary>
		public string Simbolo(string moneda) => Cargas.App.Frases.Frases.Simbolo(moneda);

		/// <summary>Una magnitud con su unidad, cuyo simbolo tampoco se traduce.</summary>
		public string ConUnidad(decimal medida, string simbolo) => LasFrases.ConUnidad(medida, simbolo);

		/// <summary>La misma magnitud dicha entera, para los kilometros.</summary>
		public string ConUnidadEntera(decimal medida, string simbolo) =>
			LasFrases.ConUnidadEntera(medida, simbolo);

		/// <summary>Cuantos dias, dicho entero para que "1 dias" no aparezca en ningun idioma.</summary>
		public string Dias(int dias) => LasFrases.Dias(dias);

		/// <summary>Un dia de calendario en anio-mes-dia, que no cambia con el idioma.</summary>
		public string Dia(DateTime dia) => LasFrases.Dia(dia);

		/// <summary>
		/// Lee lo que la persona eligio en su dispositivo y carga el catalogo que le corresponde. Si
		/// la lectura falla -- un WebView sin navigator, un archivo que no esta -- se queda con el
		/// idioma del registro en vez de dejar la pantalla sin palabras.
		/// <para>
		/// Y le dice a <see cref="Formato"/> en que idioma quedo: toda cifra de la carátula se
		/// escribe con el mismo renderizador, y aqui es donde se entera, antes del primer render.
		/// </para>
		/// </summary>
		public async Task NegociarAsync(IJSRuntime js, HttpClient http)
		{
			ArgumentNullException.ThrowIfNull(js);
			ArgumentNullException.ThrowIfNull(http);

			string[] pedidas = await LoQuePidioElDispositivoAsync(js);

			Etiqueta = LaPrimeraQueHablamos(pedidas);
			textos = await CargarAsync(http, Etiqueta);

			if (textos.Count == 0 && Etiqueta != LaDelRegistro)
			{
				Etiqueta = LaDelRegistro;
				textos = await CargarAsync(http, Etiqueta);
			}

			Formato.Hablar(Idioma);

			await FijarElIdiomaDelDocumentoAsync(js);
		}

		/// <summary>
		/// Le dice al documento en que idioma quedo, que es lo que leen el lector de pantalla y el
		/// guionado. Se fija DESPUES de negociar y nunca en el HTML: el documento se sirve igual para
		/// los dos idiomas, asi que una etiqueta escrita a mano seria falsa para la mitad de quienes
		/// lo abren.
		/// </summary>
		private async Task FijarElIdiomaDelDocumentoAsync(IJSRuntime js)
		{
			try
			{
				await using IJSObjectReference modulo =
					await js.InvokeAsync<IJSObjectReference>("import", "./idioma-interop.js");

				await modulo.InvokeVoidAsync("fijarIdiomaDelDocumento", Etiqueta);
			}
			catch (JSException)
			{
			}
			catch (InvalidOperationException)
			{
			}
		}

		/// <summary>
		/// Lo que la persona pidio en su dispositivo, en su orden. La lectura entra por un modulo
		/// propio y nunca por eval: la politica de contenido con que se sirve un modulo hospedado
		/// admite WebAssembly y no admite eval.
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
		/// Se compara por subetiqueta primaria: quien pide en-GB habla ingles, y esta app no tiene un
		/// ingles por region que ofrecerle.
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

		private static async Task<IReadOnlyDictionary<string, string>> CargarAsync(HttpClient http,
			string etiqueta)
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
}
