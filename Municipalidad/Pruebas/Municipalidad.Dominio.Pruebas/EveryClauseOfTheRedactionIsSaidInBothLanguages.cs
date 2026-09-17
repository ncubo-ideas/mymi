using System;
using System.Collections.Generic;
using ElCatalogoEnEspanol = Municipalidad.App.Frases.CatalogoEnEspanol;
using ElCatalogoEnIngles = Municipalidad.App.Frases.CatalogoEnIngles;
using LaRedaccion = Municipalidad.App.Frases.LaRedaccion;
using LasFrases = Municipalidad.App.Frases.Frases;
using NUnit.Framework;

namespace Municipalidad.Dominio.Pruebas
{
	// EL DEFECTO QUE ESTA FIXTURE EXISTE PARA IMPEDIR, y que EverySentenceTheDomainComposesNamesItsOwnSubject
	// NO PUEDE VER. Desde que la redaccion se partio en clave mas valores, Frases.Diga contesta
	// "{clave}" cuando la clave no esta en el catalogo. Esa cadena NO es vacia, SI tiene espacios, SI
	// termina donde termina la frase que la envuelve, y NO es igual a la mitad del otro idioma -- asi
	// que las cuatro afirmaciones de aquella fixture se ponen VERDES sobre una frase que lleva
	// "{variacion.cambio.mas}" adentro, a la vista de la persona.
	//
	// Y ES UN DEFECTO DE UNA SOLA LETRA: una clave que se agrega al catalogo en espanol y se olvida en
	// el ingles. El /// de CatalogoEnIngles afirmaba que una prueba lo cubria; ninguna lo hacia, y
	// estas cuatro son esa prueba.
	//
	// LAS CUATRO SE NECESITAN Y NINGUNA SOBRA:
	//
	//   - Comparar los dos juegos de claves atrapa la asimetria, INCLUSO donde la frase renderizada no
	//     la delata. El caso que solo esta atrapa: falta "etiqueta.urgente" en espanol, el respaldo
	//     devuelve el codigo, y el codigo ES la palabra "urgente" -- indistinguible del dicho correcto.
	//   - Renderizar cada clausula atrapa el hueco que quedo sin llenar, que la comparacion de claves
	//     no ve: la clave esta en los dos catalogos y el valor no se le paso.
	//   - Las etiquetas van aparte porque NO son frases: no llevan sujeto, no terminan en punto y una
	//     de ellas es vacia a proposito, asi que las afirmaciones de una frase no les aplican.
	//   - Y el inventario cierra el ciclo: una clausula nueva que nadie ejercite pone rojo aca, que es
	//     tan tarde como se descubre un script del DSL que nunca corrio.
	[TestFixture]
	public sealed class EveryClauseOfTheRedactionIsSaidInBothLanguages
	{
		private const decimal ElUmbralDeSalto = 0.25m;
		private const decimal ElRuido = 0.02m;
		private const decimal ElUmbralDeLaReserva = 0.10m;
		private const int LosDiasDeAviso = 7;

		// LOS DOS TRIMESTRES DEL SALTO DEL IBI, y el dia en que la municipalidad publico cada avaluo.
		private static readonly Periodo ElTrimestreAnterior = new Trimestre(2024, 1);
		private static readonly Periodo ElTrimestreActual = new Trimestre(2025, 1);

		// EL VENCIMIENTO CAE EL ULTIMO DIA DE SU PERIODO, asi que las tres urgencias se eligen moviendo
		// el dia de corte y no el cobro: mismo portador, tres respuestas.
		private static readonly DateTime ElDiaEnQueVence = new DateTime(2025, 10, 31);
		private static readonly DateTime UnDiaDentroDelAviso = new DateTime(2025, 10, 28);
		private static readonly DateTime UnDiaLejos = new DateTime(2025, 10, 1);

		// ------------------------------------------------------------------ 1 · los dos juegos de claves

		[Test]
		public void TheTwoCataloguesCarryExactlyTheSameKeys()
		{
			IReadOnlyDictionary<string, string> enEspanol = ElCatalogoEnEspanol.ElCatalogo();
			IReadOnlyDictionary<string, string> enIngles = ElCatalogoEnIngles.ElCatalogo();

			Assert.Multiple(() =>
			{
				Assert.That(LasQueLeFaltanA(enEspanol, enIngles), Is.Empty,
					"claves que el catalogo en espanol tiene y el ingles no: esas frases llegan con un "
					+ "{hueco} crudo a quien lea en ingles");
				Assert.That(LasQueLeFaltanA(enIngles, enEspanol), Is.Empty,
					"claves que el catalogo en ingles tiene y el espanol no: al reves, y peor, porque el "
					+ "espanol es el idioma del registro");
			});
		}

