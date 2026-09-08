using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// En que situacion esta lo que le queda a una dimension de una operacion. Conjunto cerrado de
	/// siete, porque son siete respuestas distinguibles y solo una de ellas trae un monto. Se elige
	/// siempre adentro -- es una RESPUESTA, nunca una entrada -- y sale solo como proyeccion
	/// escalar de su nombre.
	/// <para>
	/// Ninguno de los siete quiere decir "no lo se": lo que no se puede decir se dice nombrando la
	/// razon, que es para lo que hay cinco miembros en vez de uno.
	/// </para>
	/// </summary>
	internal sealed class RemainderStanding
	{
		/// <summary>Queda algo por delante del limite ya escalado.</summary>
		internal static readonly RemainderStanding StillAhead =
			new RemainderStanding("StillAhead", "Todavía falta");

		/// <summary>
		/// El limite ya se alcanzo o se paso. El umbral es INCLUSIVO, como los demas de este
		/// modulo: el limite exacto ya llego.
		/// </summary>
		internal static readonly RemainderStanding AlreadyPassed =
			new RemainderStanding("AlreadyPassed", "Ya se pasó");

		/// <summary>
		/// La ley evaluada no mide esta dimension. NO es lo mismo que no haberse podido medir, y
		/// esa diferencia es el punto entero del conjunto: lo que no existe no se puede curar.
		/// </summary>
		internal static readonly RemainderStanding OutsideTheLaw =
			new RemainderStanding("OutsideTheLaw", "No se mide así");

		/// <summary>
		/// La ley mide la dimension y no hay de donde contar. Es la UNICA de las tres suspensiones
		/// que se cura trayendo un hecho.
		/// </summary>
		internal static readonly RemainderStanding SuspendedByAnAbsentReading =
			new RemainderStanding("SuspendedByAnAbsentReading", "Falta la lectura");

		/// <summary>El limite y las mediciones estan en unidades distintas, y no se convierten.</summary>
		internal static readonly RemainderStanding SuspendedByAUnitMismatch =
			new RemainderStanding("SuspendedByAUnitMismatch", "Unidades distintas");

		/// <summary>El perfil de uso declarado ignora los limites de esta dimension.</summary>
		internal static readonly RemainderStanding SuspendedByTheUsageProfile =
			new RemainderStanding("SuspendedByTheUsageProfile", "El perfil no la mide");

		/// <summary>
		/// Esta agenda no tiene ninguna operacion activa de esa clase, asi que no hay dimension que
		/// medir. No es una suspension: nadie la cura trayendo una lectura.
		/// </summary>
		internal static readonly RemainderStanding NoOperationScheduled =
			new RemainderStanding("NoOperationScheduled", "Sin operación agendada");

		/// <summary>El nombre. Es lo unico que cruza hacia afuera; el objeto no se entrega.</summary>
		internal string Name { get; }

		/// <summary>
		/// Como se dice esta situacion en la frase que alguien lee. NO es lo que cruza la puerta:
		/// eso sigue siendo <see cref="Name"/>.
		/// </summary>
		internal string HowItIsSaid { get; }

		private RemainderStanding(string name, string howItIsSaid)
		{
			Name = name;
			HowItIsSaid = howItIsSaid;
		}
	}
}
