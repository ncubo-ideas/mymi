using Puppets.Portador;

namespace DealershipCar.App.Events
{
	public static class DealershipEventRoute
	{
		public static IMensajeDeEvento Route(SenalDeEvento signal)
		{
			IMensajeDeEvento reading = OdometerReported.Interpret(signal);
			if (reading != null) return reading;

			IMensajeDeEvento crossing = ConditionThresholdCrossed.Interpret(signal);
			if (crossing != null) return crossing;

			IMensajeDeEvento completion = OperationCompleted.Interpret(signal);
			if (completion != null) return completion;

			IMensajeDeEvento seeded = PlanOperationSeeded.Interpret(signal);
			if (seeded != null) return seeded;

			return PlanRevisionAdopted.Interpret(signal);
		}
	}
}
