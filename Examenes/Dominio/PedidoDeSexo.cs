using System;
using System.Collections.Generic;
using System.Text;

namespace Examenes.Dominio
{
	// EL DATO QUE FALTA, dicho por el dominio: cuantos resultados quedan sin leer sin
	// saber el sexo, cuales son, y por que.
	//
	// POR QUE ES UN TIPO DEL DOMINIO Y NO UN TEXTO DE LA PANTALLA. Alvaro pidio que "la
	// pantalla de la UI deberia insistir que le diga el sexo cuando aun es desconocido", y
	// la tentacion es escribir ese parrafo en el .razor. No se puede: la frase dice CUANTOS
	// resultados y CUALES, y para saberlo hay que recorrer cada serie e interpretarla con
	// el criterio de su analito. Una pantalla no tiene de donde sacar eso.
	//
	// INSISTIR BIEN NO ES UN MODAL. Lo que hace que este bloque funcione mejor que un
	// recordatorio son tres cosas, y las tres salen de aca:
	//
	//   1. es ESPECIFICO y CUANTIFICADO   "2 de tus 7 resultados", no "completá tu perfil"
	//   2. explica el porque EN TERMINOS  el costo de no responder esta a la vista
	//      DE LA PERSONA
	//   3. se responde en un toque        la superficie pone dos botones, nada mas
	//
	// SEVERIDAD INFORMATIVO, SIEMPRE. No es un hallazgo clinico y no debe pintarse de
	// ambar ni de rojo: el semaforo esta reservado para resultados. Por eso este tipo ni
	// siquiera expone una severidad variable —la dice y es una sola—.
	//
	// EL COPY VA EN CLAVE CLINICA, no de identidad: se pide para comparar contra los
	// rangos correctos. Si la persona prefiere no contestar, el dominio se queda en
	// Desconocido y lo dice con honestidad, que ya esta cubierto por el resto del diseno.
	//
	// Carrier por identidad: sin Equals, sin GetHashCode, no es llave de nada.
	internal sealed class PedidoDeSexo
	{
		private readonly List<Analito> sinLeer;

		internal Sexo Sexo { get; private set; }

		// Cuantos analitos con historia tiene el expediente. Es el denominador de la frase.
		internal int Analitos { get; private set; }

		internal PedidoDeSexo(Sexo sexo, IReadOnlyList<Analito> sinLeer, int analitos)
		{
			ArgumentNullException.ThrowIfNull(sexo);
			ArgumentNullException.ThrowIfNull(sinLeer);

			Sexo = sexo;
			this.sinLeer = new List<Analito>(sinLeer);
			Analitos = analitos;
		}

		// SI HAY QUE INSISTIR. No alcanza con que el sexo sea desconocido: si ningun
		// resultado quedo sin leer por eso —un expediente que solo tiene una orina, por
		// ejemplo— el bloque no tiene nada que reclamar, y pedirle un dato a alguien sin
		// poder decirle para que es exactamente el recordatorio generico que este diseno
		// evita.
		internal bool HaceFalta => !Sexo.EsConocido && sinLeer.Count > 0;

		internal int SinInterpretar => sinLeer.Count;

		// La severidad, dicha: es SIEMPRE informativo. Viaja para que la superficie use el
		// mismo vocabulario que en todo lo demas y no tenga que acordarse de la regla.
		internal Severidad Severidad => Severidad.Informativo;

		internal string NombreDeLaSeveridad => Severidad.Nombre;

		// LA FRASE. La escribe el dominio porque solo el dominio sabe por que falta cada
		// uno: en la hemoglobina falta contra que rango comparar, en el antigeno falta
		// saber si el examen corresponde. Son dos motivos distintos y la frase los dice
		// distinto, que es lo que una plantilla no podria hacer.
		internal string ParaSonia
		{
			get
			{
				if (!HaceFalta) return "";

				var frase = new StringBuilder();
				frase.Append("Sin saber tu sexo no puedo interpretar ");
				frase.Append(SinInterpretar);
				frase.Append(" de tus ");
				frase.Append(Analitos);
				frase.Append(" resultados: ");

				frase.Append(PorRango());
				frase.Append(PorAplicabilidad());
				frase.Append('.');

				return frase.ToString();
			}
		}

		// EL PORQUE, EN CLAVE CLINICA. Es la segunda linea del bloque y no la escribe la
		// pantalla: el encuadre del dato —parametro clinico, no declaracion de identidad—
		// es una decision del dominio, y dejarlo en un .razor lo pondria a un edit de
		// distancia de convertirse en otra cosa.
		//
		// No es una Accion del conjunto cerrado y no lleva plazo, porque no es un consejo
		// clinico: es un dato que falta.
		internal string ElPorQue =>
			HaceFalta ? "Es para comparar tus resultados contra los rangos correctos." : "";

		// Los que quedan sin leer porque SU RANGO depende del sexo. Es el segundo efecto.
		private string PorRango()
		{
			var cuales = new List<string>();
			foreach (Analito analito in sinLeer)
				if (analito.Requisito == RequisitoAnatomico.Ninguno) cuales.Add(analito.ParaMostrar.ToLowerInvariant());

			if (cuales.Count == 0) return "";

			// Se dice "el rango DE x" y no "x se compara" para no tener que elegir el
			// articulo de cada analito —la hemoglobina, el colesterol— desde una lista
			// generica. El genero de un nombre no es conocimiento clinico, y meterlo en el
			// conjunto cerrado por una frase seria pagarlo caro.
			return (cuales.Count == 1 ? "el rango de " : "los rangos de ")
				+ Enumerar(cuales)
				+ (cuales.Count == 1 ? " es distinto" : " son distintos")
				+ " según el sexo";
		}

		// Los que quedan sin leer porque no se sabe si CORRESPONDEN. Es el primer efecto, y
		// se dice con otras palabras a proposito: no es que el umbral cambie, es que el
		// examen quizas no sea de esta persona.
		private string PorAplicabilidad()
		{
			var cuales = new List<string>();
			foreach (Analito analito in sinLeer)
				if (analito.Requisito != RequisitoAnatomico.Ninguno) cuales.Add(analito.ParaMostrar.ToLowerInvariant());

			if (cuales.Count == 0) return "";

			string coma = PorRango().Length > 0 ? ", y " : "";
			return coma + "hay " + (cuales.Count == 1 ? "un examen" : $"{cuales.Count} exámenes")
				+ " —" + Enumerar(cuales) + "— que quizás no te "
				+ (cuales.Count == 1 ? "corresponda" : "correspondan");
		}

		// "la hemoglobina", "la hemoglobina y el colesterol", "a, b y c". Es armado de
		// lista, no redaccion: lo unico que decide es donde va la "y".
		private static string Enumerar(List<string> cuales)
		{
			var texto = new StringBuilder();
			for (int i = 0; i < cuales.Count; i++)
			{
				if (i > 0) texto.Append(i == cuales.Count - 1 ? " y " : ", ");
				texto.Append(cuales[i]);
			}
			return texto.ToString();
		}
	}
}
