using System;

namespace Cargas.Dominio
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
	public sealed class AnalitoNominal : Analito
	{
		internal AnalitoNominal(string nombre, string paraMostrar, params string[] sinonimos)
			: base(nombre, paraMostrar, sinonimos)
		{
		}

		// NO forma serie, y es lo unico de la taxonomia que no la forma. Un color no se
		// ordena: "ámbar, oscuro, ámbar" no es una tendencia ni una presencia, son tres
		// observaciones sueltas. Que devuelva false aca es lo que impide que el Expediente
		// le arme una serie que despues nadie podria evaluar.
		public override bool FormaSerie => false;
	}
}
