using System.Net.Http.Json;
using System.Text.Json;
using Concesionario.App.Wording;
using Microsoft.JSInterop;

namespace Concesionario.Wasm
{
	/// <summary>
	/// LO QUE ESTA PANTALLA DICE CON SUS PROPIAS PALABRAS, en el idioma que la persona eligio en su
	/// dispositivo. Las frases que compone el dominio NO pasan por aqui: esas las trae la puerta ya
	/// dichas y se citan letra por letra.
	/// <para>
	/// La eleccion se lee UNA vez, al arrancar, de navigator.languages, que es la lista ordenada que
	/// el sistema operativo le da al navegador. No se lee la cultura del proceso: en wasm esa cultura
	/// es la del runtime y no la de la persona.
	/// </para>
	/// <para>
	/// Un idioma que este modulo no habla cae al del registro, que es el espanol: el concesionario
	/// lleva el registro en espanol, y media pantalla en un idioma que nadie eligio es peor que una
	/// entera en el idioma de la institucion.
	/// </para>
	/// </summary>
	public sealed class Texts
	{
		/// <summary>Las etiquetas que este modulo tiene escritas, en el orden en que se prefieren.</summary>
		public static readonly string[] TheOnesThereAre = { "es-CR", "en" };

		private const string TheOneOfTheRecord = "es-CR";

		private IReadOnlyDictionary<string, string> texts =
			new Dictionary<string, string>(StringComparer.Ordinal);

		/// <summary>
		/// EL TITULO QUE EL MODULO DECLARA, y que NO se traduce: es voz de la institucion y no texto de
		/// esta caratula. Vive aca y no en los catalogos justamente para que quien traduzca no lo vea:
		/// lo que no esta en el archivo no se puede traducir por descuido.
		/// </summary>
		public const string TheModuleTitle = "Mi plan de mantenimiento";

		/// <summary>La etiqueta completa del catalogo que se cargo, por ejemplo es-CR.</summary>
		public string Tag { get; private set; } = TheOneOfTheRecord;

		/// <summary>
		/// La subetiqueta primaria, que es lo unico que la puerta necesita para pedirle sus frases en
		/// este idioma.
		/// </summary>
		public string Language => ThePrimarySubtagOf(Tag);

		/// <summary>Si la eleccion de la persona se pudo atender, o se cayo al idioma del registro.</summary>
		public bool TheChoiceWasAnswered { get; private set; }

		/// <summary>
		/// El texto de esa clave. Una clave que falta se dice con su propio nombre entre llaves y no
		/// revienta: una pantalla a la que le falta una palabra sigue sirviendo, y el hueco se ve.
		/// </summary>
		public string this[string key] =>
			texts.TryGetValue(key, out string said) ? said : "{" + key + "}";

		/// <summary>
		/// El texto de esa clave con sus huecos llenos. Los huecos se nombran, no se numeran: una frase
		/// traducida cambia el orden de sus partes y un hueco numerado se va al lugar equivocado sin
		/// que nada avise.
		/// </summary>
		public string With(string key, params (string Hole, string Value)[] values)
		{
			string said = this[key];

			foreach ((string hole, string value) in values)
				said = said.Replace("{" + hole + "}", value);

			return said;
		}

		/// <summary>
		/// Lo que dice un apunte que compuso una capa sin catalogo. La ausencia entra como
		/// <see cref="Note.None"/> y sale como la cadena vacia, nunca como una clave vacia entre llaves;
		/// una cita sale con las palabras con que llego.
		/// </summary>
		public string Say(Note note)
		{
			if (note is null || !note.MustBeSaid) return "";

			return note.Key.Length > 0 ? With(note.Key, note.Holes) : note.Quoted;
		}

		/// <summary>
		/// EL MISMO RENDERIZADOR con que la puerta compone las frases del dominio. Una cifra se escribe
		/// en UN solo lugar: la de una tabla y la de la frase de al lado son la misma pantalla, y dos
		/// implementaciones del separador de miles son la frase rival en pequeno.
		/// </summary>
		public Phrases ThePhrases => Phrases.Of(Language);

		/// <summary>
		/// COMO SE DICE EL NOMBRE DE UN MIEMBRO DE UN CONJUNTO CERRADO. NO es dato del registro: los
		/// elige el dominio de una lista cerrada, y en pantalla son indistinguibles a ojo de un nombre
		/// que si vino del registro. El codigo sigue cruzando intacto y esto es lo que se lee.
		/// </summary>
		public string HowItIsSaid(string family, string code) => ThePhrases.HowItIsSaid(family, code);

