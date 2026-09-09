using System;

namespace Examenes.Dominio
{
	/// <summary>
	/// Cuanto se movio un analito POR ANO, y sobre que tramo se midio.
	/// </summary>
	///
	/// Es la lectura que el antigeno prostatico exige y que ningun valor aislado da. La
	/// practica clinica no pregunta "cuanto da", pregunta "cuanto subio por ano": un PSA
	/// estable en 3 durante anos se lee distinto de uno que paso de 3 a 5 en el mismo
	/// lapso, aunque los dos terminen cerca del mismo techo.
	///
	/// SOBRE QUE TRAMO se mide es la decision del modelo, y es deliberada: el TRAMO MAS
	/// RECIENTE, no la serie entera. Con los datos del guion la diferencia decide el caso:
	///
	///   serie entera   3.0 (2023) -> 5.2 (2026)   2.2 ng/mL en 1096 dias = 0.73/ano
	///   tramo reciente 3.0 (2025) -> 5.2 (2026)   2.2 ng/mL en  365 dias = 2.20/ano
	///
	/// Con el umbral en 0.75 la serie entera diria que NO exige atencion y el tramo
	/// reciente dice que si. El tramo reciente es el que tiene razon: los tres anos de
	/// estabilidad previos no diluyen un salto de este ano, lo hacen MAS notable. Promediar
	/// sobre toda la historia es exactamente como se pierde un cambio recien empezado.
	///
	/// Carrier por identidad: sin Equals, sin GetHashCode, no es llave de nada (E9/E23).
	internal sealed class Velocidad
	{
		/// <summary>El analito del que se hablo, por su nombre canonico.</summary>
		internal string Analito { get; }

		/// <summary>El mismo analito como se le muestra a la persona, con acentos.</summary>
		internal string AnalitoParaMostrar { get; }

		/// <summary>Cambio por ano, con signo. Positivo si subio. En la unidad del analito.</summary>
		internal decimal PorAno { get; }

		/// <summary>La unidad, para poder decir "0.75 ng/mL por ano" y no "0.75".</summary>
		internal string Unidad { get; }

		/// <summary>Los dias del tramo sobre el que se calculo. Cero si no hay tramo.</summary>
		internal int DiasDelTramo { get; }

		/// <summary>Si este analito se interpreta por velocidad. El colesterol no; el PSA si.</summary>
		internal bool EsAplicable { get; }

		/// <summary>El umbral de ESTE analito, para que la superficie pueda decir contra que se comparo.</summary>
		internal decimal UmbralPorAno { get; }

		private Velocidad(Analito analito, decimal porAno, string unidad, int diasDelTramo,
			bool esAplicable, decimal umbralPorAno)
		{
			ArgumentNullException.ThrowIfNull(analito);

			Analito = analito.Nombre;
			AnalitoParaMostrar = analito.ParaMostrar;
			PorAno = porAno;
			Unidad = unidad;
			DiasDelTramo = diasDelTramo;
			EsAplicable = esAplicable;
			UmbralPorAno = umbralPorAno;
		}

		/// <summary>Una velocidad medida de verdad, sobre un tramo real.</summary>
		internal static Velocidad Medida(Analito analito, decimal porAno, string unidad, int diasDelTramo,
			decimal umbralPorAno) =>
			new Velocidad(analito, porAno, unidad, diasDelTramo, umbralPorAno > 0m, umbralPorAno);

		/// <summary>
		/// No hay velocidad que dar. Pasa por dos razones distintas y las dos son legitimas:
		/// no hay dos mediciones, o este analito no se lee por velocidad.
		/// </summary>
		internal static Velocidad NoAplica(Analito analito, string unidad, decimal umbralPorAno) =>
			new Velocidad(analito, 0m, unidad, 0, false, umbralPorAno);

		/// <summary>
		/// Si la velocidad observada supera el umbral de este analito EN LA DIRECCION que le
		/// preocupa. Es lo unico accionable de toda la clase.
		/// </summary>
		/// Que el signo importe no es un detalle: un PSA que BAJA 2 ng/mL por ano no exige
		/// nada, y sin comparar con signo el valor absoluto lo reportaria como alarma.
		internal bool ExigeAtencion => EsAplicable && PorAno >= UmbralPorAno;

		/// <summary>La velocidad ya dicha. "2.20 ng/mL por ano".</summary>
		internal string Expresada => EsAplicable
			? $"{PorAno:0.00} {Unidad} por año"
			: "no se interpreta por velocidad";
	}
}
