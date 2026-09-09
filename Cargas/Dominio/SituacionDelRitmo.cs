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
			new SituacionDelRitmo("medido", sePudo: true,
				porque: "");

		internal static readonly SituacionDelRitmo SinLecturas =
			new SituacionDelRitmo("sin lecturas", sePudo: false,
				porque: "todavia no anotaste ningun kilometraje");

		internal static readonly SituacionDelRitmo UnaSolaLectura =
			new SituacionDelRitmo("una sola lectura", sePudo: false,
				porque: "anotaste un solo kilometraje, y un ritmo necesita dos");

		internal static readonly SituacionDelRitmo LecturasDelMismoDia =
			new SituacionDelRitmo("lecturas del mismo dia", sePudo: false,
				porque: "los kilometrajes que anotaste son todos del mismo dia, "
				      + "y un ritmo por dia necesita dos dias distintos");

		/// <summary>El nombre para el conjunto cerrado que viaja a la frontera.</summary>
		internal string Nombre { get; private set; }

		/// <summary>Si hay un ritmo que decir. Cuando es false, KilometrosPorDia no
		/// significa nada y por eso viene en cero: no es un ritmo de cero.</summary>
		internal bool SePudoMedir { get; private set; }

		/// <summary>Por que no se pudo, dicho para la persona. Vacio cuando si se
		/// pudo: ahi lo que hay que decir es el ritmo.</summary>
		internal string Porque { get; private set; }

		private SituacionDelRitmo(string nombre, bool sePudo, string porque)
		{
			Nombre = nombre;
			SePudoMedir = sePudo;
			Porque = porque;
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
