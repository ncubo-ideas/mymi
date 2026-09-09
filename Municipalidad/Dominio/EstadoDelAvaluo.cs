using System;
using System.Collections.Generic;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// Con cual avaluo cobro la municipalidad cada uno de los dos periodos de un salto, y cuando no se
	/// puede decir, POR QUE. Cuatro hojas y no una con un motivo adentro: cada razon es su propio tipo.
	/// <para>
	/// La pregunta total de la base es si SE PUEDE DECIR y no si aplica: que el avaluo explique el salto
	/// lo decide la ley del rubro antes de llegar aca, asi que toda ausencia de este juego es
	/// CONTINGENTE — deja de serlo en cuanto llegue lo que falta.
	/// </para>
	/// <para>
	/// NINGUNA HOJA CONTESTA UN CERO donde no hay avaluo: un cero se leeria como una finca que no vale
	/// nada, que es afirmar como hecho algo que nadie publico. El monto se pide con guarda.
	/// </para>
	/// </summary>
	internal abstract class EstadoDelAvaluo
	{
		internal Periodo PeriodoAnterior { get; }
		internal Periodo PeriodoActual { get; }

		protected EstadoDelAvaluo(Periodo periodoAnterior, Periodo periodoActual)
		{
			ArgumentNullException.ThrowIfNull(periodoAnterior);
			ArgumentNullException.ThrowIfNull(periodoActual);
			PeriodoAnterior = periodoAnterior;
			PeriodoActual = periodoActual;
		}

		/// <summary>El periodo anterior, aplanado para que una frase lo alcance en un solo nivel.</summary>
		internal string EtiquetaDelPeriodoAnterior { get { return PeriodoAnterior.Etiqueta; } }

		/// <summary>El periodo actual, aplanado por la misma razon.</summary>
		internal string EtiquetaDelPeriodoActual { get { return PeriodoActual.Etiqueta; } }

		/// <summary>
		/// PREDICADO TOTAL: puede esta app decir con cual avaluo se cobro cada uno de los dos periodos?
		/// </summary>
		internal abstract bool SePuedeDecir { get; }

		/// <summary>El nombre de este estado, que es como cruza a una caratula.</summary>
		internal abstract string Etiqueta { get; }

		/// <summary>
		/// La frase que una persona lee sobre el avaluo de este salto. TOTAL: las cuatro hojas dicen la
		/// suya, y las tres que no pueden decirlo dicen razones distintas — callarlas dejaria el salto
		/// pareciendo inexplicable cuando lo que falta tiene nombre y tiene quien lo mande.
		/// </summary>
		internal abstract string Dice();

		/// <summary>
		/// El avaluo con que se cobro el periodo anterior. GUARDADO: preguntar SePuedeDecir antes.
		/// </summary>
		internal virtual decimal AvaluoAnterior
		{
			get { throw SinAvaluoQueContestar(); }
		}

		/// <summary>
		/// El avaluo con que se cobro el periodo actual. GUARDADO: preguntar SePuedeDecir antes.
		/// </summary>
		internal virtual decimal AvaluoActual
		{
			get { throw SinAvaluoQueContestar(); }
		}

		private static DomainException SinAvaluoQueContestar()
		{
			return new DomainException(
				"Esta app no puede decir con cual avaluo se cobro ese periodo, asi que no hay monto que "
				+ "contestar y no se inventa uno. Preguntar SePuedeDecir antes.");
		}

		/// <summary>
		/// Resuelve el estado desde los dos periodos del salto y las fincas de la cuenta. TOTAL: toda
		/// combinacion da una de las cuatro hojas y ninguna revienta.
		/// </summary>
		internal static EstadoDelAvaluo De(
			Periodo periodoAnterior, Periodo periodoActual, IReadOnlyList<Finca> fincas)
		{
			ArgumentNullException.ThrowIfNull(periodoAnterior);
			ArgumentNullException.ThrowIfNull(periodoActual);
			ArgumentNullException.ThrowIfNull(fincas);

			if (fincas.Count == 0)
				return new LaCuentaNoTieneNingunaFinca(periodoAnterior, periodoActual);

			if (fincas.Count > 1)
				return new ElReciboNoDiceDeCualFinca(periodoAnterior, periodoActual, fincas.Count);

			Finca laFinca = fincas[0];
			if (!laFinca.TieneAvaluoAl(periodoAnterior.UltimoDia)
				|| !laFinca.TieneAvaluoAl(periodoActual.UltimoDia))
				return new LaMunicipalidadNoPublicoElAvaluo(
					periodoAnterior, periodoActual, laFinca.Numero);

			return new ElAvaluoExplicaElSalto(
				periodoAnterior, periodoActual, laFinca.Numero,
				laFinca.AvaluoVigenteAl(periodoAnterior.UltimoDia),
				laFinca.AvaluoVigenteAl(periodoActual.UltimoDia));
		}
	}

	/// <summary>
	/// La cuenta no tiene ninguna finca incorporada, asi que no hay sobre cual leer un avaluo. Es
	/// contingente: la municipalidad manda la finca cuando la incorpora a la cuenta.
	/// </summary>
	internal sealed class LaCuentaNoTieneNingunaFinca : EstadoDelAvaluo
	{
		internal LaCuentaNoTieneNingunaFinca(Periodo periodoAnterior, Periodo periodoActual)
			: base(periodoAnterior, periodoActual) { }

		internal override bool SePuedeDecir { get { return false; } }
		internal override string Etiqueta { get { return "sin-finca"; } }

		internal override string Dice()
		{
			return "Esta cuenta no tiene ninguna finca incorporada, así que no hay sobre cuál leer el "
				+ "avalúo: la municipalidad manda la finca cuando la incorpora a tu cuenta, y su avalúo "
				+ "cuando lo fija. Esta app dice el salto y no de cuánto a cuánto pasó el avalúo.";
		}
	}

	/// <summary>
	/// La cuenta tiene mas de una finca y la linea del recibo no dice de cual es: el portal consolida
	/// por cedula. Elegir una seria adivinar cual, asi que no se elige.
	/// </summary>
	internal sealed class ElReciboNoDiceDeCualFinca : EstadoDelAvaluo
	{
		/// <summary>Cuantas fincas tiene la cuenta, que es lo que la frase cuenta.</summary>
		internal int CuantasFincas { get; }

		internal ElReciboNoDiceDeCualFinca(
			Periodo periodoAnterior, Periodo periodoActual, int cuantasFincas)
			: base(periodoAnterior, periodoActual)
		{
			CuantasFincas = cuantasFincas;
		}

		internal override bool SePuedeDecir { get { return false; } }
		internal override string Etiqueta { get { return "varias-fincas"; } }

		internal override string Dice()
		{
			return "Esta cuenta tiene " + CuantasFincas + " fincas y la línea del recibo no dice de cuál "
				+ "de ellas es, porque el portal consolida por cédula. Esta app no elige una por vos: "
				+ "dice el salto y no de cuánto a cuánto pasó el avalúo.";
		}
	}

	/// <summary>
	/// Hay una sola finca, pero la municipalidad no publico un avaluo suyo que cubra los dos periodos
	/// del salto. Es contingente: deja de serlo en cuanto publique uno.
	/// </summary>
	internal sealed class LaMunicipalidadNoPublicoElAvaluo : EstadoDelAvaluo
	{
		/// <summary>El numero de la finca de la que se habla, que es el SUJETO de la frase.</summary>
		internal string NumeroDeFinca { get; }

		internal LaMunicipalidadNoPublicoElAvaluo(
			Periodo periodoAnterior, Periodo periodoActual, string numeroDeFinca)
			: base(periodoAnterior, periodoActual)
		{
			NumeroDeFinca = numeroDeFinca;
		}

		internal override bool SePuedeDecir { get { return false; } }
		internal override string Etiqueta { get { return "sin-avaluo-publicado"; } }

		internal override string Dice()
		{
			return "La municipalidad no publicó un avalúo de la finca " + NumeroDeFinca + " que cubra "
				+ EtiquetaDelPeriodoAnterior + " y " + EtiquetaDelPeriodoActual + ", así que esta app "
				+ "dice el salto y no de cuánto a cuánto pasó el avalúo. Lo que vos hayás declarado es "
				+ "tuyo y no es el valor con que te cobraron.";
		}
	}

	/// <summary>
	/// La municipalidad publico el avaluo vigente en los dos periodos, asi que el salto se dice de
	/// cuanto a cuanto. Es la unica hoja que contesta un monto.
	/// </summary>
	internal sealed class ElAvaluoExplicaElSalto : EstadoDelAvaluo
	{
		/// <summary>El numero de la finca de la que se habla, que es el SUJETO de la frase.</summary>
		internal string NumeroDeFinca { get; }

		private readonly decimal avaluoAnterior;
		private readonly decimal avaluoActual;

		internal ElAvaluoExplicaElSalto(
			Periodo periodoAnterior, Periodo periodoActual, string numeroDeFinca,
			decimal avaluoAnterior, decimal avaluoActual)
			: base(periodoAnterior, periodoActual)
		{
			NumeroDeFinca = numeroDeFinca;
			this.avaluoAnterior = avaluoAnterior;
			this.avaluoActual = avaluoActual;
		}

		internal override bool SePuedeDecir { get { return true; } }
		internal override string Etiqueta { get { return "con-avaluo-publicado"; } }

		internal override decimal AvaluoAnterior { get { return avaluoAnterior; } }
		internal override decimal AvaluoActual { get { return avaluoActual; } }

		internal override string Dice()
		{
			return "El avalúo con que la municipalidad cobró la finca " + NumeroDeFinca + " pasó de "
				+ Dichos.Monto(avaluoAnterior) + " en " + EtiquetaDelPeriodoAnterior + " a "
				+ Dichos.Monto(avaluoActual) + " en " + EtiquetaDelPeriodoActual + ".";
		}
	}
}
