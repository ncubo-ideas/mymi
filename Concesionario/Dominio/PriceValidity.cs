using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// DONDE CAE EL INSTANTE PREGUNTADO dentro de la vigencia de un precio, por su codigo. El
	/// dominio no lee ningun reloj: sin instante no hay forma de decir "vencido" respecto a nada, y
	/// esa situacion tiene su propio miembro en vez de un instante en blanco.
	/// </summary>
	internal static class PriceValidity
	{
		/// <summary>Nadie aporto ningun instante, asi que no se dice si rige.</summary>
		internal const string NotAsked = "NotAsked";

		/// <summary>El instante preguntado cae antes: esa vigencia todavia no ha
		/// empezado.</summary>
		internal const string NotYetStarted = "NotYetStarted";

		/// <summary>El instante preguntado cae adentro: esa vigencia esta rigiendo.</summary>
		internal const string InForce = "InForce";

		/// <summary>El instante preguntado cae despues: esa vigencia ya paso.</summary>
		internal const string AlreadyPassed = "AlreadyPassed";
	}
}
