// WHICH LANGUAGE THIS FACE SPEAKS, negotiated with the device the same way the standalone app
// does it in Texts.NegotiateAsync: the tags the person actually asked their phone for, in their
// own order of preference, matched by PRIMARY SUBTAG against the ones this module has written.
//
// The match is by primary subtag and not by full tag on purpose: a phone set to `es-MX` is asking
// for Spanish, and refusing it because the catalogue is written for Costa Rica would answer a
// Spanish speaker in English. The region of the catalogue is the dealership's, not the reader's.
//
// AND THE FALLBACK IS THE RECORD'S LANGUAGE, not English: when nothing matches -- or when the
// browser will not say -- the screen speaks the language the dealership keeps the record in, which
// is what the person would get if this file did not exist at all.
window.Language = (function () {
    'use strict';

    // The ones there are, in the order Texts.TheOnesThereAre declares them. Two, and there is no
    // third: adding one means writing a catalogue, not editing this list.
    var TheOnesThereAre = ['es-CR', 'en'];

    // Where an unknown tag falls. Same one as Texts and as Wording.Phrases.LanguageOfTheRecord.
    var TheLanguageOfTheRecord = 'es-CR';

    function ThePrimarySubtagOf(tag) {
        if (!tag) return '';
        var clean = String(tag).trim().toLowerCase();
        var dash = clean.indexOf('-');
        return dash < 0 ? clean : clean.slice(0, dash);
    }

    // WHAT THE DEVICE ASKED FOR, in the person's own order. Guarded because a webview may not
    // publish `navigator.languages` at all, and a screen that cannot read the choice still has to
    // paint: an empty list falls to the record's language like any other unknown answer.
    function WhatTheDeviceAskedFor() {
        try {
            if (navigator.languages && navigator.languages.length)
                return Array.prototype.slice.call(navigator.languages);
            return [navigator.language || ''];
        } catch (e) {
            return [];
        }
    }

    function TheFirstOneWeSpeak() {
        var asked = WhatTheDeviceAskedFor();
        for (var i = 0; i < asked.length; i++) {
            var wanted = ThePrimarySubtagOf(asked[i]);
            for (var j = 0; j < TheOnesThereAre.length; j++)
                if (ThePrimarySubtagOf(TheOnesThereAre[j]) === wanted) return TheOnesThereAre[j];
        }
        return TheLanguageOfTheRecord;
    }

    var everyCatalogue = window.FaceText || {};
    var tag = TheFirstOneWeSpeak();
    var catalogue = everyCatalogue[tag] || everyCatalogue[TheLanguageOfTheRecord] || {};

    // A KEY THAT IS MISSING SAYS ITS OWN NAME between braces and does not throw. Same rule as
    // Wording.Phrases.Say: an answer with a word missing still serves, and the hole is visible.
    function text(key) {
        var parts = String(key).split('.');
        var node = catalogue;
        for (var i = 0; i < parts.length; i++) {
            if (!node || typeof node !== 'object') return '{' + key + '}';
            node = node[parts[i]];
        }
        return typeof node === 'string' ? node : '{' + key + '}';
    }

    function fill(key, holes) {
        var said = text(key);
        for (var hole in holes)
            if (Object.prototype.hasOwnProperty.call(holes, hole))
                said = said.split('{' + hole + '}').join(holes[hole]);
        return said;
    }

    // THE SUBTAG THE SURFACE AND THE FORMATTERS USE. It is written in the catalogue itself and not
    // derived from the tag, so the two can never disagree about which catalogue is loaded.
    var language = typeof catalogue.language === 'string' ? catalogue.language : 'es';

    document.documentElement.lang = tag;

    // THE STATIC TEXT, painted once. Everything the markup carries in Spanish is a fallback for a
    // reader that gets the page before this runs -- the attribute is what decides.
    //
    // THE TITLE IS NOT HERE: "Mi plan de mantenimiento · Sotavento Motors" is the institution's
    // voice, declared once and not translated, and it stays written in the document.
    function paintTheStaticText() {
        var said = document.querySelectorAll('[data-text]');
        for (var i = 0; i < said.length; i++)
            said[i].textContent = text(said[i].getAttribute('data-text'));

        var labelled = document.querySelectorAll('[data-text-aria]');
        for (var j = 0; j < labelled.length; j++)
            labelled[j].setAttribute('aria-label', text(labelled[j].getAttribute('data-text-aria')));
    }

    // EL PUENTE TAMBIEN HABLA, y no tiene catalogo propio: lo pone el Host y es el mismo archivo
    // en los cinco modulos. Se le pasan las frases ya elegidas; si no se las pasan, el cae al
    // espanol. Va acá y no en cara.js porque este archivo corre en el <head>, antes que puente.js.
    if (catalogue.puente) window.TextoDelPuente = catalogue.puente;

    return {
        tag: tag,
        language: language,
        text: text,
        fill: fill,
        paintTheStaticText: paintTheStaticText
    };
})();
