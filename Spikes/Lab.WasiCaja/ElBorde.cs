using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Gnothi.Contract;

namespace Lab.WasiCaja
{
	// EL BORDE, DEL LADO DE ADENTRO. Paso 1 de la caja de wasm (F2 · P19).
	//
	// LA PREGUNTA QUE ESTE ARCHIVO CONTESTA: hoy el Host encuentra los marcadores del contrato
	// -[Query], [View], [Deliver], [Cursor]- por REFLEXION sobre un Type de .NET, porque el dll de
	// la institucion esta cargado en su proceso. Cruzando a wasm eso no existe: del otro lado del
	// borde no hay un Type, hay bytes.
	//
	// Y LA RESPUESTA ES QUE LA REFLEXION NO DESAPARECE: SE MUDA ADENTRO.
	//
	// El modulo reflexiona sobre SU PROPIA superficie -su codigo, su ensamblado, su decision- y
	// PUBLICA la lista. El Host recibe una lista de nombres y llama por nombre. Eso cambia quien
	// tiene el privilegio: hoy el Host mira adentro del dll ajeno; aca el modulo dice lo que
	// ofrece y el Host le cree o no. Es el mismo reparto de autoridad que A1 pide en todo lo
	// demas.
	//
	// EL CONTRATO NO CAMBIA DE FORMA, cambia de LUGAR. Los mismos cuatro atributos, en la misma
	// clase, escritos igual por la institucion. Lo que cambia es que ahora tambien son el
	// protocolo del borde y no solo una convencion de carga.
	internal sealed class ElBorde
	{
		private readonly object superficie;
		private readonly Dictionary<string, MethodInfo> porNombre = new(StringComparer.Ordinal);
		private readonly List<string> consultas = [];
		private readonly List<string> vistas = [];
		private MethodInfo entregar;
		private MethodInfo cursor;

