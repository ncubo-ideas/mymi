using System.Collections.Generic;

namespace Compras.App.Frases
{
	/// <summary>
	/// LA REDACCION EN INGLES. Las claves son exactamente las del catalogo en espanol, y una
	/// comprobacion lo afirma: una clave que existe en uno y no en el otro es una frase que llega a
	/// medias sin que nada avise.
	/// <para>
	/// Los huecos llevan valores del registro del supermercado -- la descripcion de un producto, el
	/// nombre de una sucursal, el numero de un tiquete, el mes de una serie -- y esos NO se traducen en
	/// ningun idioma. Una pantalla en ingles que diga "leche" esta bien: es el registro de la persona.
	/// </para>
	/// </summary>
	internal static class CatalogoEnIngles
	{
		internal static IReadOnlyDictionary<string, string> ElCatalogo()
		{
			return new Dictionary<string, string>
			{
				["comun.yMas"] = "{lista} (and {resto} more)",

				["portada.nuevo"] = "NEW",
				["portada.nuevoTitulo"] = "Something new arrived in your {producto}",
				["portada.bajoDePrecio"] = "PRICE WENT DOWN",
				["portada.bajoDePrecioTitulo"] = "The {producto} you always buy went down in price",
				["portada.verlo"] = "See it",
				["portada.puedeInteresarte"] = "MIGHT INTEREST YOU",
				["portada.promoSecundario"] = "{detalle} · {vigencia}",
				["portada.asiVienen"] = "How your purchases are going",
				["portada.casiTodoHabitual.uno"] = "Mostly the usual · {cuantos} change of rhythm",
				["portada.casiTodoHabitual.varios"] = "Mostly the usual · {cuantos} changes of rhythm",
				["portada.todoComoSiempre"] = "Everything as usual here.",
				["portada.bienvenida"] = "Welcome to La Guaria",
				["portada.bienvenidaBajada"] = "Find out what always comes back and how your basket changes.",
				["portada.ultimoDia"] = "last day",
				["portada.validaHasta"] = "valid until {dia} the {numero}",
				["portada.quedanDias"] = "{dias} to go",
				["portada.dia.lunes"] = "Monday",
				["portada.dia.martes"] = "Tuesday",
				["portada.dia.miercoles"] = "Wednesday",
				["portada.dia.jueves"] = "Thursday",
				["portada.dia.viernes"] = "Friday",
				["portada.dia.sabado"] = "Saturday",
				["portada.dia.domingo"] = "Sunday",

				["abastecimiento.atrasado"] =
					"the last time {mencion} went into your basket was {hace}, {cada}",
				["abastecimiento.al-dia"] = "{mencion} went in {hace}: up to date",
				["abastecimiento.hace.meses"] = "{meses} ago",
				["abastecimiento.hace.dias"] = "{dias} ago",
				["abastecimiento.cada.medido"] = "and you buy it every {dias}",
				["abastecimiento.cada.catalogo"] = "and it usually comes back every {dias}",

				["cambioDeRitmo.sin-ritmo"] =
					"there are still not enough purchases of {producto} on both sides to say whether the "
					+ "rhythm changed",
				["cambioDeRitmo.cambio"] = "{producto} went from {antes} to {ahora}",
				["cambioDeRitmo.sigue"] = "{producto} is still {ahora}",

				["cambioEnLaCanasta.sin-comparar"] =
					"there are still no purchases on both sides to see whether anything changed",
				["cambioEnLaCanasta.sin-cambio"] =
					"your basket is the same: nothing usual came in and nothing dropped out",
				["cambioEnLaCanasta.entraron"] = "since {mes} {productos} came in",
				["cambioEnLaCanasta.salieron"] = "{productos} stopped showing up",

				["canastaFrenteAlPasado.sin-comparar"] =
					"no regular has a price observed {meses} ago or more: there is nothing to compare "
					+ "against yet",
				["canastaFrenteAlPasado.mas-caro"] =
					"buying one of each of your {cuantos} comparable regulars today costs {monto} more "
					+ "than {meses} ago",
				["canastaFrenteAlPasado.mas-barato"] =
					"buying one of each of your {cuantos} comparable regulars today costs {monto} less "
					+ "than {meses} ago",
				["canastaFrenteAlPasado.igual"] =
					"buying one of each of your {cuantos} comparable regulars today costs the same as "
					+ "{meses} ago",

				["comparacion.sin-comparacion"] =
					"there are still no two months with purchases to compare",
				["comparacion.totales"] =
					"in {mesDespues} it comes to {gastoDespues} and in {mesAntes} it was {gastoAntes}",
				["comparacion.repartida"] =
					"{totales}: the difference is spread out, with no one aisle explaining it",
				["comparacion.explican"] = "the difference is explained mostly by {titulos}",
				["comparacion.bajaron.uno"] = "while {titulos} went down",
				["comparacion.bajaron.varios"] = "while {titulos} went down",
				["comparacion.nuevo"] = "{titulo} (which did not show up in the other month)",

				["hallazgos.ninguna"] = "There is no purchase with «{buscado}».",
				["hallazgos.que.uno"] = "«{producto}»",
				["hallazgos.que.varios"] = "{cuantos} products matching «{buscado}»",
				["hallazgos.marco"] =
					"{que}: {compras}, {unidades} in total, {monto}. The last one, on {fecha}.",

				["consumo.sin-consumo"] = "there are no purchases of «{termino}» in the record",
				["consumo.que.categoria"] = "{termino}",
				["consumo.que.texto"] = "«{termino}»",
				["consumo.un-solo-mes"] =
					"{que} shows up just once, in {mes}, for {monto}: there is no series yet to say which "
					+ "way it is going",
				["consumo.tramo"] = "from {antes} in {primerMes} to {ahora} in {ultimoMes}",
				["consumo.cambio"] =
					"{que} went {tramo}, over {meses} with purchases (the highest month was {pico})",
				["consumo.se-mantuvo"] = "{que} held {tramo}",

				["firma.sin-compras"] = "there are still no purchases of yours to see a pattern in",
				["firma.sin-patron"] =
					"in your last {cuantas} purchases there is nothing that repeats enough to call it "
					+ "your signature",
				["firma.hay"] = "your week has a signature: {productos}",

				["proximaCompra.sin-historial"] =
					"there are still not enough purchases to know how often each thing comes back",
				["proximaCompra.al-dia"] =
					"nothing is overdue: everything you usually buy came in recently",
				["proximaCompra.tocan.uno"] =
					"1 product of the ones you usually buy is past its time: {productos}",
				["proximaCompra.tocan.varios"] =
					"{cuantos} products of the ones you usually buy are past their time: {productos}",

				["vaAFaltar.toca"] =
					"{producto}: usually comes back {cadencia} and it has been {desdeLaUltima} since it "
					+ "last did",
				["vaAFaltar.no-toca"] =
					"{producto}: usually comes back {cadencia}, {faltan} to go",

				["precio.sin-precios"] = "there are no purchases of «{sku}» in the record",
				["precio.una-sola-vez"] =
					"you bought {producto} just once, in {mes}, at {monto} per {unidad}: there is nothing "
					+ "to compare against yet",
				["precio.tramo"] = "from {antes} to {ahora} per {unidad}, between {desde} and {hasta}",
				["precio.subio"] = "{producto} went up {porcentaje} for you: {tramo}",
				["precio.bajo"] = "{producto} went down {porcentaje} for you: {tramo}",
				["precio.se-mantuvo"] = "{producto} held {tramo}",

				["promocion"] = "{gancho}: {detalle}, valid until {fecha}",

				["rebaja.hay"] = "{producto} costs {monto} less today than on purchase {numero}",
				["rebaja.ninguna"] =
					"nothing from purchase {numero} is cheaper today than what was paid for it",

				["sucursales.sin-datos.conCompras"] =
					"none of the {compras} says which branch it was at: the receipts back then did not "
					+ "carry that field",
				["sucursales.sin-datos.sinCompras"] = "there are no purchases in the record",
				["sucursales.una-sola"] =
					"all of your purchases with a branch ({compras}) were at {sucursal}{sinDato}",
				["sucursales.se-mueve"] =
					"you buy mostly at {sucursal} ({compras} out of {total}); since {desde} you also show "
					+ "up at {otras}{sinDato}",
				["sucursales.sinDato"] = " ({compras} older ones do not say which branch)",

				["garantia.no-encontrada"] =
					"there is no purchase under warranty matching «{buscado}»",
				["garantia.vencida"] = "the warranty on {producto} expired on {fecha}",
				["garantia.por-vencer"] =
					"the warranty on {producto} has {dias} left: it expires on {fecha}",
				["garantia.vigente"] =
					"{producto} is under warranty until {fecha} (you bought it on {compra})",

				["ritmoDeCompra.sin-ritmo"] =
					"there is still nothing to measure how often you buy {categoria}: it is read with "
					+ "the {catalogo} from the catalogue",
				["ritmoDeCompra.un-intervalo"] =
					"there is only one measured interval for {categoria}: every {cada}",
				["ritmoDeCompra.medido"] =
					"you buy {categoria} every {cada}, over {intervalos} intervals (the shortest {corto} "
					+ "and the longest {largo})",

				["ritmoDelSuper.sin-compras"] =
					"there are still no purchases of yours to see how often you go to the supermarket",
				["ritmoDelSuper.pocas-compras"] =
					"with {compras} it is still not enough to say how often you go to the supermarket",
				["ritmoDelSuper.medido"] =
					"you usually go to the supermarket every {cada}, and almost always on a {dia} in the "
					+ "{momento}",

				["momento.fila"] = "{momento} · {compras}",
				["momento.manana"] = "morning",
				["momento.tarde"] = "afternoon",
				["momento.noche"] = "evening",

				["cadencia.semanal"] = "every week",
				["cadencia.semanal.compacta"] = "1×/week",
				["cadencia.quincenal"] = "every two weeks",
				["cadencia.quincenal.compacta"] = "1×/fortnight",
				["cadencia.mensual"] = "once a month",
				["cadencia.mensual.compacta"] = "1×/month",
				["cadencia.bimensual"] = "every two months",
				["cadencia.bimensual.compacta"] = "1×/2 months",
				["cadencia.esporadica"] = "now and then",
				["cadencia.esporadica.compacta"] = "now and then",

				["constancia.nunca-falla"] = "never misses",
				["constancia.casi-nunca-falta"] = "hardly ever misses",
				["constancia.va-y-viene"] = "comes and goes",
				["constancia.de-vez-en-cuando"] = "now and then",

				["severidad.informativo"] = "for information",
				["severidad.informativo.queHacer"] = "",
				["severidad.conviene-revisar"] = "worth a look",
				["severidad.conviene-revisar.queHacer"] = "keep it handy just in case",
				["severidad.antes-de-que-se-cierre"] = "before it closes",
				["severidad.antes-de-que-se-cierre.queHacer"] = "file the claim before it expires",

				["categoria.Proteinas.titulo"] = "proteins",
				["categoria.Proteinas.mencion"] = "a protein",
				["categoria.FrutasYVerduras.titulo"] = "fruit and vegetables",
				["categoria.FrutasYVerduras.mencion"] = "a fruit or vegetable",
				["categoria.LacteosYHuevos.titulo"] = "dairy and eggs",
				["categoria.LacteosYHuevos.mencion"] = "a dairy item",
				["categoria.GranosYAbarrotes.titulo"] = "grains and staples",
				["categoria.GranosYAbarrotes.mencion"] = "a grain or staple",
				["categoria.AceitesYGrasas.titulo"] = "oils and fats",
				["categoria.AceitesYGrasas.mencion"] = "an oil or a fat",
				["categoria.Azucarados.titulo"] = "sweets and desserts",
				["categoria.Azucarados.mencion"] = "something sweet",
				["categoria.Snacks.titulo"] = "snacks",
				["categoria.Snacks.mencion"] = "a snack",
				["categoria.Embutidos.titulo"] = "cold cuts",
				["categoria.Embutidos.mencion"] = "a cold cut",
				["categoria.ComidasPreparadas.titulo"] = "prepared meals",
				["categoria.ComidasPreparadas.mencion"] = "a prepared meal",
				["categoria.Mascotas.titulo"] = "pets",
				["categoria.Mascotas.mencion"] = "pet food",
				["categoria.Limpieza.titulo"] = "cleaning",
				["categoria.Limpieza.mencion"] = "something for cleaning",
				["categoria.Electrohogar.titulo"] = "home appliances",
				["categoria.Electrohogar.mencion"] = "a home appliance",

				["dia.lunes"] = "Monday",
				["dia.martes"] = "Tuesday",
				["dia.miercoles"] = "Wednesday",
				["dia.jueves"] = "Thursday",
				["dia.viernes"] = "Friday",
				["dia.sabado"] = "Saturday",
				["dia.domingo"] = "Sunday",

				["presentacion.unidad"] = "unit",
				["presentacion.pieza"] = "{contenido} {simbolo}",
				["presentacion.paquete"] = "{piezas} u per pack",
			};
		}
	}
}
