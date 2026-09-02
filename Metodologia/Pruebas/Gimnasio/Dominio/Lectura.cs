using System;

namespace Gimnasio.Dominio
{
	// EL ESTADO DE HOY: bajo, normal o alto. Es el juicio 8, y sale del PERFIL.
	//
	// No confundir con Tendencia, que es el otro juicio y sale del OBJETIVO. El
	// Preguntas.md insiste en que no se deducen entre si: un valor puede estar alto y
	// mejorando, u otro normal y yendo hacia donde la persona no quiere.
	//
	// Y el numero VIAJA AL LADO de la palabra -capitulo 0 §8-: la medida nombra su
	// lectura porque es lo unico que puede decirlo mejor que nadie, pero no reemplaza
	// al hecho ni lo esconde.
	internal sealed class Lectura
	{
		private readonly string estado;

		internal decimal ValorMedido { get; }
		internal decimal ValorComparado { get; }
		internal string QueSeComparo { get; }

		private Lectura(string estado, decimal valorMedido, decimal valorComparado, string queSeComparo)
		{
			this.estado = estado;
			ValorMedido = valorMedido;
			ValorComparado = valorComparado;
			QueSeComparo = queSeComparo;
		}

		internal static Lectura De(decimal valorMedido, decimal valorComparado, string queSeComparo,
			Referencia referencia)
		{
			ArgumentNullException.ThrowIfNull(referencia);
			ArgumentException.ThrowIfNullOrWhiteSpace(queSeComparo);

			string estado = referencia.EsBajo(valorComparado) ? "bajo"
				: referencia.EsAlto(valorComparado) ? "alto"
				: "normal";

			return new Lectura(estado, valorMedido, valorComparado, queSeComparo);
		}

		internal bool EsBajo() { return estado == "bajo"; }
		internal bool EsNormal() { return estado == "normal"; }
		internal bool EsAlto() { return estado == "alto"; }

		public override string ToString() { return estado; }
	}
}
