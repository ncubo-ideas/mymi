using System.Text.Json;

namespace Lab.WasiCaja
{
	// SERIALIZAR ES DEL MODULO, Y ESO ES NUEVO. Hoy la superficie devuelve un `object` y lo
	// serializa el HOST, que lo tiene en la mano. Cruzando el borde el objeto no cruza: cruza
	// texto, asi que el que serializa es el modulo.
	//
	// Y TIENE PRECIO: vuelve a encender la serializacion por reflexion, que el build de WASI trae
	// apagada a proposito (`JsonSerializerIsReflectionDisabled`) porque es lo que permite recortar.
	// Esta prueba la enciende para medir el camino completo; la forma correcta es un
	// JsonSerializerContext generado, y eso el modulo lo escribe una vez. Costo medido en F8.
	internal static class Json
	{
		// LA MISMA POLITICA DE NOMBRES QUE `InProcessShell.Project`, y esto no es cosmetico: sin
		// `[JsonPropertyName]`, el Host baja la inicial y el serializador por omision NO. El mismo
		// metodo devolvia `Valor` desde la caja y `valor` en proceso, y la cara -- que busca `valor`
		// -- pintaba **undefined** en titular, sin un error.
		//
		// YA ESTABA ESCRITO QUE PASARIA. `Project` lo dice con todas las letras: la politica vive en
		// el SERIALIZADOR, no en un atributo, asi que la reflexion no la ve y HAY QUE REPLICARLA.
		// Costo un bug con el tercer modulo entonces, y otro con la caja ahora.
		//
		// LAS LLAVES DE DICCIONARIO NO SE TOCAN, por la misma razon que alla: son DATOS. `Inspection`
		// es un miembro del conjunto cerrado del dominio, y bajarle la inicial seria reescribir un
		// valor. `DictionaryKeyPolicy` en null es exactamente eso.
		private static readonly JsonSerializerOptions Opciones = new()
		{
			WriteIndented = false,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		};

		internal static string De(object o) => JsonSerializer.Serialize(o, Opciones);

		internal static string Texto(string s) => JsonSerializer.Serialize(s, Opciones);
	}
}
