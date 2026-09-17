using System;

namespace Concesionario.Dominio
{
	/// <summary>
	/// SI HAY ALGO PUBLICADO detras de lo que se pregunto, por su codigo. El codigo es identidad y
	/// cruza intacto; como se dice cada una es de quien la muestra.
	/// <para>
	/// Vive suelto y no dentro de un tipo porque lo contestan dos: la respuesta de la especificacion
	/// y la clausula de una cobertura, que no pasa por ella.
	/// </para>
	/// </summary>
	internal static class SpecificationPublication
	{
		/// <summary>Una revision sellada lo publico, y la respuesta la nombra.</summary>
		internal const string Published = "Published";

		/// <summary>Ninguna revision sellada publico nada sobre eso.</summary>
		internal const string NotPublished = "NotPublished";
	}
}
