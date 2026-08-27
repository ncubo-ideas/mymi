using System;

namespace Cargas.Dominio
{
	// Hemoglobina. Es el analito que existe para que el dominio tenga un caso donde lo
	// que preocupa es el valor BAJO, no el alto: sin el, "clasificar" podria confundirse
	// con "pasarse del techo" y el rango con dos extremos no se estaria usando.
	//
	// Y ES EL UNICO CUYA DIRECCION DESEABLE ES MANTENERSE. Los otros tres quieren bajar
	// o subir; a este le conviene quedarse adentro del rango, y alejarse es peor en
	// cualquiera de las dos direcciones. Por eso no puede juzgar un cambio por su signo
	// -DireccionDeseable.EsFavorable revienta si se lo preguntan- y tiene que mirar
	// contra donde esta parado. Un bool "la direccion preocupante es el ascenso" no tenia
	// como decir esto.
	//
	// LO QUE ESTE ANALITO SABE Y NINGUN OTRO: el GRADO de anemia. No existe el grado de
	// anemia de un colesterol.
	public sealed class Hemoglobina : ExamenDeSangre
	{
		private static readonly AnalitoContinuo Suyo = Cargas.Dominio.Analito.Hemoglobina;

		public Hemoglobina(DateTime fecha, decimal valor, string unidad)
			: base(fecha, Suyo, Medida(valor, unidad, Suyo))
		{
		}

		public override string Nombre => Suyo.ParaMostrar;

		// Rango con piso Y techo, a diferencia del colesterol y el antigeno.
		public override RangoDeReferencia RangoPara(int edad) =>
			edad >= 60
				? RangoDeReferencia.Entre(12.5m, 16.5m, Suyo.Unidad)
				: RangoDeReferencia.Entre(13.0m, 17.0m, Suyo.Unidad);

		public override decimal CambioMaximoVerosimilPorDia => 0.1m;

		public override decimal CambioMinimoSignificativo => 1.0m;

		public override DireccionDeseable Direccion => DireccionDeseable.Mantenerse;

		// Anual, y semestral si hay anemia: cuando el valor ya esta bajo, lo que importa
		// es si sigue bajando, y eso no se ve esperando un ano.
		// La hemoglobina se mueve POCO: su rango entero -de 2 a 25- es un factor de doce,
		// y duplicarse ya es mucho. Es el analito con el corte mas bajo de los cuatro.
		public override decimal MultiploQueNingunIntervaloJustifica => 2.5m;

		public override int MesesHastaElProximo(int edad) =>
			GradoDeAnemia().HayAnemia ? 6 : 12;

		// ---------------------------------------------------------------- lo suyo

		// CUANTA anemia hay, que es distinto de "esta bajo". El rango dice si cruzo el
		// piso; esto dice cuanto, y de ahi sale que hacer.
		public GradoDeAnemia GradoDeAnemia()
		{
			decimal valor = Valor.Valor;

			if (valor >= 13.0m) return Cargas.Dominio.GradoDeAnemia.Ninguna;
			if (valor >= 11.0m) return Cargas.Dominio.GradoDeAnemia.Leve;
			if (valor >= 8.0m) return Cargas.Dominio.GradoDeAnemia.Moderada;
			return Cargas.Dominio.GradoDeAnemia.Severa;
		}

		public override SerieContinua NuevaSerie() => new SerieDeHemoglobina();
	}
}
