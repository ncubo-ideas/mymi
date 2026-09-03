namespace Examenes.App
{
	/// <summary>
	/// El nombre CANONICO de cada analito, como texto, para los nodos que estan afuera del
	/// dominio: la Superficie y el servidor MCP.
	/// </summary>
	///
	// POR QUE ES TEXTO Y NO UNA REFERENCIA A Analito. El dominio es enteramente `internal`,
	// asi que ni la Superficie ni las tools del MCP pueden nombrar un tipo suyo en C#. Y no
	// les hace falta: lo unico que necesitan de un analito es COMO SE LLAMA, para pasarlo
	// como parametro primitivo a un script. La identidad, los sinonimos, si es continuo o
	// binario y que rango le corresponde siguen viviendo adentro, donde el DSL los alcanza
	// por reflexion.
	//
	// LA COPIA ESTA AMARRADA: Comprobaciones compara cada una de estas constantes contra
	// Analito.X.Nombre del dominio. El fixture es el unico lugar del repo que ve los dos
	// lados -- el dominio por InternalsVisibleTo y esto por referencia normal-- asi que es el
	// unico que puede obligarlos a coincidir. Si alguien renombra un analito y no toca esto,
	// el fixture lo dice, en vez de que el MCP empiece a contestar "sin mediciones" en
	// silencio, que es la falla cara.
	public static class NombresDeAnalito
	{
		public const string AntigenoProstatico = "Antigeno prostatico";
		public const string ColesterolTotal = "Colesterol total";
		public const string ColesterolLdl = "Colesterol LDL";
		public const string ColesterolHdl = "Colesterol HDL";
		public const string Hemoglobina = "Hemoglobina";

		// La quimica general. Los siete se leen contra un rango que NO depende del sexo, y
		// eso es la razon por la que son estos siete y no otros: cada analito que dependiera
		// del sexo le bajaria el numerador al titular de la portada.
		public const string Trigliceridos = "Trigliceridos";
		public const string GlucosaEnAyunas = "Glucosa en ayunas";
		public const string NitrogenoUreico = "Nitrogeno ureico";
		public const string Sodio = "Sodio";
		public const string Potasio = "Potasio";
		public const string Cloro = "Cloro";
		public const string ProteinasTotales = "Proteinas totales";

		public const string ColorDeLaOrina = "Color de la orina";
		public const string NitritosEnOrina = "Nitritos";
		public const string SangreEnLaOrina = "Sangre en orina";
		public const string ColorDeLasHeces = "Color de las heces";
		public const string SangreEnLasHeces = "Sangre en heces";

		// Las otras tres casillas de la tira reactiva. Ojo con las dos glucosas: la de
		// SANGRE es "Glucosa en ayunas" y responde a "glucosa"; la de ORINA responde a
		// "glucosuria" y a su propio nombre. Son dos analitos distintos con dos series
		// distintas, y confundirlos aca haria que el MCP contestara por el otro.
		public const string ProteinaEnLaOrina = "Proteina en orina";
		public const string CetonasEnOrina = "Cetonas";
		public const string GlucosaEnLaOrina = "Glucosa en orina";
	}
}
