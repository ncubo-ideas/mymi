using System.Text.Json;
using Microsoft.JSInterop;
using Municipalidad.App;
using Puppets.Portador;

namespace Municipalidad.Wasm
{
	/// <summary>
	/// Lo que este modulo le ofrece a Gnothi cuando corre hospedado en el telefono. Tres verbos, y
	/// ninguno le da iniciativa al modulo: los tres los arranca Gnothi.
	/// <para>
	/// Uno dice que sabe contestar el modulo de verdad, otro hace una consulta -- la misma que en
	/// escritorio, porque los cuerpos son los mismos -- y el tercero es el cartero entregando un sobre de
	/// eventos. Lo que va al reves, el modulo pidiendole eventos a Gnothi, viaja por el puente que Gnothi
	/// inyecta.
	/// </para>
	/// <para>
	/// ESTATICA POR OBLIGACION: el nativo solo alcanza metodos estaticos, y el estado de la app es un
	/// singleton del contenedor, asi que el arranque registra aca el vivo.
	/// </para>
	/// </summary>
	public static class SuperficieDelModulo
	{
		private static EstadoDeLaApp estado;

		/// <summary>La llama el arranque una vez, con el singleton ya construido.</summary>
		public static void Registrar(EstadoDeLaApp vivo)
		{
			ArgumentNullException.ThrowIfNull(vivo);

			estado = vivo;
		}

		/// <summary>
		/// DECLARADO CONTRA REAL: esto no lee el manifiesto, contesta lo que el modulo puede efectivamente
		/// atender. Gnothi compara las dos listas al dar de alta el modulo, y si difieren el modulo miente
		/// en su manifiesto.
		/// </summary>
		[JSInvokable]
		public static string Tools() => JsonSerializer.Serialize(ConsultasDelModulo.Declaradas);

		/// <summary>
		/// UNA CONSULTA, contra el actor VIVO y no contra un lector nuevo: en el WebView el actor ya esta
		/// hidratado y abrir un segundo pagaria el arranque del motor otra vez por cada pregunta.
		/// </summary>
		[JSInvokable]
		public static string Call(string tool, string argsJson)
		{
			if (estado is null) return Falla("el módulo todavía no arrancó");
			if (string.IsNullOrWhiteSpace(tool)) return Falla("falta el nombre de la consulta");

			try
			{
				return JsonSerializer.Serialize(estado.Consultar(tool));
			}
			catch (Exception ex)
			{
				return Falla($"{ex.GetType().Name}: {ex.Message}");
			}
		}

		/// <summary>
		/// EL CARTERO ENTREGA: Gnothi trae un sobre de la municipalidad y el modulo lo asienta por EL
		/// MISMO camino que su propio refresco -- misma ruta, mismo Check, mismo flush --, asi que
		/// entregar dos veces el mismo sobre no duplica nada.
		/// </summary>
		[JSInvokable]
		public static async Task<string> DeliverAsync(string sobreJson)
		{
			if (estado is null) return Falla("el módulo todavía no arrancó");

			try
			{
				ResultadoDeRefresh resultado = await estado.EntregarAsync(sobreJson);
				return JsonSerializer.Serialize(resultado);
			}
			catch (Exception ex)
			{
				return Falla($"{ex.GetType().Name}: {ex.Message}");
			}
		}

		/// <summary>
		/// Un fallo propio SI queda dicho: es del modulo y es auditable. Lo que no puede pasar es que una
		/// consulta que no existe se conteste con algo inventado.
		/// </summary>
		private static string Falla(string motivo) => JsonSerializer.Serialize(new { ok = false, motivo });
	}
}
