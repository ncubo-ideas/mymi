using System;

namespace Compras.Dominio
{
	/// <summary>
	/// CUANTO HACE que no entra una categoria a la canasta, y si eso ya es demasiado
	/// para ELLA. Es la unica pregunta de este dominio que mira lo que NO ESTA.
	/// </summary>
	// Todo lo demas interpreta lo que hay: el ticket, la canasta, la comparacion, la
	// garantia. Esto mira el hueco.
	//
	// POR QUE IMPORTA QUE SEA LO QUE NO ESTA. Una pantalla solo puede mostrar lo que
	// tiene: puede ordenar las compras, sumarlas, pintarlas. No puede renderizar una
	// ausencia que nadie le paso. "Hace tres meses que no entra una verdura" no es un
	// dato del historial: es una conclusion sobre el historial mas el calendario mas lo
	// que cada categoria considera su periodicidad.
	//
	// La fecha llega como PARAMETRO. Quien tiene derecho a saber que dia es hoy es la
	// superficie, no el modelo: un dominio que lee el reloj deja de ser reproducible.
	public class AbastecimientoPendiente
	{
		public Categoria Categoria { get; private set; }

		/// <summary>Siempre hay una: de algo que nunca se compro no se puede decir que falta.</summary>
		public DateTime UltimaCompra { get; private set; }

		public int DiasDesdeLaUltima { get; private set; }

		/// <summary>Lo que ESTA categoria considera su periodicidad.</summary>
		public int DiasEsperados { get; private set; }

		public AbastecimientoPendiente(Categoria categoria, DateTime ultimaCompra, int diasDesdeLaUltima)
		{
			ArgumentNullException.ThrowIfNull(categoria);
			if (!categoria.SeReabastece)
				throw new ArgumentException(
					$"La categoria '{categoria.Nombre}' no espera reponerse, asi que no puede estar pendiente.",
					nameof(categoria));
			if (diasDesdeLaUltima < 0)
				throw new ArgumentOutOfRangeException(nameof(diasDesdeLaUltima), diasDesdeLaUltima,
					"No se puede haber comprado en el futuro.");

			Categoria = categoria;
			UltimaCompra = ultimaCompra;
			DiasDesdeLaUltima = diasDesdeLaUltima;
			DiasEsperados = categoria.DiasDeReabastecimiento;
		}

		public bool EstaVencido => DiasDesdeLaUltima > DiasEsperados;

		/// <summary>Con signo: negativo si ya se paso.</summary>
		public int DiasQueFaltan => DiasEsperados - DiasDesdeLaUltima;

		// Al DOBLE del intervalo deja de ser "se me paso" y pasa a ser que la categoria
		// salio de la canasta.
		public Severidad Severidad
		{
			get
			{
				if (!EstaVencido) return Severidad.Informativo;
				return DiasDesdeLaUltima > DiasEsperados * 2
					? Severidad.ConvieneRevisar
					: Severidad.Informativo;
			}
		}

		public string NombreDeLaCategoria => Categoria.Nombre;

		public string NombreDeLaSeveridad => Severidad.Nombre;

		public bool EsRecomendado => Categoria.EsComestible && !Categoria.Perfil.EsOcasional;

		/// <summary>La linea, escrita por el dominio con SUS hechos. No es un consejo: es lo que paso.</summary>
		public string ParaSonia
		{
			get
			{
				string hace = DiasDesdeLaUltima >= 60
					? $"hace {DiasDesdeLaUltima / 30} meses"
					: $"hace {DiasDesdeLaUltima} días";

				return EstaVencido
					? $"la última vez que entró {Legible()} a tu canasta fue {hace}, y suele reponerse "
						+ $"cada {DiasEsperados} días"
					: $"{Legible()} entró {hace}: al día";
			}
		}

		// El nombre del repertorio es una llave (FrutasYVerduras); lo que se le muestra a
		// la persona es otra cosa.
		private string Legible() => Categoria.Nombre switch
		{
			"Proteinas" => "una proteína",
			"FrutasYVerduras" => "una fruta o verdura",
			"LacteosYHuevos" => "un lácteo",
			"GranosYAbarrotes" => "un grano o abarrote",
			"Limpieza" => "algo de limpieza",
			_ => Categoria.Nombre,
		};
	}
}
