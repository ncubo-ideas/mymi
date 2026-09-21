using Gnothi.Assistant;
using Gnothi.Host;

namespace Gnothi.MobileShared;

// LA FOTO QUE EL HOST LE DEJA A LA CARATULA.
//
// EL PROBLEMA QUE RESUELVE, en una linea: en un telefono el unico asistente que puede hablarle
// a una app es el del sistema, y en iOS ese asistente solo entra por App Intents, que solo
// existen en Swift. El Host esta en .NET. Los dos no se pueden tocar directamente sin pagar
// NativeAOT y perder la carga dinamica de modulos (NotasGnothiHost/33).
//
// Asi que no se tocan: el Host DEJA ESCRITO lo que la caratula va a necesitar, y la caratula
// lo lee.
//
//     Gnothi.iOS corre  ->  escribe esta foto en el contenedor compartido
//                                    |
//     Siri: "cuanto gaste en Gnothi" |
//                                    v
//              App Intent (Swift)  ->  lee la foto  ->  contesta
//
// POR QUE SE PUEDE. Las dos apps son NUESTRAS, o sea mismo Team ID, que es exactamente lo que
// App Groups exige. Es la razon por la que esto NO sirve para que una institucion entregue su
// propia caratula -- ahi el Team ID es otro -- y si sirve entre las dos nuestras.
//
// EL PRECIO, dicho de frente: la respuesta sale de una foto, no en vivo. Siri contesta con lo
// que el Host dejo la ultima vez que la app corrio.
//
// Y eso NO es un compromiso nuevo. Las instrucciones que el modulo ya le da al asistente dicen
// textual: «si el total no cambio, pedile que abra su app y apriete Refresh». El journal solo
// avanza cuando la app corre. La foto repite un limite que ya existia; no agrega uno.
//
// EL CONSENTIMIENTO SE RESPETA POR CONSTRUCCION, y esto era el agujero que habia que cerrar.
// La foto se arma recorriendo Dispatcher.Catalog(), que YA viene filtrado por lo que la persona
// autorizo, y cada hecho se pide por Dispatcher.Serve, que vuelve a chequear el permiso. O sea:
// aca no puede caer nada que el asistente no tuviera derecho a ver. Y como se reescribe entera
// cada vez que el consentimiento cambia, apagar un modulo lo BORRA de la foto -- no queda una
// foto vieja contestando por el.
public static class Snapshot
{
	/// El nombre es fijo y lo conocen los dos lados. Si cambia, cambia en Swift tambien.
	public const string FileName = "gnothi-instantanea.json";

	/// <param name="carpeta">
	/// El contenedor compartido. Lo elige la plataforma: en iOS sale de
	/// NSFileManager.GetContainerUrl del App Group. En Android es null y esto no corre --
	/// alla la caratula habla por binder, en vivo, y no necesita foto.
	/// </param>
	public static async Task Write(Dispatcher dispatcher, string carpeta, Action<string> log, CancellationToken ct)
	{
		try
		{
			Directory.CreateDirectory(carpeta);

			List<object?> modulos = [];

			foreach (IGrouping<Module, (Module module, DeclaredTool tool)> porModulo in
				dispatcher.Catalog().GroupBy(x => x.module))
			{
				Module m = porModulo.Key;
				List<object?> tools = [];

				foreach ((_, DeclaredTool t) in porModulo)
				{
					Dictionary<string, object?> declarada = new()
					{
						["nombre"] = t.Name,
						["publico"] = Module.PublicName(m.Name, t.Name),
						["titulo"] = t.Title,
						["descripcion"] = t.Description,
					};

					// SOLO SE FOTOGRAFIAN LAS QUE NO PIDEN NADA. Una tool con parametros tiene
					// infinitas respuestas y no hay foto que las contenga; se declara igual para
					// que la caratula pueda decir "esta existe pero hay que preguntarla desde la
					// app", que es mejor que ocultarla.
					if (t.Input.Count > 0)
					{
						declarada["hechos"] = null;
						// EN INGLES, y no es un descuido: `porQueNo` lo lee un MODELO, no una persona.
						// La foto entera se le entrega a Siri con un encuadre que le pide contestar «en su
						// idioma»; el campo es insumo, no pantalla. Estaba en espanol mientras el otro
						// origen del mismo campo -- el motivo que compone Dispatcher, unas lineas abajo --
						// venia en ingles, asi que el archivo mezclaba los dos idiomas en una sola clave.
						declarada["porQueNo"] = "asks for parameters: it cannot be answered in advance";
						tools.Add(declarada);
						continue;
					}

					Response r = await dispatcher.Serve(new Request(m.Name, t.Name, null), ct);

					// Un modulo que falla NO tumba la foto: se anota el motivo y siguen los
					// demas. Veinte modulos instalados son veinte formas de que algo fuera de
					// nuestro control falle, y la caratula tiene que poder contestar por los
					// que si andan.
					if (!r.Authorized) { declarada["hechos"] = null; declarada["porQueNo"] = r.Reason; }
					else declarada["hechos"] = r.Content;

					tools.Add(declarada);
				}

				modulos.Add(new Dictionary<string, object?>
				{
					["nombre"] = m.Name,
					// La linea que identifica sale del manifiesto y no de aca, igual que en la
					// pantalla: es una sola regla para las tres caras y para la foto.
					["identidad"] = m.Manifest.Identity,
					["titulo"] = m.Manifest.Title,
					["institucion"] = m.Manifest.Institution,
					// El emoji por rubro. La foto no puede pintar un logo -- la lee un intent del
					// sistema, no un WebView -- asi que aca el icono del Host sigue siendo lo
					// unico que hay, y ahora al menos sale de lo que la institucion declaro ser.
					["icono"] = Shell.Icon(m.Manifest),
					["tools"] = tools,
				});
			}

			string json = Json.Write(new Dictionary<string, object?>
			{
				// Para que la caratula pueda decir "esto es de hace tres dias" en vez de
				// presentarlo como si fuera de ahora.
				["escritaEn"] = DateTimeOffset.UtcNow,
				["modulos"] = modulos,
			});

			// ATOMICO. Siri puede disparar la intent justo mientras esto escribe, y leer medio
			// archivo es peor que leer uno viejo: uno viejo se entiende, uno partido revienta el
			// parser y la caratula no sabe por que.
			string destino = Path.Combine(carpeta, FileName);
			string temporal = destino + ".tmp";
			File.WriteAllText(temporal, json);
			File.Move(temporal, destino, overwrite: true);

			log($"instantanea: {modulos.Count} modulos, {json.Length} caracteres -> {destino}");
		}
		catch (Exception e)
		{
			// Que no se pueda escribir la foto NO puede tumbar la app: la Puerta 2 adentro de
			// la app sigue andando igual. Lo que se pierde es que Siri conteste, y eso tiene
			// que verse en el log y no como un crash.
			log($"instantanea: FALLO {e.GetType().Name}: {e.Message}");
		}
	}
}
