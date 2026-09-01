using System;

namespace Examenes.Dominio
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
	internal sealed class Hemoglobina : ExamenDeSangre
	{
		private static readonly AnalitoContinuo Suyo = Examenes.Dominio.Analito.Hemoglobina;

		internal Hemoglobina(DateTime fecha, decimal valor, string unidad)
			: base(fecha, Suyo, Medida(valor, unidad, Suyo))
		{
		}

		internal override string Nombre => Suyo.ParaMostrar;

		// Rango con piso Y techo, a diferencia del colesterol y el antigeno.
		//
		// Y DEPENDE DEL SEXO, que es el SEGUNDO de los tres efectos: este analito aplica a
		// cualquier persona —a diferencia del antigeno— y se compara contra otros umbrales.
		// La diferencia no es cosmetica: una hemoglobina de 12.2 es anemia leve en un
		// hombre y es normal en una mujer, y es exactamente el mismo numero.
		//
		// NULL CUANDO NO SE SABE EL SEXO, y no el rango masculino "mientras tanto". Ese
		// default silencioso es el que produce el peor resultado posible: una clasificacion
		// que se ve igual de firme que las demas, para la mitad de la poblacion, y esta mal.
		// Lo que el dominio hace en cambio es decir que no puede (ver Veredicto.RequiereSexo).
		//
		// ⚠️ Los cortes son los de uso corriente en adultos y son ORIENTATIVOS. Varian
		// ademas por embarazo y por altitud de residencia —el Valle Central esta por encima
		// de los 1.000 m— y ese ajuste necesita un dato que hoy nadie aporta: queda nombrado
		// y sin implementar, y entraria por Paciente igual que entro el sexo.
		internal override RangoDeReferencia RangoPara(Paciente paciente)
		{
			ArgumentNullException.ThrowIfNull(paciente);

			if (!paciente.SeSabeElSexo) return null;

			bool mayor = paciente.Edad >= 60;

			if (paciente.Sexo.EsElMismo(Sexo.Mujer))
				return mayor
					? RangoDeReferencia.Entre(11.5m, 15.5m, Suyo.Unidad)
					: RangoDeReferencia.Entre(12.0m, 16.0m, Suyo.Unidad);

			return mayor
				? RangoDeReferencia.Entre(12.5m, 16.5m, Suyo.Unidad)
				: RangoDeReferencia.Entre(13.0m, 17.0m, Suyo.Unidad);
		}

		internal override decimal CambioMaximoVerosimilPorDia => 0.1m;

		internal override decimal CambioMinimoSignificativo => 1.0m;

		internal override DireccionDeseable Direccion => DireccionDeseable.Mantenerse;

		// Anual, y semestral si hay anemia: cuando el valor ya esta bajo, lo que importa
		// es si sigue bajando, y eso no se ve esperando un ano.
		// La hemoglobina se mueve POCO: su rango entero -de 2 a 25- es un factor de doce,
		// y duplicarse ya es mucho. Es el analito con el corte mas bajo de los cuatro.
		internal override decimal MultiploQueNingunIntervaloJustifica => 2.5m;

		// Anual, y semestral SI SE SABE que hay anemia. Sin sexo no se sabe, y ahi la
		// periodicidad es la normal: inventar un control semestral por las dudas seria
		// decidir clinica con un dato que falta.
		internal override int MesesHastaElProximo(Paciente paciente) =>
			HayRangoPara(paciente) && GradoDeAnemia(paciente).HayAnemia ? 6 : 12;

		// ---------------------------------------------------------------- lo suyo

		// CUANTA anemia hay, que es distinto de "esta bajo". El rango dice si cruzo el
		// piso; esto dice cuanto, y de ahi sale que hacer.
		//
		// EL PISO LO PONE EL RANGO, y ese es el cambio: hasta ahora habia un 13.0 escrito
		// aca que era, sin decirlo, el piso masculino. O sea que el grado de anemia ya
		// dependia del sexo —del sexo supuesto— y ademas discrepaba del propio rango, que a
		// los 60 arranca en 12.5. Preguntandoselo al rango las dos cosas dicen lo mismo por
		// construccion y la dependencia queda declarada en un solo lugar.
		//
		// Los GRADOS —leve, moderada, severa— siguen en valores absolutos: son la escala de
		// profundidad de la anemia, no el umbral de que la haya, y esa gradacion gruesa no
		// se corre con el sexo.
		//
		// EXIGE SABER EL SEXO. Se alcanza solo despues de que la serie descarto el caso
		// "no se puede determinar"; ver SerieDeHemoglobina.
		internal GradoDeAnemia GradoDeAnemia(Paciente paciente)
		{
			// Un valor por encima del techo no es anemia: es otra cosa, y llamarla
			// "ninguna" es correcto porque de anemia no hay.
			if (EsNormalPara(paciente)) return Examenes.Dominio.GradoDeAnemia.Ninguna;

			decimal valor = Valor.Valor;
			if (valor > RangoPara(paciente).Maximo.Valor) return Examenes.Dominio.GradoDeAnemia.Ninguna;

			if (valor >= 11.0m) return Examenes.Dominio.GradoDeAnemia.Leve;
			if (valor >= 8.0m) return Examenes.Dominio.GradoDeAnemia.Moderada;
			return Examenes.Dominio.GradoDeAnemia.Severa;
		}

		internal override SerieContinua NuevaSerie() => new SerieDeHemoglobina();
	}
}
