using System;

namespace Examenes.Dominio
{
	// El rango en el que un analito se considera normal, en una unidad concreta.
	//
	// Los rangos VIVEN EN EL CODIGO del dominio, no en los datos del evento. Es una
	// decision, no una comodidad: un rango de referencia es conocimiento clinico del
	// negocio, y si viniera en el payload el proveedor podria cambiar la definicion de
	// "normal" sin que la app se enterara. Que este aca es lo que permite que la app
	// interprete, no solo muestre.
	//
	// Los rangos por EDAD los arma cada tipo de examen (ver RangoPara en cada
	// subclase): esta clase solo sabe contener y clasificar.
	internal class RangoDeReferencia
	{
		internal Medicion Minimo { get; private set; }
		internal Medicion Maximo { get; private set; }

		internal RangoDeReferencia(Medicion minimo, Medicion maximo)
		{
			ArgumentNullException.ThrowIfNull(minimo);
			ArgumentNullException.ThrowIfNull(maximo);
			if (!maximo.MismaUnidadQue(minimo))
				throw new ArgumentException(
					$"Un rango no puede mezclar unidades: {minimo.Expresado} .. {maximo.Expresado}.", nameof(maximo));
			if (maximo.Valor < minimo.Valor)
				throw new ArgumentException(
					$"El maximo del rango ({maximo.Expresado}) no puede ser menor que el minimo ({minimo.Expresado}).",
					nameof(maximo));

			Minimo = minimo;
			Maximo = maximo;
		}

		// Atajo para el caso normal: mismo simbolo de unidad para los dos extremos.
		internal static RangoDeReferencia Entre(decimal minimo, decimal maximo, UnidadDeMedida unidad) =>
			new RangoDeReferencia(new Medicion(minimo, unidad), new Medicion(maximo, unidad));

		// Un rango que solo tiene techo (el caso del antigeno prostatico y del
		// colesterol: no hay un "demasiado bajo" que preocupe en el demo).
		internal static RangoDeReferencia HastaUnMaximoDe(decimal maximo, UnidadDeMedida unidad) =>
			new RangoDeReferencia(new Medicion(0m, unidad), new Medicion(maximo, unidad));

		internal string Expresado => $"{Minimo.Valor} a {Maximo.Valor} {Maximo.Unidad.Simbolo}";

		internal bool Contiene(Medicion medicion)
		{
			ArgumentNullException.ThrowIfNull(medicion);
			if (!medicion.MismaUnidadQue(Maximo))
				throw new InvalidOperationException(
					$"No se puede evaluar {medicion.Expresado} contra un rango en {Maximo.Unidad.Simbolo}.");

			return medicion.Valor >= Minimo.Valor && medicion.Valor <= Maximo.Valor;
		}

		internal ClasificacionDeValor Clasificar(Medicion medicion)
		{
			ArgumentNullException.ThrowIfNull(medicion);
			if (Contiene(medicion)) return ClasificacionDeValor.Normal;

			return medicion.Valor > Maximo.Valor
				? ClasificacionDeValor.Alto
				: ClasificacionDeValor.Bajo;
		}
	}
}