		// ------------------------------------------------------------------ 2 · cada clausula, los dos idiomas

		[Test]
		public void EveryClauseIsWrittenInBothLanguagesAndLeavesNoHole()
		{
			Assert.Multiple(() =>
			{
				foreach ((string caso, Func<LasFrases, string> redactar) in LasRedacciones())
				{
					string enEspanol = redactar(LasFrases.De("es"));
					string enIngles = redactar(LasFrases.De("en"));

					Assert.That(enEspanol, Is.Not.Null.And.Not.Empty, caso + ": no dice nada en espanol");
					Assert.That(enIngles, Is.Not.Null.And.Not.Empty, caso + ": no dice nada en ingles");

					// NINGUN HUECO SIN LLENAR, que es la marca de las dos fallas a la vez: una clave que
					// el catalogo no tiene sale como {clave}, y un valor que nadie paso deja su {hueco}.
					Assert.That(enEspanol, Does.Not.Contain("{").And.Not.Contain("}"),
						caso + ": queda un hueco sin llenar en espanol");
					Assert.That(enIngles, Does.Not.Contain("{").And.Not.Contain("}"),
						caso + ": queda un hueco sin llenar en ingles");

					Assert.That(enIngles, Is.Not.EqualTo(enEspanol),
						caso + ": dice lo mismo en los dos idiomas, asi que una de las dos mitades quedo "
						+ "sin traducir");
				}
			});
		}

		// ------------------------------------------------------------------ 3 · los nombres de estado

		// UN CONJUNTO CERRADO NO ES DATO DEL REGISTRO. Estos trece los elige el dominio de una lista
		// suya, no salieron de la municipalidad, y por eso se traducen -- al reves que el nombre de un
		// rubro, que viaja intacto. El codigo es identidad y cruza el wire sin tocarse; esto es lo que
		// una persona lee.
		private static readonly string[] LosCodigosDeEstado =
		{
			"cambio-de-tarifa", "cambio-de-avaluo", "cambio-menor", "sin-cambio",
			"urgente", "aviso", "ninguna",
			"rubro-desconocido", "sin-historia", "con-historia",
			"sin-consulta", "nada-sin-comprobante", "hay-periodos-sin-comprobante",
		};

		[Test]
		public void EveryStateLabelIsSaidInBothLanguages()
		{
			LasFrases enEspanol = LasFrases.De("es");
			LasFrases enIngles = LasFrases.De("en");

			Assert.Multiple(() =>
			{
				foreach (string codigo in LosCodigosDeEstado)
				{
					string es = LaRedaccion.Etiqueta(enEspanol, codigo);
					string en = LaRedaccion.Etiqueta(enIngles, codigo);

					// LA URGENCIA QUE NO APURA NO SE DICE CON UNA PALABRA, en ningun idioma: una etiqueta
					// que dijera "ninguna" en la pantalla seria un sello sobre una fila que no lo necesita.
					if (codigo == "ninguna")
					{
						Assert.That(es, Is.Empty, "ninguna: no se dice en espanol, a proposito");
						Assert.That(en, Is.Empty, "ninguna: tampoco en ingles");
						continue;
					}

					Assert.That(es, Is.Not.Empty, codigo + ": no se dice en espanol");
					Assert.That(en, Is.Not.Empty, codigo + ": no se dice en ingles");

					// EL RESPALDO DE Etiqueta ES EL CODIGO MISMO, asi que una etiqueta que falte se ve en
					// ingles como el slug en espanol. En espanol no se puede afirmar lo mismo: "urgente" y
					// "aviso" se dicen igual que su codigo, y esas dos las cubre la prueba de las claves.
					Assert.That(en, Is.Not.EqualTo(codigo),
						codigo + ": en ingles salio el codigo, asi que su etiqueta no esta en el catalogo");

					Assert.That(en, Is.Not.EqualTo(es),
						codigo + ": se dice igual en los dos idiomas");
				}
			});
		}

		// ------------------------------------------------------------------ 4 · el inventario

