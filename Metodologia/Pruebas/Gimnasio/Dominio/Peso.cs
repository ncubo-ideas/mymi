using System;

namespace Gimnasio.Dominio
{
	// EL PESO, en kilos.
	//
	// El v1 y el v2 lo convertian a IMC con la altura del perfil y ese numero iba contra
	// el rango: "sola no dice nada - su rango solo existe combinada con la altura". El v3
	// lo cambio, y lo cambio Jorge (hueco 19, 2026-09-08): EL IMC LO MANDA EL GIMNASIO,
	// ya calculado y ya evaluado, con aparatos mas especializados que una division. Dos
	// fuentes de la misma verdad eran una de mas.
	//
	// Asi que el peso ya no se lee contra un rango propio: su lectura es la del IMC del
	// aparato, y ElAparatoLaEvalua lo dice. Lo que el peso conserva es la SERIE -como
	// vengo-, que sigue siendo de la app.
	internal sealed class Peso : Medida
	{
		internal Peso(decimal kilos) : base(kilos) { }

		internal override string Nombre { get { return "peso"; } }

		internal override bool ElAparatoLaEvalua { get { return true; } }

		internal override string ClaveDeReferencia(Perfil perfil)
		{
			throw new InvalidOperationException(
				"El peso no se lee contra un rango propio: el gimnasio manda el IMC ya evaluado (hueco 19). "
				+ "Preguntar ElAparatoLaEvalua antes, y pedir la evaluacion del imc.");
		}
	}
}
