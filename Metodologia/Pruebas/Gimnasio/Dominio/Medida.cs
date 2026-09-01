using System;

namespace Gimnasio.Dominio
{
	// UNA MEDIDA DEL PERFIL CORPORAL.
	//
	// Es abstracta, y cada medida concreta es su propia clase. NO hay una clase unica
	// con un `tipo` adentro: eso obligaria a un switch en cada lectura, que es la
	// porosidad que el capitulo 4 manda evitar.
	//
	// Lo que hace distinta a cada una -y es el juicio 7 del Preguntas.md- son dos
	// cosas, y ninguna es cosmetica:
	//
	//   1. COMO SE INDEXA SU REFERENCIA. El rango de la grasa se mueve con el genero y
	//      la edad; el de la cintura solo con el genero; el del peso no depende del
	//      perfil una vez convertido a IMC. Cada una lo sabe de si misma.
	//
	//   2. QUE COMPARA. El peso no se compara consigo mismo: se convierte usando la
	//      altura. Las demas se comparan tal cual.
	//
	// El perfil es PARAMETRO OBLIGATORIO y no un campo opcional: sin el no se puede
	// pedir una lectura, y eso lo hace cumplir el compilador.
	internal abstract class Medida
	{
		internal decimal Valor { get; }

		protected Medida(decimal valor)
		{
			if (valor <= 0)
				throw new ArgumentOutOfRangeException(nameof(valor), valor,
					"Una medida corporal no puede ser cero ni negativa.");
			Valor = valor;
		}

		internal abstract string Nombre { get; }

		// Con que llave se busca la referencia de ESTA medida para ESTE perfil.
		// Es lo que hace que el perfil cargue peso de verdad y no sea ceremonia.
		internal abstract string ClaveDeReferencia(Perfil perfil);

		// Que numero se compara contra el rango, y como se llama esa magnitud.
		// Para casi todas es el propio valor; el peso es la excepcion.
		internal virtual decimal ValorComparable(Perfil perfil) { return Valor; }
		internal virtual string QueSeCompara { get { return Nombre; } }

		// HACIA DONDE ES DESEABLE QUE SE MUEVA. Por defecto lo decide el objetivo
		// -juicio 1-, y una subclase puede fijarlo. Es herencia cargando verdad de
		// dominio: MasaMuscular no consulta el objetivo porque no le hace falta.
		internal virtual Direccion DireccionDeseable(Objetivo objetivo)
		{
			ArgumentNullException.ThrowIfNull(objetivo);
			return objetivo.DireccionPara(this);
		}

		internal Lectura Leer(Perfil perfil, Referencia referencia)
		{
			ArgumentNullException.ThrowIfNull(perfil);
			ArgumentNullException.ThrowIfNull(referencia);
			return Lectura.De(Valor, ValorComparable(perfil), QueSeCompara, referencia);
		}

		internal bool EsDeLaMismaClaseQue(Medida otra)
		{
			return otra != null && otra.Nombre == Nombre;
		}
	}
}
