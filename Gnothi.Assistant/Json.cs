using System.Collections;
using System.Text.Json;
using Gnothi.Host;

namespace Gnothi.Assistant;

// JSON SIN REFLEXION PARA LO QUE NO TIENE TIPO.
//
// EL PROBLEMA QUE RESUELVE. Gnothi.Host/HostJson.cs cubre los tipos concretos del Host, porque
// el generador los conoce al compilar. Pero por la Puerta 2 viaja mucho que NO tiene tipo:
//
//   los hechos de un modulo    Response.Content es `object` A PROPOSITO -- el Dispatcher no
//                              mira adentro, y si lo mirara tendria que entender el dominio
//   el cuerpo de la API        Dictionary<string, object?> armado a mano por cada modelo
//   el input schema            IReadOnlyDictionary<string, object> armado desde el manifiesto
//
// De eso el generador no puede hacer nada: no hay tipo que generar. Y `Serialize(object)` cae
// en reflexion, que es justo lo que NativeAOT prohibe (NotasGnothiHost/33).
//
// LA SALIDA. Escribir el arbol a mano con Utf8JsonWriter. Suena peor de lo que es: lo que
// puede aparecer ahi adentro es una lista corta y CONOCIDA, porque la armamos nosotros.
//
//   null · string · bool · los numericos · decimal · DateTime · DateTimeOffset · Guid · enum
//   JsonElement                     -- lo que devuelve StdioShell, se escribe crudo
//   diccionario con clave string    -- lo que proyecta InProcessShell
//   cualquier IEnumerable           -- listas y arreglos
//   los resultados de las tools del Host  -- por HostJson.TryWrite, que los tiene generados
//
// Y SI APARECE ALGO MAS, TIRA. Es deliberado y es la parte importante del diseño: el modo
// anterior -caer en reflexion- funcionaba en escritorio y fallaba en el iPhone en silencio,
// con un campo vacio y sin error. Preferimos enterarnos en la primera corrida y con el nombre
// del tipo escrito.
// PUBLICO Y NO INTERNO desde que ModelFoundation.cs -el IModel del modelo de Apple- se compila
// tambien en Gnothi.Desktop, que es otro ensamblado. En el telefono no se notaba porque iOS y
// Android compilan este archivo por glob. Un IModel de afuera tiene el mismo derecho a escribir
// json que los de adentro.
public static class Json
{
	/// Un JsonElement desde texto, SIN reflexion.
	///
	/// `JsonSerializer.Deserialize&lt;JsonElement&gt;` levanta IL2026 e IL3050 aunque no haya
	/// ningun tipo que descubrir -- el analizador no puede saber que JsonElement es el caso
	/// trivial. JsonDocument.Parse hace lo mismo por el camino que no reflexiona.
	///
	/// El Clone() no es opcional: sin el, el JsonElement queda colgando de un JsonDocument que
	/// se dispone al salir del using, y leerlo despues tira ObjectDisposedException.
	public static JsonElement Parse(string text)
	{
		using JsonDocument doc = JsonDocument.Parse(text);
		return doc.RootElement.Clone();
	}

	/// El arbol, a json, sin reflexion.
	public static string Write(object? value)
	{
		var buffer = new System.Buffers.ArrayBufferWriter<byte>();
		using (var w = new Utf8JsonWriter(buffer)) { WriteValue(w, value); }
		return System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
	}

	private static void WriteValue(Utf8JsonWriter w, object? v)
	{
		switch (v)
		{
			case null: w.WriteNullValue(); return;

			// Crudo: ya es json valido y volver a parsearlo solo puede empeorarlo.
			case JsonElement e: e.WriteTo(w); return;
			case JsonDocument d: d.RootElement.WriteTo(w); return;

			case string s: w.WriteStringValue(s); return;
			case bool b: w.WriteBooleanValue(b); return;

			// El enum va como TEXTO, igual que lo proyecta InProcessShell. Un enum como numero
			// no le dice nada a un modelo.
			case Enum en: w.WriteStringValue(en.ToString()); return;

			case byte or sbyte or short or ushort or int or uint or long:
				w.WriteNumberValue(Convert.ToInt64(v)); return;
			case ulong ul: w.WriteNumberValue(ul); return;
			case float or double: w.WriteNumberValue(Convert.ToDouble(v)); return;
			case decimal m: w.WriteNumberValue(m); return;

			// ISO 8601. Es lo que hace System.Text.Json por omision, y hay que replicarlo o la
			// misma fecha sale distinta segun la puerta -- que es exactamente lo que la suite
			// de conformance existe para impedir.
			case DateTime dt: w.WriteStringValue(dt); return;
			case DateTimeOffset dto: w.WriteStringValue(dto); return;
			case Guid g: w.WriteStringValue(g); return;
			case TimeSpan ts: w.WriteStringValue(ts.ToString()); return;

			// Diccionario. Se prueba el generico primero porque es el caso real -- lo que
			// proyecta InProcessShell -- y el no generico queda de red.
			//
			// OJO CON LO QUE NO HAY QUE AGREGAR ACA: un case para
			// IReadOnlyDictionary<string, object> -- SIN el '?' -- no compila, y el error dice
			// «switch case is unreachable». No es un bug: la anotacion de nulabilidad SE BORRA
			// al compilar, asi que en runtime los dos son el mismo tipo. Por eso el
			// IReadOnlyDictionary<string, object> que devuelve ToolIndex.InputSchema entra por
			// esta linea sin necesitar una propia.
			case IReadOnlyDictionary<string, object?> ro: WriteObject(w, ro); return;
			case IDictionary<string, object?> d2: WriteObject(w, d2); return;
			case IDictionary plain: WriteLooseDictionary(w, plain); return;

			// Despues de los diccionarios, porque un diccionario tambien es IEnumerable. Y
			// despues de string por lo mismo.
			case IEnumerable list:
				w.WriteStartArray();
				foreach (object? item in list) WriteValue(w, item);
				w.WriteEndArray();
				return;
		}

		// Ultimo recurso ANTES de rendirse: los resultados de las tools del propio Host, que
		// SI tienen lector y escritor generados al compilar.
		string? generado = HostJson.TryWrite(v);
		if (generado is not null) { w.WriteRawValue(generado); return; }

		throw new NotSupportedException(
			$"Json.Write no sabe escribir '{v.GetType().FullName}' sin reflexion. " +
			"Si es un tipo del Host, agregalo a HostJson; si viene de un modulo, la superficie " +
			"tiene que devolver diccionarios y primitivos, que es lo que InProcessShell proyecta.");
	}

	private static void WriteObject(Utf8JsonWriter w, IEnumerable<KeyValuePair<string, object?>> pairs)
	{
		w.WriteStartObject();
		foreach ((string key, object? value) in pairs) { w.WritePropertyName(key); WriteValue(w, value); }
		w.WriteEndObject();
	}

	private static void WriteLooseDictionary(Utf8JsonWriter w, IDictionary d)
	{
		w.WriteStartObject();
		foreach (DictionaryEntry e in d)
		{
			// Una clave que no es texto no tiene lugar en un objeto json. ToString() es lo
			// mismo que hace System.Text.Json.
			w.WritePropertyName(e.Key.ToString() ?? "");
			WriteValue(w, e.Value);
		}
		w.WriteEndObject();
	}
}
