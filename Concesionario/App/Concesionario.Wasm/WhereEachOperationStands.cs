using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Concesionario.App;

namespace Concesionario.Wasm
{
	/// <summary>
	/// Donde esta parada CADA operacion de este vehiculo, leida entera en un solo momento y por la
	/// unica puerta: lo que le falta a cada una y lo que ya se le hizo. Ninguna frase se compone aca.
	/// Va aparte de la ficha y de la mitad del vehiculo porque la ficha es de un modelo y la mitad del
	/// vehiculo se mide contra el sobre, y esto es la agenda de un carro.
	/// </summary>
	public sealed class WhereEachOperationStands
	{
		/// <summary>
		/// El instante en que la pantalla pregunto. Hace falta en la pantalla porque ninguna frase de
		/// la cuenta atras nombra su propio instante.
		/// </summary>
		public string AskedAt { get; init; } = "";

		/// <summary>
		/// Si la agenda rechazo la pregunta de alguna de las clases. Es lo que decide si la pantalla
		/// dice quien puede corregir el hecho de atras.
		/// </summary>
		public bool SomeCountdownWasRefused { get; init; }

		/// <summary>
		/// Una fila por clase del canon, en el orden en que el dominio las nombra. Estan las nueve y
		/// no solo las agendadas: no agendada es una de las situaciones y el dominio la dice con
		/// palabras, asi que filtrarla la dejaria inalcanzable.
		/// </summary>
		public IReadOnlyList<HowOneOperationStands> Operations { get; init; } =
			new List<HowOneOperationStands>();

		/// <summary>
		/// EL HISTORIAL ENTERO como filas, de la mas reciente a la mas vieja y entre todas las clases.
		/// No toma ningun instante: cada fila trae el suyo y la cadena es la que es.
		/// </summary>
		public IReadOnlyList<AThingThatWasDone> WhatHasBeenDone { get; init; } =
			new List<AThingThatWasDone>();

		/// <summary>
		/// Lee las dos mitades por la puerta con UN solo instante, para que lo que le falta a una
		/// operacion y lo que se le hizo sean del mismo momento del registro.
		/// </summary>
		public static WhereEachOperationStands ReadThrough(DealershipCarAPI api, DateTime asOf)
		{
			ArgumentNullException.ThrowIfNull(api);

			var operations = new List<HowOneOperationStands>();
			bool refused = false;

			foreach (string kind in AppState.TheNineKinds)
			{
				HowOneOperationStands row = TheStandingOf(api, kind, asOf);

				if (row.CountdownRefused) refused = true;

				operations.Add(row);
			}

			EveryKindCompletionRecord record = api.CompletionsOfEveryKind();

			var done = new List<AThingThatWasDone>();

			foreach (int row in Enumerable.Range(0, record.Kind.Length))
				done.Add(new AThingThatWasDone
				{
					Kind = record.Kind[row],
					CompletedAt = record.CompletedAt[row],
					Reading = record.Reading[row],
					Says = record.Says[row],
				});

			return new WhereEachOperationStands
			{
				AskedAt = TheMoment(asOf),
				SomeCountdownWasRefused = refused,
				Operations = operations,
				WhatHasBeenDone = done,
			};
		}

		/// <summary>
		/// La fila de esa clase. TOTAL: una clase que el canon dejara de nombrar contesta una fila
		/// vacia en vez de reventar la pantalla entera.
		/// </summary>
		public HowOneOperationStands Of(string kind)
		{
			foreach (HowOneOperationStands each in Operations)
				if (each.Kind == kind) return each;

			return new HowOneOperationStands { Kind = kind };
		}

		private static HowOneOperationStands TheStandingOf(
			DealershipCarAPI api, string kind, DateTime asOf)
		{
			bool completed = api.HasAnyCompletion(kind);
			string completions = api.DescribeCompletions(kind);

			try
			{
				bool distance = api.CanDistanceRemainingBeSaid(kind, asOf);
				bool days = api.CanDaysRemainingBeSaid(kind, asOf);

				return new HowOneOperationStands
				{
					Kind = kind,
					Says = api.DescribeCountdown(kind, asOf),
					DistanceStanding = api.ReadDistanceRemainderStanding(kind, asOf),
					DistanceSays = api.DescribeDistanceRemaining(kind, asOf),
					DistanceCanBeSaid = distance,
					DistanceRemaining = distance ? api.ReadDistanceRemaining(kind, asOf) : 0,
					DistanceUnit = distance ? api.ReadDistanceRemainingUnit(kind, asOf) : "",
					TimeStanding = api.ReadTimeRemainderStanding(kind, asOf),
					DaysSays = api.DescribeDaysRemaining(kind, asOf),
					DaysCanBeSaid = days,
					DaysRemaining = days ? api.ReadDaysRemaining(kind, asOf) : 0,
					HasBeenCompleted = completed,
					CompletionsSays = completions,
				};
			}
			catch (InvalidOperationException refused)
			{
				return new HowOneOperationStands
				{
					Kind = kind,
					CountdownRefused = true,
					RefusalSays = refused.Message,
					HasBeenCompleted = completed,
					CompletionsSays = completions,
				};
			}
		}

