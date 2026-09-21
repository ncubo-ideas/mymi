using System;

namespace Examenes.Dominio
{
	/// <summary>
	/// Sangre oculta en heces. Forma 5 — binaria.
	/// </summary>
	///
	/// LA OTRA MITAD DEL EJEMPLO DE ALVARO. Leerla al lado de <see cref="SangreEnOrina"/>
	/// es el punto de que existan las dos: misma forma, misma pregunta, conducta opuesta.
	///
	///   sangre en orina  → AtencionInmediata   "que lo vea alguien pronto"
	///   sangre en heces  → Seguimiento         "conviene revisarlo"
	///
	/// Las dos clases se parecen tanto a proposito. Si la unica diferencia entre ellas
	/// fuera el nombre, sobraria una y el dominio no estaria aportando nada; la diferencia
	/// esta abajo, en SeveridadDelHallazgo, y es la unica parte que importa.
	///
	/// ES TAMBIEN EL ANALITO QUE OBLIGO A QUE EXISTA UN EXAMEN DE HECES. No se puede vivir
	/// en el urianalisis: es OTRA MUESTRA. Que el dominio tenga dos examenes distintos con
	/// un binario cada uno es lo que hace que la comparacion sea real y no dos campos de la
	/// misma fila.
	internal sealed class SangreEnHeces : MagnitudBinaria
	{
		internal static readonly SangreEnHeces Positivo =
			new SangreEnHeces("positivo", realizada: true, positivo: true);
		internal static readonly SangreEnHeces Negativo =
			new SangreEnHeces("negativo", realizada: true, positivo: false);
		internal static readonly SangreEnHeces NoRealizado =
			new SangreEnHeces("no realizado", realizada: false, positivo: false);

		internal string Etiqueta { get; }

		private readonly bool realizada;
		private readonly bool positivo;

		private SangreEnHeces(string etiqueta, bool realizada, bool positivo)
		{
			Etiqueta = etiqueta;
			this.realizada = realizada;
			this.positivo = positivo;
		}

		internal override string Clave => Etiqueta;

		internal override bool FueRealizada => realizada;

		internal override bool EsPositivo
		{
			get
			{
				if (!realizada)
					throw new InvalidOperationException(
						"La sangre en heces no se midio en este examen: preguntar por el "
						+ "resultado exige consultar antes FueRealizada.");

				return positivo;
			}
		}

		internal override bool EsHallazgo => realizada && positivo;

		/// <summary>
		/// SE REVISA, NO SE CORRE. La causa mas frecuente es local y banal —el ejemplo del
		/// propio feedback es el estrenimiento—, asi que alarmar seria el consejo ciego que
		/// hay que evitar. Pero tampoco es nada: corresponde revisarlo.
		///
		/// Este es EL contraste. Cambiar este valor a AtencionInmediata haria que las dos
		/// sangres se leyeran igual, y la comprobacion que compara las dos severidades
		/// fallaria — que es exactamente lo que tiene que pasar.
		///
		/// ⚠️ Refleja el criterio del feedback y sirve para modelar la estructura. Conviene
		/// que lo revise alguien con criterio medico antes de mostrarlo fuera del equipo.
		/// </summary>
		internal override Severidad SeveridadDelHallazgo =>
			EsHallazgo ? Severidad.Vigilar : Severidad.Informativo;

		// Se repite, y no espera al tamizaje del ano que viene: lo que hay que saber es
		// si vuelve a salir.
		internal override Accion AccionDelHallazgo =>
			EsHallazgo ? Accion.RepetirElExamen(Plazo.EsteMes) : null;

		internal override string ClaveDeLaFrase =>
			!realizada ? "resultado.no-realizado"
			: positivo ? "resultado.sangre-en-heces.positivo"
			: "resultado.sangre-en-heces.negativo";

		internal static SangreEnHeces PorNombre(string nombre)
		{
			if (string.IsNullOrWhiteSpace(nombre)) return NoRealizado;

			string normalizado = nombre.Trim().ToLowerInvariant();
			if (normalizado == Positivo.Etiqueta) return Positivo;
			if (normalizado == Negativo.Etiqueta) return Negativo;
			if (normalizado == NoRealizado.Etiqueta) return NoRealizado;

			throw new ArgumentException(
				$"'{nombre}' no es un resultado de sangre en heces. Los validos son: "
				+ $"{Positivo.Etiqueta}, {Negativo.Etiqueta}, {NoRealizado.Etiqueta}.", nameof(nombre));
		}

		/// <summary>El plural de este analito. Ver <see cref="SerieDeSangreEnHeces"/>.</summary>
		internal override SerieBinaria NuevaSerie() => new SerieDeSangreEnHeces();
	}
}