		// LO QUE ESTA FIXTURE EJERCITA, clave por clave. Es una afirmacion INDEPENDIENTE y no una copia
		// del catalogo: escrita aca, una clausula nueva obliga a pasar por este archivo, y quien pase
		// tiene al lado la lista de casos que la redactan. Sin esto, una clausula que nadie ejercite se
		// descubre tan tarde como un script del DSL que nunca corrio.
		private static readonly string[] LasClavesQueEstaPruebaEjercita =
		{
			"variacion.marco",
			"variacion.cambio.mas", "variacion.cambio.menos", "variacion.cambio.igual",
			"variacion.explicacion.cambio-de-tarifa", "variacion.explicacion.cambio-de-avaluo",
			"variacion.explicacion.cambio-menor", "variacion.explicacion.sin-cambio",

			"ley.por-unidad", "ley.sobre-base-imponible",

			"avaluo.con-avaluo-publicado", "avaluo.sin-finca", "avaluo.varias-fincas",
			"avaluo.sin-avaluo-publicado",

			"serie.rubro-desconocido", "serie.rubro-desconocido.sinNombre", "serie.sin-historia",
			"serie.con-historia.una", "serie.con-historia.varias",

			"cotejo.sin-consulta", "cotejo.nada-sin-comprobante",
			"cotejo.hay-periodos-sin-comprobante.uno", "cotejo.hay-periodos-sin-comprobante.varios",

			"periodoSinComprobante",
			"atraso",

			"vencimiento.marco",
			"vencimiento.apura.urgente", "vencimiento.apura.aviso", "vencimiento.apura.ninguna",

			"reservaPropuesta",

			"faltante.marco",
			"faltante.falta.cubre", "faltante.falta.falta",
			"faltante.aviso.avisa", "faltante.aviso.no-avisa",
			"faltante.vence.urgente", "faltante.vence.solo-vence", "faltante.vence.alcanza",

			"etiqueta.cambio-de-tarifa", "etiqueta.cambio-de-avaluo", "etiqueta.cambio-menor",
			"etiqueta.sin-cambio", "etiqueta.urgente", "etiqueta.aviso", "etiqueta.ninguna",
			"etiqueta.rubro-desconocido", "etiqueta.sin-historia", "etiqueta.con-historia",
			"etiqueta.sin-consulta", "etiqueta.nada-sin-comprobante",
			"etiqueta.hay-periodos-sin-comprobante",
		};

		[Test]
		public void NoKeyOfTheCatalogueIsLeftWithoutASentenceThatExercisesIt()
		{
			IReadOnlyDictionary<string, string> elCatalogo = ElCatalogoEnEspanol.ElCatalogo();

			List<string> sinEjercitar = new List<string>();
			foreach (KeyValuePair<string, string> cada in elCatalogo)
				if (Array.IndexOf(LasClavesQueEstaPruebaEjercita, cada.Key) < 0) sinEjercitar.Add(cada.Key);
			sinEjercitar.Sort(StringComparer.Ordinal);

			List<string> queYaNoExisten = new List<string>();
			foreach (string cada in LasClavesQueEstaPruebaEjercita)
				if (!elCatalogo.ContainsKey(cada)) queYaNoExisten.Add(cada);
			queYaNoExisten.Sort(StringComparer.Ordinal);

			Assert.Multiple(() =>
			{
				Assert.That(sinEjercitar, Is.Empty,
					"clausulas del catalogo que ninguna frase de esta fixture redacta: agregue el caso que "
					+ "las hace salir, y despues la clave a LasClavesQueEstaPruebaEjercita");
				Assert.That(queYaNoExisten, Is.Empty,
					"claves que esta fixture dice ejercitar y el catalogo ya no tiene");
			});
		}

		// ------------------------------------------------------------------ las redacciones

