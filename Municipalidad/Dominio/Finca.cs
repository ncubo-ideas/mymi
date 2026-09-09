using System;
using System.Collections.Generic;

namespace Municipalidad.Dominio
{
	/// <summary>
	/// La finca. Existe en el modelo aunque ningun papel la nombre: el portal consolida por cedula, pero
	/// lo que se cobra sobre la base imponible es de la finca y no del contribuyente.
	/// <para>
	/// SU VALOR NO LO TRAE NINGUN ARTEFACTO. Entra por comando, cuando la persona lo declara, y cuando
	/// no esta se dice en vez de inventarse: TieneValorAl contesta si hay uno y ValorVigenteAl esta
	/// guardado. Si lo fijo un avaluo o una declaracion, el v1 no lo distingue.
	/// </para>
	/// </summary>
	internal sealed class Finca
	{
		private readonly List<ValorDeFinca> valores = new List<ValorDeFinca>();

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

		internal void DeclararValor(ValorDeFinca valor)
		{
			ArgumentNullException.ThrowIfNull(valor);
			if (!PuedeDeclararValor(valor.Fecha))
				throw new DomainException(
					"Ya hay un valor declarado con fecha posterior. Preguntar PuedeDeclararValor antes.");
			valores.Add(valor);
		}

		/// <summary>
		/// PREDICADO TOTAL para el Check: los valores entran en orden de fecha, y UNA DECLARACION POR
		/// DIA. La misma fecha otra vez es el mismo hecho otra vez, asi que se rechaza.
		/// </summary>
		internal bool PuedeDeclararValor(DateTime fecha)
		{
			if (valores.Count == 0) return true;
			return fecha.Date > valores[valores.Count - 1].Fecha;
		}

		internal int CuantosValores { get { return valores.Count; } }

		/// <summary>PREDICADO TOTAL: habia un valor vigente a esa fecha?</summary>
		internal bool TieneValorAl(DateTime fecha)
		{
			foreach (ValorDeFinca v in valores)
				if (v.Fecha <= fecha.Date) return true;
			return false;
		}

		/// <summary>
		/// El valor vigente a esa fecha. GUARDADO: sin valor declarado no se inventa uno, ni se contesta
		/// un cero que se leeria como una finca que no vale nada. Preguntar TieneValorAl antes.
		/// </summary>
		internal decimal ValorVigenteAl(DateTime fecha)
		{
			ValorDeFinca vigente = null;
			foreach (ValorDeFinca v in valores)
				if (v.Fecha <= fecha.Date) vigente = v;
			if (vigente == null)
				throw new DomainException(
					"Esta finca no tiene valor declarado a esa fecha: ningun artefacto lo trae, y no se inventa uno. "
					+ "Preguntar TieneValorAl antes.");
			return vigente.Monto;
		}
	}
}
