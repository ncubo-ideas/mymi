window.TextoDeLaCara = window.TextoDeLaCara || {};
window.TextoDeLaCara['es-CR'] = {
  "//": "LO QUE ESTA CARA DICE CON SUS PROPIAS PALABRAS, en espanol. Las frases que compone el dominio NO estan aca: esas llegan ya dichas por la superficie, que recibe la etiqueta del idioma como un parametro mas de la vista. Casi todo este archivo esta COPIADO de Municipalidad.Wasm/wwwroot/texto/es-CR.json para que las dos pantallas digan lo mismo; lo poco que es de esta cara va bajo 'cara' y 'pie'. El espanol es de Costa Rica y es voseo: no lo pases a tuteo. Los nombres de los rubros y los periodos son del registro de la municipalidad y no se traducen. {clave} es un hueco que el codigo llena.",

  "idioma": "es",

  "app": {
    "apunte": "Tu cuenta, en tu carpeta"
  },

  "comun": {
    "contestadoAl": "Contestado al {dia}.",
    "deTotal": "{cuantos} de {total}",
    "//dias": "EL MISMO PLURAL QUE Frases.Dias, del otro lado del puente: '1 dias' no aparece nunca, en ningun idioma.",
    "unDia": "1 día",
    "dias": "{dias} días"
  },

  "//etiqueta": "COMO SE DICE UN CODIGO DEL DOMINIO. El codigo es identidad y cruza el puente sin tocarse -- 'cambio-de-tarifa', 'hay-periodos-sin-comprobante' --; esto es lo que una persona lee. Estan copiadas de Frases/CatalogoEnEspanol.cs, que es donde las tiene la app suelta: la vista manda el codigo y no la etiqueta, asi que la unica manera de no mostrar el codigo crudo es tenerlas de este lado tambien.",
  "etiqueta": {
    "cambio-de-tarifa": "cambio de tarifa",
    "cambio-de-avaluo": "cambio de avalúo",
    "cambio-menor": "cambio menor",
    "sin-cambio": "sin cambio",
    "urgente": "urgente",
    "aviso": "aviso",
    "ninguna": "",
    "rubro-desconocido": "rubro desconocido",
    "sin-historia": "sin historia",
    "con-historia": "con historia",
    "sin-consulta": "sin consulta",
    "nada-sin-comprobante": "nada sin comprobante",
    "hay-periodos-sin-comprobante": "hay períodos sin comprobante"
  },

  "porQuePagoMas": {
    "titulo": "Por qué pago más",
    "bajada": "Entre los dos últimos comprobantes de cada rubro: de cuánto a cuánto cambió, y qué lo explica. Lo que explica un aumento depende de cómo se cobra ese rubro, y ésa es la mitad que el portal no contesta.",
    "umbrales": "Un cambio se llama salto cuando pasa el {salto}, y por debajo del {ruido} se considera ruido.",
    "periodos": "Períodos",
    "porPeriodo": "Por período",
    "elCambio": "El cambio",
    "sinVariacion": {
      "titulo": "Todavía no hay ninguna variación que ver.",
      "cuerpo": "Una variación necesita dos comprobantes del mismo rubro. Aparecen solas conforme tus pagos vayan llegando a esta app."
    },
    "finca": {
      "avaluo": "avalúo: {monto}",
      "sinAvaluo": "sin avalúo publicado"
    }
  },

  "queSeMeVence": {
    "titulo": "Qué se me vence",
    "bajada": "Ésta es la pregunta que tu portal no contesta. El portal te dice cuánto debés hoy; esto te dice qué está por vencerse y qué ya se venció.",
    "umbrales": "Se avisa {dias} antes del vencimiento, y el costo de pagar tarde se proyecta a una tasa anual del {tasa}.",
    "porVencer": "Por vencer",
    "nadaPorVencer": "Al {dia} no hay nada dentro de la ventana de aviso. Eso no dice que no tengás nada que pagar más adelante: dice que nada cae dentro de los próximos {dias}.",
    "vence": "Vence",
    "faltan": "Faltan",
    "monto": "Monto",
    "yaVencidos": "Ya vencidos",
    "nadaVencido": "No hay nada vencido sin pagar en lo que esta app ha visto. Eso no es una constancia de estar al día con la municipalidad: tu portal es la autoridad sobre eso.",
    "vencio": "Venció",
    "deAtraso": "De atraso",
    "proyeccionDeMora": "Proyección de mora",
    "noSeSuman": "Las dos listas no se suman",
    "noSeSumanCuerpo": "Un período por vencer y uno vencido son situaciones distintas, y un total que los mezcle no es un número que nadie haya calculado. Si querés saber cuánto debés en total, eso lo dice tu portal."
  },

  "cuantoApartar": {
    "titulo": "Cuánto apartar",
    "bajada": "Dos cosas de dos dueños: lo que <strong>esta app propone</strong> a partir de lo que ya pagaste, y lo que <strong>vos adoptaste</strong>. Que no coincidan no es un error.",
    "laPropuesta": "Lo que esta app propone",
    "proyeccionDe": "Proyección de {anio}",
    "porMes": "Por mes",
    "loAdoptado": "Lo que vos adoptaste",
    "adoptadoPorMes": "Adoptado por mes",
    "leFalta": "Le falta",
    "venceEsteMes": "Vence este mes",
    "umbral": "Se te avisa cuando lo adoptado queda más de un {umbral} por debajo de lo propuesto."
  },

  "sinComprobante": {
    "titulo": "Qué quedó sin comprobante",
    "tituloCorto": "Sin comprobante",
    "bajada": "Los períodos que tu portal da por pagados y de los que esta app nunca vio un recibo. Se dice, no se tapa: un hueco nombrado es una respuesta, y uno callado se ve igual que no tener huecos.",
    "sinConsulta": "Todavía no se puede cotejar",
    "sinConsultaCuerpo": "<strong>No poder cotejar no es estar en orden.</strong> Es una tercera respuesta, y cambia en cuanto entre una consulta al portal.",
    "cotejadoContra": "cotejado contra el {dia}",
    "noEsDeuda": "Esto no es una deuda",
    "noEsDeudaCuerpo": "Tu portal da esos períodos por saldados; lo que falta es el respaldo aquí, casi siempre porque se pagaron por un camino que no deja comprobante digital —la ventanilla, por ejemplo. No los sumés a nada que se te venza.",
    "fotoConFecha": "La consulta es una foto con fecha: el portal dice qué está pendiente hoy, y mañana dice otra cosa. El cotejo es contra el {dia} y no contra hoy."
  },

  "cuantoHePagado": {
    "titulo": "Cuánto he pagado",
    "bajada": "Lo que pagaste en un año, y la serie de comprobantes de cada rubro. El total trae la mora adentro, porque es lo que pagaste de verdad.",
    "pagadoEn": "Pagado en {anio}",
    "sinNingunComprobante": "Esta app todavía no ha visto ningún comprobante tuyo, así que no hay serie que mostrar y el total viene en cero. Llegan solos, con el resto de tus movimientos.",
    "anioSinComprobantes": "De {anio} esta app no tiene ningún comprobante. Un total en cero no es un año sin pagos: puede ser que los recibos de ese año todavía no hayan llegado aquí. Los años de los que sí hay están en la lista de arriba.",
    "incluyeLaMora": "El total incluye la mora",
    "incluyeLaMoraCuerpo": "Es lo que pagaste de verdad, no el precio del servicio. Cada línea trae la mora en su propia columna si querés verlas aparte, y no hay que restar un total de otro.",
    "mora": "Mora",
    "cubre": "Cubre",
    "porPeriodo": "Por período",
    "inferido": "inferido",
    "loQueSeInfirio": "Las líneas marcadas «inferido» las dedujo esta app",
    "loQueSeInfirioCuerpo": "Cuando una línea es la primera de un rubro no hay una anterior con que saber cuántos períodos cubre, así que esta app supone uno. Es una suposición suya y no un dato de tu recibo."
  },

  "queTanAlDia": {
    "titulo": "Qué tan al día está esto",
    "tituloCorto": "Qué tan al día",
    "bajada": "Hasta dónde leyó esta app lo que tu municipalidad publicó, y qué falta. Tu cuenta es híbrida: ella manda los cobros, los recibos y la mora; vos aportás cuánto querés apartar.",
    "hastaDondeLeyo": "Hasta dónde leyó",
    "ultimoHecho": "Último hecho leído",
    "rubrosConComprobante": "Rubros con algún comprobante",
    "ultimaConsulta": "Última consulta al portal",
    "ninguna": "ninguna",
    "queLlego": "Qué llegó de cada rubro",
    "conComprobantes": "con comprobantes",
    "sinComprobantes": "sin comprobantes",
    "rubroSinComprobantes": "Esta app no ha visto ningún comprobante de este rubro. Eso no quiere decir que no lo pagués: quiere decir que tus pagos de este rubro todavía no han llegado aquí.",
    "queFalta": "Qué falta",
    "noFaltaNada": "No falta ninguna de las dos mitades",
    "noFaltaNadaCuerpo": "Tu municipalidad publicó el avalúo de cada finca, hay una consulta al portal con que cotejar, y vos ya adoptaste una reserva. Eso no dice que no haya hechos nuevos sin traer: para eso está el número de arriba.",
    "faltaLaReserva": "Falta tu mitad: no has adoptado una reserva",
    "faltaLaReservaCuerpo": "Cuánto querés apartar por mes es lo único que tu municipalidad no puede saber, y es lo único que esta app te pide. Se adopta en",
    "faltaLaReservaEnlace": "cuánto apartar",
    "faltaUnaConsulta": "Falta una consulta al portal",
    "faltaUnaConsultaCuerpo": "Sin una consulta registrada no hay con qué cotejar lo que esta app sí vio pagar, así que",
    "faltaUnaConsultaCierre": "todavía no puede contestar. <strong>No poder cotejar no es estar en orden.</strong>",
    "faltaUnAvaluo": "Falta la mitad de tu municipalidad: un avalúo",
    "faltaUnAvaluoCuerpo": {
      "una": "Hay {cuantas} de {total} finca sin avalúo publicado. El avalúo lo fija ella y es el valor con que te cobra: sin él, esta app puede decir que tu cobro de bienes inmuebles subió por un avalúo nuevo, pero no de cuánto a cuánto. <strong>No hay nada que llenar</strong> — llega solo cuando ella lo publique.",
      "varias": "Hay {cuantas} de {total} fincas sin avalúo publicado. El avalúo lo fija ella y es el valor con que te cobra: sin él, esta app puede decir que tu cobro de bienes inmuebles subió por un avalúo nuevo, pero no de cuánto a cuánto. <strong>No hay nada que llenar</strong> — llega solo cuando ella lo publique."
    }
  },

  "arranque": {
    "traer": "Traer lo que haya publicado la institución",
    "trayendo": "Trayendo…"
  },

  "//cara": "LO QUE SOLO EXISTE EN ESTA CARA, y por eso no esta copiado de ningun lado: la app suelta no le pregunta a nadie -- ella ES la app --, y aca la pantalla le pide a Mymi.",
  "cara": {
    "preguntando": "Preguntándole a Mymi…",
    "sinNada": "Todavía no hay nada de tu cuenta en este dispositivo.",
    "adoptarNoEstaAqui": "Adoptar una reserva es una escritura, y esta pantalla sólo lee. Se adopta desde la app de la municipalidad."
  },

  "pie": {
    "explicacion": "El registro vive en este dispositivo. Esta pantalla no sale a la red: le pregunta a Mymi, y Mymi responde con el motor que le presta."
  },

  "puente": {
    "//": "LAS FRASES DEL PUENTE, que las dice el Host y hasta el 2026-09-18 escribia en espanol: el pie de una cara en ingles terminaba con «al dia, en 69 ms». El puente las lee de window.TextoDelPuente y cae a estas mismas en espanol si no estan. Las llaves y los huecos NO se traducen: son el cable.",
    "sinPuente": "esta página solo corre dentro de Mymi, en la forma híbrida: no hay puente, y sin puente no tiene a quién preguntarle",
    "sinPuenteCorto": "sin puente: esta pantalla no corre dentro de Mymi",
    "pidiendo": "pidiéndole a Mymi que traiga lo nuevo…",
    "alDia": "al día",
    "alDiaCon": "al día · {dicho}",
    "noAlcance": "no se pudo llegar a tu institución: revisá tu conexión y volvé a intentar",
    "noSePudo": "no se pudo traer lo nuevo: volvé a intentar en un momento"
  }
};
