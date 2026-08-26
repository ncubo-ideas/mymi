using Puppets.Portador;

namespace DealershipCar.App.Events
{
	// LA COSTURA DE ROUTING DE ESTE NEGOCIO. Forma de InputRouting: senal cruda -> mensaje tipado,
	// y null DESCARTA.
	//
	// CINCO CASOS Y UN SOLO ORDEN POSIBLE, porque cada Interpretar empieza rechazando lo que no es
	// suyo por el tag: una senal atraviesa los cinco y a lo sumo uno la reconoce. Eso es lo que
	// hace que agregar un tipo nuevo sea agregar una linea -- los que ya estan siguen devolviendo
	// null sobre lo que no consumen, sin que haya que tocarlos.
	//
	// UN TIPO QUE ESTA APP NO CONSUME SE DESCARTA, no se traga. El concesionario publica su
	// narrativa entera y este modulo modela una rebanada de ella: recibir un evento de garantia,
	// de facturacion o de cita es lo NORMAL, no un defecto. Inventar un comando para tragarselo
	// pondria en el journal de la persona una entrada que no significa nada, y el motor la
	// escribiria antes de descubrirlo (C22).
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
