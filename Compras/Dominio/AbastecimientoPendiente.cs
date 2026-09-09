using System;

namespace Compras.Dominio
{
	/// <summary>
	/// CUÁNTO HACE que no entra una categoría a la canasta, y si eso ya es demasiado
	/// PARA ELLA. Es la única pregunta de este dominio que mira lo que NO ESTÁ.
	/// </summary>
	// Todo lo demas interpreta lo que hay: el ticket, la canasta, la comparacion, la
	// garantia, el precio. Esto mira el hueco.
	//
	// POR QUE IMPORTA QUE SEA LO QUE NO ESTA. Una pantalla solo puede mostrar lo que
	// tiene: puede ordenar las compras, sumarlas, pintarlas. No puede renderizar una
	// ausencia que nadie le paso. "Hace tres meses que no entra una verdura" no es un dato
	// del historial: es una conclusion sobre el historial mas el calendario mas el ritmo.
	//
	// EL RITMO ES EL DE ELLA, y eso corrige una mentira piadosa. Antes se leia contra una
	// periodicidad fija por categoria que habiamos elegido nosotros: para una familia que
	// compra cada quince dias, "hace 12 dias que no entra un lacteo" no era un hueco, era
	// martes. Cuando su historial no alcanza para medir un ritmo, se cae al numero del
	// catalogo y EsRitmoObservado lo dice, en vez de presentar una suposicion como un
	// hecho.
	//
	// La fecha llega como PARAMETRO. Quien tiene derecho a saber que dia es hoy es la
	// superficie, no el modelo.
	internal class AbastecimientoPendiente
	{
		internal Categoria Categoria { get; private set; }

		/// <summary>Siempre hay una: de algo que nunca se compró no se puede decir que falta.</summary>
		internal DateTime UltimaCompra { get; private set; }

		internal int DiasDesdeLaUltima { get; private set; }

		/// <summary>El ritmo con el que se lee este hueco: el de ella, o el del catálogo.</summary>
		internal RitmoDeCompra Ritmo { get; private set; }

		/// <summary>Los días que se esperaban entre compras. Salen del ritmo.</summary>
		internal int DiasEsperados => Ritmo.DiasParaLeerElHueco;

		/// <summary>Si los días esperados se MIDIERON en su historial o se supusieron.</summary>
		internal bool EsRitmoObservado => Ritmo.EsObservado;

		internal AbastecimientoPendiente(Categoria categoria, DateTime ultimaCompra, int diasDesdeLaUltima,
			RitmoDeCompra ritmo)
		{
			ArgumentNullException.ThrowIfNull(categoria);
			ArgumentNullException.ThrowIfNull(ritmo);
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
			Ritmo = ritmo;
		}

		internal bool EstaVencido => DiasDesdeLaUltima > DiasEsperados;

		/// <summary>Con signo: negativo si ya se pasó.</summary>
		internal int DiasQueFaltan => DiasEsperados - DiasDesdeLaUltima;

		// Al DOBLE del intervalo deja de ser "se me paso" y pasa a ser que la categoria
		// salio de la canasta.
		internal Severidad Severidad
		{
			get
			{
				if (!EstaVencido) return Severidad.Informativo;
				return DiasDesdeLaUltima > DiasEsperados * 2
					? Severidad.ConvieneRevisar
					: Severidad.Informativo;
			}
		}

		internal string NombreDeLaCategoria => Categoria.Nombre;

		internal string NombreDeLaSeveridad => Severidad.Nombre;

		internal bool EsComestible => Categoria.EsComestible;

		/// <summary>La línea, escrita por el dominio con SUS hechos. No es un consejo: es lo que pasó.</summary>
		internal string ParaElAsistente
		{
			get
			{
				string hace = DiasDesdeLaUltima >= 60
					? $"hace {DiasDesdeLaUltima / 30} meses"
					: $"hace {DiasDesdeLaUltima} días";

				// Se dice DE DONDE sale el numero con el que se lo esta juzgando: "suele
				// reponerse cada X" no es lo mismo si el X lo midio ella o lo supusimos.
				string cada = EsRitmoObservado
					? $"y vos lo comprás cada {DiasEsperados} días"
					: $"y suele reponerse cada {DiasEsperados} días";

				// La mencion sale del repertorio: el nombre es una llave (FrutasYVerduras) y
				// eso no se le muestra a nadie.
				return EstaVencido
					? $"la última vez que entró {Categoria.Mencion} a tu canasta fue {hace}, {cada}"
					: $"{Categoria.Mencion} entró {hace}: al día";
			}
		}
	}
}
