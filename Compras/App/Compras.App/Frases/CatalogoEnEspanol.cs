using System.Collections.Generic;

namespace Compras.App.Frases
{
	/// <summary>
	/// LA REDACCION EN EL IDIOMA DEL REGISTRO. El espanol de este catalogo es el de Costa Rica y es
	/// voseo -- comprás, tenelo, hacé --: no lo pase a tuteo.
	/// <para>
	/// Los huecos llevan valores del registro del supermercado -- la descripcion de un producto, el
	/// nombre de una sucursal, el numero de un tiquete, el mes de una serie -- y esos NO se traducen en
	/// ningun idioma.
	/// </para>
	/// <para>
	/// ESTE MODULO NO JUZGA LA ALIMENTACION DE NADIE, y el catalogo es donde eso se puede romper de un
	/// adjetivo: una frase dice lo que paso y se calla. No hay consejos aca.
	/// </para>
	/// </summary>
	internal static class CatalogoEnEspanol
	{
		internal static IReadOnlyDictionary<string, string> ElCatalogo()
		{
			return new Dictionary<string, string>
			{
				["comun.yMas"] = "{lista} (y {resto} más)",

				["portada.nuevo"] = "NUEVO",
				["portada.nuevoTitulo"] = "Llegó algo nuevo de tus {producto}",
				["portada.bajoDePrecio"] = "BAJÓ DE PRECIO",
				["portada.bajoDePrecioTitulo"] = "El {producto} que comprás siempre bajó de precio",
				["portada.verlo"] = "Verlo",
				["portada.puedeInteresarte"] = "PUEDE INTERESARTE",
				["portada.promoSecundario"] = "{detalle} · {vigencia}",
				["portada.asiVienen"] = "Así vienen tus compras",
				["portada.casiTodoHabitual.uno"] = "Casi todo habitual · {cuantos} cambio de ritmo",
				["portada.casiTodoHabitual.varios"] = "Casi todo habitual · {cuantos} cambios de ritmo",
				["portada.todoComoSiempre"] = "Todo como siempre por acá.",
				["portada.bienvenida"] = "Bienvenida a La Guaria",
				["portada.bienvenidaBajada"] = "Descubrí lo que siempre vuelve y cómo cambia tu carrito.",
				["portada.ultimoDia"] = "último día",
				["portada.validaHasta"] = "válida hasta el {dia} {numero}",
				["portada.quedanDias"] = "quedan {dias}",
				["portada.dia.lunes"] = "lunes",
				["portada.dia.martes"] = "martes",
				["portada.dia.miercoles"] = "miércoles",
				["portada.dia.jueves"] = "jueves",
				["portada.dia.viernes"] = "viernes",
				["portada.dia.sabado"] = "sábado",
				["portada.dia.domingo"] = "domingo",

				["abastecimiento.atrasado"] =
					"la última vez que entró {mencion} a tu canasta fue {hace}, {cada}",
				["abastecimiento.al-dia"] = "{mencion} entró {hace}: al día",
				["abastecimiento.hace.meses"] = "hace {meses}",
				["abastecimiento.hace.dias"] = "hace {dias}",
				["abastecimiento.cada.medido"] = "y vos lo comprás cada {dias}",
				["abastecimiento.cada.catalogo"] = "y suele reponerse cada {dias}",

				["cambioDeRitmo.sin-ritmo"] =
					"todavía no hay suficientes compras de {producto} de los dos lados como para decir si "
					+ "cambió el ritmo",
				["cambioDeRitmo.cambio"] = "{producto} pasó de {antes} a {ahora}",
				["cambioDeRitmo.sigue"] = "{producto} sigue {ahora}",

				["cambioEnLaCanasta.sin-comparar"] =
					"todavía no hay compras de los dos lados con las que ver si algo cambió",
				["cambioEnLaCanasta.sin-cambio"] =
					"tu canasta viene igual: no entró ni se cayó nada de lo habitual",
				["cambioEnLaCanasta.entraron"] = "desde {mes} entraron {productos}",
				["cambioEnLaCanasta.salieron"] = "dejó de aparecer {productos}",

				["canastaFrenteAlPasado.sin-comparar"] =
					"ningún habitual tiene un precio observado de hace {meses} o más: todavía no hay con "
					+ "qué comparar",
				["canastaFrenteAlPasado.mas-caro"] =
					"comprar hoy una pieza de cada uno de tus {cuantos} habituales comparables cuesta "
					+ "{monto} más que hace {meses}",
				["canastaFrenteAlPasado.mas-barato"] =
					"comprar hoy una pieza de cada uno de tus {cuantos} habituales comparables cuesta "
					+ "{monto} menos que hace {meses}",
				["canastaFrenteAlPasado.igual"] =
					"comprar hoy una pieza de cada uno de tus {cuantos} habituales comparables cuesta lo "
					+ "mismo que hace {meses}",

				["comparacion.sin-comparacion"] = "todavía no hay dos meses con compras para comparar",
				["comparacion.totales"] =
					"en {mesDespues} llevás {gastoDespues} y en {mesAntes} fueron {gastoAntes}",
				["comparacion.repartida"] =
					"{totales}: la diferencia está repartida, sin un pasillo que la explique",
				["comparacion.explican"] = "la diferencia se explica sobre todo por {titulos}",
				["comparacion.bajaron.uno"] = "en cambio {titulos} bajó",
				["comparacion.bajaron.varios"] = "en cambio {titulos} bajaron",
				["comparacion.nuevo"] = "{titulo} (que no aparecía el otro mes)",

				["hallazgos.ninguna"] = "No hay ninguna compra con «{buscado}».",
				["hallazgos.que.uno"] = "«{producto}»",
				["hallazgos.que.varios"] = "{cuantos} productos que coinciden con «{buscado}»",
				["hallazgos.marco"] =
					"{que}: {compras}, {unidades} en total, {monto}. La última, el {fecha}.",

				["consumo.sin-consumo"] = "no hay compras de «{termino}» en el historial",
				["consumo.que.categoria"] = "{termino}",
				["consumo.que.texto"] = "«{termino}»",
				["consumo.un-solo-mes"] =
					"{que} aparece una sola vez, en {mes}, por {monto}: todavía no hay serie para decir "
					+ "para dónde viene",
				["consumo.tramo"] = "de {antes} en {primerMes} a {ahora} en {ultimoMes}",
				["consumo.cambio"] =
					"{que} pasó {tramo}, sobre {meses} con compras (el mes más alto fue {pico})",
				["consumo.se-mantuvo"] = "{que} se mantuvo {tramo}",

				["firma.sin-compras"] = "todavía no hay compras tuyas con las que ver un patrón",
				["firma.sin-patron"] =
					"en tus últimas {cuantas} compras no hay nada que se repita lo suficiente como para "
					+ "llamarlo tu firma",
				["firma.hay"] = "tu semana tiene una firma: {productos}",

				["proximaCompra.sin-historial"] =
					"todavía no hay compras suficientes para saber cada cuánto vuelve cada cosa",
				["proximaCompra.al-dia"] = "no hay nada atrasado: todo lo que solés llevar entró hace poco",
				["proximaCompra.tocan.uno"] =
					"1 producto de los que solés llevar ya pasó su tiempo: {productos}",
				["proximaCompra.tocan.varios"] =
					"{cuantos} productos de los que solés llevar ya pasaron su tiempo: {productos}",

				["vaAFaltar.toca"] =
					"{producto}: suele volver {cadencia} y hace {desdeLaUltima} que no entra",
				["vaAFaltar.no-toca"] = "{producto}: suele volver {cadencia}, le faltan {faltan}",

				["precio.sin-precios"] = "no hay compras de «{sku}» en el historial",
				["precio.una-sola-vez"] =
					"{producto} lo compraste una sola vez, en {mes}, a {monto} por {unidad}: todavía no "
					+ "hay con qué comparar",
				["precio.tramo"] = "de {antes} a {ahora} por {unidad}, entre {desde} y {hasta}",
				["precio.subio"] = "{producto} te subió {porcentaje}: {tramo}",
				["precio.bajo"] = "{producto} te bajó {porcentaje}: {tramo}",
				["precio.se-mantuvo"] = "{producto} se mantuvo {tramo}",

				["promocion"] = "{gancho}: {detalle}, válida hasta el {fecha}",

				["rebaja.hay"] = "{producto} hoy cuesta {monto} menos que en la compra {numero}",
				["rebaja.ninguna"] =
					"nada de la compra {numero} está hoy más barato que lo que pagaste",

				["sucursales.sin-datos.conCompras"] =
					"ninguna de las {compras} dice en qué sucursal fue: los tiquetes de ese entonces no "
					+ "traían el dato",
				["sucursales.sin-datos.sinCompras"] = "no hay compras en el historial",
				["sucursales.una-sola"] =
					"todas tus compras con sucursal ({compras}) fueron en {sucursal}{sinDato}",
				["sucursales.se-mueve"] =
					"comprás sobre todo en {sucursal} ({compras} de {total}); desde {desde} aparecés "
					+ "también en {otras}{sinDato}",
				["sucursales.sinDato"] = " ({compras} más viejas no dicen sucursal)",

				["garantia.no-encontrada"] =
					"no hay ninguna compra con garantía que coincida con «{buscado}»",
				["garantia.vencida"] = "la garantía de {producto} venció el {fecha}",
				["garantia.por-vencer"] =
					"a la garantía de {producto} le quedan {dias}: vence el {fecha}",
				["garantia.vigente"] =
					"{producto} tiene garantía hasta el {fecha} (la compraste el {compra})",

				["ritmoDeCompra.sin-ritmo"] =
					"todavía no hay con qué medir cada cuánto comprás {categoria}: se lee con los "
					+ "{catalogo} del catálogo",
				["ritmoDeCompra.un-intervalo"] =
					"solo hay un intervalo medido de {categoria}: cada {cada}",
				["ritmoDeCompra.medido"] =
					"{categoria} lo comprás cada {cada}, sobre {intervalos} intervalos (el más corto de "
					+ "{corto} y el más largo de {largo})",

				["ritmoDelSuper.sin-compras"] =
					"todavía no hay compras tuyas con las que ver cada cuánto vas al súper",
				["ritmoDelSuper.pocas-compras"] =
					"con {compras} todavía no alcanza para decir cada cuánto vas al súper",
				["ritmoDelSuper.medido"] =
					"solés ir al súper cada {cada}, y casi siempre un {dia} por la {momento}",

				["momento.fila"] = "{momento} · {compras}",
				["momento.manana"] = "mañana",
				["momento.tarde"] = "tarde",
				["momento.noche"] = "noche",

				["cadencia.semanal"] = "todas las semanas",
				["cadencia.semanal.compacta"] = "1 vez/semana",
				["cadencia.quincenal"] = "cada dos semanas",
				["cadencia.quincenal.compacta"] = "1 vez/quincena",
				["cadencia.mensual"] = "una vez al mes",
				["cadencia.mensual.compacta"] = "1 vez/mes",
				["cadencia.bimensual"] = "cada dos meses",
				["cadencia.bimensual.compacta"] = "1 vez/2 meses",
				["cadencia.esporadica"] = "de vez en cuando",
				["cadencia.esporadica.compacta"] = "de vez en cuando",

				["constancia.nunca-falla"] = "nunca falla",
				["constancia.casi-nunca-falta"] = "casi nunca falta",
				["constancia.va-y-viene"] = "va y viene",
				["constancia.de-vez-en-cuando"] = "de vez en cuando",

				["severidad.informativo"] = "informativo",
				["severidad.informativo.queHacer"] = "",
				["severidad.conviene-revisar"] = "conviene revisar",
				["severidad.conviene-revisar.queHacer"] = "tenelo a mano por si acaso",
				["severidad.antes-de-que-se-cierre"] = "antes de que se cierre",
				["severidad.antes-de-que-se-cierre.queHacer"] = "hacé el reclamo antes de que venza",

				["categoria.Proteinas.titulo"] = "proteínas",
				["categoria.Proteinas.mencion"] = "una proteína",
				["categoria.FrutasYVerduras.titulo"] = "frutas y verduras",
				["categoria.FrutasYVerduras.mencion"] = "una fruta o verdura",
				["categoria.LacteosYHuevos.titulo"] = "lácteos y huevos",
				["categoria.LacteosYHuevos.mencion"] = "un lácteo",
				["categoria.GranosYAbarrotes.titulo"] = "granos y abarrotes",
				["categoria.GranosYAbarrotes.mencion"] = "un grano o abarrote",
				["categoria.AceitesYGrasas.titulo"] = "aceites y grasas",
				["categoria.AceitesYGrasas.mencion"] = "un aceite o una grasa",
				["categoria.Azucarados.titulo"] = "dulces y postres",
				["categoria.Azucarados.mencion"] = "algo dulce",
				["categoria.Snacks.titulo"] = "snacks",
				["categoria.Snacks.mencion"] = "un snack",
				["categoria.Embutidos.titulo"] = "embutidos",
				["categoria.Embutidos.mencion"] = "un embutido",
				["categoria.ComidasPreparadas.titulo"] = "comidas preparadas",
				["categoria.ComidasPreparadas.mencion"] = "una comida preparada",
				["categoria.Mascotas.titulo"] = "mascotas",
				["categoria.Mascotas.mencion"] = "comida para la mascota",
				["categoria.Limpieza.titulo"] = "limpieza",
				["categoria.Limpieza.mencion"] = "algo de limpieza",
				["categoria.Electrohogar.titulo"] = "electrodomésticos",
				["categoria.Electrohogar.mencion"] = "un electrodoméstico",

				["dia.lunes"] = "lunes",
				["dia.martes"] = "martes",
				["dia.miercoles"] = "miércoles",
				["dia.jueves"] = "jueves",
				["dia.viernes"] = "viernes",
				["dia.sabado"] = "sábado",
				["dia.domingo"] = "domingo",

				["presentacion.unidad"] = "unidad",
				["presentacion.pieza"] = "{contenido} {simbolo}",
				["presentacion.paquete"] = "{piezas} u por paquete",
			};
		}
	}
}
