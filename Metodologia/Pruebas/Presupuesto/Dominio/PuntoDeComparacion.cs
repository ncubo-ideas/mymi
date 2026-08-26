using System;

namespace Presupuesto.Dominio
{
	// CONTRA QUE SE MIDE SI UN BIEN "VALE LA PENA". Juicio 7 de Preguntas.md, y es el mas
	// fino de los ocho.
	//
	// La respuesta de la entrevista fue que TODAS las opciones son validas y que dependen de
	// la persona: a uno le basta con que la propiedad se pague sola; otro la compara contra
	// un certificado de deposito o contra cripto, para decidir si le conviene vender e
	// invertir en otra cosa.
	//
	// POR ESO ESTA BASE ES ABIERTA Y NO UN CONJUNTO CERRADO. Es la excepcion deliberada a la
	// regla de que los conjuntos del dominio se cierran: aca cerrarlo seria decidir por la
	// persona cual es su alternativa, y eso es precisamente lo que el juicio dice que no se
	// hace.
	//
	// EL PISO SI ES FIJO. "Comparar contra periodos anteriores" siempre esta disponible,
	// porque no necesita que nadie declare nada. Es la unica implementacion que el dominio
	// puede garantizar.
	//
	// Vale registrar el error que este juicio evito: sin la entrevista, aca habria quedado
	// quemado "vale la pena si el neto es positivo". No habria reventado nunca; habria
	// mentido siempre.
	public abstract class PuntoDeComparacion
	{
		public abstract string Nombre { get; }

		// Cuanto habria rendido esa misma plata en la alternativa, durante el periodo.
		// Devuelve un RENDIMIENTO ANUAL en proporcion: 0,07 son siete por ciento.
		public abstract decimal RendimientoAnualEsperado { get; }

		// .Se puede usar sin que la persona declare nada?
		public abstract bool EstaSiempreDisponible { get; }
	}

	// EL PISO. No compara contra ninguna alternativa: compara el bien consigo mismo.
	// Siempre se puede, y por eso es el unico que el dominio ofrece sin preguntar.
	public sealed class ContraSuPropiaHistoria : PuntoDeComparacion
	{
		public override string Nombre => "sus propios periodos anteriores";

		// Cero no significa "no rinde": significa que la alternativa contra la que se compara
		// es no haber hecho nada distinto. La lectura la da el cambio contra el periodo
		// anterior, no este numero.
		public override decimal RendimientoAnualEsperado => 0m;

		public override bool EstaSiempreDisponible => true;
	}

	// LA ALTERNATIVA QUE LA PERSONA DECLARA. Un certificado de deposito, cripto, lo que sea.
	// El dominio no sabe cuanto rinde ninguna de esas cosas y no tiene por que saberlo: lo
	// RECIBE. Si lo calculara, estaria dando consejo de inversion, que no es lo suyo.
	public sealed class ContraUnaTasaAnual : PuntoDeComparacion
	{
		private readonly string nombre;
		private readonly decimal tasa;

		public ContraUnaTasaAnual(string nombre, decimal tasaAnual)
		{
			if (string.IsNullOrWhiteSpace(nombre))
				throw new ArgumentException("La alternativa necesita un nombre que la persona reconozca.", nameof(nombre));

			this.nombre = nombre;
			tasa = tasaAnual;
		}

		public override string Nombre => nombre;

		public override decimal RendimientoAnualEsperado => tasa;

		public override bool EstaSiempreDisponible => false;
	}
}