		/// <summary>Un numero entero con el separador de miles del idioma.</summary>
		public string Grouped(int value) => ThePhrases.Grouped(value);

		/// <summary>Cuantos dias, dicho entero para que "1 dias" no aparezca en ningun idioma.</summary>
		public string Days(int days) => ThePhrases.Days(days);

		/// <summary>Un dia del registro dicho en palabras, en el orden que pide el idioma.</summary>
		public string ADay(string yyyyMMdd) => ThePhrases.ADay(yyyyMMdd);

		/// <summary>El mismo dia, corto, para una linea de tiempo.</summary>
		public string AShortDay(string yyyyMMdd) => ThePhrases.AShortDay(yyyyMMdd);

		/// <summary>
		/// Lee lo que la persona eligio en su dispositivo y carga el catalogo que le corresponde. Si la
		/// lectura falla -- un WebView sin navigator, un archivo que no esta -- se queda con el idioma
		/// del registro en vez de dejar la pantalla sin palabras.
		/// </summary>
		public async Task NegotiateAsync(IJSRuntime js, HttpClient http)
		{
			ArgumentNullException.ThrowIfNull(js);
			ArgumentNullException.ThrowIfNull(http);

			string[] asked = await WhatTheDeviceAskedForAsync(js);
			Tag = TheFirstOneWeSpeak(asked);
			TheChoiceWasAnswered = asked.Length > 0
				&& ThePrimarySubtagOf(asked[0]) == ThePrimarySubtagOf(Tag);
			texts = await LoadAsync(http, Tag);

			await SetTheDocumentsLanguageAsync(js);
		}

		/// <summary>
		/// Le dice al documento en que idioma quedo, que es lo que leen el lector de pantalla y el
		/// guionado. Se fija DESPUES de negociar y nunca en el HTML: el documento se sirve igual para
		/// los dos idiomas, asi que una etiqueta escrita a mano seria falsa para la mitad de quienes
		/// lo abren.
		/// </summary>
		private async Task SetTheDocumentsLanguageAsync(IJSRuntime js)
		{
			try
			{
				await using IJSObjectReference module =
					await js.InvokeAsync<IJSObjectReference>("import", "./language-interop.js");

				await module.InvokeVoidAsync("setTheDocumentsLanguage", Tag);
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
		private static async Task<string[]> WhatTheDeviceAskedForAsync(IJSRuntime js)
		{
			try
			{
				await using IJSObjectReference module =
					await js.InvokeAsync<IJSObjectReference>("import", "./language-interop.js");

				return await module.InvokeAsync<string[]>("theLanguagesAskedFor");
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
		/// La primera etiqueta pedida cuya lengua este modulo habla, respetando el ORDEN de la persona.
		/// Se compara por subetiqueta primaria: quien pide en-GB habla ingles, y este modulo no tiene
		/// un ingles por region que ofrecerle.
		/// </summary>
		private static string TheFirstOneWeSpeak(string[] asked)
		{
			foreach (string one in asked)
			{
				string tongue = ThePrimarySubtagOf(one);

				foreach (string ours in TheOnesThereAre)
					if (ThePrimarySubtagOf(ours) == tongue) return ours;
			}

			return TheOneOfTheRecord;
		}

		private static async Task<IReadOnlyDictionary<string, string>> LoadAsync(HttpClient http, string tag)
		{
			try
			{
				JsonElement root = await http.GetFromJsonAsync<JsonElement>("text/" + tag + ".json");
				var flat = new Dictionary<string, string>(StringComparer.Ordinal);
				Flatten(root, "", flat);
				return flat;
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
		private static void Flatten(JsonElement node, string prefix, Dictionary<string, string> flat)
		{
			if (node.ValueKind != JsonValueKind.Object)
			{
				if (prefix.Length > 0 && node.ValueKind == JsonValueKind.String)
					flat[prefix] = node.GetString() ?? "";
				return;
			}

			foreach (JsonProperty leaf in node.EnumerateObject())
			{
				if (leaf.Name == "//") continue;
				Flatten(leaf.Value, prefix.Length == 0 ? leaf.Name : prefix + "." + leaf.Name, flat);
			}
		}

		private static string ThePrimarySubtagOf(string tag)
		{
			if (string.IsNullOrWhiteSpace(tag)) return "";

			string clean = tag.Trim().ToLowerInvariant();
			int dash = clean.IndexOf('-');

			return dash < 0 ? clean : clean.Substring(0, dash);
		}
	}
}
