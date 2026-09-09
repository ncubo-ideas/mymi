using System;

namespace Examenes.Dominio
{
	// DE QUIEN ES EL EXPEDIENTE, en lo que este dominio necesita saber de esa persona para
	// poder interpretar: su EDAD y su SEXO.
	//
	// POR QUE APARECE AHORA. La edad ya viajaba por todo el dominio como un `int` suelto:
	// RangoPara(edad), Interpretar(edad), MesesHastaElProximo(edad). Mientras fuera uno
	// solo eso alcanzaba; en cuanto el sexo entra como segundo parametro clinico, sumar un
	// segundo argumento a doce firmas seria empezar a arrastrar una tupla sin nombre. Y
	// hay un tercero ya anotado y no construido —la altitud de residencia, que corre el
	// rango de la hemoglobina— que entraria por el mismo lugar.
	//
	// Lo que hace este tipo, entonces, es que un analito reciba A LA PERSONA y le pregunte
	// lo que necesita, en vez de recibir la lista de sus atributos.
	//
	// LA ANATOMIA SE DERIVA ACA Y EN NINGUN OTRO LADO. Es la unica linea del dominio que
	// traduce sexo -> anatomia, y esta puesta asi a proposito: el dia que haya un dato
	// mejor —una prostatectomia declarada, una terapia hormonal— cambia aca y ningun
	// analito se entera, porque ninguno pregunta por el sexo.
	//
	// Carrier INMUTABLE. El expediente NO lo guarda: lo arma cada vez que alguien se lo
	// pide, desde las dos afirmaciones sobre el sexo que si guarda. Asi no puede quedar
	// desincronizado de ellas, y ninguna consulta a medio hacer ve una persona que cambio
	// debajo.
	internal sealed class Paciente
	{
		internal int Edad { get; private set; }

		internal Sexo Sexo { get; private set; }

		internal Paciente(int edad, Sexo sexo)
		{
			ArgumentNullException.ThrowIfNull(sexo);
			if (edad <= 0 || edad > 120)
				throw new ArgumentOutOfRangeException(nameof(edad), edad,
					"La edad del paciente tiene que estar entre 1 y 120.");

			Edad = edad;
			Sexo = sexo;
		}

		// El paciente del que todavia no se sabe el sexo. Es el estado en el que arranca
		// un expediente: el laboratorio no lo publico y la persona no lo declaro.
		internal static Paciente DeEdad(int edad) => new Paciente(edad, Sexo.Desconocido);

		// SI TIENE PROSTATA. Tres estados y no un bool, porque el dato del que se deriva
		// tiene tres: con el sexo desconocido esto no es falso, es indeterminado — y
		// devolver false "mientras tanto" haria desaparecer una tarjeta que quizas
		// corresponde, en silencio.
		internal Aplicabilidad TieneProstata
		{
			get
			{
				if (Sexo.EsElMismo(Examenes.Dominio.Sexo.Hombre)) return Aplicabilidad.Aplica;
				if (Sexo.EsElMismo(Examenes.Dominio.Sexo.Mujer)) return Aplicabilidad.NoAplica;
				return Aplicabilidad.Indeterminada;
			}
		}

		// Si de esta persona se sabe lo que hace falta para comparar contra un rango que
		// depende del sexo. Lo preguntan los analitos que tienen ese rango —la hemoglobina,
		// el HDL— y NO los que no lo tienen, que siguen interpretandose igual que siempre.
		internal bool SeSabeElSexo => Sexo.EsConocido;

		public override string ToString() => $"{Edad} años, {Sexo.ParaMostrar}";
	}
}
