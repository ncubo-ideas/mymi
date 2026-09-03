using System;

namespace Examenes.Dominio
{
	// UN ANALITO QUE SE INFORMA COMO UNA CATEGORIA, sin numero y sin unidad. Forma 6 de la
	// taxonomia: el color y el aspecto.
	//
	// No tiene unidad y NO es que le falte: es que un color no se mide en nada. Preguntarle
	// "cuanto da" no es una pregunta, igual que preguntarle si viene subiendo.
	//
	// Que exista este tipo es lo que permite que el color de la orina sea un analito. Antes
	// no podia serlo -Analito exigia unidad- y por eso terminaba aplanado adentro del
	// string del examen.
	internal sealed class AnalitoNominal : Analito
	{
		internal AnalitoNominal(string nombre, string paraMostrar, params string[] sinonimos)
			: base(nombre, paraMostrar, RequisitoAnatomico.Ninguno, sinonimos)
		{
		}
	}
}
