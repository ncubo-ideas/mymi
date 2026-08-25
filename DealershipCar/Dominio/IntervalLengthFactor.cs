using System;

namespace DealershipCar.Dominio
{
	// TRES AUSENCIAS DELIBERADAS, que es lo unico de este tipo que no se lee del codigo.
	//
	//   NO HAY MIEMBRO QUE DEVUELVA UNA REGLA NI UNA BASE. OHS-07 cerro una tension real --
	//   "una sucursal que acorta intervalos para vender servicios destruye la confianza; una
	//   que no puede adaptarse a las condiciones locales es inutil" -- decidiendo que el plan
	//   del fabricante es la linea base intocable y que estos factores son el UNICO ajuste
	//   permitido. Este tipo no evita cambiar la base: no tiene con que. Hay una prueba con
	//   lista blanca sobre sus miembros, como la de DistanceUnit, porque una ausencia no se
	//   puede afirmar llamando a algo.
	//
	//   COMMERCIAL NO TIENE FACTOR. §6 nombra dos y ninguno es el suyo, asi que no escala nada.
	//   Inventarle uno seria acortar intervalos por una via que nadie declaro.
	//
	//   LOS UMBRALES DE POLITICA NO SE ESCALAN. El anticipo y la gracia son de la agencia y
	//   valen igual para todas las operaciones; el factor toca el largo que declaro el
	//   fabricante. Escalar los dos seria escalar dos veces la misma decision.
	//
	// LA ALTERNATIVA RECHAZADA para LowDistance: un factor de distancia en cero. Un limite de
	// cero unidades venceria en el acto, que es lo contrario de "los limites de distancia se
	// ignoran" (§6). Por eso ignorar viaja como una pregunta y no como un numero.

	/// <summary>
	/// El escalamiento que un <see cref="UsageProfile"/> aplica al LARGO de un intervalo:
	/// cuantos dias y cuanta distancia quedan de los que el plan del fabricante declaro.
	/// Soporta la ley L8 (OINV-22). Nunca cambia la base de una operacion ni la quita: no
	/// expone nada con que hacerlo.
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
		/// El perfil de uso del que sale este escalamiento. OHS-07 exige que la app siempre
		/// pueda mostrar CUAL perfil aplico, asi que el factor no se separa nunca de su perfil.
		/// </summary>
		internal UsageProfile Profile { get; }

		/// <summary>
		/// Si este perfil IGNORA los limites de distancia (§6, LowDistance). Es una pregunta y
		/// no un numero a proposito: ignorar no es escalar por cero -- un limite de cero
		/// unidades venceria en el acto. Una distancia ignorada es una dimension que NO SE
		/// PUEDE EVALUAR, igual que una con discrepancia de unidad (GAP G2).
		/// </summary>
		internal bool DistanceIsIgnored { get; }

		/// <summary>
		/// El escalamiento del perfil declarado. Cada perfil trae el suyo de §6; los que §6 no
		/// nombra no escalan nada, porque un factor que nadie declaro seria acortar intervalos
		/// por una via que OHS-07 cerro.
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
		/// Cuantos dias quedan del limite de tiempo que el fabricante declaro, una vez aplicado
		/// el escalamiento del perfil. Un limite ausente -- cero -- sigue ausente: escalar una
		/// ausencia no la convierte en un limite.
		/// </summary>
		internal int ApplyToTimeLimitDays(int declaredTimeLimitDays)
		{
			return ScaledLength(declaredTimeLimitDays, _timeFactor);
		}

		/// <summary>
		/// Cuanta distancia queda del limite que el fabricante declaro, una vez aplicado el
		/// escalamiento del perfil. Escala el LARGO en la unidad del limite: no la convierte y
		/// no la cambia de unidad (OINV-21). Cuando el perfil IGNORA la distancia
		/// (<see cref="DistanceIsIgnored"/>) este numero no se usa para nada -- la dimension no
		/// se evalua, y evaluarla contra un limite escalado seria evaluarla igual.
		/// </summary>
		internal int ApplyToDistanceLimit(int declaredDistanceLimit)
		{
			return ScaledLength(declaredDistanceLimit, _distanceFactor);
		}

		// El redondeo es una decision con nombre y no un truncamiento accidental: 365 por 0.70
		// son 255.5, y truncar acortaria el intervalo una segunda vez ademas del factor. Se
		// redondea al entero mas cercano, alejandose del cero en el empate.
		//
		// El piso de uno existe para que un limite declarado nunca desaparezca al escalarlo: un
		// intervalo de un dia por 0.70 da 0.7, y un limite de cero dias es la ausencia de
		// limite -- que es una cosa distinta de un limite muy corto.
		private static int ScaledLength(int declaredLength, decimal factor)
		{
			if (declaredLength <= 0) return 0;

			int scaled = (int)Math.Round(declaredLength * factor, MidpointRounding.AwayFromZero);

			return scaled < 1 ? 1 : scaled;
		}
	}
}
