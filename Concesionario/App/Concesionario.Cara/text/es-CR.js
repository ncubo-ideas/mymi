window.FaceText = window.FaceText || {};
window.FaceText['es-CR'] = {
  "//": "LO QUE ESTA CARA DICE CON SUS PROPIAS PALABRAS, en espanol. Las frases que compone el DOMINIO no estan aca -- el `says` de una operacion, el de una estadia, el de la placa --: esas llegan ya dichas por la superficie, que recibe la etiqueta del idioma como un parametro mas de la vista. Donde una frase existe igual en la app suelta, el valor esta copiado de Concesionario.Wasm/wwwroot/text/es-CR.json y la clave es la misma, para que las dos pantallas digan lo mismo y se pueda verificar leyendo. El bloque `board` esta copiado de Wording/TheSpanishCatalogue.cs, que es el mismo diccionario con que el dominio nombra sus conjuntos cerrados. El bloque `face` es lo unico que esta cara dice y la app suelta no. El espanol es de Costa Rica y es voseo: no lo pases a tuteo. El modelo, el VIN y la placa son del registro del concesionario y no se traducen.",

  "language": "es",

  "home": {
    "kicker": "Servicio al propietario",
    "sub": "Tu concesionario lleva este registro y es el único que lo cambia. Aquí lo leés.",
    "stateTitle": "Estado de tu vehículo",
    "healthWhat": "operaciones al día de las que tu concesionario programó",
    "overdueOne": "{count} vencido",
    "overdueMany": "{count} vencidos",
    "dueOne": "{count} te toca ahora",
    "dueMany": "{count} te tocan ahora",
    "missed": "{count} sin hacer",
    "upToDate": "{count} al día",
    "nextKicker": "Lo que sigue",
    "nothingPending": "Nada pendiente",
    "allGood": "Tu carro está al día",
    "saysLabel": "Lo que dice tu registro",
    "nothingDue": "Ninguna de las {count} operaciones que tu concesionario programó está vencida ni te toca ahora.",
    "odometerTile": "han recorrido juntos",
    "readingTile": "última lectura que envió tu concesionario",
    "noReading": "sin lectura",
    "glance": "Un vistazo a tu carro",
    "refreshFetching": "Trayendo…",
    "refresh": "Traer lo que haya nuevo",
    "headlineAttention": "Necesita atención",
    "headlineSomething": "Algo por hacer",
    "headlineOk": "Al día",
    "greetingAttention": "{car} necesita atención",
    "greetingSomething": "{car} tiene algo por hacer",
    "greetingOk": "{car} está al día",
    "yourCar": "Tu carro",
    "yourModel": "Tu {model}"
  },

  "vehicle": {
    "rowVin": "VIN",
    "rowPlate": "Placa"
  },

  "history": {
    "staysNoneTitle": "Ninguna estadía registrada en este periodo"
  },

  "board": {
    "operation": {
      "Inspection": "Revisión",
      "FluidChange": "Cambio de líquidos",
      "FilterChange": "Cambio de filtro",
      "WearPartReplacement": "Piezas de desgaste",
      "TimedComponent": "Componente por tiempo",
      "SoftwareUpdate": "Actualización de software",
      "RecallRemedy": "Llamado a revisión",
      "Diagnostic": "Diagnóstico",
      "Repair": "Reparación",

      "short": {
        "Inspection": "Revisión",
        "FluidChange": "Líquidos",
        "FilterChange": "Filtro",
        "WearPartReplacement": "Desgaste",
        "TimedComponent": "Por tiempo",
        "SoftwareUpdate": "Software",
        "RecallRemedy": "Llamado",
        "Diagnostic": "Diagnóstico",
        "Repair": "Reparación"
      }
    },

    "dueState": {
      "Overdue": "Vencido",
      "Due": "Toca ahora",
      "DueSoon": "Se acerca",
      "NotDue": "Al día",
      "Deferred": "Pospuesto",
      "Completed": "Realizado",
      "Missed": "No se hizo",
      "Superseded": "Reemplazado"
    }
  },

  "day": {
    "short": "{day} {month} {year}"
  },

  "month": {
    "short": {
      "1": "ene", "2": "feb", "3": "mar", "4": "abr", "5": "may", "6": "jun",
      "7": "jul", "8": "ago", "9": "set", "10": "oct", "11": "nov", "12": "dic"
    }
  },

  "face": {
    "//": "LO QUE SOLO DICE ESTA CARA. Ninguna de estas frases tiene equivalente en la pantalla de la app suelta: o hablan de Mymi -- que en la app suelta no existe -- o nombran una seccion que aqui esta y alla vive en otra pestaña.",
    "asking": "Preguntándole a Mymi…",
    "nothingHereYet": "Todavía no hay nada de tu carro en este dispositivo.",
    "pressRefresh": "Apretá «{refresh}» para que Mymi traiga lo que publicó tu concesionario.",
    "nothingScheduled": "Tu concesionario todavía no programó operaciones para este vehículo.",
    "noOperations": "Todavía no hay operaciones programadas para este vehículo.",
    "thisYear": "Lo que pasó este año",
    "explanation": "El registro vive en este dispositivo. Esta pantalla no sale a la red: le pregunta a Mymi, y Mymi responde con el motor que le presta."
  },

  "puente": {
    "//": "LAS FRASES DEL PUENTE, que las dice el Host y hasta el 2026-09-18 escribia en espanol: el pie de una cara en ingles terminaba con «al dia, en 69 ms». El puente las lee de window.TextoDelPuente y cae a estas mismas en espanol si no estan. Las llaves y los huecos NO se traducen: son el cable.",
    "sinPuente": "esta página solo corre dentro de Mymi, en la forma híbrida: no hay puente, y sin puente no tiene a quién preguntarle",
    "sinPuenteCorto": "sin puente: esta pantalla no corre dentro de Mymi",
    "pidiendo": "pidiéndole a Mymi que traiga lo nuevo…",
    "alDia": "al día, en {ms} ms · {dicho}",
    "noSePudo": "no se pudo traer lo nuevo ({ms} ms): {porque}",
    "tiro": "pull tiró: {porque}"
  }
};
