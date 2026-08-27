using System;

namespace Examenes.Dominio
{
	// SI ESTE ANALITO EXISTE PARA ESTA PERSONA. Es el primero de los tres efectos que el
	// sexo produce, y separarlo de los otros dos es el punto de diseno entero:
	//
	//   APLICABILIDAD  el analito NO EXISTE para esta persona   antigeno en una mujer
	//   RANGO          existe, con otros umbrales               hemoglobina, HDL
	//   INTERPRETACION mismo valor, distinta lectura            segun el analito
	//
	// Confundir la primera con "fuera de rango" es el error grave: un antigeno prostatico
	// en una mujer NO es un valor anormal, es UNA PREGUNTA SIN SENTIDO. Un modelo que las
	// mezcle en un solo `if` termina clasificando lo inexistente como lo patologico.
	//
	// SON TRES ESTADOS Y NO DOS, y ese tercero es lo que hace que este tipo exista en vez
	// de ser un bool: cuando el sexo todavia no se sabe, la respuesta no es "no aplica"
	// —eso seria esconder la tarjeta por las dudas— ni "aplica" —eso seria interpretar un
	// analito que quizas no corresponde—. Es "no se puede saber", y tiene su propio
	// veredicto (ver Veredicto.RequiereSexo).
	public sealed class Aplicabilidad
	{
		public static readonly Aplicabilidad Aplica = new Aplicabilidad("aplica");

		public static readonly Aplicabilidad NoAplica = new Aplicabilidad("no aplica");

		// No se puede decidir con lo que se sabe hoy de la persona. Se resuelve en dos
		// toques, y por eso la superficie insiste.
		public static readonly Aplicabilidad Indeterminada = new Aplicabilidad("indeterminada");

		public string Nombre { get; private set; }

		private Aplicabilidad(string nombre)
		{
			Nombre = nombre;
		}

		public bool EsAplica => ReferenceEquals(this, Aplica);

		public bool EsNoAplica => ReferenceEquals(this, NoAplica);

		public bool EsIndeterminada => ReferenceEquals(this, Indeterminada);

		// Si de este analito se puede decir ALGO. Es lo que la superficie pregunta para
		// saber si la tarjeta existe: con NoAplica no existe, con Indeterminada existe
		// mostrando el valor y reteniendo el veredicto.
		public bool HayAlgoQueDecir => !EsNoAplica;

		public override string ToString() => Nombre;
	}
}
