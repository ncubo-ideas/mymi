using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// De que clase es un gasto: uno que el periodo en que se pago consume entero, o uno que sigue
	/// dando valor despues. Conjunto cerrado de dos, porque son las dos que hacen legible un total.
	/// <para>
	/// Se elige SIEMPRE adentro, por cual comando registro la entrada, y sale solo como proyeccion de
	/// su nombre. No hay ningun sitio por donde entre como dato: el dominio no la infiere y nadie de
	/// afuera la dice.
	/// </para>
	/// </summary>
	internal sealed class CostClassification
	{
		/// <summary>
		/// El gasto sigue dando valor despues del periodo en que se pago, asi que se reparte a lo
		/// largo de su vida util. Llantas, una bateria, una faja de tiempo.
		/// </summary>
		internal static readonly CostClassification Durable =
			new CostClassification("Durable", "Duradero");

		/// <summary>
		/// El gasto se consume en el periodo en que se incurrio. Gasolina, un cambio de aceite, un
		/// lavado.
		/// </summary>
		internal static readonly CostClassification Recurring =
			new CostClassification("Recurring", "Recurrente");

		/// <summary>El nombre. Es lo unico que cruza hacia afuera; el objeto no se entrega.</summary>
		internal string Name { get; }

		/// <summary>
		/// Como se dice esta clase en la frase que alguien lee. NO es lo que cruza la puerta: eso
		/// sigue siendo <see cref="Name"/>.
		/// </summary>
		internal string HowItIsSaid { get; }

		private CostClassification(string name, string howItIsSaid)
		{
			Name = name;
			HowItIsSaid = howItIsSaid;
		}
	}
}
