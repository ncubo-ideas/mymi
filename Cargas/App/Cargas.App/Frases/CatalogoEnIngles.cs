using System.Collections.Generic;

namespace Cargas.App.Frases
{
	/// <summary>
	/// LA REDACCION EN INGLES, clave por clave la misma que la espanola. Se traduce el MARCO; los
	/// valores que entran en los huecos vienen del registro de la estacion y cruzan intactos.
	/// <para>
	/// Toda clave que exista aca tiene que existir alla, y al reves: una clave que falta se dice
	/// con su propio nombre entre llaves, y esa cadena no es vacia, tiene espacios y termina donde
	/// termina la frase que la envuelve, asi que ninguna otra comprobacion la ve.
	/// </para>
	/// </summary>
	internal static class CatalogoEnIngles
	{
		internal static IReadOnlyDictionary<string, string> ElCatalogo()
		{
			return new Dictionary<string, string>
			{
				["lectura.total-no-cuadra"] =
					"the total on your {fecha} fill-up does not match price times volume",
				["lectura.sin-tramo"] =
					"I still cannot work out your fuel economy: {falta} is missing",
				["lectura.dato-sospechoso"] =
					"one leg came out at {actual} against your usual {habitual}: you almost "
					+ "certainly filled up and it was never recorded",
				["lectura.no-comparable"] =
					"your latest leg was measured with {reserva}: I cannot compare it against the "
					+ "earlier ones",
				["lectura.indeterminado"] =
					"only one leg computed ({actual}): there is no series to compare against yet",
				["lectura.estable"] = "your car is doing {actual}, right at your usual level",
				["lectura.mejoro"] =
					"you were doing {habitual} and the latest leg came out at {actual}: {cambio}",
				["lectura.empeoro"] =
					"you were doing {habitual} and the latest leg came out at {actual}: {cambio}",

				["cambio.mas"] = "{porcentaje} more",
				["cambio.menos"] = "{porcentaje} less",
				["cambio.igual"] = "exactly the same",

				["falta.kilometraje-de-una-carga"] = "the odometer on your {fecha} fill-up",
				["falta.segunda-carga"] = "a second fill-up with a full tank and an odometer reading",
				["falta.segundo-lleno"] = "a second full tank with an odometer reading",

				["reserva.otro-surtidor"] = "a different pump",
				["reserva.otro-combustible"] = "a different fuel",
				["reserva.cambio-de-combustible-adentro"] = "a fuel change inside the leg",
				["reserva.varios-surtidores-adentro"] = "several pumps inside the leg",

				["accion.marco"] = "{que} {cuando}",
				["accion.anotar-el-kilometraje"] = "write down the odometer",
				["accion.confirmar-el-tanque"] = "confirm whether the tank ended up full",
				["accion.registrar-la-carga-que-falto"] = "record the fill-up that is missing",
				["accion.revisarlo-con-tu-taller"] = "have your shop look at it",
				["accion.mostrarle-esto-a-tu-estacion"] = "show this to your station",

				["plazo.proxima-carga"] = "on your next fill-up",
				["plazo.este-mes"] = "this month",
				["plazo.esta-semana"] = "this week",
				["plazo.pronto"] = "soon",
				["plazo.cuando-queras"] = "whenever you like",

				["ritmo.medido"] =
					"{porDia} {unidad} per day: {kilometros} {unidad} between {desde} and {hasta}, "
					+ "over {lecturas} you wrote down yourself",
				["ritmo.sin-lecturas"] = "you have not written down a single odometer reading yet",
				["ritmo.una-sola-lectura"] =
					"you wrote down a single odometer reading, and a rate needs two",
				["ritmo.lecturas-del-mismo-dia"] =
					"the odometer readings you wrote down are all from the same day, and a daily "
					+ "rate needs two different days",

				["completitud"] = "{conKilometraje} of {cargas} with an odometer reading · {tramos}",

				["faltante.pide"] =
					"I need {falta} on your {fecha} fill-up to work out {desbloqueo}",
				["faltante.supone"] = "{suposicion}. If that is right, we can work out {desbloqueo}",
				["faltante.falta.kilometraje"] = "the odometer",
				["faltante.falta.confirmacion-del-tanque"] = "the tank confirmation",
				["faltante.desbloquea.rendimiento-del-tramo"] =
					"how your car did over these {kilometros} km between {desde} and {hasta}",
				["faltante.desbloquea.tramo-abierto"] = "the leg that was left open",
				["faltante.desbloquea.rendimiento-hasta-la-siguiente"] =
					"how your car did between this fill-up and the next one",

				["suposicion.lleno"] = "{corte}, so we assume the tank ended up full",
				["suposicion.parcial"] = "{corte}, so we assume the tank did not end up full",

				["corte.automatico"] = "the pump shut off on its own",
				["corte.preseteado"] = "you bought a fixed amount",

				["boletos.sin-sorteo"] = "this fill-up is not in any draw",
				["boletos.fuera-de-las-fechas"] =
					"this fill-up is from before or after every draw I know about",
				["boletos.otra-moneda"] =
					"the draw gives tickets in {monedaDeLaRegla} and this fill-up was paid in {moneda}",
				["boletos.falta-un-dato"] =
					"for this fill-up I see {emitidos} and the draw rule gives {regla}: something "
					+ "is probably still on its way",
				["boletos.no-alcanzo"] =
					"this fill-up did not reach a ticket: {sobrante} short of the first one",
				["boletos.dio-boletos"] = "this fill-up gave {emitidos}",
				["boletos.dio-boletos-y-sobro"] =
					"this fill-up gave {emitidos}; {sobrante} was left over, and it does not carry over",

				["sorteo.sin-anunciar"] = "{boletos} for a draw the station has not announced yet",
				["sorteo.ya-se-rifo"] = "{boletos} for the {fecha} draw, which has already been drawn",
				["sorteo.es-hoy"] = "{boletos} for the {fecha} draw, today",
				["sorteo.faltan-dias"] = "{boletos} for the {fecha} draw, in {dias}",

				["gasto"] = "{monto} over {cargas}",
				["costo"] = "{monto} per km, over {kilometros} km",

				["tramo.calculado"] = "{kilometros} km / {litros} L = {rendimiento} km/L",
				["tramo.bloqueado"] = "blocked: the odometer on your {fecha} fill-up is missing",

				["kilometraje.conOrigen"] = "{kilometraje} ({origen})",
				["tanque.conOrigen"] = "{tanque} ({origen})",

				["tanque.lleno"] = "full",
				["tanque.parcial"] = "partial",

				["origen.observado"] = "observed",
				["origen.estimado"] = "estimated",
				["origen.confirmado"] = "confirmed",

				["etiqueta.estable"] = "steady",
				["etiqueta.mejoro"] = "improved",
				["etiqueta.empeoro"] = "got worse",
				["etiqueta.indeterminado"] = "undetermined",
				["etiqueta.sin-tramo"] = "no leg yet",
				["etiqueta.no-comparable"] = "not comparable",
				["etiqueta.dato-sospechoso"] = "suspect reading",
				["etiqueta.total-no-cuadra"] = "the total does not match",

				["etiqueta.informativo"] = "for information",
				["etiqueta.vigilar"] = "keep an eye on it",
				["etiqueta.consulte-pronto"] = "look into it soon",
				["etiqueta.urgente"] = "urgent",

				["etiqueta.medido"] = "measured",
				["etiqueta.sin-lecturas"] = "no readings",
				["etiqueta.una-sola-lectura"] = "a single reading",
				["etiqueta.lecturas-del-mismo-dia"] = "readings from the same day",

				["etiqueta.observado"] = "observed",
				["etiqueta.estimado"] = "estimated",
				["etiqueta.confirmado"] = "confirmed",

				["etiqueta.lleno"] = "full",
				["etiqueta.parcial"] = "partial",

				["etiqueta.automatico"] = "automatic shut-off",
				["etiqueta.preseteado"] = "preset amount",
				["etiqueta.indeterminado-corte"] = "undetermined",

				["etiqueta.base"] = "base",
				["etiqueta.plata"] = "silver",
				["etiqueta.oro"] = "gold",
			};
		}
	}
}
