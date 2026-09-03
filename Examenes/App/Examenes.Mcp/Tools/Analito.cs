using Examenes.App;

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

	// La quimica general. Se leen todos contra un rango que no depende del sexo, asi que la
	// IA puede preguntarles la tendencia con lo que el expediente ya tiene.
	Trigliceridos,
	GlucosaEnAyunas,
	NitrogenoUreico,
	Sodio,
	Potasio,
	Cloro,
	ProteinasTotales,
}

/// <summary>
/// Los analitos BINARIOS: los que se informan como presencia o ausencia. Conjunto cerrado.
/// </summary>
///
/// Es un enum aparte y no tres valores mas del otro, y esa separacion es el contrato: las
/// preguntas que se les pueden hacer NO son las mismas. Un nitrito no viene subiendo ni
/// tiene velocidad; aparece, persiste o cede. Poner los doce en un enum unico dejaria que
/// la IA pidiera `velocidad_de(SangreEnOrina)` y recibiera una respuesta vacia que despues
/// tendria que interpretar.
///
/// Con dos enums el schema mismo lo impide, que es el mismo argumento por el que estos
/// valores no son strings: la restriccion vive en el contrato y no en la prosa.
public enum AnalitoBinario
{
	Nitritos,
	SangreEnOrina,
	SangreEnHeces,

	// Las otras tres casillas de la tira. Los tres se cuentan igual y concluyen distinto:
	// una proteina aislada pide repetirse, unas cetonas aisladas se explican por el ayuno, y
	// una glucosa en la orina ya amerita mostrarla la primera vez.
	ProteinaEnOrina,
	Cetonas,
	GlucosaEnOrina,
}

/// <summary>
/// Los analitos NOMINALES: los que se informan como una categoría de un conjunto. Cerrado.
/// </summary>
///
/// Tercer enum por la misma razón que hay un segundo: las preguntas que se les pueden hacer
/// no son las mismas. Un color no viene subiendo ni aparece — SALE, y sale de un conjunto
/// donde algunas categorías son las esperables y otras no. `presencia_de(ColorDeLaOrina)`
/// devolvería una respuesta vacía que la IA después tendría que interpretar.
public enum AnalitoNominal
{
	ColorDeLaOrina,
	ColorDeLasHeces,
}

internal static class AnalitoDelDominio
{
	/// <summary>
	/// La única traducción entre el enum de la frontera y el nombre con el que el dominio
	/// conoce a cada analito.
	/// </summary>
	///
	/// Los nombres salen de NombresDeAnalito, que es la copia que Examenes.App publica para
	/// los nodos de afuera: desde que el dominio es enteramente `internal`, este ensamblado
	/// -un servidor MCP, que no es ni la Superficie- no puede nombrarle un tipo. La copia
	/// queda amarrada en Comprobaciones, que compara cada constante contra el dominio: si un
	/// analito se renombra el fixture lo dice, en vez de que esto empiece a devolver "sin
	/// mediciones" en silencio, que es la falla cara.
	///
	/// No hace falta el `default` con excepción que llevaría un resolver desde string (la
	/// función total-con-throw de rigor): un enum no puede traer un valor que no esté, así
	/// que la rama imposible no se escribe. Eso es exactamente lo que se gana al tipar.
	internal static string Nombre(this Analito analito) => analito switch
	{
		// A la izquierda el enum de ESTA frontera; a la derecha el nombre canonico con el
		// que el dominio conoce al analito, como texto.
		Analito.AntigenoProstatico => NombresDeAnalito.AntigenoProstatico,
		Analito.ColesterolTotal    => NombresDeAnalito.ColesterolTotal,
		Analito.ColesterolLdl      => NombresDeAnalito.ColesterolLdl,
		Analito.Hemoglobina        => NombresDeAnalito.Hemoglobina,
		Analito.Trigliceridos      => NombresDeAnalito.Trigliceridos,
		Analito.GlucosaEnAyunas    => NombresDeAnalito.GlucosaEnAyunas,
		Analito.NitrogenoUreico    => NombresDeAnalito.NitrogenoUreico,
		Analito.Sodio              => NombresDeAnalito.Sodio,
		Analito.Potasio            => NombresDeAnalito.Potasio,
		Analito.Cloro              => NombresDeAnalito.Cloro,
		Analito.ProteinasTotales   => NombresDeAnalito.ProteinasTotales,
		_ => throw new ArgumentOutOfRangeException(nameof(analito), analito,
			"Se agregó un analito al enum y no se lo mapeó al dominio."),
	};

	internal static string Nombre(this AnalitoBinario analito) => analito switch
	{
		AnalitoBinario.Nitritos        => NombresDeAnalito.NitritosEnOrina,
		AnalitoBinario.SangreEnOrina   => NombresDeAnalito.SangreEnLaOrina,
		AnalitoBinario.SangreEnHeces   => NombresDeAnalito.SangreEnLasHeces,
		AnalitoBinario.ProteinaEnOrina => NombresDeAnalito.ProteinaEnLaOrina,
		AnalitoBinario.Cetonas         => NombresDeAnalito.CetonasEnOrina,
		AnalitoBinario.GlucosaEnOrina  => NombresDeAnalito.GlucosaEnLaOrina,
		_ => throw new ArgumentOutOfRangeException(nameof(analito), analito,
			"Se agregó un analito binario al enum y no se lo mapeó al dominio."),
	};

	internal static string Nombre(this AnalitoNominal analito) => analito switch
	{
		AnalitoNominal.ColorDeLaOrina  => NombresDeAnalito.ColorDeLaOrina,
		AnalitoNominal.ColorDeLasHeces => NombresDeAnalito.ColorDeLasHeces,
		_ => throw new ArgumentOutOfRangeException(nameof(analito), analito,
			"Se agregó un analito nominal al enum y no se lo mapeó al dominio."),
	};
}
