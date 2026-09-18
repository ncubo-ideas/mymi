using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Municipalidad.Wasm
{
	/// <summary>
	/// Lo que esta pantalla dice con sus propias palabras, en el idioma que la persona eligio en su
	/// dispositivo. Las frases que compone el dominio NO pasan por aqui: esas las trae la puerta ya
	/// dichas, y lo unico que esta capa le manda es la etiqueta del idioma.
	/// <para>
	/// La eleccion se lee UNA vez, al arrancar, de navigator.languages, que es la lista ordenada que el
	/// sistema operativo le da al navegador. No se lee la cultura del proceso: en wasm esa cultura es la
	/// del runtime y no la de la persona.
	/// </para>
	/// <para>
	/// Un idioma que esta app no habla cae al del registro, que es el espanol: la municipalidad lleva la
	/// cuenta en espanol y una pantalla a medias en un idioma que nadie eligio es peor que una entera en
	/// el idioma de la institucion.
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
		/// EL TITULO QUE EL MODULO DECLARA EN SU MANIFIESTO, y que NO se traduce: es voz de la
		/// institucion, no texto de esta carátula. Vive aca y no en los catalogos justamente para que
		/// quien traduzca no lo vea: lo que no esta en el archivo no se puede traducir por descuido.
		/// </summary>
		public const string TituloDelModulo = "MONTEVERDE Municipal Services";

		/// <summary>La etiqueta completa del catalogo que se cargo, por ejemplo es-CR.</summary>
		public string Etiqueta { get; private set; } = LaDelRegistro;

		/// <summary>
		/// La subetiqueta primaria, que es lo unico que la puerta necesita para pedirle al dominio sus
		/// frases en este idioma.
		/// </summary>
		public string Idioma => SubetiquetaPrimaria(Etiqueta);

		/// <summary>Si la eleccion de la persona se pudo atender, o se cayo al idioma del registro.</summary>
		public bool AtendioLaEleccion { get; private set; }

		/// <summary>
		/// El texto de esa clave. Una clave que falta se dice con su propio nombre entre llaves y no
		/// revienta: una pantalla a la que le falta una palabra sigue sirviendo, y el hueco se ve.
		/// </summary>
		public string this[string clave] => textos.TryGetValue(clave, out string dicho) ? dicho : "{" + clave + "}";

		/// <summary>
		/// El texto de esa clave con sus huecos llenos. Los huecos se nombran, no se numeran: una frase
		/// traducida cambia el orden de sus partes y un hueco numerado se va al lugar equivocado sin que
		/// nada avise.
		/// </summary>
		public string Con(string clave, params (string Hueco, string Valor)[] valores)
		{
			string dicho = this[clave];
			foreach ((string hueco, string valor) in valores)
				dicho = dicho.Replace("{" + hueco + "}", valor);
			return dicho;
		}

		/// <summary>
		/// La frase de un apunte que compuso una capa sin catalogo. La ausencia entra como
		/// <see cref="Apunte.Ninguno"/> y sale como la cadena vacia, nunca como una clave vacia entre
		/// llaves.
		/// </summary>
		public string Diga(Apunte apunte) =>
			apunte.HayQueDecirlo ? Con(apunte.Clave, apunte.Huecos) : "";

		/// <summary>
		/// COMO SE DICE EL NOMBRE DE UN ESTADO que el dominio eligio de una lista cerrada. NO es dato del
		/// registro: `cambio-de-tarifa` y `sin-consulta` los eligio el dominio, no la municipalidad, y en
		/// pantalla son indistinguibles a ojo de un rubro que si vino del registro. El codigo sigue
		/// cruzando intacto -- es identidad, y con el se decide que ofrecer -- y esto es lo que se lee.
		/// </summary>
		public string ComoSeDice(string codigo)
		{
			return Municipalidad.App.Frases.LaRedaccion.Etiqueta(LasFrases, codigo);
		}

		/// <summary>
		/// EL MISMO RENDERIZADOR con que la puerta compone las frases del dominio. Una cifra se escribe
		/// en UN solo lugar: la de una tabla y la de la frase de al lado son la misma pantalla, y dos
		/// implementaciones de la coma decimal son la frase rival en pequeno.
		/// </summary>
		private Municipalidad.App.Frases.Frases LasFrases => Municipalidad.App.Frases.Frases.De(Idioma);

		/// <summary>Un monto, con los digitos que trae su columna y sin simbolo de moneda.</summary>
		public string Monto(decimal monto) => LasFrases.Monto(monto);

		/// <summary>Una proporcion dicha como porcentaje.</summary>
		public string Porcentaje(decimal proporcion) => LasFrases.Porcentaje(proporcion);

		/// <summary>Cuantos dias, dicho entero para que "1 dias" no aparezca en ningun idioma.</summary>
		public string Dias(int dias) => LasFrases.Dias(dias);

		/// <summary>
		/// Lee lo que la persona eligio en su dispositivo y carga el catalogo que le corresponde. Si la
		/// lectura falla -- un WebView sin navigator, un archivo que no esta -- se queda con el idioma del
		/// registro en vez de dejar la pantalla sin palabras.
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

			AtendioLaEleccion = pedidas.Length > 0
				&& SubetiquetaPrimaria(pedidas[0]) == SubetiquetaPrimaria(Etiqueta);

			await FijarElIdiomaDelDocumentoAsync(js);
		}

		/// <summary>
		/// Le dice al documento en que idioma quedo, que es lo que leen el lector de pantalla y el
		/// guionado. Se fija DESPUES de negociar y nunca en el HTML: el documento se sirve igual para
		/// los dos idiomas, asi que una etiqueta escrita a mano seria falsa para la mitad de quienes lo
		/// abren.
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
		/// La primera etiqueta pedida cuya lengua esta app habla, respetando el ORDEN de la persona. Se
		/// compara por subetiqueta primaria: quien pide en-GB habla ingles, y esta app no tiene un ingles
		/// por region que ofrecerle.
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
}
