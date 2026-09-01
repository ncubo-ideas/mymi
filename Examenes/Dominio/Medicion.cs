using System;

namespace Examenes.Dominio
{
	// Un valor medido CON su unidad. La razon de que esto sea una clase y no un decimal
	// suelto es la pregunta "200 que": el numero solo no es un dato clinico.
	//
	// Lo que compra la abstraccion es que la comparacion sea imposible de hacer mal.
	// Restar un colesterol en mg/dL de un antigeno en ng/mL es un error que con dos
	// decimales sueltos compila y da un numero; aca revienta, porque Diferencia exige
	// la misma unidad.
	//
	// Es una entidad por identidad, no un value object: no tiene Equals ni
	// GetHashCode, y no se usa como llave de ningun diccionario. Lo que expone son
	// operaciones con nombre.
	// Es la FORMA 1 de la taxonomia de Magnitud: continua CON unidad. Que herede de
	// MagnitudContinua y no de Magnitud a secas es lo que le da acceso a la serie y a la
	// velocidad, y lo que se lo niega al color de la orina y a los nitritos.
	internal class Medicion : MagnitudContinua
	{
		internal override decimal Valor { get; }
		internal UnidadDeMedida Unidad { get; private set; }

		internal Medicion(decimal valor, UnidadDeMedida unidad)
		{
			ArgumentNullException.ThrowIfNull(unidad);
			if (valor < 0)
				throw new ArgumentOutOfRangeException(nameof(valor), valor,
					"Un valor medido no puede ser negativo.");

			Valor = valor;
			Unidad = unidad;
		}

		// El texto que se muestra. No es un ToString de volcado de estado: es la forma
		// en que el dominio dice un valor medido, con unidad, que es la unica forma
		// completa de decirlo.
		internal override string Expresado => $"{Valor} {Unidad.Simbolo}";

		internal bool MismaUnidadQue(Medicion otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			return Unidad.EsLaMisma(otra.Unidad);
		}

		// La diferencia CON SIGNO respecto de una medicion anterior. Positiva si subio.
		//
		// Exigir la misma unidad es el punto de toda la clase. Si alguna vez el
		// proveedor cambia la unidad de un analito a mitad de la serie, esto lo
		// descubre en vez de reportar una tendencia falsa.
		internal decimal DiferenciaDesde(Medicion anterior)
		{
			ArgumentNullException.ThrowIfNull(anterior);
			if (!MismaUnidadQue(anterior))
				throw new InvalidOperationException(
					$"No se puede comparar {Expresado} con {anterior.Expresado}: son unidades distintas.");

			return Valor - anterior.Valor;
		}

		internal bool EsMayorQue(Medicion otra)
		{
			ArgumentNullException.ThrowIfNull(otra);
			if (!MismaUnidadQue(otra))
				throw new InvalidOperationException(
					$"No se puede ordenar {Expresado} contra {otra.Expresado}: son unidades distintas.");

			return Valor > otra.Valor;
		}
	}
}
