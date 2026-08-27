using System;

namespace DealershipCar.Dominio
{
	/// <summary>
	/// El escalamiento que un <see cref="UsageProfile"/> aplica al LARGO de un intervalo (ley
	/// L8, OINV-22). Nunca cambia la base de una operacion ni la quita: no expone nada con que
	/// hacerlo.
	/// </summary>
	internal sealed class IntervalLengthFactor
	{
		private readonly decimal _timeFactor;
		private readonly decimal _distanceFactor;

		private IntervalLengthFactor(
			UsageProfile profile, decimal timeFactor, decimal distanceFactor, bool distanceIsIgnored)
		{
			Profile = profile;
			_timeFactor = timeFactor;
			_distanceFactor = distanceFactor;
			DistanceIsIgnored = distanceIsIgnored;
		}

		/// <summary>
		/// El perfil del que sale. OHS-07 exige poder mostrar cual aplico, asi que el factor no se
		/// separa nunca de su perfil.
		/// </summary>
		internal UsageProfile Profile { get; }

		/// <summary>
		/// Si este perfil ignora los limites de distancia (§6, LowDistance). Es una pregunta y no
		/// un numero: ignorar no es escalar por cero, que venceria en el acto.
		/// </summary>
		internal bool DistanceIsIgnored { get; }

		/// <summary>
		/// El escalamiento del perfil declarado. Los que §6 no nombra no escalan nada.
		/// </summary>
		internal static IntervalLengthFactor For(UsageProfile profile)
		{
			if (profile == null)
				throw new DomainException(
					"No hay escalamiento sin perfil de uso: el factor es la politica de UN "
					+ "perfil, no un numero suelto.");

			if (profile == UsageProfile.SevereDuty)
				return new IntervalLengthFactor(
					profile,
					SchedulePolicy.SevereDutyIntervalFactor,
					SchedulePolicy.SevereDutyIntervalFactor,
					false);

			if (profile == UsageProfile.LowDistance)
				return new IntervalLengthFactor(
					profile,
					SchedulePolicy.LowDistanceTimeFactor,
					SchedulePolicy.LowDistanceTimeFactor,
					true);

			return new IntervalLengthFactor(profile, 1.00m, 1.00m, false);
		}

		/// <summary>
		/// Los dias que quedan del limite declarado tras escalarlo. Un limite ausente -- cero --
		/// sigue ausente.
		/// </summary>
		internal int ApplyToTimeLimitDays(int declaredTimeLimitDays)
		{
			return ScaledLength(declaredTimeLimitDays, _timeFactor);
		}

		/// <summary>
		/// La distancia que queda del limite declarado tras escalarlo, en la unidad del limite: no
		/// la convierte (OINV-21). No se usa cuando <see cref="DistanceIsIgnored"/>.
		/// </summary>
		internal int ApplyToDistanceLimit(int declaredDistanceLimit)
		{
			return ScaledLength(declaredDistanceLimit, _distanceFactor);
		}

		private static int ScaledLength(int declaredLength, decimal factor)
		{
			if (declaredLength <= 0) return 0;

			int scaled = (int)Math.Round(declaredLength * factor, MidpointRounding.AwayFromZero);

			return scaled < 1 ? 1 : scaled;
		}
	}
}