		// LA MISMA BUSQUEDA POR NOMBRE DE ATRIBUTO que hace InProcessShell, y a proposito: si
		// aca se usara `typeof(QueryAttribute)` el modulo tendria que cargar EL MISMO ensamblado
		// del contrato que el Host, y eso es justo el acoplamiento que la forma cargo evita
		// prestando la copia. Por nombre, las dos copias sirven.
		internal ElBorde(object superficie)
		{
			this.superficie = superficie ?? throw new ArgumentNullException(nameof(superficie));

			foreach (MethodInfo m in superficie.GetType().GetMethods(
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
			{
				foreach (object a in m.GetCustomAttributes(false))
				{
					switch (a.GetType().Name)
					{
						case nameof(QueryAttribute):
							Registrar(consultas, ((QueryAttribute)a).Name, m);
							break;
						case nameof(ViewAttribute):
							Registrar(vistas, ((ViewAttribute)a).Name, m);
							break;
						case nameof(DeliverAttribute): entregar = m; break;
						case nameof(CursorAttribute): cursor = m; break;
					}
				}
			}
		}

		private void Registrar(List<string> donde, string nombre, MethodInfo m)
		{
			donde.Add(nombre);
			porNombre[nombre] = m;
		}

		// LO QUE EL MODULO DECLARA, y es lo unico que el Host va a saber de el. Sale como una
		// linea de texto: nombres, no tipos.
		internal string Declaracion()
		{
			var sb = new System.Text.StringBuilder("{\"consultas\":[");
			for (int i = 0; i < consultas.Count; i++)
				sb.Append(i > 0 ? ",\"" : "\"").Append(consultas[i]).Append('"');
			sb.Append("],\"vistas\":[");
			for (int i = 0; i < vistas.Count; i++)
				sb.Append(i > 0 ? ",\"" : "\"").Append(vistas[i]).Append('"');
			sb.Append("],\"entrega\":").Append(entregar is not null ? "true" : "false")
			  .Append(",\"cursor\":").Append(cursor is not null ? "true" : "false").Append('}');
			return sb.ToString();
		}

		// LA PUERTA, Y SIGUE SIENDO UNA PUERTA POR CONJUNTO. `consultar` solo llega a [View];
		// el asistente solo llega a [Query]; entregar es el unico que escribe. Que el borde sea
		// serializado no afloja eso: lo hace MAS explicito, porque el conjunto se elige antes de
		// buscar el nombre. Ver B8.
		internal string Atender(string verbo, string nombre, string carga)
		{
			switch (verbo)
			{
				case "declarar":
					return Ok(Declaracion());

				case "consultar":
					if (!vistas.Contains(nombre)) return Error($"'{nombre}' no es una vista de este modulo");
					return Ok(Json.De(Llamar(porNombre[nombre], carga)));

				case "preguntar":
					if (!consultas.Contains(nombre)) return Error($"'{nombre}' no es una consulta de este modulo");
					return Ok(Json.De(Llamar(porNombre[nombre], carga)));

				case "cursor":
					if (cursor is null) return Error("este modulo no dice hasta donde leyo");
					return Ok(cursor.Invoke(superficie, null).ToString());

				case "entregar":
					if (entregar is null) return Error("este modulo no recibe entregas");
					return Ok(Json.Texto((string)entregar.Invoke(superficie, [carga])));

				default:
					return Error($"verbo desconocido: {verbo}");
			}
		}

		// LOS ARGUMENTOS, que la primera version DESCARTABA -- invocaba con `null` -- y costo media
		// pantalla. De las siete vistas de examenes, dos los piden: `Serie(string analito)` y
		// `Tendencia(string analito)`. Sin ligarlos, esas dos tiran adentro, el borde contesta
		// `ok:false` y la cara degrada EN SILENCIO: no muestra un error, muestra menos pantalla.
		// Se vio poniendo la caja en Gnothi de verdad; el arnes nunca lo habria encontrado, porque
		// preguntaba lo que el arnes sabia preguntar.
		//
		// EL SOBRE LO ARMA EL HOST y tiene dos campos a proposito:
		//
		//     { "args": { … lo que pregunto quien pregunta … }, "accepts": "…" }
		//
		// `accepts` NO puede venir de adentro de `args`, y ese orden es el punto: si se leyera del
		// diccionario de quien pregunta, una consulta podria elegir su propio envoltorio, y el
		// formato no es de la pregunta. Es la misma regla que InProcessShell.Bind aplica leyendo el
		// parametro reservado ANTES que los argumentos.
		private object Llamar(MethodInfo metodo, string sobre)
		{
			ParameterInfo[] piden = metodo.GetParameters();
			if (piden.Length == 0) return metodo.Invoke(superficie, null);

			JsonElement args = default;
			string acepta = "";
			if (sobre.Length > 0)
			{
				using JsonDocument d = JsonDocument.Parse(sobre);
				if (d.RootElement.TryGetProperty("args", out JsonElement a)) args = a.Clone();
				if (d.RootElement.TryGetProperty("accepts", out JsonElement ac)) acepta = ac.GetString() ?? "";
			}

			var valores = new object[piden.Length];
			for (int i = 0; i < piden.Length; i++) valores[i] = Ligar(piden[i], args, acepta);
			return metodo.Invoke(superficie, valores);
		}

		private static object Ligar(ParameterInfo p, JsonElement args, string acepta)
		{
			// EL PARAMETRO RESERVADO, y se mira PRIMERO. Una superficie que quiere elegir su formato
			// declara `accepts` y lo llena el HOST, nunca quien pregunta. Una que no lo declara ni se
			// entera de que existe, que es la misma indistinguibilidad del resto del diseno.
			if (string.Equals(p.Name, "accepts", StringComparison.Ordinal))
				return p.ParameterType == typeof(string) ? acepta : (object)acepta;

			if (args.ValueKind == JsonValueKind.Object
				&& args.TryGetProperty(p.Name ?? "", out JsonElement v)
				&& v.ValueKind != JsonValueKind.Null)
			{
				string texto = v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : v.GetRawText();
				return p.ParameterType.IsEnum
					? Enum.Parse(p.ParameterType, texto, ignoreCase: true)
					: Convert.ChangeType(texto, p.ParameterType);
			}

			// Falta y tiene omision: se usa. Falta y no tiene: se dice cual, que es lo unico que le
			// sirve a quien lee el log.
			return p.HasDefaultValue
				? p.DefaultValue
				: throw new ArgumentException($"falta el parametro '{p.Name}'");
		}

		private static string Ok(string cuerpo) => "{\"ok\":true,\"dice\":" + cuerpo + "}";

		private static string Error(string por) => "{\"ok\":false,\"dice\":" + Json.Texto(por) + "}";
	}
}
