window.TextoDeLaCara = window.TextoDeLaCara || {};
window.TextoDeLaCara['en'] = {
  "//": "WHAT THIS FACE SAYS IN ITS OWN WORDS, in English. The sentences the domain composes are NOT here: those arrive already said from the surface, which takes the language tag as one more parameter of the view. Almost all of this file is COPIED from Municipalidad.Wasm/wwwroot/texto/en.json so that both screens say the same thing; the little that belongs to this face lives under 'cara' and 'pie'. Charge names and period labels come from the municipality's record and are not translated. {clave} is a hole the code fills.",

  "idioma": "en",

  "app": {
    "apunte": "Your account, in your folder"
  },

  "comun": {
    "contestadoAl": "Answered as of {dia}.",
    "deTotal": "{cuantos} of {total}",
    "//dias": "THE SAME PLURAL AS Frases.Dias, across the bridge: '1 days' never appears, in either language.",
    "unDia": "1 day",
    "dias": "{dias} days"
  },

  "//etiqueta": "HOW A DOMAIN CODE IS SAID. The code is identity and crosses the bridge untouched -- 'cambio-de-tarifa', 'hay-periodos-sin-comprobante' --; this is what a person reads. These are copied from Frases/CatalogoEnIngles.cs, where the standalone app keeps them: the view sends the code and not the label, so having them on this side too is the only way not to show the raw code.",
  "etiqueta": {
    "cambio-de-tarifa": "rate change",
    "cambio-de-avaluo": "assessment change",
    "cambio-menor": "minor change",
    "sin-cambio": "no change",
    "urgente": "urgent",
    "aviso": "notice",
    "ninguna": "",
    "rubro-desconocido": "unknown charge",
    "sin-historia": "no history",
    "con-historia": "with history",
    "sin-consulta": "no enquiry",
    "nada-sin-comprobante": "nothing without a receipt",
    "hay-periodos-sin-comprobante": "periods without a receipt"
  },

  "porQuePagoMas": {
    "titulo": "Why I pay more",
    "bajada": "Between the last two receipts for each charge: what it went from and to, and what explains it. What explains an increase depends on how that charge is billed, and that is the half the portal does not answer.",
    "umbrales": "A change is called a jump when it passes {salto}, and below {ruido} it counts as noise.",
    "periodos": "Periods",
    "porPeriodo": "Per period",
    "elCambio": "The change",
    "sinVariacion": {
      "titulo": "There is no change to look at yet.",
      "cuerpo": "A change needs two receipts for the same charge. They appear on their own as your payments reach this app."
    },
    "finca": {
      "avaluo": "assessment: {monto}",
      "sinAvaluo": "no assessment published"
    }
  },

  "queSeMeVence": {
    "titulo": "What falls due",
    "bajada": "This is the question your portal does not answer. The portal tells you what you owe today; this tells you what is about to fall due and what already has.",
    "umbrales": "You are warned {dias} before the due date, and the cost of paying late is projected at an annual rate of {tasa}.",
    "porVencer": "Falling due",
    "nadaPorVencer": "As of {dia} there is nothing inside the warning window. That does not say you have nothing to pay later on: it says nothing falls within the next {dias}.",
    "vence": "Due",
    "faltan": "To go",
    "monto": "Amount",
    "yaVencidos": "Already overdue",
    "nadaVencido": "There is nothing overdue and unpaid in what this app has seen. That is not proof of being up to date with the municipality: your portal is the authority on that.",
    "vencio": "Fell due",
    "deAtraso": "Overdue by",
    "proyeccionDeMora": "Projected interest",
    "noSeSuman": "The two lists do not add up",
    "noSeSumanCuerpo": "A period falling due and one already overdue are different situations, and a total mixing them is not a number anyone calculated. If you want to know what you owe in total, your portal says that."
  },

  "cuantoApartar": {
    "titulo": "How much to set aside",
    "bajada": "Two things with two owners: what <strong>this app suggests</strong> from what you have already paid, and what <strong>you adopted</strong>. Their not matching is not an error.",
    "laPropuesta": "What this app suggests",
    "proyeccionDe": "{anio} projection",
    "porMes": "Per month",
    "loAdoptado": "What you adopted",
    "adoptadoPorMes": "Adopted per month",
    "leFalta": "Short by",
    "venceEsteMes": "Due this month",
    "umbral": "You are warned when what you adopted falls more than {umbral} below what is suggested."
  },

  "sinComprobante": {
    "titulo": "What has no receipt",
    "tituloCorto": "No receipt",
    "bajada": "The periods your portal treats as paid and for which this app never saw a receipt. It is said, not hidden: a named gap is an answer, and a silent one looks exactly like having no gaps.",
    "sinConsulta": "There is nothing to check against yet",
    "sinConsultaCuerpo": "<strong>Not being able to check is not being in order.</strong> It is a third answer, and it changes as soon as an enquiry to the portal arrives.",
    "cotejadoContra": "checked against {dia}",
    "noEsDeuda": "This is not a debt",
    "noEsDeudaCuerpo": "Your portal treats those periods as settled; what is missing is the evidence here, almost always because they were paid by a route that leaves no digital receipt — over the counter, for instance. Do not add them to anything falling due.",
    "fotoConFecha": "An enquiry is a dated snapshot: the portal says what is outstanding today, and tomorrow it says something else. The check is against {dia} and not against today."
  },

  "cuantoHePagado": {
    "titulo": "How much I have paid",
    "bajada": "What you paid in a year, and the series of receipts for each charge. The total has the interest inside, because that is what you actually paid.",
    "pagadoEn": "Paid in {anio}",
    "sinNingunComprobante": "This app has not seen a single receipt of yours yet, so there is no series to show and the total comes back at zero. They arrive on their own, with the rest of your records.",
    "anioSinComprobantes": "This app has no receipt from {anio}. A total of zero is not a year without payments: it may be that that year's receipts have not reached here yet. The years there are receipts for are in the list above.",
    "incluyeLaMora": "The total includes interest",
    "incluyeLaMoraCuerpo": "It is what you actually paid, not the price of the service. Each line carries its interest in its own column if you want to see them apart, and there is no total to subtract from another.",
    "mora": "Interest",
    "cubre": "Covers",
    "porPeriodo": "Per period",
    "inferido": "inferred",
    "loQueSeInfirio": "The lines marked “inferred” were deduced by this app",
    "loQueSeInfirioCuerpo": "When a line is the first for a charge there is no earlier one to tell how many periods it covers, so this app assumes one. That is its assumption and not a fact from your receipt."
  },

  "queTanAlDia": {
    "titulo": "How up to date this is",
    "tituloCorto": "How up to date",
    "bajada": "How far this app has read what your municipality published, and what is missing. Your account is hybrid: they send the charges, the receipts and the interest; you contribute how much you want to set aside.",
    "hastaDondeLeyo": "How far it read",
    "ultimoHecho": "Last fact read",
    "rubrosConComprobante": "Charges with at least one receipt",
    "ultimaConsulta": "Last enquiry to the portal",
    "ninguna": "none",
    "queLlego": "What arrived for each charge",
    "conComprobantes": "with receipts",
    "sinComprobantes": "without receipts",
    "rubroSinComprobantes": "This app has not seen a single receipt for this charge. That does not mean you are not paying it: it means your payments for this charge have not reached here yet.",
    "queFalta": "What is missing",
    "noFaltaNada": "Neither half is missing",
    "noFaltaNadaCuerpo": "Your municipality published an assessment for every property, there is an enquiry to the portal to check against, and you have adopted a reserve. That does not say there are no new facts left to fetch: that is what the number above is for.",
    "faltaLaReserva": "Your half is missing: you have not adopted a reserve",
    "faltaLaReservaCuerpo": "How much you want to set aside each month is the one thing your municipality cannot know, and the one thing this app asks you for. It is adopted under",
    "faltaLaReservaEnlace": "how much to set aside",
    "faltaUnaConsulta": "An enquiry to the portal is missing",
    "faltaUnaConsultaCuerpo": "With no enquiry on record there is nothing to check what this app did see paid against, so",
    "faltaUnaConsultaCierre": "cannot answer yet. <strong>Not being able to check is not being in order.</strong>",
    "faltaUnAvaluo": "Your municipality's half is missing: an assessment",
    "faltaUnAvaluoCuerpo": {
      "una": "There is {cuantas} of {total} property with no published assessment. The assessment is set by them and is the value you are billed on: without it, this app can say your property tax went up because of a new assessment, but not what it went from and to. <strong>There is nothing to fill in</strong> — it arrives on its own once they publish it.",
      "varias": "There are {cuantas} of {total} properties with no published assessment. The assessment is set by them and is the value you are billed on: without it, this app can say your property tax went up because of a new assessment, but not what it went from and to. <strong>There is nothing to fill in</strong> — it arrives on its own once they publish it."
    }
  },

  "arranque": {
    "traer": "Fetch whatever the institution has published",
    "trayendo": "Fetching…"
  },

  "//cara": "WHAT ONLY EXISTS IN THIS FACE, and is therefore copied from nowhere: the standalone app asks nobody -- it IS the app -- and here the screen asks Mymi.",
  "cara": {
    "preguntando": "Asking Mymi…",
    "sinNada": "There is nothing from your account on this device yet.",
    "adoptarNoEstaAqui": "Adopting a reserve is a write, and this screen only reads. It is adopted from the municipality's own app."
  },

  "pie": {
    "explicacion": "The record lives on this device. This screen does not go out to the network: it asks Mymi, and Mymi answers with the engine it lends."
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
