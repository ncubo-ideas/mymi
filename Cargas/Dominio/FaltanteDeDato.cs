using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// UN dato que falta, con lo que desbloquea. Es la consulta que motiva el ingreso:
	/// no dice "completa tu perfil", dice que carga esta trunca, que dato le falta y que
	/// calculo aparece si Sonia lo pone. La mitad ya esta hecha -los kilometros salen de
	/// los odometros que SI puso- y ver el trabajo a medio hacer motiva mas que un
	/// formulario en blanco.
	/// <para>
	/// TODO LO QUE SALE DE AQUI SON CLAVES Y VALORES, nunca una frase: la redaccion es del
	/// idioma en que se pregunte, y este tipo no tiene uno.
	/// </para>
	/// </summary>
	internal sealed class FaltanteDeDato
	{
		/// <summary>El kilometraje: el dato que solo Sonia tiene.</summary>
		internal const string ElKilometraje = "kilometraje";

		/// <summary>El visto bueno a lo que la forma de corte infirio.</summary>
		internal const string LaConfirmacionDelTanque = "confirmacion-del-tanque";

		/// <summary>Con dos anclas alrededor, lo que se desbloquea es un tramo con nombre.</summary>
		internal const string ElRendimientoDelTramo = "rendimiento-del-tramo";

		/// <summary>Sin anclas alrededor, solo se puede nombrar el tramo que quedo abierto.</summary>
		internal const string ElTramoQueQuedoAbierto = "tramo-abierto";

		/// <summary>Lo que desbloquea confirmar un tanque: el tramo hasta la carga siguiente.</summary>
		internal const string ElRendimientoHastaLaSiguiente = "rendimiento-hasta-la-siguiente";

		internal DateTime FechaDeLaCarga { get; private set; }
		internal string NumeroDeLaTransaccion { get; private set; }

		/// <summary>EL CODIGO de que falta, de un conjunto cerrado de dos.</summary>
		internal string ClaveDeLoQueFalta { get; private set; }

		/// <summary>EL CODIGO de lo que aparece si lo pone.</summary>
		internal string ClaveDeLoQueDesbloquea { get; private set; }

		/// <summary>Los kilometros que YA se saben entre las dos anclas, cero cuando no hay dos.
		/// Salen de los odometros que Sonia si puso: el trabajo a medio hacer motiva mas que un
		/// formulario en blanco.</summary>
		internal decimal KilometrosDelTramo { get; private set; }

		/// <summary>Las fechas de las dos anclas, por defecto cuando no hay dos.</summary>
		internal DateTime DesdeLaCarga { get; private set; }
		internal DateTime HastaLaCarga { get; private set; }

		/// <summary>EL CODIGO DEL ESTADO QUE LA APP SUPONE y esta pidiendo confirmar: lleno o
		/// parcial. Vacio cuando el faltante no nace de una inferencia -- un kilometraje no se
		/// infiere de nada, asi que no hay nada que confirmar, hay que ponerlo.</summary>
		internal string LoQueSeSupone { get; private set; }

		/// <summary>EL CODIGO DE COMO SE DETUVO EL SURTIDOR, que es de donde sale la suposicion.</summary>
		internal string ClaveDelCorte { get; private set; }

		internal bool NaceDeUnaSuposicion => LoQueSeSupone.Length > 0;

		internal FaltanteDeDato(DateTime fechaDeLaCarga, string numeroDeLaTransaccion,
			string claveDeLoQueFalta, string claveDeLoQueDesbloquea,
			decimal kilometrosDelTramo = 0m, DateTime desdeLaCarga = default,
			DateTime hastaLaCarga = default,
			string loQueSeSupone = "", string claveDelCorte = "")
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(numeroDeLaTransaccion);
			ArgumentNullException.ThrowIfNullOrWhiteSpace(claveDeLoQueFalta);

			FechaDeLaCarga = fechaDeLaCarga;
			NumeroDeLaTransaccion = numeroDeLaTransaccion;
			ClaveDeLoQueFalta = claveDeLoQueFalta;
			ClaveDeLoQueDesbloquea = claveDeLoQueDesbloquea ?? "";
			KilometrosDelTramo = kilometrosDelTramo;
			DesdeLaCarga = desdeLaCarga;
			HastaLaCarga = hastaLaCarga;
			LoQueSeSupone = loQueSeSupone ?? "";
			ClaveDelCorte = claveDelCorte ?? "";
		}
	}
}
