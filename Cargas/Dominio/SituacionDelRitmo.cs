using System;

namespace Cargas.Dominio
{
	/// <summary>
	/// EN CUAL DE CUATRO SITUACIONES ESTA EL RITMO DE UN VEHICULO. Es un conjunto cerrado
	/// y no un bool porque "no se pudo medir" tiene TRES causas que se arreglan distinto:
	/// no anotaste ninguno, anotaste uno solo, o los que anotaste son del mismo dia. Antes
	/// las tres daban el mismo cero, y un cero no le dice a nadie que hacer.
	///
	/// Y NINGUNA DE LAS TRES QUIERE DECIR QUE EL CARRO NO SE MOVIO. Un carro quieto no se
	/// puede ni registrar: el odometro es ESTRICTAMENTE creciente y EsKilometrajeCoherente
	/// rechaza dos lecturas iguales. Asi que Medido siempre trae un ritmo mayor que cero,
	/// y el cero de las otras tres nunca compite con una medicion.
	/// </summary>
	internal sealed class SituacionDelRitmo
	{
		internal static readonly SituacionDelRitmo Medido =
			new SituacionDelRitmo("medido", sePudo: true);

		internal static readonly SituacionDelRitmo SinLecturas =
			new SituacionDelRitmo("sin-lecturas", sePudo: false);

		internal static readonly SituacionDelRitmo UnaSolaLectura =
			new SituacionDelRitmo("una-sola-lectura", sePudo: false);

		internal static readonly SituacionDelRitmo LecturasDelMismoDia =
			new SituacionDelRitmo("lecturas-del-mismo-dia", sePudo: false);

		/// <summary>
		/// EL CODIGO de esta situacion: identidad, y lo unico que cruza el wire. Por que no se
		/// pudo medir es texto de presentacion, y lo busca con este codigo la capa que redacta,
		/// en el idioma que le pidieron.
		/// </summary>
		internal string Nombre { get; private set; }

		/// <summary>Si hay un ritmo que decir. Cuando es false, KilometrosPorDia no
		/// significa nada y por eso viene en cero: no es un ritmo de cero.</summary>
		internal bool SePudoMedir { get; private set; }

		private SituacionDelRitmo(string nombre, bool sePudo)
		{
			Nombre = nombre;
			SePudoMedir = sePudo;
		}

		internal static SituacionDelRitmo PorNombre(string nombre)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(nombre);

			string normalizado = nombre.Trim().ToLowerInvariant();
			if (normalizado == Medido.Nombre) return Medido;
			if (normalizado == SinLecturas.Nombre) return SinLecturas;
			if (normalizado == UnaSolaLectura.Nombre) return UnaSolaLectura;
			if (normalizado == LecturasDelMismoDia.Nombre) return LecturasDelMismoDia;

			throw new ErrorDeDominio(
				$"'{nombre}' no es una situacion de ritmo conocida. Las validas son: "
				+ $"{Medido.Nombre}, {SinLecturas.Nombre}, {UnaSolaLectura.Nombre}, "
				+ $"{LecturasDelMismoDia.Nombre}.");
		}
	}
}
