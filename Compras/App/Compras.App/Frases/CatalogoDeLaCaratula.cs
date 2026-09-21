using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Compras.App.Frases
{
	/// <summary>
	/// EL FORMATO DEL CATALOGO DE LA CARATULA: anidado por pantalla en el archivo, para que se lea, y
	/// plano con la clave separada por puntos en memoria, para que se busque.
	/// <para>
	/// Vive en la puerta y no en la caratula porque lo leen DOS: la caratula, que sirve el catalogo, y
	/// el arnes, que compara los dos idiomas. Dos aplanados serian dos reglas, y el arnes terminaria
	/// comprobando una que la caratula no usa.
	/// </para>
	/// <para>
	/// Es el catalogo de las PANTALLAS. Las frases que compone el dominio no estan aca: esas las
	/// redacta <see cref="Frases"/> desde un catalogo compilado.
	/// </para>
	/// </summary>
	public static class CatalogoDeLaCaratula
	{
		/// <summary>
		/// Si esa clave es una NOTA del archivo y no una frase que alguien vaya a pedir. Una nota lleva
		/// un nombre propio detras de las dos barras para que pueda haber varias en el mismo objeto,
		/// que es la convencion del manifiesto.
		/// </summary>
		public static bool EsUnaNota(string clave)
		{
			return clave != null && clave.StartsWith("//", StringComparison.Ordinal);
		}

		/// <summary>El catalogo aplanado: la clave de cada frase, separada por puntos, con su dicho.</summary>
		public static IReadOnlyDictionary<string, string> Aplanado(JsonElement raiz)
		{
			var plano = new Dictionary<string, string>(StringComparer.Ordinal);
			Aplanar(raiz, "", plano);
			return plano;
		}

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
				if (EsUnaNota(hoja.Name)) continue;
				Aplanar(hoja.Value, prefijo.Length == 0 ? hoja.Name : prefijo + "." + hoja.Name, plano);
			}
		}
	}
}
