window.TextoDeLaCara = window.TextoDeLaCara || {};
window.TextoDeLaCara['en'] = {
    // THE SAME FACE IN THE OTHER LANGUAGE THIS MODULE SPEAKS. The keys are exactly the ones in
    // the record's catalogue: a key that is in one file and not in the other is a sentence that
    // arrives half-written, and nothing else on screen shows it.
    //
    // The lab's name, its tagline and the module's title are NOT here: they are the institution's
    // voice, they live in idioma.js and index.html, and they are never translated.
    app: {
        preguntando: 'Asking Mymi…',
        actualizar: 'Refresh',
        saludo: 'Hello.',
        kicker: 'See where you are heading, not just how you did today.',
        ultimoChequeo: 'Your last check-up',
        pie: 'The record lives on this device. This screen does not go out to the network: it asks Mymi, and Mymi answers with the engine it lends.'
    },
    portada: {
        urgente: 'There is something worth looking at soon.',
        consultePronto: 'It is worth mentioning a result to your doctor.',
        enGeneralBien: 'Overall, your results are doing fine.'
    },
    atencion: {
        rotulo: 'worth attention',
        conteo: '1 result',
        conteoVarios: '{cuantos} results',
        tuResultado: 'Your result · {valor}',
        referencia: 'Reference · {referencia}',
        referenciaMinuscula: 'reference: {referencia}',
        posibleErrorDeMedicion: 'possible measurement error',
        vieneAumentando: 'has been rising',
        vieneBajando: 'has been falling',
        enSeguimiento: 'being followed',
        seMantieneEstable: 'holding steady',
        pruebasNinguna: '{pruebas} tests, none',
        pruebasAlgunas: '{presentes} of {pruebas}',
        dentroDeLoEsperable: '{dentro} of {veces} within what is expected'
    },
    tranquilo: {
        rotulo: 'the rest is fine',
        titulo: 'These results are within what is expected.',
        enRango: 'in range',
        nota: '{mediciones} · {tendencia}',
        medicion: '1 measurement',
        mediciones: '{cuantas} measurements'
    },
    cambios: {
        titulo: 'What changed since your last check-up',
        intro: 'This is how your recent results have changed.'
    },
    sinTendencia: {
        rotulo: 'no trend to show yet',
        intro: "They still have few measurements. With each one's next result we'll be able to start showing you how they're changing.",
        conUna: 'With your next result we can start showing you how it is changing.',
        conVarias: 'There is not enough data yet to show how it is changing.'
    },
    historial: {
        titulo: 'Your history',
        sinExamenes: 'No tests yet. Press <em>Refresh</em>.',
        intro: 'Here you can see your earlier results.',
        resultado: '1 result',
        resultados: '{cuantos} results'
    },
    pedido: {
        faltaUnDato: 'One detail about you is missing'
    },
    meses: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],

    // THE TRAJECTORY FOOTNOTES. Same keys and values as the standalone app.
    forma: {
        nivelDeSiempre: 'your usual level, {valor}',
        esperableHasta: 'expected up to {valor}'
    },

    // THE BRIDGE'S PHRASES, said by the Host and not by the face. The bridge is the same file in
    // all five modules and has no catalogue of its own: it reads them from window.TextoDelPuente,
    // which idioma.js publishes, and falls back to Spanish when they are missing. Same keys as the
    // Spanish catalogue: they are the cable, not copy.
    puente: {
        sinPuente: 'this page only runs inside Mymi, in the hybrid form: there is no bridge, and without one it has no one to ask',
        sinPuenteCorto: 'no bridge: this screen is not running inside Mymi',
        pidiendo: 'asking Mymi to bring what is new…',
        alDia: 'up to date',
        alDiaCon: 'up to date · {dicho}',
        noAlcance: 'could not reach your institution: check your connection and try again',
        noSePudo: 'could not bring what is new: try again in a moment'
    }
};
