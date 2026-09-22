window.TextoDeLaCara = window.TextoDeLaCara || {};
window.TextoDeLaCara['es-CR'] = {
    // LO QUE ESTA CARA DICE CON SUS PROPIAS PALABRAS, en el idioma del registro. El espanol es
    // el de Costa Rica y es voseo. El nombre del laboratorio, su lema y el titulo del modulo NO
    // estan aca: son voz de la institucion, viven en idioma.js y en index.html, y no se traducen.
    // Las frases del dominio tampoco estan aca: llegan ya dichas.
    app: {
        preguntando: 'Preguntándole a Mymi…',
        actualizar: 'Actualizar',
        saludo: 'Hola.',
        kicker: 'Entendé cómo venís, no solo cómo saliste hoy.',
        ultimoChequeo: 'Tu último chequeo',
        pie: 'El registro vive en este dispositivo. Esta pantalla no sale a la red: le pregunta a Mymi, y Mymi responde con el motor que le presta.'
    },
    portada: {
        urgente: 'Hay algo que conviene revisar pronto.',
        consultePronto: 'Conviene comentar un resultado con tu médico.',
        enGeneralBien: 'En general, tus resultados vienen bien.',
        // LA CUARTA, y es la que faltaba: cuando el expediente NO SE PUDO LEER. No es una de las
        // tres de arriba —ninguna se puede afirmar sin haber leído— ni es el texto de carga, que
        // dice que todavía se está preguntando. Dice lo único cierto: que no se pudo.
        noSePudoLeer: 'No se pudo leer tu expediente ahora.'
    },
    atencion: {
        rotulo: 'merece atención',
        conteo: '1 resultado',
        conteoVarios: '{cuantos} resultados',
        tuResultado: 'Tu resultado · {valor}',
        referencia: 'Referencia · {referencia}',
        referenciaMinuscula: 'referencia: {referencia}',
        posibleErrorDeMedicion: 'posible error de medición',
        vieneAumentando: 'viene aumentando',
        vieneBajando: 'viene bajando',
        enSeguimiento: 'en seguimiento',
        seMantieneEstable: 'se mantiene estable',
        pruebasNinguna: '{pruebas} pruebas, ninguna',
        pruebasAlgunas: '{presentes} de {pruebas}',
        dentroDeLoEsperable: '{dentro} de {veces} dentro de lo esperable'
    },
    tranquilo: {
        rotulo: 'lo demás está bien',
        titulo: 'Estos resultados están dentro de lo esperado.',
        enRango: 'en rango',
        nota: '{mediciones} · {tendencia}',
        medicion: '1 medición',
        mediciones: '{cuantas} mediciones'
    },
    cambios: {
        titulo: 'Qué cambió desde tu último chequeo',
        intro: 'Así han cambiado tus resultados recientes.'
    },
    sinTendencia: {
        rotulo: 'todavía no podemos ver la tendencia',
        conUna: 'Con tu próximo resultado podremos empezar a mostrarte cómo viene cambiando.',
        conVarias: 'Todavía no hay datos suficientes para mostrar cómo viene cambiando.'
    },
    historial: {
        titulo: 'Tu historial',
        sinExamenes: 'Todavía no hay exámenes. Apretá <em>Actualizar</em>.',
        intro: 'Aquí podés ver tus resultados anteriores.',
        resultado: '1 resultado',
        resultados: '{cuantos} resultados'
    },
    pedido: {
        faltaUnDato: 'Falta un dato tuyo'
    },
    meses: ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'],

    // LAS NOTAS DEL PIE DE LA TRAYECTORIA. Mismas claves y mismos valores que la app suelta: eran las dos ultimas frases que esta cara escribia en espanol duro.
    forma: {
        nivelDeSiempre: 'tu nivel de siempre, {valor}',
        esperableHasta: 'esperable hasta {valor}'
    },

    // LAS FRASES DEL PUENTE, que las dice el Host y no la cara. El puente es el mismo archivo en
    // los cinco modulos y no tiene catalogo propio: las lee de window.TextoDelPuente, que publica
    // idioma.js, y si no estan cae al espanol -- que es lo que esta cara hacia, asi que su pie
    // decia «al dia, en 69 ms» debajo de una pantalla en ingles. Las llaves y los huecos NO se
    // traducen: son el cable, como window.gnothi y consultar.
    puente: {
        sinPuente: 'esta página solo corre dentro de Mymi, en la forma híbrida: no hay puente, y sin puente no tiene a quién preguntarle',
        sinPuenteCorto: 'sin puente: esta pantalla no corre dentro de Mymi',
        pidiendo: 'pidiéndole a Mymi que traiga lo nuevo…',
        alDia: 'al día, en {ms} ms · {dicho}',
        noSePudo: 'no se pudo traer lo nuevo ({ms} ms): {porque}',
        tiro: 'pull tiró: {porque}'
    }
};
