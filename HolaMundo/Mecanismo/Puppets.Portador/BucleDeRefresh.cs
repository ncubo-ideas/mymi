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
	public sealed class BucleDeRefresh
	{
		private readonly IDiarioDeLaApp diario;
		private readonly IFuenteDeEventos fuente;
		private readonly Ruta ruta;
		private readonly Func<Task> flush;
		private readonly Func<CancellationToken, Task<string>> renovarCredencial;

		// flush corre DESPUES de cada comando, no por timer: angosta al maximo la
		// ventana en la que un cierre abrupto perderia entradas ya commiteadas en
		// el actor pero todavia no bajadas a la carpeta del usuario. En la consola
		// es un no-op (el journal ya esta en disco de verdad); en wasm es el puente
		// MEMFS -> carpeta.
		// renovarCredencial: que hacer cuando el proveedor rechaza el token. Devuelve el
		// access token nuevo. Es opcional porque no todo host puede rotar (la consola de
		// medicion no lo necesita), pero la app instalada SI lo pasa: el access token es
		// corto a proposito, asi que sin rotacion la app se rompe sola al rato.
		public BucleDeRefresh(IDiarioDeLaApp diario, IFuenteDeEventos fuente, Ruta ruta,
			Func<Task> flush = null, Func<CancellationToken, Task<string>> renovarCredencial = null)
		{
			ArgumentNullException.ThrowIfNull(diario);
			ArgumentNullException.ThrowIfNull(fuente);
			ArgumentNullException.ThrowIfNull(ruta);

			this.diario = diario;
			this.fuente = fuente;
			this.ruta = ruta;
			this.flush = flush;
			this.renovarCredencial = renovarCredencial;
		}

		public async Task<ResultadoDeRefresh> Ejecutar(CancellationToken ct = default)
		{
			// Ambos salen del journal. El "desde" NO se guarda entre llamadas en un
			// campo: si la app se cayo a mitad del bloque anterior, el journal es el
			// unico que sabe la verdad.
			long desde = diario.UltimoEventoProcesado();
			string token = diario.TokenGuardado();

			if (string.IsNullOrWhiteSpace(token))
				throw new InvalidOperationException(
					"La app no tiene web token guardado. Guardalo primero (es un comando aparte) y volve a refrescar.");

			IReadOnlyList<SenalDeEvento> senales = await LeerRotandoSiHaceFalta(desde, token, ct).ConfigureAwait(false);

			var resultado = new ResultadoDeRefresh { UltimoEvento = desde };
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

		// Un solo reintento, y solo ante un rechazo de credencial. Si el token rotado
		// tambien es rechazado, el problema no es la expiracion y reintentar seria
		// castigar al proveedor con pedidos que ya sabemos que fallan.
		private async Task<IReadOnlyList<SenalDeEvento>> LeerRotandoSiHaceFalta(
			long desde, string token, CancellationToken ct)
		{
			try
			{
				return await fuente.LeerDesde(desde, token, ct).ConfigureAwait(false);
			}
			catch (CredencialRechazadaException) when (renovarCredencial != null)
			{
				string renovado = await renovarCredencial(ct).ConfigureAwait(false);
				if (string.IsNullOrWhiteSpace(renovado)) throw;

				return await fuente.LeerDesde(desde, renovado, ct).ConfigureAwait(false);
			}
		}
	}
}
