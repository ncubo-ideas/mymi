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
	}
}
