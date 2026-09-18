using System.Collections.Generic;

namespace Cargas.App.Frases
{
	/// <summary>
	/// LA REDACCION EN ESPANOL, que es la que el dominio traia escrita adentro hasta que la frase
	/// se partio en clave y valores. Palabra por palabra la misma, salvo los acentos que el
	/// dominio no podia llevar: mover la redaccion no es reescribirla.
	/// <para>
	/// Cada clave es una FRASE ENTERA y no un pedazo. El espanol concuerda en genero y el ingles
	/// no, asi que un catalogo que pegara fragmentos daria concordancias rotas que ninguna prueba
	/// ve. Los huecos llevan valores del registro, que cruzan sin traducirse.
	/// </para>
	/// <para>
	/// Las claves <c>etiqueta.*</c> son los DICHOS DE LOS CONJUNTOS CERRADOS. Ninguno se leyo del
	/// registro: los elige el dominio de una lista cerrada, y en pantalla son indistinguibles a
	/// ojo de un nombre de surtidor que si vino de la estacion. El codigo queda intacto -- es
	/// identidad, y con el se decide que ofrecer -- y esto es lo que se lee.
	/// </para>
	/// </summary>
	internal static class CatalogoEnEspanol
	{
		internal static IReadOnlyDictionary<string, string> ElCatalogo()
		{
			return new Dictionary<string, string>
			{
				["lectura.total-no-cuadra"] =
					"el total de tu carga del {fecha} no cuadra con precio por volumen",
				["lectura.sin-tramo"] =
					"todavía no puedo calcular tu rendimiento: falta {falta}",
				["lectura.dato-sospechoso"] =
					"un tramo dio {actual} contra tu nivel de siempre de {habitual}: casi seguro "
					+ "cargaste y no quedó registrada",
				["lectura.no-comparable"] =
					"tu último tramo se midió con {reserva}: no te lo puedo comparar con los "
					+ "anteriores",
				["lectura.indeterminado"] =
					"un solo tramo calculado ({actual}): todavía no hay serie para comparar",
				["lectura.estable"] = "tu carro rinde {actual}, tu nivel de siempre",
				["lectura.mejoro"] =
					"venías rindiendo {habitual} y el último tramo dio {actual}: {cambio}",
				["lectura.empeoro"] =
					"venías rindiendo {habitual} y el último tramo dio {actual}: {cambio}",

				["cambio.mas"] = "un {porcentaje} más",
				["cambio.menos"] = "un {porcentaje} menos",
				["cambio.igual"] = "exactamente lo mismo",

				["falta.kilometraje-de-una-carga"] = "el kilometraje de tu carga del {fecha}",
				["falta.segunda-carga"] = "una segunda carga con tanque lleno y kilometraje",
				["falta.segundo-lleno"] = "un segundo tanque lleno con kilometraje",

				["reserva.otro-surtidor"] = "otro surtidor",
				["reserva.otro-combustible"] = "otro combustible",
				["reserva.cambio-de-combustible-adentro"] = "cambio de combustible dentro del tramo",
				["reserva.varios-surtidores-adentro"] = "varios surtidores dentro del tramo",

				["accion.marco"] = "{que} {cuando}",
				["accion.anotar-el-kilometraje"] = "anotá el kilometraje",
				["accion.confirmar-el-tanque"] = "confirmá si el tanque quedó lleno",
				["accion.registrar-la-carga-que-falto"] = "registrá la carga que faltó",
				["accion.revisarlo-con-tu-taller"] = "revisalo con tu taller",
				["accion.mostrarle-esto-a-tu-estacion"] = "mostrále esto a tu estación",

				["plazo.proxima-carga"] = "en tu próxima carga",
				["plazo.este-mes"] = "este mes",
				["plazo.esta-semana"] = "esta semana",
				["plazo.pronto"] = "pronto",
				["plazo.cuando-queras"] = "cuando querás",

				["ritmo.medido"] =
					"{porDia} {unidad} por día: {kilometros} {unidad} entre el {desde} y el "
					+ "{hasta}, sobre {lecturas} que anotaste vos",
				["ritmo.sin-lecturas"] = "todavía no anotaste ningún kilometraje",
				["ritmo.una-sola-lectura"] =
					"anotaste un solo kilometraje, y un ritmo necesita dos",
				["ritmo.lecturas-del-mismo-dia"] =
					"los kilometrajes que anotaste son todos del mismo día, y un ritmo por día "
					+ "necesita dos días distintos",

				["completitud"] = "{conKilometraje} de {cargas} con kilometraje · {tramos}",

				["faltante.pide"] = "necesito {falta} de tu carga del {fecha} para calcular {desbloqueo}",
				["faltante.supone"] = "{suposicion}. Si es así, podemos calcular {desbloqueo}",
				["faltante.falta.kilometraje"] = "el kilometraje",
				["faltante.falta.confirmacion-del-tanque"] = "la confirmación del tanque",
				["faltante.desbloquea.rendimiento-del-tramo"] =
					"cuánto rindió tu carro en estos {kilometros} km entre el {desde} y el {hasta}",
				["faltante.desbloquea.tramo-abierto"] = "el tramo que quedó abierto",
				["faltante.desbloquea.rendimiento-hasta-la-siguiente"] =
					"cuánto rindió tu carro entre esta carga y la siguiente",

				["suposicion.lleno"] = "{corte}, así que suponemos que el tanque quedó lleno",
				["suposicion.parcial"] = "{corte}, así que suponemos que el tanque no quedó lleno",

				["corte.automatico"] = "el surtidor cortó solo",
				["corte.preseteado"] = "cargaste un monto fijo",

				["boletos.sin-sorteo"] = "esta carga no entra en ningún sorteo",
				["boletos.fuera-de-las-fechas"] =
					"esta carga es de antes o de después de los sorteos que conozco",
				["boletos.otra-moneda"] =
					"el sorteo da boletos por {monedaDeLaRegla} y esta carga fue en {moneda}",
				["boletos.falta-un-dato"] =
					"por esta carga veo {emitidos} y la regla del sorteo da {regla}: probablemente "
					+ "falta un dato por llegar",
				["boletos.no-alcanzo"] =
					"esta carga no alcanzó para un boleto: faltaron {sobrante} para el primero",
				["boletos.dio-boletos"] = "esta carga dio {emitidos}",
				["boletos.dio-boletos-y-sobro"] =
					"esta carga dio {emitidos}; sobraron {sobrante}, que no se acumulan",

				["sorteo.sin-anunciar"] = "{boletos} de un sorteo que la estación todavía no anunció",
				["sorteo.ya-se-rifo"] = "{boletos} para el sorteo del {fecha}, que ya se rifó",
				["sorteo.es-hoy"] = "{boletos} para el sorteo del {fecha}, hoy",
				["sorteo.faltan-dias"] = "{boletos} para el sorteo del {fecha}, en {dias}",

				["gasto"] = "{monto} en {cargas}",
				["costo"] = "{monto} por km, sobre {kilometros} km",

				["tramo.calculado"] = "{kilometros} km / {litros} L = {rendimiento} km/L",
				["tramo.bloqueado"] = "bloqueado: falta el kilometraje de tu carga del {fecha}",

				["kilometraje.conOrigen"] = "{kilometraje} ({origen})",
				["tanque.conOrigen"] = "{tanque} ({origen})",

				["tanque.lleno"] = "lleno",
				["tanque.parcial"] = "parcial",

				["origen.observado"] = "observado",
				["origen.estimado"] = "estimado",
				["origen.confirmado"] = "confirmado",

				["etiqueta.estable"] = "estable",
				["etiqueta.mejoro"] = "mejoró",
				["etiqueta.empeoro"] = "empeoró",
				["etiqueta.indeterminado"] = "indeterminado",
				["etiqueta.sin-tramo"] = "sin tramo",
				["etiqueta.no-comparable"] = "no comparable",
				["etiqueta.dato-sospechoso"] = "dato sospechoso",
				["etiqueta.total-no-cuadra"] = "el total no cuadra",

				["etiqueta.informativo"] = "informativo",
				["etiqueta.vigilar"] = "vigilar",
				["etiqueta.consulte-pronto"] = "consultá pronto",
				["etiqueta.urgente"] = "urgente",

				["etiqueta.medido"] = "medido",
				["etiqueta.sin-lecturas"] = "sin lecturas",
				["etiqueta.una-sola-lectura"] = "una sola lectura",
				["etiqueta.lecturas-del-mismo-dia"] = "lecturas del mismo día",

				["etiqueta.observado"] = "observado",
				["etiqueta.estimado"] = "estimado",
				["etiqueta.confirmado"] = "confirmado",

				["etiqueta.lleno"] = "lleno",
				["etiqueta.parcial"] = "parcial",

				["etiqueta.automatico"] = "corte automático",
				["etiqueta.preseteado"] = "monto preseteado",
				["etiqueta.indeterminado-corte"] = "sin determinar",

				["etiqueta.base"] = "base",
				["etiqueta.plata"] = "plata",
				["etiqueta.oro"] = "oro",
			};
		}
	}
}
