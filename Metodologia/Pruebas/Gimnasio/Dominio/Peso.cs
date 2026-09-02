using System;

namespace Gimnasio.Dominio
{
	// EL PESO, en kilos.
	//
	// Lo propio, del Preguntas.md: "sola no dice nada - su rango solo existe combinada
	// con la altura". Es la unica medida que NO se compara consigo misma: se convierte
	// a IMC y ese es el numero que va contra el rango.
	//
	// Y por eso su referencia NO depende del perfil: una vez que es IMC, el rango es
	// el mismo para todos. La altura ya hizo su trabajo en la conversion. Es la unica
	// que funciona asi, y es exactamente lo que la vuelve una clase propia.
	internal sealed class Peso : Medida
	{
		internal Peso(decimal kilos) : base(kilos) { }

		internal override string Nombre { get { return "peso"; } }

		internal override string ClaveDeReferencia(Perfil perfil)
		{
			ArgumentNullException.ThrowIfNull(perfil);
			return "imc";
		}

		internal override decimal ValorComparable(Perfil perfil)
		{
			ArgumentNullException.ThrowIfNull(perfil);
			decimal metros = perfil.AlturaEnCentimetros / 100m;
			return Valor / (metros * metros);
		}

		internal override string QueSeCompara { get { return "imc"; } }
	}
}
