window.FaceText = window.FaceText || {};
window.FaceText['en'] = {
  "//": "WHAT THIS FACE SAYS IN ITS OWN WORDS, in English. The sentences the DOMAIN composes are not here -- an operation's `says`, a stay's, the plate's --: those arrive already said from the surface, which takes the language tag as one more parameter of the view. Where a sentence also exists in the standalone app, the value is copied from Concesionario.Wasm/wwwroot/text/en.json under the same key, so that both screens say the same thing and it can be checked by reading. The `board` block is copied from Wording/TheEnglishCatalogue.cs, the same dictionary the domain names its closed sets with. The `face` block is the only thing this face says that the standalone app does not. The model, the VIN and the plate belong to the dealership's record and are not translated.",

  "language": "en",

  "home": {
    "kicker": "Owner service",
    "sub": "Your dealership keeps this record and is the only one that changes it. Here you read it.",
    "stateTitle": "Your vehicle's state",
    "healthWhat": "operations up to date, of those your dealership scheduled",
    "overdueOne": "{count} overdue",
    "overdueMany": "{count} overdue",
    "dueOne": "{count} due now",
    "dueMany": "{count} due now",
    "missed": "{count} never done",
    "upToDate": "{count} up to date",
    "nextKicker": "What comes next",
    "nothingPending": "Nothing pending",
    "allGood": "Your car is up to date",
    "saysLabel": "What your record says",
    "nothingDue": "None of the {count} operations your dealership scheduled is overdue or due now.",
    "odometerTile": "travelled together",
    "readingTile": "last reading your dealership sent",
    "noReading": "no reading",
    "glance": "A glance at your car",
    "refreshFetching": "Fetching…",
    "refresh": "Fetch whatever is new",
    "headlineAttention": "Needs attention",
    "headlineSomething": "Something to do",
    "headlineOk": "Up to date",
    "greetingAttention": "{car} needs attention",
    "greetingSomething": "{car} has something to do",
    "greetingOk": "{car} is up to date",
    "yourCar": "Your car",
    "yourModel": "Your {model}"
  },

  "vehicle": {
    "rowVin": "VIN",
    "rowPlate": "Plate"
  },

  "history": {
    "staysNoneTitle": "No stay recorded in this period"
  },

  "board": {
    "operation": {
      "Inspection": "Inspection",
      "FluidChange": "Fluid change",
      "FilterChange": "Filter change",
      "WearPartReplacement": "Wear parts",
      "TimedComponent": "Timed component",
      "SoftwareUpdate": "Software update",
      "RecallRemedy": "Recall remedy",
      "Diagnostic": "Diagnostic",
      "Repair": "Repair",

      "short": {
        "Inspection": "Inspection",
        "FluidChange": "Fluids",
        "FilterChange": "Filter",
        "WearPartReplacement": "Wear",
        "TimedComponent": "Timed",
        "SoftwareUpdate": "Software",
        "RecallRemedy": "Recall",
        "Diagnostic": "Diagnostic",
        "Repair": "Repair"
      }
    },

    "dueState": {
      "Overdue": "Overdue",
      "Due": "Due now",
      "DueSoon": "Coming up",
      "NotDue": "Up to date",
      "Deferred": "Put off",
      "Completed": "Done",
      "Missed": "Never done",
      "Superseded": "Replaced"
    }
  },

  "day": {
    "short": "{day} {month} {year}"
  },

  "month": {
    "short": {
      "1": "Jan", "2": "Feb", "3": "Mar", "4": "Apr", "5": "May", "6": "Jun",
      "7": "Jul", "8": "Aug", "9": "Sep", "10": "Oct", "11": "Nov", "12": "Dec"
    }
  },

  "face": {
    "//": "WHAT ONLY THIS FACE SAYS. None of these has an equivalent on the standalone app's screen: either they speak about Mymi -- which does not exist over there -- or they name a section that lives here and, over there, in another tab.",
    "asking": "Asking Mymi…",
    "nothingHereYet": "There is nothing about your car on this device yet.",
    "pressRefresh": "Press «{refresh}» so Mymi brings whatever your dealership published.",
    "nothingScheduled": "Your dealership has not scheduled any operation for this vehicle yet.",
    "noOperations": "There is no scheduled operation for this vehicle yet.",
    "thisYear": "What happened this year",
    "explanation": "The record lives on this device. This screen does not reach the network: it asks Mymi, and Mymi answers with the engine it lends."
  },

  "puente": {
    "//": "THE BRIDGE'S PHRASES. Same keys as the Spanish catalogue: they are the cable, not copy.",
    "sinPuente": "this page only runs inside Mymi, in the hybrid form: there is no bridge, and without one it has no one to ask",
    "sinPuenteCorto": "no bridge: this screen is not running inside Mymi",
    "pidiendo": "asking Mymi to bring what is new…",
    "alDia": "up to date",
    "alDiaCon": "up to date · {dicho}",
    "noAlcance": "could not reach your institution: check your connection and try again",
    "noSePudo": "could not bring what is new: try again in a moment"
  }
};
