using System;

namespace Examenes.Dominio
{
	// EL SEXO DEL PACIENTE, como PARAMETRO CLINICO. Es lo que determina que se mide y
	// contra que se compara, no una declaracion de identidad, y conviene que el copy de la
	// superficie lo refleje: "para comparar tus resultados con los rangos correctos".
	//
	// Alvaro, textual: "el dominio de Renan DEBE depender ahi de la variable sexo. Que
	// inicialmente puede ser hombre / mujer / o desconocido."
	//
	// TRES VALORES, y el tercero no es un hueco. `Desconocido` NO es un default silencioso
	// que se resuelve al masculino "mientras tanto": es un estado con consecuencias
	// visibles —hay analitos que no se pueden interpretar y el dominio lo dice— y por eso
	// esta en el conjunto y no representado por un null.
	//
	// ES UNA CLASE Y NO UN ENUM, igual que Severidad, Veredicto, Plazo y todos los demas
	// conjuntos cerrados de este dominio: se compara por referencia, lleva su propio
	// vocabulario -Nombre para serializar, ParaMostrar para la persona- y resuelve un
	// texto de afuera en la frontera con una funcion total-con-excepcion. La spec lo
	// escribio como enum; acá el idioma de la casa es este.
	internal sealed class Sexo
	{
		internal static readonly Sexo Hombre = new Sexo("hombre");

		internal static readonly Sexo Mujer = new Sexo("mujer");

		// Todavia nadie lo dijo: ni el laboratorio lo publico ni la persona lo declaro.
		internal static readonly Sexo Desconocido = new Sexo("desconocido");

		private static readonly Sexo[] todos = { Hombre, Mujer, Desconocido };

		// La etiqueta interna, sin acentos: con este texto se compara y se serializa —es
		// el que viaja en el comando del journal— y no se le habla a nadie.
		internal string Nombre { get; private set; }



		private Sexo(string nombre)
		{
			Nombre = nombre;
		}

		// Si hay dato. Es la pregunta que gobierna todo lo demas: sin esto el dominio no
		// puede decidir aplicabilidad ni rango, y lo DICE en vez de suponer.
		internal bool EsConocido => !ReferenceEquals(this, Desconocido);

		// Operacion con nombre en vez de sobrecargar ==, igual que Analito y UnidadDeMedida.
		internal bool EsElMismo(Sexo otro)
		{
			ArgumentNullException.ThrowIfNull(otro);
			return ReferenceEquals(this, otro);
		}

		// Funcion total con excepcion, igual que Analito.PorNombre: el texto llega de
		// afuera -de un evento del laboratorio, de un boton de la pantalla- y solo hay tres
		// valores validos. Que no resuelva ninguno es un defecto de quien lo manda, no un
		// sexo nuevo que este dominio tenga que inventar.
		//
		// Un texto VACIO si resuelve, y a Desconocido: es lo que manda un laboratorio que
		// no publica el dato, y eso no es un error de nadie.
		internal static Sexo PorNombre(string nombre)
		{
			Sexo hallado = Resolver(nombre);
			if (hallado != null) return hallado;

			throw new ArgumentException(
				$"'{nombre}' no es un sexo que este dominio conozca. Los que conoce son: "
				+ "hombre, mujer, desconocido.", nameof(nombre));
		}

		// Para quien tiene que DECIDIR en vez de fallar: la ruta de eventos descarta lo que
		// no entiende, y descartar no es un error — un laboratorio puede publicar en el
		// campo `sexo` algo que este dominio no modela.
		internal static bool EsConocidoElNombre(string nombre) => Resolver(nombre) != null;

		// Null cuando no resuelve. Es el unico lugar donde se decide que textos entran, y
		// las dos funciones de la frontera —la que revienta y la que descarta— salen de
		// aca para que no puedan discrepar.
		private static Sexo Resolver(string nombre)
		{
			// Un texto VACIO si resuelve, y a Desconocido: es lo que manda un laboratorio
			// que no publica el dato, y eso no es un error de nadie.
			if (string.IsNullOrWhiteSpace(nombre)) return Desconocido;

			string llave = NombreNormalizado.De(nombre);
			foreach (Sexo sexo in todos)
				if (NombreNormalizado.De(sexo.Nombre) == llave) return sexo;

			// Las abreviaturas con las que llega de un sistema de laboratorio real. No son
			// sinonimos libres: son las letras que trae un HL7 o un CSV de expediente.
			if (llave == "M" || llave == "MASCULINO" || llave == "VARON") return Hombre;
			if (llave == "F" || llave == "FEMENINO") return Mujer;

			return null;
		}

		public override string ToString() => Nombre;
	}
}
