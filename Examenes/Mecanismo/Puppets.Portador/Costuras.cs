using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Puppets.Portador
{
	// Una senal cruda como la entrego el medio, ANTES de que el routing decida que
	// comando anima. Es la forma de InputSignal de Choreography: identidad para el
	// checkpoint, un tag de tipo, y el payload sin interpretar.
	//
	// No lleva forma de comando ni ruta de transporte: el medio es de la fuente y
	// el comando es del receptor.
	public readonly struct SenalDeEvento
	{
		// El numero del evento en la narrativa del proveedor. Es el "after" de la
		// proxima lectura y lo que se journaliza como checkpoint.
		public long Numero { get; }

		// El tag de routing. Compacto y opaco: NUNCA el texto de un comando.
		public string Tipo { get; }

		// El payload tal como vino. La app lo interpreta; el Portador no lo mira.
		public string Contenido { get; }

		public SenalDeEvento(long numero, string tipo, string contenido)
		{
			Numero = numero;
			Tipo = tipo ?? "";
			Contenido = contenido ?? "";
		}
	}

	// Un mensaje ya tipado, listo para animar UN comando. Forma de IDispatchMessage.
	public interface IMensajeDeEvento
	{
		long Evento { get; }
		string Tipo { get; }
	}

	// Forma de InputRouting: senal cruda -> mensaje tipado.
	// Devolver null DESCARTA la senal. Eso es lo correcto para un frame de control
	// o para un tipo de evento que esta app no consume: se descarta, no se inventa
	// un comando para tragarselo.
	public delegate IMensajeDeEvento Ruta(SenalDeEvento senal);

	// Forma de IInputSource: el medio. La unica lectura que la app le hace al
	// proveedor es su propia narrativa a partir de un punto.
	public interface IFuenteDeEventos
	{
		string Nombre { get; }

		Task<IReadOnlyList<SenalDeEvento>> LeerDesde(long despuesDe, string token, CancellationToken ct);
	}

	// Lo que el bucle necesita del journal de la app. La implementa la app, que es
	// la que sabe hacer los performs.
	public interface IDiarioDeLaApp
	{
		// Del JOURNAL, no de una variable en memoria: es lo que hace exacto el
		// resume tras una caida.
		long UltimoEventoProcesado();

		string TokenGuardado();

		// Aplica el mensaje como UN comando. Devuelve "" si se aplico, o el mensaje
		// del Check si se rechazo (contrato de PerformCheckThenCommand).
		string Aplicar(IMensajeDeEvento mensaje);
	}

	public sealed class ResultadoDeRefresh
	{
		public int Leidos { get; internal set; }
		public int Aplicados { get; internal set; }
		public int Descartados { get; internal set; }
		public int Rechazados { get; internal set; }
		public long UltimoEvento { get; internal set; }

		public override string ToString() =>
			$"leidos={Leidos} aplicados={Aplicados} descartados={Descartados} rechazados={Rechazados} ultimoEvento={UltimoEvento}";
	}
}
