using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Puppets.Portador
{
	// LA MISMA LECTURA, PERO HOSPEDADA. Cuando la app corre dentro de Gnothi no tiene
	// red -la CSP se la niega- asi que no puede hacer su propio pull. Le PIDE a Gnothi:
	//
	//     modulo ── gnothi.pull(after=N) ──► Gnothi ── HTTPS ──► proveedor
	//     modulo ◄──────── el sobre ────────  Gnothi ◄────────────┘
	//
	// EL CURSOR SIGUE SIENDO DEL ACTOR. El N sale del replay de su propio journal, igual
	// que en el modo suelto, y Gnothi lo releva sin guardarlo: no hay dos verdades sobre
	// hasta donde leyo. Gnothi pone la credencial y el endpoint -fijos, registrados al
	// instalar el modulo- y no elige el cursor. (Especificacion 13 §8.)
	//
	// El pedido entra como delegado y no como interop: este ensamblado no sabe de JS ni
	// de WebViews, y asi se puede probar con una funcion en memoria.
	public sealed class FuenteDeEventosPorGnothi : IFuenteDeEventos
	{
		private readonly Func<long, CancellationToken, Task<string>> pedir;

		public string Nombre { get; }

		public FuenteDeEventosPorGnothi(
			Func<long, CancellationToken, Task<string>> pedir, string nombre = "gnothi.pull")
		{
			ArgumentNullException.ThrowIfNull(pedir);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

			this.pedir = pedir;
			Nombre = nombre;
		}

		public async Task<SobreLeido> LeerDesde(long despuesDe, CancellationToken ct)
		{
			// Sin token: la credencial vive del otro lado del puente. Si Gnothi no pudo
			// hablar con el proveedor, contesta un sobre vacio o tira; en ninguno de los
			// dos casos el modulo asienta nada, que es lo correcto.
			string sobre = await pedir(despuesDe, ct).ConfigureAwait(false);
			return SobreDeEventos.Interpretar(sobre);
		}
	}
}