		private static string TheMoment(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd HH:mm:ss'Z'", CultureInfo.InvariantCulture);
		}
	}

	/// <summary>
	/// UNA clase de operacion: lo que le falta en las dos dimensiones, en cual de las siete
	/// situaciones esta cada mitad, y si tiene algo hecho en el registro. Las frases son las que
	/// compuso el dominio y ninguna se arma aca.
	/// </summary>
	public sealed class HowOneOperationStands
	{
		/// <summary>La clase, por el nombre del miembro del canon.</summary>
		public string Kind { get; init; } = "";

		/// <summary>
		/// Si la agenda RECHAZO la pregunta de esta clase. Cuando es cierto las nueve respuestas de la
		/// cuenta atras llegan vacias, asi que se lee esto antes que la frase.
		/// </summary>
		public bool CountdownRefused { get; init; }

		/// <summary>
		/// El rechazo en las palabras de la puerta. Es la constancia de que la fila es un RECHAZO y no
		/// una ausencia, y NO va a la pantalla: nombra la clase por el nombre del canon y el instante
		/// con siete decimales, que es una respuesta para quien programa y no para quien maneja.
		/// </summary>
		public string RefusalSays { get; init; } = "";

		/// <summary>
		/// LA CUENTA ATRAS ENTERA EN LA FRASE QUE COMPUSO EL DOMINIO: las dos dimensiones y las dos
		/// situaciones, sin elegir ninguna. Sin operacion agendada lo dice una sola vez.
		/// </summary>
		public string Says { get; init; } = "";

		/// <summary>En cual de las siete situaciones esta la mitad de distancia, por su nombre.</summary>
		public string DistanceStanding { get; init; } = "";

		/// <summary>Lo que le queda de distancia, en la frase del dominio.</summary>
		public string DistanceSays { get; init; } = "";

		/// <summary>Si la mitad de distancia se puede decir a ese instante.</summary>
		public bool DistanceCanBeSaid { get; init; }

		/// <summary>
		/// Lo que le queda de distancia, compuesto adentro contra el limite ya escalado. Pasado el
		/// limite es el EXCESO y nunca un negativo: cual de las dos lo dice la situacion. Cuando no se
		/// puede decir llega en cero, que NO es una distancia.
		/// </summary>
		public int DistanceRemaining { get; init; }

		/// <summary>La unidad de ese monto, por su nombre. Vacia cuando no se puede decir.</summary>
		public string DistanceUnit { get; init; } = "";

		/// <summary>En cual de las siete situaciones esta la mitad de tiempo, por su nombre.</summary>
		public string TimeStanding { get; init; } = "";

		/// <summary>Lo que le queda de plazo, en la frase del dominio.</summary>
		public string DaysSays { get; init; } = "";

		/// <summary>Si la mitad de tiempo se puede decir a ese instante.</summary>
		public bool DaysCanBeSaid { get; init; }

		/// <summary>
		/// Los dias que le quedan del plazo ya escalado. Su unidad esta en el nombre del miembro: el
		/// plazo se mide en una sola, a diferencia de la distancia. Cuando no se puede decir llega en
		/// cero, que NO es un plazo.
		/// </summary>
		public int DaysRemaining { get; init; }

		/// <summary>Si esta clase tiene alguna completacion en su cadena.</summary>
		public bool HasBeenCompleted { get; init; }

		/// <summary>
		/// El historial de esta clase en la frase del dominio. Nunca completada y no agendada son dos
		/// ausencias distintas y esta frase dice cual de las dos, que es lo unico que el historial
		/// entero no puede decir.
		/// </summary>
		public string CompletionsSays { get; init; } = "";
	}

	/// <summary>
	/// UNA cosa que le hicieron al vehiculo, con sus propias columnas y con la frase que el dominio
	/// compuso para ella. La fila la camina la cadena de ocurrencias en cada lectura y no esta
	/// guardada en ningun lado.
	/// </summary>
	public sealed class AThingThatWasDone
	{
		/// <summary>De que clase fue, por el nombre del miembro del canon.</summary>
		public string Kind { get; init; } = "";

		/// <summary>Cuando se hizo, como un instante con su hora y nunca como un dia a secas.</summary>
		public string CompletedAt { get; init; } = "";

		/// <summary>
		/// Con que lectura del odometro se hizo, con su unidad, o la constancia en palabras de que no
		/// hay ninguna. Nunca un cero y nunca la celda vacia.
		/// </summary>
		public string Reading { get; init; } = "";

		/// <summary>Lo que se hizo, entero, en la frase que compuso el dominio.</summary>
		public string Says { get; init; } = "";
	}
}
