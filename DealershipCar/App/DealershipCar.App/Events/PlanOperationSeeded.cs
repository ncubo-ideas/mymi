using System;
using System.Text.Json;
using Puppets.Portador;

namespace DealershipCar.App.Events
{
	// EL FABRICANTE SIEMBRA UNA OPERACION DEL PLAN. Trae sus limites, sus dos clasificaciones y el
	// ANCLA COMPLETA -- instante, lectura y la unidad de ESA lectura (C18) -- porque una operacion
	// nunca completada se mide desde algun lado y el dominio no puede inventarlo.
	//
	// Es el mensaje mas ancho de los cinco, y por eso el que mas formas tiene de venir roto: diez
	// campos, cuatro conjuntos cerrados y dos limites que solo valen segun la base declarada.
	//
	// LA COMBINACION BASE/LIMITES NO SE VALIDA ACA, y esta bien: la valida la puerta llamando a la
	// guarda del dominio -- IntervalRule.BasisIsNeverInferred -- que es la misma y unica fuente de
	// verdad, y corre ANTES de que el motor escriba la entrada. Duplicar la tabla aca seria una
	// segunda fuente para una ley, que es justo lo que E32 y C2 argumentan en contra.
	//
	// Ver OdometerReported para por que los codigos se validan aca.
	public sealed class PlanOperationSeeded : IMensajeDeEvento
	{
		public const string TheEventType = "PlanOperationSeeded";

		private PlanOperationSeeded() { }

		public long EventNumber { get; private set; }

		public string Kind { get; private set; }

		public string Basis { get; private set; }

		public int TimeLimitDays { get; private set; }

		public int DistanceLimit { get; private set; }

		public string Unit { get; private set; }

		public bool IsMandatory { get; private set; }

		public bool IsSafety { get; private set; }

		public DateTime AnchoredAt { get; private set; }

		public int AnchorReading { get; private set; }

		public string AnchorUnit { get; private set; }

		long IMensajeDeEvento.Evento { get { return EventNumber; } }

		string IMensajeDeEvento.Tipo { get { return TheEventType; } }

		public static PlanOperationSeeded Interpret(SenalDeEvento signal)
		{
			if (signal.Tipo != TheEventType) return null;
			if (signal.Numero <= 0) return null;
			if (signal.Contenido.Length == 0) return null;

			try
			{
				using (JsonDocument document = JsonDocument.Parse(signal.Contenido))
				{
					JsonElement root = document.RootElement;

					string kind = Payload.Text(root, "kind");
					string basis = Payload.Text(root, "basis");
					string unit = Payload.Text(root, "unit");
					string anchorUnit = Payload.Text(root, "anchorUnit");

					if (!WireName.NamesAnOperationKind(kind)) return null;
					if (!WireName.NamesAnIntervalBasis(basis)) return null;
					if (!WireName.NamesADistanceUnit(unit)) return null;
					if (!WireName.NamesADistanceUnit(anchorUnit)) return null;

					// LOS DOS LIMITES SE EXIGEN PRESENTES aunque valgan cero. Cero es "esta
					// dimension no tiene limite" -- una afirmacion del plan, no un campo que falta
					// -- y la guarda del dominio decide con los dos juntos si la base declarada se
					// sostiene. Un evento que omite uno no esta diciendo cero: no esta diciendo.
					int timeLimitDays;
					if (!Payload.Whole(root, "timeLimitDays", out timeLimitDays)) return null;

					int distanceLimit;
					if (!Payload.Whole(root, "distanceLimit", out distanceLimit)) return null;

					int anchorReading;
					if (!Payload.Whole(root, "anchorReading", out anchorReading)) return null;

					if (timeLimitDays < 0 || distanceLimit < 0 || anchorReading < 0) return null;

					DateTime anchoredAt;
					if (!Payload.Instant(root, "anchoredAt", out anchoredAt)) return null;

					bool isMandatory;
					if (!Payload.Truth(root, "isMandatory", out isMandatory)) return null;

					bool isSafety;
					if (!Payload.Truth(root, "isSafety", out isSafety)) return null;

					return new PlanOperationSeeded
					{
						EventNumber = signal.Numero,
						Kind = kind,
						Basis = basis,
						TimeLimitDays = timeLimitDays,
						DistanceLimit = distanceLimit,
						Unit = unit,
						IsMandatory = isMandatory,
						IsSafety = isSafety,
						AnchoredAt = anchoredAt,
						AnchorReading = anchorReading,
						AnchorUnit = anchorUnit,
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
