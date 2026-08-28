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

			IMensajeDeEvento planRevision = PlanRevisionAdopted.Interpret(signal);
			if (planRevision != null) return planRevision;

			return ASpecificationFact(signal);
		}

		private static IMensajeDeEvento ASpecificationFact(SenalDeEvento signal)
		{
			IMensajeDeEvento opened = SpecificationRevisionOpened.Interpret(signal);
			if (opened != null) return opened;

			IMensajeDeEvento fluid = FluidSpecificationPublished.Interpret(signal);
			if (fluid != null) return fluid;

			IMensajeDeEvento tyre = TyreSpecificationPublished.Interpret(signal);
			if (tyre != null) return tyre;

			IMensajeDeEvento fuel = FuelGradePublished.Interpret(signal);
			if (fuel != null) return fuel;

			IMensajeDeEvento interval = ServiceIntervalPublished.Interpret(signal);
			if (interval != null) return interval;

			IMensajeDeEvento price = ServicePricePublished.Interpret(signal);
			if (price != null) return price;

			IMensajeDeEvento system = CoveredSystemPublished.Interpret(signal);
			if (system != null) return system;

			IMensajeDeEvento exclusion = CoverageExclusionPublished.Interpret(signal);
			if (exclusion != null) return exclusion;

			IMensajeDeEvento policy = ServicingPolicyPublished.Interpret(signal);
			if (policy != null) return policy;

			IMensajeDeEvento sealing = SpecificationRevisionSealed.Interpret(signal);
			if (sealing != null) return sealing;

			return SpecificationRevisionSuperseded.Interpret(signal);
		}
	}
}
