using Examenes.Dominio;

namespace Examenes.Mcp.Tools;

/// <summary>
/// Los analitos que este expediente modela. Conjunto CERRADO.
/// </summary>
///
/// Es un enum y no un string, y eso es la corrección de `ENVIRONMENT v2`: *"sin strings en
/// la frontera semántica"*.
///
/// Con un string, la lista de valores válidos vivía en la **descripción** de la tool —
/// prosa que la IA tiene que leer, entender y copiar bien. Con un enum vive en el
/// **schema**, que es un contrato: el cliente MCP no puede mandar un valor que no exista, y
/// no hace falta validar que exista ni explicar la lista en la prosa.
///
/// Es el mismo argumento que el conjunto cerrado del dominio, un piso más arriba: un string
/// suelto admite "colesterol", "Colesterol", "COLESTEROL TOTAL" y "colestrol" como cuatro
/// cosas distintas para la misma.
public enum Analito
{
	AntigenoProstatico,
	ColesterolTotal,
	ColesterolLdl,
	Hemoglobina,
}

internal static class AnalitoDelDominio
{
	/// <summary>
	/// La única traducción entre el enum de la frontera y el nombre con el que el dominio
	/// conoce a cada analito.
	/// </summary>
	///
	/// Los nombres salen de las constantes del propio dominio, no de literales repetidos
	/// acá: si un analito se renombra, esto deja de compilar en vez de empezar a devolver
	/// "sin mediciones" en silencio, que es la falla cara.
	///
	/// No hace falta el `default` con excepción que llevaría un resolver desde string (la
	/// función total-con-throw de rigor): un enum no puede traer un valor que no esté, así
	/// que la rama imposible no se escribe. Eso es exactamente lo que se gana al tipar.
	internal static string Nombre(this Analito analito) => analito switch
	{
		// El namespace por delante no es ruido: en este archivo `Analito` a secas es el
		// enum de la frontera, y el de la derecha es el conjunto cerrado del dominio. Son
		// dos cosas distintas con el mismo nombre, y esta es justamente la linea donde se
		// traduce una a la otra.
		Analito.AntigenoProstatico => Examenes.Dominio.Analito.AntigenoProstatico.Nombre,
		Analito.ColesterolTotal    => Examenes.Dominio.Analito.ColesterolTotal.Nombre,
		Analito.ColesterolLdl      => Examenes.Dominio.Analito.ColesterolLdl.Nombre,
		Analito.Hemoglobina        => Examenes.Dominio.Analito.Hemoglobina.Nombre,
		_ => throw new ArgumentOutOfRangeException(nameof(analito), analito,
			"Se agregó un analito al enum y no se lo mapeó al dominio."),
	};
}
