using System;
using System.Globalization;

namespace Concesionario.Dominio
{
	/// <summary>
	/// Una estadia del vehiculo en el taller: donde estuvo, por que entro, cuando entro y -- cuando
	/// ya lo entregaron -- cuando salio y que dias de calendario cubrio. Es un INTERVALO y no un
	/// evento: la completacion de una operacion tiene un instante y vive en la agenda, no aqui.
	/// <para>
	/// Su estado ES su tipo: abierta y cerrada son dos tipos, y cerrar devuelve otro objeto. Queda
	/// abierta a herencia porque esa es la variacion que carga.
	/// </para>
	/// </summary>
	internal abstract class AStay
	{
		private readonly string _branch;
		private readonly StaySubject _subject;
		private readonly DateTime _enteredAt;

		/// <summary>
		/// Lo que toda estadia tiene desde que empieza. Una sucursal en blanco o un asunto ausente
		/// son defectos de quien llama: sin ellos la estadia no dice donde estuvo ni por que entro.
		/// </summary>
		internal AStay(string branch, StaySubject subject, DateTime enteredAt)
		{
			if (string.IsNullOrWhiteSpace(branch))
				throw new DomainException(
					"Una estadia sin sucursal no dice donde estuvo el vehiculo, y donde estuvo es "
					+ "la mitad de lo que una estadia registra.");

			if (subject == null)
				throw new DomainException(
					"Una estadia sin asunto no dice por que entro el vehiculo.");

			_branch = branch;
			_subject = subject;
			_enteredAt = enteredAt;
		}

		/// <summary>La sucursal donde estuvo, con las palabras que llegaron. Texto sin catalogo.</summary>
		internal string Branch
		{
			get { return _branch; }
		}

		/// <summary>El asunto con que entro.</summary>
		internal StaySubject Subject
		{
			get { return _subject; }
		}

		/// <summary>Cuando entro. El instante recibido.</summary>
		internal DateTime EnteredAt
		{
			get { return _enteredAt; }
		}

		/// <summary>
		/// Si todavia no la han cerrado. TOTAL, y es la pregunta que hay que hacer antes de pedir la
		/// salida o los dias.
		/// </summary>
		internal abstract bool IsOpen();

		/// <summary>Si esta estadia cubrio ese dia de calendario. TOTAL.</summary>
		internal abstract bool Covers(StayDay day);

		/// <summary>
		/// Si alguno de los dias que cubrio cae entre esos dos, ambos inclusive. TOTAL.
		/// </summary>
		internal abstract bool OverlapsDays(StayDay from, StayDay to);

		/// <summary>Si el vehiculo estaba adentro en ese instante. TOTAL.</summary>
		internal abstract bool IsInsideAt(DateTime asOf);

		/// <summary>
		/// En cual de las situaciones esta, por su codigo. TOTAL, y es lo que deja que quien redacta
		/// elija el marco sin preguntar por el tipo.
		/// </summary>
		internal abstract string TheShapeItHas();

		/// <summary>
		/// La estadia como forma y valores. TOTAL: la que sigue abierta tambien contesta, con su
		/// propio codigo y sin inventarse una salida ni ningun dia.
		/// </summary>
		internal abstract AStayClause TheClause();

		/// <summary>
		/// Cuando salio. Accesor GUARDADO: una estadia abierta no tiene salida, y una fecha
		/// centinela en su lugar seria una salida que nadie registro.
		/// </summary>
		internal virtual DateTime CollectedAt
		{
			get
			{
				throw new DomainException(
					"Esta estadia sigue abierta, asi que no tiene instante de salida: hay que "
					+ "preguntar primero si sigue abierta.");
			}
		}

		/// <summary>
		/// Los dias de calendario que cubrio, en el orden en que llegaron. Accesor GUARDADO: los
		/// dias llegan con la salida, asi que una estadia abierta no tiene ninguno todavia -- que no
		/// es lo mismo que no haber estado adentro.
		/// </summary>
		internal virtual StayDay[] TheDaysItCovered()
		{
			throw new DomainException(
				"Esta estadia sigue abierta y los dias llegan con la salida, asi que todavia no hay "
				+ "dias que decir: hay que preguntar primero si sigue abierta.");
		}

		/// <summary>
		/// Un instante dicho como instante, con su hora. Nunca se dice como un dia de calendario a
		/// secas: eso seria un dia sacado de un instante, y sacarlo pide una zona que este registro
		/// no tiene.
		/// </summary>
		protected static string TheInstant(DateTime instant)
		{
			return instant.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
		}
	}
}
