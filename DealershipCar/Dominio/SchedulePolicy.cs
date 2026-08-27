using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// Los umbrales de politica de la agencia: anticipo, gracia, tope y ventana de
	/// aplazamiento, factores de perfil de uso y ventana de agrupamiento. Iguales para todas las
	/// operaciones, a diferencia de los limites por operacion, que llegan del plan del fabricante.
	/// <para>
	/// Que sean constantes es una decision de este slice, no una creencia. Este tipo es el
	/// SEAM por donde va a entrar la configuracion; ninguna regla lleva el numero adentro.
	/// </para>
	/// </summary>
	internal static class SchedulePolicy
	{
		/// <summary>Cuantos dias antes del vencimiento se avisa. Cortesia, nunca parada.</summary>
		internal const int DueSoonLeadDays = 30;

		/// <summary>
		/// A cuantas unidades del limite se avisa, en la unidad del limite de la operacion: la
		/// politica no lleva unidad porque no hay conversion.
		/// </summary>
		internal const int DueSoonLeadDistance = 1000;

		/// <summary>Cuantos dias pasado el vencimiento se toleran antes de contarla como no hecha.</summary>
		internal const int OverdueGraceDays = 14;

		/// <summary>Cuantas unidades pasado el limite se toleran, en la unidad del limite.</summary>
		internal const int OverdueGraceDistance = 500;

		/// <summary>
		/// Cuantas veces se puede aplazar una operacion. En el tope el siguiente intento
		/// se rechaza, y eso es un estado, no una excepcion.
		/// </summary>
		internal const int MaxDeferralsPerOperation = 2;

		/// <summary>
		/// Cuantos dias dura cada aplazamiento. Cumplida la ventana la operacion vuelve a
		/// Due y la cuenta usada se conserva.
		/// </summary>
		internal const int DeferralWindowDays = 60;

		/// <summary>
		/// Cuanto del largo declarado queda con SevereDuty. Escala largo y nada mas: no toca la
		/// base ni quita operaciones.
		/// </summary>
		internal const decimal SevereDutyIntervalFactor = 0.70m;

		/// <summary>
		/// Cuanto del largo declarado queda con LowDistance: los limites de tiempo quedan intactos.
		/// Que los de distancia se ignoren es comportamiento y vive en IntervalLengthFactor.
		/// </summary>
		internal const decimal LowDistanceTimeFactor = 1.00m;

		/// <summary>
		/// Dentro de cuantos dias conviene juntar operaciones en una visita. Declarado porque la
		/// politica lo nombra; todavia no lo usa nadie.
		/// </summary>
		internal const int BundlingWindowDays = 60;

		/// <summary>Su mitad de distancia, en la unidad del limite. Tambien de un slice posterior.</summary>
		internal const int BundlingWindowDistance = 2000;
	}
}