		// UN CASO POR CLAUSULA, y cada uno entra por el MISMO camino que la puerta: el portador del
		// dominio entrega su clave y sus valores, y App/Frases redacta. Ninguno arma la frase a mano.
		private static IEnumerable<(string Caso, Func<LasFrases, string> Redactar)> LasRedacciones()
		{
			// LAS TRES DIRECCIONES DEL CAMBIO. El mismo rubro por unidad en las tres, asi que lo unico
			// que se mueve entre ellas es el signo.
			yield return ("variacion · sube, y el rubro se cobra por unidad",
				f => LaFraseDe(LaVariacion(Rubro.Basura(), new Mes(2024, 3), 5000m, new Mes(2025, 3), 9000m,
					ElAvaluo(SinNingunaFinca())), f));

			yield return ("variacion · baja",
				f => LaFraseDe(LaVariacion(Rubro.Policia(), new Mes(2024, 3), 6000m, new Mes(2025, 3), 3000m,
					ElAvaluo(SinNingunaFinca())), f));

			yield return ("variacion · exactamente lo mismo",
				f => LaFraseDe(LaVariacion(Rubro.Policia(), new Mes(2024, 3), 6000m, new Mes(2025, 3), 6000m,
					ElAvaluo(SinNingunaFinca())), f));

			// LAS CUATRO EXPLICACIONES. Las dos primeras son el mismo +80 % con la ley del rubro como
			// unica diferencia, que es el caso que hace concreta la decision entera.
			yield return ("variacion · el salto lo explica la tarifa",
				f => LaFraseDe(ElSaltoDeBasura(), f));

			yield return ("variacion · el salto lo explica el avaluo, y la municipalidad lo publico",
				f => LaFraseDe(ElSaltoDelIbiCon(ConLosDosAvaluosPublicados()), f));

			yield return ("variacion · cambio menor",
				f => LaFraseDe(LaVariacion(Rubro.Policia(), new Mes(2024, 3), 6000m, new Mes(2025, 3), 6600m,
					ElAvaluo(SinNingunaFinca())), f));

			yield return ("variacion · sin cambio",
				f => LaFraseDe(LaVariacion(Rubro.Policia(), new Mes(2024, 3), 6000m, new Mes(2025, 3), 6006m,
					ElAvaluo(SinNingunaFinca())), f));

			// LAS TRES RAZONES POR LAS QUE EL AVALUO NO SE PUEDE DECIR. Las tres son saltos de un rubro
			// sobre base imponible: lo que cambia es que tiene la cuenta.
			yield return ("variacion · la municipalidad no publico el avaluo",
				f => LaFraseDe(ElSaltoDelIbiCon(SinAvaluoPublicado()), f));

			yield return ("variacion · la cuenta no tiene ninguna finca",
				f => LaFraseDe(ElSaltoDelIbiCon(SinNingunaFinca()), f));

			yield return ("variacion · el recibo no dice de cual finca es",
				f => LaFraseDe(ElSaltoDelIbiCon(ConVariasFincas()), f));

			// LA SERIE DE UN RUBRO. El plural y el nombre en blanco son dos claves suyas, no dos formas
			// de la misma: una frase que dijera "1 lineas" o "«» no es ningun rubro" ya estaria mal.
			yield return ("serie · el rubro no existe",
				f => LaFraseDe(EstadoDeLaSerie.De("cementerio", new List<LineaPagada>()), f));

			yield return ("serie · no se pidio ningun rubro",
				f => LaFraseDe(EstadoDeLaSerie.De("", new List<LineaPagada>()), f));

			yield return ("serie · el rubro existe y no tiene historia",
				f => LaFraseDe(EstadoDeLaSerie.De("parques-y-zonas-verdes", new List<LineaPagada>()), f));

			yield return ("serie · una sola linea",
				f => LaFraseDe(EstadoDeLaSerie.De(Rubro.Basura().Nombre, LineasDeBasura(1)), f));

			yield return ("serie · varias lineas",
				f => LaFraseDe(EstadoDeLaSerie.De(Rubro.Basura().Nombre, LineasDeBasura(2)), f));

			// EL COTEJO. Las dos que traen cero periodos son las que una caratula confunde mas facil, y
			// por eso las dos estan.
			yield return ("cotejo · no hay consulta de ese dia",
				f => LaFraseDe(ElCotejo(hayConsulta: false, cuantos: 0), f));

			yield return ("cotejo · la consulta no destapa nada",
				f => LaFraseDe(ElCotejo(hayConsulta: true, cuantos: 0), f));

			yield return ("cotejo · destapa un periodo",
				f => LaFraseDe(ElCotejo(hayConsulta: true, cuantos: 1), f));

			yield return ("cotejo · destapa varios",
				f => LaFraseDe(ElCotejo(hayConsulta: true, cuantos: 2), f));

			yield return ("un periodo que el portal da por pagado y no tiene recibo",
				f => LaRedaccion.PeriodoSinComprobante(f,
					UnPeriodoSinComprobante().EtiquetaDelPeriodo, UnPeriodoSinComprobante().NombreDelRubro));

			yield return ("un atraso, con su proyeccion de mora",
				f => LaFraseDe(UnAtraso(), f));

			// LAS TRES URGENCIAS DE UN VENCIMIENTO, elegidas moviendo el dia de corte sobre el MISMO
			// cobro: el dia en que vence, un dia dentro de la ventana de aviso, y uno lejos.
			yield return ("vencimiento · vence hoy",
				f => LaFraseDe(UnVencimiento(), ElDiaEnQueVence, f));

			yield return ("vencimiento · dentro de la ventana de aviso",
				f => LaFraseDe(UnVencimiento(), UnDiaDentroDelAviso, f));

			yield return ("vencimiento · todavia no apura",
				f => LaFraseDe(UnVencimiento(), UnDiaLejos, f));

			yield return ("lo que la app propone apartar",
				f => LaRedaccion.ReservaPropuesta(f, UnaPropuesta().Anio,
					UnaPropuesta().ProyeccionDelAnio, UnaPropuesta().MontoMensual));

			// LAS TRES CLAUSULAS DEL FALTANTE CAMBIAN POR SU CUENTA, asi que hacen falta cuatro casos
			// para verlas todas: cubre trae consigo no-avisa, y solo-vence exige faltar sin avisar.
			yield return ("faltante · lo adoptado cubre la propuesta",
				f => LaFraseDe(new FaltanteDeReserva(12000m, 12000m, 5000m), f));

			yield return ("faltante · falta, se avisa, y lo que vence alcanza",
				f => LaFraseDe(new FaltanteDeReserva(12000m, 8000m, 5000m), f));

			yield return ("faltante · falta, se avisa, y lo que vence lo vuelve urgente",
				f => LaFraseDe(new FaltanteDeReserva(12000m, 8000m, 20000m), f));

			yield return ("faltante · falta poco para avisar, y aun asi vence de mas",
				f => LaFraseDe(new FaltanteDeReserva(12000m, 11500m, 20000m), f));
		}

