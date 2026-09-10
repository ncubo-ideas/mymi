using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Puppets.Portador
{
	// El refresh. Mantiene la FORMA del dispatch de Choreography
	// (IFuenteDeEventos -> Ruta -> IMensajeDeEvento -> un comando por evento) pero
	// el motor es un recorrido secuencial propio, no el work queue de Dispatch.
	//
	// Por que no Choreography.Dispatch: su WorkerLoop se bloquea en
	// BlockingCollection.GetConsumingEnumerable sobre threads del pool. En wasm
	// single-thread eso lanza PlatformNotSupportedException ("Cannot wait on
	// monitors on this runtime") - la misma pared que el replay del motor. Medido.
	//
	// Lo que se pierde al no usar Dispatch: paralelismo acotado, ventana de
	// idempotencia y liveness gating. Ninguno hace falta aca: los eventos DEBEN
	// aplicarse de a uno y en orden, la idempotencia la da la guarda Check contra
	// el checkpoint journalizado (mas fuerte que una ventana en memoria, porque
	// sobrevive al reinicio), y no hay handover de nodos en la PC de una persona.
	//
	// LO QUE ESTE BUCLE NO SABE, y es el punto: de donde salen los eventos y con que
	// credencial. Eso es del transporte. La fuente HTTP se consigue su token y lo
	// rota; la fuente que pasa por Gnothi no tiene ninguno. El bucle solo sabe hasta
	// donde leyo el actor, y eso lo saca del journal.
	public sealed class BucleDeRefresh
	{
		private readonly IDiarioDeLaApp diario;
		private readonly IFuenteDeEventos fuente;
		private readonly Ruta ruta;
		private readonly Func<Task> flush;

		// flush corre DESPUES de cada comando, no por timer: angosta al maximo la
		// ventana en la que un cierre abrupto perderia entradas ya commiteadas en
		// el actor pero todavia no bajadas a la carpeta del usuario. En la consola
		// es un no-op (el journal ya esta en disco de verdad); en wasm es el puente
		// MEMFS -> carpeta, y hospedado es el puente MEMFS -> archivos de Gnothi.
		public BucleDeRefresh(IDiarioDeLaApp diario, IFuenteDeEventos fuente, Ruta ruta,
			Func<Task> flush = null)
		{
			ArgumentNullException.ThrowIfNull(diario);
			ArgumentNullException.ThrowIfNull(fuente);
			ArgumentNullException.ThrowIfNull(ruta);

			this.diario = diario;
			this.fuente = fuente;
			this.ruta = ruta;
			this.flush = flush;
		}

		public async Task<ResultadoDeRefresh> Ejecutar(CancellationToken ct = default)
		{
			// EL CURSOR ES DEL ACTOR y sale del journal. NO se guarda entre llamadas en
			// un campo: si la app se cayo a mitad del bloque anterior, el journal es el
			// unico que sabe la verdad.
			long desde = diario.UltimoEventoProcesado();

			SobreLeido sobre = await fuente.LeerDesde(desde, ct).ConfigureAwait(false);

			var resultado = new ResultadoDeRefresh { UltimoEvento = desde };
			if (sobre == null) return resultado;

			// El estado de la cuenta sube SIEMPRE, aunque no venga ningun evento: es
			// justamente lo que la app suelta necesita para enterarse de que ya se mudo.
			resultado.Cuenta = sobre.Cuenta;

			IReadOnlyList<SenalDeEvento> senales = sobre.Senales;
			if (senales == null) return resultado;

			resultado.Leidos = senales.Count;

			foreach (SenalDeEvento senal in senales)
			{
				ct.ThrowIfCancellationRequested();

				IMensajeDeEvento mensaje = ruta(senal);
				if (mensaje == null)
				{
					// La app no consume este tipo. Se descarta; no se inventa comando.
					resultado.Descartados++;
					continue;
				}

				string rechazo = diario.Aplicar(mensaje);
				if (rechazo.Length > 0)
				{
					// La guarda Check lo rechazo (evento ya procesado): NO quedo entrada
					// en el journal, asi que el checkpoint no se movio y no hay nada que
					// bajar a disco. Seguimos con el siguiente.
					resultado.Rechazados++;
					continue;
				}

				resultado.Aplicados++;
				resultado.UltimoEvento = mensaje.Evento;

				if (flush != null) await flush().ConfigureAwait(false);
			}

			return resultado;
		}
	}
}
