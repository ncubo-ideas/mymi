window.TextoDeLaCara = window.TextoDeLaCara || {};
window.TextoDeLaCara['es-CR'] = {
  "//": "LO QUE ESTA CARA DICE CON SUS PROPIAS PALABRAS, en espanol. Las frases que compone el dominio -paraElAsistente, la cadencia, la severidad- NO estan aca: esas llegan ya dichas por la superficie, que recibe la etiqueta del idioma como un parametro mas de la vista. Donde una frase existe igual en la app suelta, el valor esta COPIADO de Compras.Wasm/wwwroot/texto/es-CR.json y con su misma clave, para que las dos pantallas digan lo mismo y para que se vea de un vistazo cual es cual. El espanol es de Costa Rica y es voseo -comprás, apretá-: no lo pases a tuteo. Las descripciones de los productos, los nombres de las sucursales y los numeros de tiquete son del registro del supermercado y no se traducen; el nombre del super y su bajada tampoco, que son voz de la institucion y por eso el <title> de index.html no pasa por aca. {clave} es un hueco que el codigo llena.",

  "idioma": "es",

  "comun": {
    "inicio": "Inicio",
    "compras": "Compras",
    "sinCompras": "Todavía no hay compras.",
    "compra": "compra",
    "comprasPlural": "compras",
    "fechaLarga": "{numero} de {mes} de {anio}",
    "mesDeAnio": "{mes} de {anio}",

    "mes": {
      "1": "ene", "2": "feb", "3": "mar", "4": "abr", "5": "may", "6": "jun",
      "7": "jul", "8": "ago", "9": "sep", "10": "oct", "11": "nov", "12": "dic"
    },
    "mesLargo": {
      "1": "enero", "2": "febrero", "3": "marzo", "4": "abril", "5": "mayo", "6": "junio",
      "7": "julio", "8": "agosto", "9": "setiembre", "10": "octubre", "11": "noviembre",
      "12": "diciembre"
    },
    "dia": {
      "lunes": "lunes", "martes": "martes", "miercoles": "miércoles", "jueves": "jueves",
      "viernes": "viernes", "sabado": "sábado", "domingo": "domingo"
    }
  },

  "hero": {
    "//": "EL CARRUSEL. Las mismas claves que ArmarHeroBanners de la app suelta: la superficie manda los ingredientes -que producto, cuantos ritmos- y la frase se arma aca. `casiTodoHabitual` esta aplanado a `Uno`/`Varios` porque este catalogo no anida el plural.",
    "puedeInteresarte": "PUEDE INTERESARTE",
    "nuevo": "NUEVO",
    "nuevoTitulo": "Llegó algo nuevo de tus {producto}",
    "bajoDePrecio": "BAJÓ DE PRECIO",
    "bajoDePrecioTitulo": "El {producto} que comprás siempre bajó de precio",
    "verlo": "Verlo",
    "asiVienen": "Así vienen tus compras",
    "casiTodoHabitualUno": "Casi todo habitual · {cuantos} cambio de ritmo",
    "casiTodoHabitualVarios": "Casi todo habitual · {cuantos} cambios de ritmo",
    "todoComoSiempre": "Todo como siempre por acá.",
    "bienvenida": "Bienvenida a La Guaria",
    "bienvenidaBajada": "Descubrí lo que siempre vuelve y cómo cambia tu carrito."
  },

  "inicio": {
    "saludo": "¡Hola!",
    "buscando": "Buscando…",
    "buscarNuevas": "Buscar compras nuevas",
    "compraTuya": "{cuantas} compra tuya",
    "comprasTuyas": "{cuantas} compras tuyas",
    "desde": "desde {cuando}",

    "hospedada": "<strong>Se está guardando en este teléfono.</strong> Tu registro es tuyo y está acá: Mymi le dio a este supermercado una carpeta en tu teléfono, y tu asistente la puede leer desde ahí.",

    "tusCompras": "Tus compras",
    "cambioReciente": "Hay {cuantos} cambio reciente",
    "cambiosRecientes": "Hay {cuantos} cambios recientes",
    "todoComoSiempre": "Todo como siempre",

    "cambioPill": "CAMBIÓ",
    "cambioDesde": "cambió desde {mes}",

    "enQueSeLlena": "En qué se llena tu carrito",
    "categoriaPrincipal": "Tu categoría principal representa",
    "categoriasPrincipales": "Tus {cuantas} categorías principales representan",
    "representanEl": "{cuantas} el <strong class=\"mk-carrito-cifra\">{porcentaje}</strong> de tus compras.",
    "otros": "Otros",

    "tuHabitual": "TU HABITUAL",
    "mejorPrecio": "MEJOR PRECIO",
    "ahora": "Ahora",
    "menos": "{monto} menos",

    "quizaTeToque": "Quizá ya te toque",
    "sueleVolver": "suele volver {cadencia}",
    "hace": "hace {dias}",
    "yMasFaltantes": "Y {cuantos} más. Tu asistente te puede dar la lista completa.",

    "tuMes": "Tu mes en el súper",
    "menosQueEn": "menos que en {mes}",
    "masQueEn": "más que en {mes}",

    "loQueDejoDeEntrar": "Lo que dejó de entrar",
    "tusGarantias": "Tus garantías",
    "cubreHasta": "te cubre hasta el {fecha}",

    "ultimoDia": "último día",
    "validaHasta": "válida hasta el {dia} {numero}",
    "quedanDias": "quedan {dias}"
  },

  "precios": {
    "titulo": "Tus habituales"
  },

  "recibos": {
    "pestana": "Tus compras"
  },

  "ritmo": {
    "cada": "Cada {dias}",
    "volves": "volvés al súper",
    "mirando": "Mirando tus últimas {compras} compras."
  },

  "//plural": "LO QUE EN LA APP SUELTA NO ES CATALOGO SINO CODIGO: Frases.Dias escribe «1 día» o «N días» para que «1 días» no aparezca nunca. Aca la palabra tiene que estar escrita en algun lado, y este es el lado.",
  "plural": {
    "dia": "día",
    "dias": "días"
  },

  "//cara": "LO QUE ESTA PANTALLA DICE Y LA APP SUELTA NO. No son traducciones pendientes: son rotulos de tarjetas que esta cara tiene y la app reparte en otras pantallas -o al reves, frases que en la app las dice el Host. Van con clave propia para que nadie las confunda con una copia del catalogo de la institucion.",
  "cara": {
    "preguntando": "Preguntándole a Mymi…",
    "pie": "El registro vive en este dispositivo. Esta pantalla no sale a la red: le pregunta a Mymi, y Mymi responde con el motor que le presta.",

    "sinComprasAca": "Todavía no hay compras en este dispositivo. Apretá {boton}.",
    "sinMesCerrado": "Todavía no hay un mes cerrado que mirar.",

    "productosQueVuelven": "Productos que siempre vuelven",
    "comprasMiradas": "Compras miradas para saberlo",
    "mesesConCompras": "Meses con compras",

    "gastoDelMes": "Gasto del mes",
    "enComida": "En comida",
    "enLoDemas": "En lo demás",
    "contraElMesAnterior": "Contra el mes anterior",
    "igual": "igual",

    "loQueSeMovio": "Lo que se movió",
    "preciosQueSeMovieron": "Precios que se movieron",
    "ritmosQueCambiaron": "Ritmos que cambiaron",
    "nadaSeMovio": "Nada se movió: tus precios y tus ritmos vienen iguales.",
    "antes": "antes {valor}",
    "solesCada": "solés cada {dias}",

    "nadaPendiente": "Nada pendiente: ni lista, ni reposiciones, ni garantías por vencer."
  }
};
