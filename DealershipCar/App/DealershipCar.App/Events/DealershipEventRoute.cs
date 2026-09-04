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

			IMensajeDeEvento sheet = ASpecificationFact(signal);
			if (sheet != null) return sheet;

			IMensajeDeEvento ofThisVehicle = AVehicleRecordFact(signal);
			if (ofThisVehicle != null) return ofThisVehicle;

			IMensajeDeEvento ofTheShop = AShopFact(signal);
			if (ofTheShop != null) return ofTheShop;

			return ACostFact(signal);
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

		private static IMensajeDeEvento AVehicleRecordFact(SenalDeEvento signal)
		{
			IMensajeDeEvento delivery = VehicleDelivered.Interpret(signal);
			if (delivery != null) return delivery;

			return WarrantyEnvelopePublished.Interpret(signal);
		}

		private static IMensajeDeEvento AShopFact(SenalDeEvento signal)
		{
			IMensajeDeEvento entry = ShopEntryRecorded.Interpret(signal);
			if (entry != null) return entry;

			IMensajeDeEvento collection = ShopCollectionRecorded.Interpret(signal);
			if (collection != null) return collection;

			return ShopStayRecorded.Interpret(signal);
		}

		private static IMensajeDeEvento ACostFact(SenalDeEvento signal)
		{
			IMensajeDeEvento recurring = RecurringCostRecorded.Interpret(signal);
			if (recurring != null) return recurring;

			return DurableCostRecorded.Interpret(signal);
		}
	}
}
