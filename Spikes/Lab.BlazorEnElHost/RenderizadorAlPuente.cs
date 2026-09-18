using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lab.BlazorEnElHost;

/// EL RENDERIZADOR QUE VIVE EN EL HOST. Corre los componentes de la institucion en el .NET
/// nativo de la app y empuja los diffs por el puente; del otro lado hay un JS de 120 lineas que
/// los aplica al DOM. No hay wasm, no hay MAUI, no hay SignalR.
///
/// ES EL MISMO MECANISMO QUE BLAZOR SERVER, con el circuito cambiado: alla el transporte es una
/// conexion websocket a un servidor, aca es `@JavascriptInterface` contra el WebView de al lado.
/// `Renderer` no sabe ni le importa cual de los dos es -- por eso es abstracta.
///
/// LO QUE HAY QUE SUBCLASEAR son tres miembros, y estan los tres abajo: el `Dispatcher` que
/// serializa las llamadas, `HandleException`, y `UpdateDisplayAsync`, que es donde llega el lote.
internal sealed class RenderizadorAlPuente : Renderer
{
	private readonly Action<string> empujar;

	public RenderizadorAlPuente(IServiceProvider servicios, Action<string> empujar)
		: base(servicios, NullLoggerFactory.Instance)
	{
		this.empujar = empujar;
	}

	// UN SOLO HILO PARA TODO EL ARBOL, que es lo que el modelo de componentes supone. El
	// `Dispatcher` por omision lo garantiza, y de paso resuelve del lado del Host el problema que
	// en la forma hibrida cada institucion resolvia a mano con su propio candado.
	public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

	// CON LA TRAZA, y no solo el mensaje: `Object reference not set` sin pila no dice nada, y en un
	// spike que monta codigo ajeno la pila ES el resultado -- nombra el miembro del andamio que
	// falto.
	protected override void HandleException(Exception e) =>
		empujar("{\"tipo\":\"error\",\"dice\":" + Json(e.Message)
			+ ",\"donde\":" + Json(e.StackTrace) + "}");

	/// AQUI LLEGA EL LOTE, y esto es lo que el spike vino a averiguar: que un `RenderBatch` se
	/// puede leer y serializar del lado nativo.
	///
	/// EL LOTE NO ES HTML: son ediciones contra un arbol que el otro lado ya tiene. Por eso el
	/// primer render manda todo y los siguientes mandan tres o cuatro ediciones -- y por eso esto
	/// puede ser barato aunque la pantalla sea grande.
	protected override Task UpdateDisplayAsync(in RenderBatch lote)
	{
		var b = new StringBuilder(1024);
		b.Append("{\"tipo\":\"lote\",\"marcos\":[");

		// LOS MARCOS DE REFERENCIA van primero y enteros: las ediciones los apuntan por indice.
		var marcos = lote.ReferenceFrames;
		for (int i = 0; i < marcos.Count; i++)
		{
			if (i > 0) b.Append(',');
			Marco(b, marcos.Array[i]);
		}

		b.Append("],\"componentes\":[");
		var editados = lote.UpdatedComponents;
		for (int c = 0; c < editados.Count; c++)
		{
			if (c > 0) b.Append(',');
			ref var diff = ref editados.Array[c];
			b.Append("{\"id\":").Append(diff.ComponentId).Append(",\"ediciones\":[");
			for (int e = 0; e < diff.Edits.Count; e++)
			{
				if (e > 0) b.Append(',');
				Edicion(b, diff.Edits.Array[e]);
			}
			b.Append("]}");
		}

		b.Append("],\"bajas\":[");
		var bajas = lote.DisposedComponentIDs;
		for (int i = 0; i < bajas.Count; i++)
		{
			if (i > 0) b.Append(',');
			b.Append(bajas.Array[i]);
		}
		b.Append("]}");

		empujar(b.ToString());
		return Task.CompletedTask;
	}

