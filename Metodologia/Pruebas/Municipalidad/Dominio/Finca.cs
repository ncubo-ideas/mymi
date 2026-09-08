using System;
using System.Collections.Generic;

namespace Municipalidad.Dominio
{
	// LA FINCA. Existe en el modelo aunque ningun papel la nombre: el portal consolida
	// por cedula, pero el IBI es de la finca, no del contribuyente. Es la correccion del
	// bloque 1 aplicada desde el primer dia: consolidarla seria repetir el error de la
	// membresia del gimnasio.
	//
	// SU VALOR ES EL HUECO 2: ningun artefacto lo trae. Entra por comando -la persona lo
	// declara- y cuando no esta, SE DICE (TieneValorAl / ValorVigenteAl guardado), no se
	// inventa. Quien lo fijo -avaluo o declaracion- es el hueco 10, y en el v1 no se
	// distingue.
	internal sealed class Finca
	{
		private readonly List<ValorDeFinca> valores = new List<ValorDeFinca>();

		internal string Numero { get; }

		internal Finca(string numero)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(numero);
			Numero = numero.Trim();
		}

		internal bool EsLaMismaQue(string numero)
		{
			return !string.IsNullOrWhiteSpace(numero) && Numero == numero.Trim();
		}

		internal void DeclararValor(ValorDeFinca valor)
		{
			ArgumentNullException.ThrowIfNull(valor);
			if (!PuedeDeclararValor(valor.Fecha))
				throw new InvalidOperationException(
					"Ya hay un valor declarado con fecha posterior. Preguntar PuedeDeclararValor antes.");
			valores.Add(valor);
		}

		// PREDICADO TOTAL para el Check: los valores entran en orden de fecha, y UNA
		// DECLARACION POR DIA. La misma fecha otra vez es el mismo hecho otra vez -regla
		// 18-: lo destapo el ciclo con journal corrido dos veces, que dejo entrar la misma
		// declaracion en la segunda corrida porque aca decia >= (hueco 12).
		internal bool PuedeDeclararValor(DateTime fecha)
		{
			if (valores.Count == 0) return true;
			return fecha.Date > valores[valores.Count - 1].Fecha;
		}

		internal int CuantosValores { get { return valores.Count; } }

		// PREDICADO TOTAL: habia un valor vigente a esa fecha?
		internal bool TieneValorAl(DateTime fecha)
		{
			foreach (ValorDeFinca v in valores)
				if (v.Fecha <= fecha.Date) return true;
			return false;
		}

		// GUARDADO: sin valor declarado no se inventa uno. Es la leccion de T1 -el Fin del
		// Plan que devolvia 01/01/0001 en silencio- aplicada al valor de la finca.
		internal decimal ValorVigenteAl(DateTime fecha)
		{
			ValorDeFinca vigente = null;
			foreach (ValorDeFinca v in valores)
				if (v.Fecha <= fecha.Date) vigente = v;
			if (vigente == null)
				throw new InvalidOperationException(
					"Esta finca no tiene valor declarado a esa fecha -hueco 2-. Preguntar TieneValorAl antes.");
			return vigente.Monto;
		}
	}
}
