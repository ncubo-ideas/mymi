namespace Municipalidad.Wasm
{
	/// <summary>
	/// Traduce identificadores del registro a texto legible, PURAMENTE PARA MOSTRAR. No toca el dominio
	/// ni el registro: si un identificador no está en el diccionario (una finca, un rubro nuevo), se
	/// devuelve tal cual llegó -- nunca inventa un nombre para algo que no reconoce.
	/// </summary>
	public static class NombreLegible
	{
		/// <summary>Los cinco rubros del catálogo (Municipalidad.Dominio.Rubro), dichos para una persona.</summary>
		public static string DelRubro(string rubro) => rubro switch
		{
			"recoleccion-de-basura" => "Recolección de basura",
			"limpieza-de-calles" => "Limpieza de calles",
			"servicio-policia" => "Servicio de policía",
			"bienes-inmuebles" => "Bienes inmuebles",
			"parques-y-zonas-verdes" => "Parques y zonas verdes",
			_ => rubro,
		};

		/// <summary>Las cuatro etiquetas que compone Municipalidad.Dominio.Variacion, dichas para una persona.</summary>
		public static string DeLaExplicacionDeVariacion(string etiqueta) => etiqueta switch
		{
			"cambio-de-tarifa" => "Cambio de tarifa",
			"cambio-de-avaluo" => "Cambio de avalúo",
			"cambio-menor" => "Cambio menor",
			"sin-cambio" => "Sin cambio",
			_ => etiqueta,
		};

		/// <summary>
		/// El icono real del sprite para un rubro del catalogo, cuando existe. Los rubros sin icono
		/// dedicado (limpieza-de-calles) usan uno generico ya existente -- nunca uno inventado.
		/// </summary>
		public static string IconoDelRubro(string rubro) => rubro switch
		{
			"recoleccion-de-basura" => "basura",
			"servicio-policia" => "policia",
			"bienes-inmuebles" => "inmuebles",
			"parques-y-zonas-verdes" => "parques",
			_ => "calendario-chico",
		};

		/// <summary>
		/// Puramente presentacional: "2024-06" -> "Jun 2024", "2024-T2" -> "T2 2024". Son los dos unicos
		/// formatos que produce el dominio (Mes o Trimestre); cualquier otra cosa se devuelve tal cual.
		/// </summary>
		public static string Periodo(string etiqueta)
		{
			string[] partes = etiqueta.Split('-', 2);
			if (partes.Length != 2) return etiqueta;

			string anio = partes[0];
			string resto = partes[1];

			if (resto.StartsWith('T')) return $"{resto} {anio}";

			if (int.TryParse(resto, out int mes) && mes is >= 1 and <= 12)
				return $"{Meses[mes - 1]} {anio}";

			return etiqueta;
		}

		/// <summary>
		/// Puramente presentacional: "2025-09-03" -> "3 sep 2025". El unico formato que produce el
		/// dominio para un dia de calendario (Dichos.Dia / ElDiaDe); cualquier otra cosa vuelve tal cual.
		/// </summary>
		public static string Fecha(string diaCalendario)
		{
			string[] partes = diaCalendario.Split('-');
			if (partes.Length != 3) return diaCalendario;

			string anio = partes[0];
			if (!int.TryParse(partes[1], out int mes) || mes is < 1 or > 12) return diaCalendario;
			if (!int.TryParse(partes[2], out int dia)) return diaCalendario;

			return $"{dia} {Meses[mes - 1].ToLowerInvariant()} {anio}";
		}

		private static readonly string[] Meses =
		{
			"Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic",
		};
	}
}