		// ------------------------------------------------------------------ los portadores

		private static Variacion LaVariacion(Rubro rubro, Periodo antes, decimal montoAntes,
			Periodo despues, decimal montoDespues, EstadoDelAvaluo elAvaluo)
		{
			return Variacion.Entre(
				new LineaPagada(rubro, antes, montoAntes, 0m, 1, false),
				new LineaPagada(rubro, despues, montoDespues, 0m, 1, false),
				ElUmbralDeSalto, ElRuido, elAvaluo);
		}

		private static Variacion ElSaltoDeBasura()
		{
			return LaVariacion(Rubro.Basura(), new Mes(2024, 3), 5000m, new Mes(2025, 3), 9000m,
				ElAvaluo(SinNingunaFinca()));
		}

		private static Variacion ElSaltoDelIbiCon(EstadoDelAvaluo elAvaluo)
		{
			return LaVariacion(Rubro.BienesInmuebles(), ElTrimestreAnterior, 20000m,
				ElTrimestreActual, 36000m, elAvaluo);
		}

		private static EstadoDelAvaluo ElAvaluo(EstadoDelAvaluo cual) { return cual; }

		private static EstadoDelAvaluo SinNingunaFinca()
		{
			return EstadoDelAvaluo.De(ElTrimestreAnterior, ElTrimestreActual, new List<Finca>());
		}

		private static EstadoDelAvaluo SinAvaluoPublicado()
		{
			return EstadoDelAvaluo.De(ElTrimestreAnterior, ElTrimestreActual,
				new List<Finca> { new Finca("F-1") });
		}

		private static EstadoDelAvaluo ConVariasFincas()
		{
			return EstadoDelAvaluo.De(ElTrimestreAnterior, ElTrimestreActual,
				new List<Finca> { new Finca("F-1"), new Finca("F-2") });
		}

		private static EstadoDelAvaluo ConLosDosAvaluosPublicados()
		{
			Finca laFinca = new Finca("F-1");
			laFinca.FijarAvaluo(new ValorDeFinca(new DateTime(2023, 1, 15), 18500000m));
			laFinca.FijarAvaluo(new ValorDeFinca(new DateTime(2025, 1, 20), 33300000m));

			return EstadoDelAvaluo.De(ElTrimestreAnterior, ElTrimestreActual,
				new List<Finca> { laFinca });
		}

		private static IReadOnlyList<LineaPagada> LineasDeBasura(int cuantas)
		{
			List<LineaPagada> lineas = new List<LineaPagada>();
			for (int cual = 0; cual < cuantas; cual++)
				lineas.Add(new LineaPagada(Rubro.Basura(), new Mes(2024, 3 + cual), 5000m, 0m, 1, false));

			return lineas;
		}

