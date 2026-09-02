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

		// RESOLUTOR TOTAL DEL CONJUNTO CERRADO DE MEDIDAS.
		//
		// No estaba, y lo pidio el perform. En C# se construye la subclase directo
		// -new Cintura(88)-; desde un script no se puede: un ternario no unifica dos
		// subclases a su base comun (regla 12) y un conjunto cerrado sin resolutor es
		// inalcanzable desde un comando (regla 11).
		//
		// Que las cuatro medidas eran un conjunto cerrado estaba a la vista desde el
		// principio. Que hacia falta esta puerta, no: aparecio al atarlo al perform.
		internal static Medida Crear(string nombre, decimal valor)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
			switch (nombre.Trim().ToLowerInvariant())
			{
				case "peso": return new Peso(valor);
				case "grasa": return new PorcentajeDeGrasa(valor);
				case "cintura": return new Cintura(valor);
				case "masa-muscular": return new MasaMuscular(valor);
				default:
					throw new ArgumentOutOfRangeException(nameof(nombre), nombre,
						"Medida desconocida. Las que existen son: peso, grasa, cintura, masa-muscular.");
			}
		}

		// El predicado total que acompana al resolutor: es lo que permite preguntar
		// desde un Check antes de escribir, en vez de reventar a mitad del comando.
		internal static bool EsUnNombreConocido(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return false;
			switch (nombre.Trim().ToLowerInvariant())
			{
				case "peso":
				case "grasa":
				case "cintura":
				case "masa-muscular": return true;
				default: return false;
			}
		}
	}
}
