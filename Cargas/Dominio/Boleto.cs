using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// UN BOLETO DE TOMBOLA. Entidad de identidad: lo que lo distingue es su numero, que
	/// lo imprime la estacion. La app no lo calcula ni lo inventa — solo lo registra.
	///
	/// El boleto NO sabe si esta vencido: eso lo juzga su sorteo, y depende del dia. El
	/// dominio no lee el reloj.
	/// </summary>
	internal sealed class Boleto
	{
		internal NumeroDeBoleto Numero { get; private set; }
		internal CodigoDeSorteo CodigoDelSorteo { get; private set; }
		internal DateTime Fecha { get; private set; }
		internal NumeroDeTransaccion Transaccion { get; private set; }

		// Primitivos del wire, y la escala adentro: es la forma en que el DSL journaliza.
		internal Boleto(string numeroDeBoleto, string codigoDelSorteo, DateTime fecha,
			string numeroDeTransaccion)
		{
			if (fecha == default)
				throw new ErrorDeDominio("Un boleto sin fecha no se puede ordenar.");

			Numero = new NumeroDeBoleto(numeroDeBoleto);
			CodigoDelSorteo = new CodigoDeSorteo(codigoDelSorteo);
			Fecha = fecha;
			Transaccion = new NumeroDeTransaccion(numeroDeTransaccion);
		}

		internal bool EsDeLaTransaccion(string numeroDeTransaccion) =>
			string.Equals(Transaccion.Texto, numeroDeTransaccion?.Trim(),
				StringComparison.OrdinalIgnoreCase);

		// ------------------------------- proyecciones planas, para el print del actor

		internal string TextoDelNumero => Numero.Texto;
		internal string TextoDelSorteo => CodigoDelSorteo.Texto;
		internal string NumeroDeLaTransaccion => Transaccion.Texto;
	}
}
