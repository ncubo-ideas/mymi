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

		// SIN TOKEN, a proposito. Quien lee necesita una credencial o no segun POR DONDE
		// lee, y eso es del transporte: la fuente HTTP se consigue la suya, y la fuente que
		// pasa por Gnothi no tiene ninguna -la credencial la sostiene Gnothi, del otro lado
		// del puente-. El bucle no tiene por que saber cual es el caso.
		Task<SobreLeido> LeerDesde(long despuesDe, CancellationToken ct);
	}

	// Lo que el bucle necesita del journal de la app. La implementa la app, que es
	// la que sabe hacer los performs.
	//
	// LO QUE NO ESTA ACA, a proposito: la credencial. Hasta el 2026-09-03 el diario
	// exponia TokenGuardado() y el bucle le pedia el token AL JOURNAL. Un token no es
	// historia de la persona sino contexto de sesion del host, asi que ahora el bucle
	// lo recibe por un delegado del host (ver BucleDeRefresh) y el journal no lo conoce.
	public interface IDiarioDeLaApp
	{
		// Del JOURNAL, no de una variable en memoria: es lo que hace exacto el
		// resume tras una caida.
		long UltimoEventoProcesado();

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

		// El estado de la cuenta que vino en el sobre, si el proveedor lo manda. Null cuando
		// no: es un campo aditivo y un proveedor viejo simplemente no lo trae.
		public EstadoDeCuenta Cuenta { get; internal set; }

		public override string ToString() =>
			$"leidos={Leidos} aplicados={Aplicados} descartados={Descartados} rechazados={Rechazados} ultimoEvento={UltimoEvento}"
			+ (Cuenta is null ? "" : $" cuenta={Cuenta.Estado}");
	}
}
