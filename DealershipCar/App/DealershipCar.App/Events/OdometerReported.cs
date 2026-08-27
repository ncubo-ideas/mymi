using System;
using System.Text.Json;
using Puppets.Portador;

namespace DealershipCar.App.Events
{
	// EL CONCESIONARIO INFORMA UNA LECTURA DEL ODOMETRO. Es el hecho recibido mas frecuente y el
	// unico que llega de tres procedencias distintas -- telemetria, mostrador y la persona --
	// porque de donde viene es lo que decide cuanta confianza carga (ReadingSourceCode).
	//
	// FORMA DE IDispatchMessage: un tag de routing compacto y un Interpretar que entiende SU
	// propio wire. El tag es "OdometerReported" y el layout es el JSON del concesionario; ninguno
	// de los dos aparece jamas en el texto de un comando.
	//
	// ESTE TIPO ES LA FRONTERA. De aca para adentro no entra nada de la forma del proveedor: la
	// puerta recibe valores tipados y limpios. Y solo se usa como ENTRADA de un comando -- lo que
	// una pantalla ve sale siempre de una lectura sobre el actor, nunca de un mensaje.
	//
	// LOS CODIGOS SE VALIDAN ACA, contra los conjuntos cerrados del dominio y sin reventar. Un
	// codigo desconocido en un evento recibido es normal -- un concesionario publica lo que
	// publica -- asi que el mensaje se DESCARTA. Dejarlo pasar seria peor que perderlo: el motor
	// escribe la entrada antes de ejecutar el comando, asi que un codigo invalido quedaria marcado
	// para siempre en el journal de la persona (C22, C25).
	public sealed class OdometerReported : IMensajeDeEvento
	{
		public const string TheEventType = "OdometerReported";

		private OdometerReported() { }

		// El numero del evento en la narrativa del concesionario. Es el punto de control que se
		// journaliza DENTRO del mismo comando que aplica el hecho.
		public long EventNumber { get; private set; }

		public int Value { get; private set; }

		public string Unit { get; private set; }

		public DateTime ObservedAt { get; private set; }

		public string Source { get; private set; }

		// LOS DOS MIEMBROS DE LA INTERFAZ VAN EXPLICITOS, y no es ceremonia. IMensajeDeEvento es
		// del mecanismo compartido y sus nombres estan en espanol; los de este modulo estan en
		// ingles (CLAUDE.md). Implementarlos explicitamente cumple el contrato sin poner un nombre
		// espanol en la superficie de este negocio. Queda escrito en CONTRADICTIONS C29.
		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		// Devuelve null si la senal no es nuestra o no esta bien formada. El bucle DESCARTA un
		// null; no inventa un comando para tragarse la senal.
		public static OdometerReported Interpret(SenalDeEvento signal)
		{
			if (signal.Tipo != TheEventType) return null;
			if (signal.Numero <= 0) return null;
			if (signal.Contenido.Length == 0) return null;

			try
			{
				using (JsonDocument document = JsonDocument.Parse(signal.Contenido))
				{
					JsonElement root = document.RootElement;

					string unit = Payload.Text(root, "unit");
					string source = Payload.Text(root, "source");

					if (!WireName.NamesADistanceUnit(unit)) return null;
					if (!WireName.NamesAReadingSource(source)) return null;

					// Una lectura sin numero no es una lectura, y una sin instante no se puede
					// ubicar en la linea de observacion, que es donde OINV-54 la pone.
					int value;
					if (!Payload.Whole(root, "value", out value)) return null;

					DateTime observedAt;
					if (!Payload.Instant(root, "observedAt", out observedAt)) return null;

					// El odometro no cuenta hacia atras desde cero. Un negativo no es una lectura
					// baja: es un evento roto.
					if (value < 0) return null;

					return new OdometerReported
					{
						EventNumber = signal.Numero,
						Value = value,
						Unit = unit,
						ObservedAt = observedAt,
						Source = source,
					};
				}
			}
			catch (JsonException)
			{
				return null;
			}
		}
	}
}