	// UN MARCO. Solo los cinco tipos que una pantalla usa; si aparece otro se dice, no se ignora
	// en silencio -- un spike que se come lo que no entiende contesta que si por no haber mirado.
	private static void Marco(StringBuilder b, in RenderTreeFrame f)
	{
		switch (f.FrameType)
		{
			case RenderTreeFrameType.Element:
				b.Append("{\"t\":\"e\",\"n\":").Append(Json(f.ElementName))
				 .Append(",\"largo\":").Append(f.ElementSubtreeLength).Append('}');
				return;

			case RenderTreeFrameType.Text:
				b.Append("{\"t\":\"x\",\"v\":").Append(Json(f.TextContent)).Append('}');
				return;

			case RenderTreeFrameType.Markup:
				b.Append("{\"t\":\"m\",\"v\":").Append(Json(f.MarkupContent)).Append('}');
				return;

			// EL EVENTO VIAJA COMO UN NUMERO. `AttributeEventHandlerId` es la unica pieza que el
			// otro lado necesita para devolver el clic: el manejador -- codigo de la institucion --
			// se queda de este lado y nunca cruza. Eso es lo bueno del mecanismo y, a la vez, lo
			// que lo hace una puerta ancha: ese codigo corre nativo.
			case RenderTreeFrameType.Attribute:
				b.Append("{\"t\":\"a\",\"n\":").Append(Json(f.AttributeName))
				 .Append(",\"v\":").Append(Valor(f.AttributeValue))
				 .Append(",\"ev\":").Append(f.AttributeEventHandlerId).Append('}');
				return;

			case RenderTreeFrameType.Component:
				b.Append("{\"t\":\"c\",\"id\":").Append(f.ComponentId)
				 .Append(",\"largo\":").Append(f.ComponentSubtreeLength).Append('}');
				return;

			case RenderTreeFrameType.Region:
				b.Append("{\"t\":\"r\",\"largo\":").Append(f.RegionSubtreeLength).Append('}');
				return;

			default:
				b.Append("{\"t\":\"?\",\"n\":").Append(Json(f.FrameType.ToString())).Append('}');
				return;
		}
	}

	private static void Edicion(StringBuilder b, in RenderTreeEdit e)
	{
		b.Append("{\"t\":").Append(Json(e.Type.ToString()))
		 .Append(",\"i\":").Append(e.SiblingIndex)
		 .Append(",\"ref\":").Append(e.ReferenceFrameIndex);

		if (e.RemovedAttributeName is not null)
			b.Append(",\"attr\":").Append(Json(e.RemovedAttributeName));

		b.Append('}');
	}

	/// EL VALOR DE UN ATRIBUTO, Y UN BOOL NO ES LA CADENA «True».
	///
	/// Esto salia por `ToString()`, asi que `aria-hidden="true"` del razor llegaba al DOM como
	/// `aria-hidden="True"` y un atributo booleano en `false` llegaba como la cadena «False» --
	/// que es verdadera en HTML. Blazor distingue: `false` y `null` QUITAN el atributo, `true` lo
	/// pone vacio. Se serializa como booleano de JSON y el otro lado ya sabe que hacer.
	private static string Valor(object? v) => v switch
	{
		null => "null",
		bool si => si ? "true" : "false",
		string s => Json(s),
		_ => Json(v.ToString()),
	};

	// JSON A MANO Y NO JsonSerializer, por la razon de Lab.Aot: el serializador por reflexion es
	// el que mete los IL2026/IL3050 que ese spike conto. Con StringBuilder no hay ninguno, y esto
	// es la unica cadena caliente del camino.
	private static string Json(string? s)
	{
		if (s is null) return "null";
		var b = new StringBuilder(s.Length + 2).Append('"');
		foreach (char c in s)
			switch (c)
			{
				case '"': b.Append("\\\""); break;
				case '\\': b.Append("\\\\"); break;
				case '\n': b.Append("\\n"); break;
				case '\r': b.Append("\\r"); break;
				case '\t': b.Append("\\t"); break;
				default:
					if (c < ' ') b.Append("\\u").Append(((int)c).ToString("x4"));
					else b.Append(c);
					break;
			}
		return b.Append('"').ToString();
	}

	/// MONTAR POR TIPO Y NO POR INSTANCIA, y esto es un hallazgo del spike que costo una hora.
	///
	/// La primera version recibia un `IComponent` ya construido con `new`. Compila, monta, y
	/// **todos los `@inject` quedan en null**: la inyeccion por propiedad la hace
	/// `ComponentFactory.InstantiateComponent`, que es a quien hay que pedirle el componente. Un
	/// componente que uno instancia a mano nunca pasa por ahi.
	///
	/// El sintoma era el peor posible: `NullReferenceException` adentro de `BuildRenderTree` del
	/// codigo de la institucion, sin numero de linea -- y con la pila apuntando a SU archivo, que
	/// es donde uno NO va a buscar el error. Es, otra vez, la forma de falla de toda la semana:
	/// nada avisa, y lo que se ve culpa al lugar equivocado.
	///
	/// `InstantiateComponent` es `protected` en la clase base. Que lo sea es la pista de que el
	/// camino previsto es este.
	public async Task<int> Montar(Type componente, ParameterView parametros)
	{
		int id = AssignRootComponentId(InstantiateComponent(componente));
		await RenderRootComponentAsync(id, parametros);
		return id;
	}

	public Task Evento(ulong manejador) =>
		Dispatcher.InvokeAsync(() => DispatchEventAsync(manejador, null, EventArgs.Empty));
}
