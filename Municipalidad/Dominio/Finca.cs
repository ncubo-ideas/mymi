using System;
using System.Collections.Generic;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// La finca. Existe en el modelo aunque ningun papel la nombre: el portal consolida por cedula, pero
	/// lo que se cobra sobre la base imponible es de la finca y no del contribuyente.
	/// <para>
	/// SU VALOR TIENE DOS ORIGENES Y DOS LINEAS DE TIEMPO SEPARADAS. El AVALUO lo fija la municipalidad
	/// y es el valor con que cobra; la DECLARACION la hace la persona y es suya. Estan aparte porque
	/// contestan preguntas distintas: un salto en un rubro sobre base imponible solo lo explica el
	/// avaluo, y una declaracion no puede hablar por lo que cobro la institucion.
	/// </para>
	/// <para>
	/// Cada linea acepta UN hecho por dia y en orden, y cuando no hay uno vigente SE DICE en vez de
	/// inventarse: TieneAvaluoAl y TieneValorDeclaradoAl contestan si hay, y las dos lecturas del monto
	/// estan guardadas.
	/// </para>
	/// </summary>
	internal sealed class Finca
	{
		private readonly List<ValorDeFinca> avaluos = new List<ValorDeFinca>();
		private readonly List<ValorDeFinca> declaraciones = new List<ValorDeFinca>();

		internal string Numero { get; }

		internal Finca(string numero)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(numero);
			Numero = numero.Trim();
		}

		internal bool EsLaMismaQue(string numero)
		{
			return !string.IsNullOrWhiteSpace(numero) && Numero == numero.Trim();
		}

		/// <summary>
		/// La municipalidad fija el avaluo de esta finca. Es el valor con que cobra los rubros sobre base
		/// imponible, y por eso es el unico que puede explicar un salto en ellos.
		/// </summary>
		internal void FijarAvaluo(ValorDeFinca avaluo)
		{
			ArgumentNullException.ThrowIfNull(avaluo);
			if (!PuedeFijarAvaluo(avaluo.Fecha))
				throw new DomainException(
					"Ya hay un avaluo con fecha igual o posterior. Preguntar PuedeFijarAvaluo antes.");
			avaluos.Add(avaluo);
		}

		/// <summary>
		/// PREDICADO TOTAL para el Check: los avaluos entran en orden de fecha, y UN AVALUO POR DIA. La
		/// misma fecha otra vez es el mismo hecho otra vez, asi que se rechaza.
		/// </summary>
		internal bool PuedeFijarAvaluo(DateTime fecha)
		{
			return EsPosteriorAlUltimoDe(avaluos, fecha);
		}

		internal int CuantosAvaluos { get { return avaluos.Count; } }

		/// <summary>PREDICADO TOTAL: habia un avaluo vigente a esa fecha?</summary>
		internal bool TieneAvaluoAl(DateTime fecha)
		{
			return HayVigenteEn(avaluos, fecha);
		}

		/// <summary>
		/// El avaluo vigente a esa fecha. GUARDADO: sin avaluo publicado no se inventa uno, ni se
		/// contesta un cero que se leeria como una finca que no vale nada. Preguntar TieneAvaluoAl antes.
		/// </summary>
		internal decimal AvaluoVigenteAl(DateTime fecha)
		{
			ValorDeFinca vigente = VigenteEn(avaluos, fecha);
			if (vigente == null)
				throw new DomainException(
					"Esta finca no tiene avaluo publicado a esa fecha, y no se inventa uno. "
					+ "Preguntar TieneAvaluoAl antes.");
			return vigente.Monto;
		}

		/// <summary>
		/// La persona declara el valor de su finca. Es un aporte suyo: queda en su carpeta y su asistente
		/// lo lee, y no es el valor con que la municipalidad cobro.
		/// </summary>
		internal void DeclararValor(ValorDeFinca valor)
		{
			ArgumentNullException.ThrowIfNull(valor);
			if (!PuedeDeclararValor(valor.Fecha))
				throw new DomainException(
					"Ya hay un valor declarado con fecha posterior. Preguntar PuedeDeclararValor antes.");
			declaraciones.Add(valor);
		}

		/// <summary>
		/// PREDICADO TOTAL para el Check: los valores entran en orden de fecha, y UNA DECLARACION POR
		/// DIA. La misma fecha otra vez es el mismo hecho otra vez, asi que se rechaza.
		/// </summary>
		internal bool PuedeDeclararValor(DateTime fecha)
		{
			return EsPosteriorAlUltimoDe(declaraciones, fecha);
		}

		internal int CuantosValoresDeclarados { get { return declaraciones.Count; } }

		/// <summary>PREDICADO TOTAL: habia un valor declarado vigente a esa fecha?</summary>
		internal bool TieneValorDeclaradoAl(DateTime fecha)
		{
			return HayVigenteEn(declaraciones, fecha);
		}

		/// <summary>
		/// El valor declarado vigente a esa fecha. GUARDADO: sin declaracion no se inventa una.
		/// Preguntar TieneValorDeclaradoAl antes.
		/// </summary>
		internal decimal ValorDeclaradoVigenteAl(DateTime fecha)
		{
			ValorDeFinca vigente = VigenteEn(declaraciones, fecha);
			if (vigente == null)
				throw new DomainException(
					"Esta finca no tiene valor declarado a esa fecha, y no se inventa uno. "
					+ "Preguntar TieneValorDeclaradoAl antes.");
			return vigente.Monto;
		}

		/// <summary>
		/// EL UNICO RESOLUTOR de las dos lineas: el ultimo hecho con fecha menor o igual a la pedida, o
		/// nada. Las cuatro lecturas lo envuelven, para que ninguna resuelva la vigencia por su cuenta.
		/// </summary>
		private static ValorDeFinca VigenteEn(List<ValorDeFinca> serie, DateTime fecha)
		{
			ValorDeFinca vigente = null;
			foreach (ValorDeFinca cada in serie)
				if (cada.Fecha <= fecha.Date) vigente = cada;
			return vigente;
		}

		private static bool HayVigenteEn(List<ValorDeFinca> serie, DateTime fecha)
		{
			return VigenteEn(serie, fecha) != null;
		}

		private static bool EsPosteriorAlUltimoDe(List<ValorDeFinca> serie, DateTime fecha)
		{
			if (serie.Count == 0) return true;
			return fecha.Date > serie[serie.Count - 1].Fecha;
		}
	}
}
