window.TextoDeLaCara = window.TextoDeLaCara || {};
window.TextoDeLaCara['en'] = {
  "//": "WHAT THIS FACE SAYS IN ITS OWN WORDS, in English. The sentences the domain composes -paraElAsistente, the cadence, the severity- are NOT here: those arrive already said from the surface, which takes the language tag as one more parameter of the view. Where a sentence also exists in the standalone app, the value is COPIED from Compras.Wasm/wwwroot/texto/en.json under the very same key, so both screens say the same thing and so it is visible at a glance which is which. Keys are exactly the ones in es-CR.js. Product descriptions, branch names and receipt numbers come from the supermarket's record and are never translated; the supermarket's own name and tagline are not translated either -they are the institution's voice, which is why the <title> of index.html does not go through here. {clave} is a hole the code fills.",

  "idioma": "en",

  "comun": {
    "inicio": "Home",
    "compras": "Purchases",
    "sinCompras": "No purchases yet.",
    "compra": "purchase",
    "comprasPlural": "purchases",
    "fechaLarga": "{mes} {numero}, {anio}",
    "mesDeAnio": "{mes} {anio}",

    "mes": {
      "1": "Jan", "2": "Feb", "3": "Mar", "4": "Apr", "5": "May", "6": "Jun",
      "7": "Jul", "8": "Aug", "9": "Sep", "10": "Oct", "11": "Nov", "12": "Dec"
    },
    "mesLargo": {
      "1": "January", "2": "February", "3": "March", "4": "April", "5": "May", "6": "June",
      "7": "July", "8": "August", "9": "September", "10": "October", "11": "November",
      "12": "December"
    },
    "dia": {
      "lunes": "Monday", "martes": "Tuesday", "miercoles": "Wednesday", "jueves": "Thursday",
      "viernes": "Friday", "sabado": "Saturday", "domingo": "Sunday"
    }
  },

  "hero": {
    "//": "THE CAROUSEL. Same keys as the standalone app's ArmarHeroBanners.",
    "puedeInteresarte": "MIGHT INTEREST YOU",
    "nuevo": "NEW",
    "nuevoTitulo": "Something new arrived in your {producto}",
    "bajoDePrecio": "PRICE WENT DOWN",
    "bajoDePrecioTitulo": "The {producto} you always buy went down in price",
    "verlo": "See it",
    "asiVienen": "How your purchases are going",
    "casiTodoHabitualUno": "Mostly the usual · {cuantos} change of rhythm",
    "casiTodoHabitualVarios": "Mostly the usual · {cuantos} changes of rhythm",
    "todoComoSiempre": "Everything as usual here.",
    "bienvenida": "Welcome to La Guaria",
    "bienvenidaBajada": "Find out what always comes back and how your basket changes."
  },

  "inicio": {
    "saludo": "Hello!",
    "buscando": "Looking…",
    "buscarNuevas": "Look for new purchases",
    "compraTuya": "{cuantas} purchase of yours",
    "comprasTuyas": "{cuantas} purchases of yours",
    "desde": "since {cuando}",

    "hospedada": "<strong>It is being saved on this phone.</strong> Your record is yours and it is here: Mymi gave this supermarket a folder on your phone, and your assistant can read it from there.",

    "tusCompras": "Your purchases",
    "cambioReciente": "There is {cuantos} recent change",
    "cambiosRecientes": "There are {cuantos} recent changes",
    "todoComoSiempre": "Everything as usual",

    "cambioPill": "CHANGED",
    "cambioDesde": "changed since {mes}",

    "enQueSeLlena": "What fills your basket",
    "categoriaPrincipal": "Your main category accounts for",
    "categoriasPrincipales": "Your {cuantas} main categories account for",
    "representanEl": "{cuantas} <strong class=\"mk-carrito-cifra\">{porcentaje}</strong> of your purchases.",
    "otros": "Others",

    "tuHabitual": "YOUR REGULAR",
    "mejorPrecio": "BEST PRICE",
    "ahora": "Now",
    "menos": "{monto} less",

    "quizaTeToque": "You may be due",
    "sueleVolver": "usually comes back {cadencia}",
    "hace": "{dias} ago",
    "yMasFaltantes": "And {cuantos} more. Your assistant can give you the full list.",

    "tuMes": "Your month at the supermarket",
    "menosQueEn": "less than in {mes}",
    "masQueEn": "more than in {mes}",

    "loQueDejoDeEntrar": "What stopped coming in",
    "tusGarantias": "Your warranties",
    "cubreHasta": "covered until {fecha}",

    "ultimoDia": "last day",
    "validaHasta": "valid until {dia} the {numero}",
    "quedanDias": "{dias} to go"
  },

  "precios": {
    "titulo": "Your regulars"
  },

  "recibos": {
    "pestana": "Your purchases"
  },

  "ritmo": {
    "cada": "Every {dias}",
    "volves": "you go back to the supermarket",
    "mirando": "Looking at your last {compras} purchases."
  },

  "//plural": "WHAT IS NOT A CATALOGUE IN THE STANDALONE APP BUT CODE: Frases.Dias writes «1 day» or «N days» so that «1 days» never shows up. Here the word has to be written down somewhere, and this is that somewhere.",
  "plural": {
    "dia": "day",
    "dias": "days"
  },

  "//cara": "WHAT THIS SCREEN SAYS AND THE STANDALONE APP DOES NOT. These are not pending translations: they are labels of cards this face has and the app spreads over other screens -or the other way round, sentences the Host says in the app. They carry their own key so nobody mistakes them for a copy of the institution's catalogue.",
  "cara": {
    "preguntando": "Asking Mymi…",
    "pie": "The record lives on this device. This screen does not reach the network: it asks Mymi, and Mymi answers with the engine it lends.",

    "sinComprasAca": "No purchases on this device yet. Press {boton}.",
    "sinMesCerrado": "There is no closed month to look at yet.",

    "productosQueVuelven": "Products that always come back",
    "comprasMiradas": "Purchases looked at to know it",
    "mesesConCompras": "Months with purchases",

    "gastoDelMes": "Spending this month",
    "enComida": "On food",
    "enLoDemas": "On everything else",
    "contraElMesAnterior": "Against last month",
    "igual": "the same",

    "loQueSeMovio": "What moved",
    "preciosQueSeMovieron": "Prices that moved",
    "ritmosQueCambiaron": "Rhythms that changed",
    "nadaSeMovio": "Nothing moved: your prices and your rhythms come in the same.",
    "antes": "was {valor}",
    "solesCada": "usually every {dias}",

    "nadaPendiente": "Nothing pending: no list, no restocking, no warranty about to expire."
  },

  "puente": {
    "//": "THE BRIDGE'S PHRASES. Same keys as the Spanish catalogue: they are the cable, not copy.",
    "sinPuente": "this page only runs inside Mymi, in the hybrid form: there is no bridge, and without one it has no one to ask",
    "sinPuenteCorto": "no bridge: this screen is not running inside Mymi",
    "pidiendo": "asking Mymi to bring what is new…",
    "alDia": "up to date, in {ms} ms · {dicho}",
    "noAlcance": "could not reach your institution: check your connection and try again",
    "noSePudo": "could not bring what is new: try again in a moment"
  }
};