		private static EstadoDeLosPeriodosSinComprobante ElCotejo(bool hayConsulta, int cuantos)
		{
			List<PeriodoSinComprobante> periodos = new List<PeriodoSinComprobante>();
			for (int cual = 0; cual < cuantos; cual++)
				periodos.Add(new PeriodoSinComprobante(Rubro.Basura(), new Mes(2024, 7 + cual)));

			return EstadoDeLosPeriodosSinComprobante.De(new DateTime(2025, 9, 3), hayConsulta, periodos);
		}

		private static PeriodoSinComprobante UnPeriodoSinComprobante()
		{
			return new PeriodoSinComprobante(Rubro.Basura(), new Mes(2024, 7));
		}

		private static Atraso UnAtraso()
		{
			return new Atraso(new Cobro(Rubro.Basura(), new Mes(2025, 9), 8000m, new DateTime(2025, 9, 10)));
		}

		private static VencimientoProximo UnVencimiento()
		{
			return new VencimientoProximo(
				new Cobro(Rubro.Basura(), new Mes(2025, 10), 8000m, new DateTime(2025, 9, 10)));
		}

		private static ReservaPropuesta UnaPropuesta()
		{
			return new ReservaPropuesta(2025, 144000m);
		}

		// ------------------------------------------------------------------ el camino de la puerta

		private static string LaFraseDe(Variacion v, LasFrases f)
		{
			return LaRedaccion.Variacion(f,
				v.NombreDelRubro, v.EtiquetaDelPeriodoAnterior, v.EtiquetaDelPeriodoActual,
				v.MontoAnteriorPorPeriodo, v.MontoActualPorPeriodo, v.Proporcion,
				v.ClaveDelCambio, v.Etiqueta, v.NombreDeLaLey,
				v.EtiquetaDelAvaluo, v.NumeroDeLaFincaDelAvaluo, v.CuantasFincasDelAvaluo,
				v.LosAvaluosDelSalto);
		}

		private static string LaFraseDe(EstadoDeLaSerie e, LasFrases f)
		{
			return LaRedaccion.EstadoDeLaSerie(f, e.Etiqueta, e.NombreDelRubro, e.CuantasLineas,
				string.Join(", ", Rubro.LosQueHay()));
		}

		private static string LaFraseDe(EstadoDeLosPeriodosSinComprobante e, LasFrases f)
		{
			return LaRedaccion.Cotejo(f, e.Etiqueta, e.DiaDeLaConsulta, e.CuantosPeriodos);
		}

		private static string LaFraseDe(Atraso a, LasFrases f)
		{
			DateTime alDia = new DateTime(2025, 10, 5);
			return LaRedaccion.Atraso(f, a.EtiquetaDelPeriodo, a.NombreDelRubro, f.Dia(a.Vencio),
				a.Monto, a.DiasDeAtraso(alDia), alDia, 0.062m, a.CostoDePagarTarde(alDia, 0.062m));
		}

		private static string LaFraseDe(VencimientoProximo v, DateTime alDia, LasFrases f)
		{
			return LaRedaccion.Vencimiento(f, v.EtiquetaDelPeriodo, v.NombreDelRubro, f.Dia(v.Vence),
				v.Monto, v.Urgencia(alDia, LosDiasDeAviso).Etiqueta, v.DiasQueFaltan(alDia), alDia,
				LosDiasDeAviso);
		}

		private static string LaFraseDe(FaltanteDeReserva el, LasFrases f)
		{
			return LaRedaccion.Faltante(f,
				el.PropuestoPorMes, el.DeclaradoPorMes,
				el.ClaveDeLoQueFalta, el.Faltante, el.Proporcion,
				el.ClaveDelAviso(ElUmbralDeLaReserva), ElUmbralDeLaReserva,
				el.ClaveDeLoQueVence(ElUmbralDeLaReserva), el.VenceEsteMes);
		}

		private static List<string> LasQueLeFaltanA(
			IReadOnlyDictionary<string, string> estas, IReadOnlyDictionary<string, string> aquellas)
		{
			List<string> faltan = new List<string>();
			foreach (KeyValuePair<string, string> cada in estas)
				if (!aquellas.ContainsKey(cada.Key)) faltan.Add(cada.Key);

			faltan.Sort(StringComparer.Ordinal);
			return faltan;
		}
	}
}
