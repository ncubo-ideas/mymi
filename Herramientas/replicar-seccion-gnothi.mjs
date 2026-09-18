// node Herramientas/replicar-seccion-gnothi.mjs
//
// Replicates La Guaria's "Se usa con Gnothi" section on the other four institution pages.
// One template, three voices (usted / vos / English), and a palette mapped onto each page's
// own CSS variables so the section wears the institution's brand. Class names are prefixed
// gnothi__ because the pages already own .boton, .paso and .tarjeta.
import { readFile, writeFile } from 'node:fs/promises';

import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

// The repo root, relative to this file: Herramientas/ hangs from it.
const raiz = resolve(dirname(fileURLToPath(import.meta.url)), '..');

// ------------------------------------------------------------------ the three voices
const voces = {
  usted: {
    titulo: 'Se usa con <em>Gnothi</em>.',
    lead: (c) => `Gnothi es la aplicación que junta lo que cada institución le entrega —su laboratorio, su supermercado— y se lo muestra a su asistente de IA, en su propio aparato. <strong>Se instala una sola vez</strong>, gratis; después, cada institución se agrega con un toque. ${c.quien} es una de ellas.`,
    tablist: 'Elija su aparato',
    tabs: ['Android', 'iPhone', 'Computadora'],
    android: {
      h3: 'En su teléfono Android',
      nota: (c) => `Gnothi está en Google Play. ${c.marca} se agrega desde esta misma página, con un toque.`,
      tienda: ['Disponible en', 'Google Play'],
      agregar: (c) => `Agregar ${c.marca} a Gnothi`,
      pasos: (c) => [
        ['Instale Gnothi desde Google Play.', 'Es gratuita y se instala una sola vez: la misma Gnothi sirve para todas sus instituciones.'],
        [`Vuelva a esta página en el teléfono y toque «Agregar ${c.marca} a Gnothi».`, `Gnothi baja el módulo desde esta misma dirección y le muestra una hoja: quién lo entrega, qué preguntas va a contestar y en qué carpeta del teléfono guarda ${c.registro}. Nada se escribe antes de que usted lo lea.`],
        ['Toque «Instalar en mi teléfono».', `${c.marca} aparece en su lista de instituciones.`],
        ['Pregunte.', `Su asistente ve a ${c.marca} junto a sus otras instituciones. Cada consulta tiene un interruptor: usted apaga la que no quiera que lea.`],
      ],
      enlace: (c) => `¿Está leyendo esto en una computadora? En Gnothi toque <b>Pegar su enlace</b> y pegue este. Si le llegó el archivo <code>${c.modulo}.gmod</code> por correo o WhatsApp, ábralo: el teléfono le ofrece Gnothi.`,
    },
    iphone: {
      h3: 'En su iPhone',
      nota: 'Los mismos pasos que en Android. Gnothi para iPhone está en camino a la App Store.',
      tienda: ['Próximamente en', 'App Store'],
      pasos: (c) => [
        ['Instale Gnothi desde la App Store', 'cuando esté publicada. Una sola vez, para todas sus instituciones.'],
        [`Vuelva a esta página en el iPhone y toque «Agregar ${c.marca} a Gnothi».`, 'Gnothi se abre con la hoja del módulo: quién lo entrega, qué contesta y dónde guarda ' + c.registro + '.'],
        ['Toque «Instalar en mi teléfono».', ''],
        ['Pregunte.', `Su asistente ve a ${c.marca}, y usted decide qué consultas quedan encendidas.`],
      ],
    },
    computadora: {
      h3: 'En su computadora',
      nota: 'Gnothi para Windows se descarga de aquí. No pide permisos de administrador y no toca nada fuera de su carpeta.',
      tienda: ['Descargar para', 'Windows'],
      modulo: (c) => `Descargar el módulo de ${c.marca}`,
      pasos: (c) => [
        ['Descargue e instale Gnothi.', 'Una sola vez. Si Windows muestra el aviso de SmartScreen, elija «Más información» y «Ejecutar de todas formas». La primera vez que abre, Gnothi le pregunta antes de registrarse con su asistente (Claude u otro) y hace un respaldo de la configuración que va a tocar.'],
        ['Abra Gnothi y elija «Agregar una institución».', `Pegue el enlace de ${c.marca} que está abajo, o toque «Elegir el archivo…» y busque el <code>${c.modulo}.gmod</code> que bajó con el botón.`],
        ['Lea la hoja y toque «Instalar en esta computadora».', `Le dice quién entrega el módulo, qué va a contestar y en qué carpeta guarda ${c.registro}: <code>Documentos\\Gnothi\\${c.modulo}</code>. La misma que usa la aplicación.`],
        ['Reinicie su asistente y pregunte.', `${c.pregunta} Todo se contesta desde su computadora; nada sube a la nube.`],
      ],
      enlace: (c) => `El enlace de ${c.marca}, para pegar en Gnothi:`,
    },
    copiar: ['Copiar', 'Copiado'],
    suelta: (c) => `<b>¿Sin Gnothi?</b> La aplicación también funciona suelta, en el navegador: <a href="/app/">ábrala</a> y elija la carpeta donde quiere guardar ${c.registro}. Cuando instale Gnothi, va a leer esa misma carpeta.`,
  },

  vos: {
    titulo: 'Se usa con <em>Gnothi</em>.',
    lead: (c) => `Gnothi es la aplicación que junta lo que cada institución te entrega —tu laboratorio, tu supermercado— y se lo muestra a tu asistente de IA, en tu propio aparato. <strong>Se instala una sola vez</strong>, gratis; después, cada institución se agrega con un toque. ${c.quien} es una de ellas.`,
    tablist: 'Elegí tu aparato',
    tabs: ['Android', 'iPhone', 'Computadora'],
    android: {
      h3: 'En tu teléfono Android',
      nota: (c) => `Gnothi está en Google Play. ${c.marca} se agrega desde esta misma página, con un toque.`,
      tienda: ['Disponible en', 'Google Play'],
      agregar: (c) => `Agregar ${c.marca} a Gnothi`,
      pasos: (c) => [
        ['Instalá Gnothi desde Google Play.', 'Es gratuita y se instala una sola vez: la misma Gnothi sirve para todas tus instituciones.'],
        [`Volvé a esta página en el teléfono y tocá «Agregar ${c.marca} a Gnothi».`, `Gnothi baja el módulo desde esta misma dirección y te muestra una hoja: quién lo entrega, qué preguntas va a contestar y en qué carpeta del teléfono guarda ${c.registro}. Nada se escribe antes de que la leas.`],
        ['Tocá «Instalar en mi teléfono».', `${c.marca} aparece en tu lista de instituciones.`],
        ['Preguntá.', `Tu asistente ve a ${c.marca} junto a tus otras instituciones. Cada consulta tiene un interruptor: apagás la que no querés que lea.`],
      ],
      enlace: (c) => `¿Estás leyendo esto en una computadora? En Gnothi tocá <b>Pegar su enlace</b> y pegá este. Si te llegó el archivo <code>${c.modulo}.gmod</code> por correo o WhatsApp, abrilo: el teléfono te ofrece Gnothi.`,
    },
    iphone: {
      h3: 'En tu iPhone',
      nota: 'Los mismos pasos que en Android. Gnothi para iPhone está en camino a la App Store.',
      tienda: ['Próximamente en', 'App Store'],
      pasos: (c) => [
        ['Instalá Gnothi desde la App Store', 'cuando esté publicada. Una sola vez, para todas tus instituciones.'],
        [`Volvé a esta página en el iPhone y tocá «Agregar ${c.marca} a Gnothi».`, 'Gnothi se abre con la hoja del módulo: quién lo entrega, qué contesta y dónde guarda ' + c.registro + '.'],
        ['Tocá «Instalar en mi teléfono».', ''],
        ['Preguntá.', `Tu asistente ve a ${c.marca}, y vos decidís qué consultas quedan encendidas.`],
      ],
    },
    computadora: {
      h3: 'En tu computadora',
      nota: 'Gnothi para Windows se descarga de acá. No pide permisos de administrador y no toca nada fuera de su carpeta.',
      tienda: ['Descargar para', 'Windows'],
      modulo: (c) => `Descargar el módulo de ${c.marca}`,
      pasos: (c) => [
        ['Descargá e instalá Gnothi.', 'Una sola vez. Si Windows muestra el aviso de SmartScreen, elegí «Más información» y «Ejecutar de todas formas». La primera vez que abre, Gnothi te pregunta antes de registrarse con tu asistente (Claude u otro) y hace un respaldo de la configuración que va a tocar.'],
        ['Abrí Gnothi y elegí «Agregar una institución».', `Pegá el enlace de ${c.marca} que está abajo, o tocá «Elegir el archivo…» y buscá el <code>${c.modulo}.gmod</code> que bajaste con el botón.`],
        ['Leé la hoja y tocá «Instalar en esta computadora».', `Te dice quién entrega el módulo, qué va a contestar y en qué carpeta guarda ${c.registro}: <code>Documentos\\Gnothi\\${c.modulo}</code>. La misma que usa la app.`],
        ['Reiniciá tu asistente y preguntá.', `${c.pregunta} Todo se contesta desde tu computadora; nada sube a la nube.`],
      ],
      enlace: (c) => `El enlace de ${c.marca}, para pegar en Gnothi:`,
    },
    copiar: ['Copiar', 'Copiado'],
    suelta: (c) => `<b>¿Sin Gnothi?</b> La app también funciona suelta, en el navegador: <a href="/app/">abrila</a> y elegí la carpeta donde querés guardar ${c.registro}. Cuando instalés Gnothi, va a leer esa misma carpeta.`,
  },

  en: {
    titulo: 'It works with <em>Gnothi</em>.',
    lead: (c) => `Gnothi is the app that gathers what each institution hands you —your lab, your supermarket— and shows it to your AI assistant, on your own device. <strong>You install it once</strong>, for free; after that, each institution is added with one tap. ${c.quien} is one of them.`,
    tablist: 'Choose your device',
    tabs: ['Android', 'iPhone', 'Computer'],
    android: {
      h3: 'On your Android phone',
      nota: (c) => `Gnothi is on Google Play. ${c.marca} is added from this very page, with one tap.`,
      tienda: ['Get it on', 'Google Play'],
      agregar: (c) => `Add ${c.marca} to Gnothi`,
      pasos: (c) => [
        ['Install Gnothi from Google Play.', 'It is free and you install it once: the same Gnothi serves every institution you have.'],
        [`Come back to this page on your phone and tap “Add ${c.marca} to Gnothi”.`, `Gnothi downloads the module from this same address and shows you a sheet: who hands it over, what questions it will answer and in which folder of the phone it keeps ${c.registro}. Nothing is written before you read it.`],
        ['Tap “Install on my phone”.', `${c.marca} appears in your list of institutions.`],
        ['Ask.', `Your assistant sees ${c.marca} next to your other institutions. Every query has a switch: you turn off the ones you do not want it to read.`],
      ],
      enlace: (c) => `Reading this on a computer? In Gnothi tap <b>Paste their link</b> and paste this one. If the <code>${c.modulo}.gmod</code> file reached you by mail or WhatsApp, open it: the phone offers Gnothi.`,
    },
    iphone: {
      h3: 'On your iPhone',
      nota: 'The same steps as on Android. Gnothi for iPhone is on its way to the App Store.',
      tienda: ['Coming soon to the', 'App Store'],
      pasos: (c) => [
        ['Install Gnothi from the App Store', 'once it is published. Once, for every institution you have.'],
        [`Come back to this page on your iPhone and tap “Add ${c.marca} to Gnothi”.`, 'Gnothi opens with the module’s sheet: who hands it over, what it answers and where it keeps ' + c.registro + '.'],
        ['Tap “Install on my phone”.', ''],
        ['Ask.', `Your assistant sees ${c.marca}, and you decide which queries stay on.`],
      ],
    },
    computadora: {
      h3: 'On your computer',
      nota: 'Gnothi for Windows downloads from here. It asks for no administrator rights and touches nothing outside its folder.',
      tienda: ['Download for', 'Windows'],
      modulo: (c) => `Download the ${c.marca} module`,
      pasos: (c) => [
        ['Download and install Gnothi.', 'Once. If Windows shows the SmartScreen notice, choose “More info” and “Run anyway”. The first time it opens, Gnothi asks before registering with your assistant (Claude or another) and backs up the configuration it is about to touch.'],
        ['Open Gnothi and choose “Add an institution”.', `Paste the ${c.marca} link below, or tap “Pick the file…” and find the <code>${c.modulo}.gmod</code> you downloaded with the button.`],
        ['Read the sheet and tap “Install on this computer”.', `It tells you who hands over the module, what it will answer and in which folder it keeps ${c.registro}: <code>Documents\\Gnothi\\${c.modulo}</code>. The same one the app uses.`],
        ['Restart your assistant and ask.', `${c.pregunta} Everything is answered from your computer; nothing goes to the cloud.`],
      ],
      enlace: (c) => `The ${c.marca} link, to paste into Gnothi:`,
    },
    copiar: ['Copy', 'Copied'],
    suelta: (c) => `<b>No Gnothi?</b> The app also works on its own, in the browser: <a href="/app/">open it</a> and pick the folder where you want ${c.registro} kept. When you install Gnothi, it reads that same folder.`,
  },
};

// ------------------------------------------------------------------ the template
const iconos = {
  android: `<svg viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"><path d="M3.6 6.4h8.8v5.2a1 1 0 0 1-1 1h-.6v2.2a.9.9 0 1 1-1.8 0v-2.2H7v2.2a.9.9 0 1 1-1.8 0v-2.2h-.6a1 1 0 0 1-1-1V6.4Zm-1.5.1a.9.9 0 0 1 .9.9v3.6a.9.9 0 1 1-1.8 0V7.4a.9.9 0 0 1 .9-.9Zm11.8 0a.9.9 0 0 1 .9.9v3.6a.9.9 0 1 1-1.8 0V7.4a.9.9 0 0 1 .9-.9ZM8 1.6c2.2 0 4 1.5 4.4 3.6H3.6C4 3.1 5.8 1.6 8 1.6Zm-1.7 2a.5.5 0 1 0 0 1 .5.5 0 0 0 0-1Zm3.4 0a.5.5 0 1 0 0 1 .5.5 0 0 0 0-1Z"/></svg>`,
  iphone: `<svg viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"><path d="M11.3 8.5c0-1.5 1.2-2.2 1.3-2.2-.7-1-1.8-1.2-2.2-1.2-.9-.1-1.8.5-2.3.5s-1.2-.5-2-.5C4.9 5.1 3 6.4 3 9c0 .8.1 1.6.4 2.4.4 1.2 1.9 3.9 3.4 3.9.7 0 1.2-.5 2.1-.5.9 0 1.3.5 2.1.5 1.5 0 2.8-2.5 3.2-3.6-1.9-.9-2.9-2.1-2.9-3.2ZM9.9 3.9c.5-.6.8-1.4.7-2.2-.7 0-1.5.5-2 1.1-.4.5-.8 1.3-.7 2.1.8.1 1.5-.4 2-1Z"/></svg>`,
  computadora: `<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="1.5" y="2.5" width="13" height="8.5" rx="1.2"/><path d="M5.5 14h5M8 11v3"/></svg>`,
  play: `<svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"><path d="M2.5 1.6c0-.5.5-.8.9-.6l10.4 6.3c.4.3.4.9 0 1.2L3.4 14.9c-.4.3-.9 0-.9-.5V1.6Z"/></svg>`,
  bajar: `<svg width="15" height="15" viewBox="0 0 15 15" fill="none" aria-hidden="true"><path d="M7.5 1v10M3.4 7.2l4.1 4.1 4.1-4.1M1.6 13.6h11.8" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round"/></svg>`,
};

const pasos = (lista) => `<ol class="gnothi__pasos">${lista.map(([b, s]) =>
  `\n          <li><p><b>${b}</b>${s ? ` <span>${s}</span>` : ''}</p></li>`).join('')}\n        </ol>`;

function seccion(c) {
  const v = voces[c.voz];
  const [tA, tI, tC] = v.tabs;
  const t = c.tema;
  return `
  <!-- SE USA CON GNOTHI: EL PASO A PASO POR AMBIENTE. Es la misma sección que estrenó la página
       de La Guaria (Compras/PaginaDeDescarga/descarga/index.html) y la genera el mismo molde
       (Herramientas/replicar-seccion-gnothi.mjs) con la voz y la paleta de esta institución; si cambia el
       flujo, cambia en el molde y se vuelve a correr.

       Primero Gnothi (una vez), después la institución (un toque). Un panel por ambiente porque
       los pasos no son los mismos: en el teléfono Gnothi viene de la tienda y el módulo entra por
       el enlace al dominio de Gnothi (go.gnothi.app/alta, especificación 13 §7); en Windows Gnothi
       se descarga de acá y el módulo se agrega desde la ventana de Gnothi. Los rótulos entre
       comillas son los del catálogo del Host (Gnothi.Host/textos.json).

       LOS DESCARGABLES no van al repo y hay que armarlos antes de levantar URL1 (ver nginx.conf):
         descargas/${c.modulo}.gmod        la forma cargo, para el escritorio
         descargas/movil/${c.modulo}.gmod  la forma hospedada, para el teléfono; lleva el ORIGEN
                                       adentro y tiene que ser el que sirve esta página
         descargas/gnothi/Gnothi.Host.zip  Gnothi para Windows

       EL ENLACE DE ALTA SE ARMA EN EL NAVEGADOR con location.origin (esta página es estática y no
       sabe desde dónde la sirven), sin sello &h=: el sello lo calcula quien puede leer el zip.
       El panel inicial lo elige el user agent; sin JavaScript se ven los tres. -->
  <style>
    .gnothi {
      --g-acento: ${t.acento}; --g-acento-hondo: ${t.acentoHondo}; --g-sobre-acento: ${t.sobreAcento};
      --g-tinta: ${t.tinta}; --g-tenue: ${t.tenue}; --g-linea: ${t.linea};
      --g-suave: ${t.suave}; --g-carta: ${t.carta}; --g-radio: ${t.radio};
      color: var(--g-tinta); ${t.extra || ''}
    }
    .gnothi h2 { font-size: ${t.h2 || '30px'}; line-height: 1.12; margin: 0 0 12px; letter-spacing: -.01em; font-weight: ${t.pesoTitulo || 700}; }
    .gnothi h2 em { font-style: normal; color: var(--g-acento); }
    .gnothi__lead { margin: 0; color: var(--g-tenue); font-size: 15.5px; max-width: 62ch; line-height: 1.6; }
    .gnothi__lead strong { color: var(--g-tinta); font-weight: 600; }

    .gnothi__ambientes { display: none; gap: 8px; flex-wrap: wrap; margin: 26px 0 0; padding: 0; list-style: none; }
    .js .gnothi__ambientes { display: flex; }
    .gnothi__ambientes li { margin: 0; }
    .gnothi__ambientes button {
      font: inherit; font-size: 14px; font-weight: 600; color: var(--g-tinta); background: transparent;
      border: 1px solid var(--g-linea); border-radius: 999px; padding: 9px 18px; cursor: pointer;
      display: inline-flex; align-items: center; gap: 8px; transition: background .18s, color .18s, border-color .18s;
    }
    .gnothi__ambientes button:hover { background: var(--g-suave); }
    .gnothi__ambientes button[aria-selected="true"] { background: var(--g-acento); color: var(--g-sobre-acento); border-color: var(--g-acento); }
    .gnothi__ambientes svg { width: 15px; height: 15px; flex: none; }

    .gnothi__ambiente { padding: 30px 0 34px; border-bottom: 1px solid var(--g-linea); display: grid; grid-template-columns: minmax(0, 250px) minmax(0, 1fr); gap: 40px; }
    .gnothi__ambiente:last-of-type { border-bottom: 0; }
    .js .gnothi__ambiente:not(.activo) { display: none; }
    .js .gnothi__ambiente { border-bottom: 0; }
    .gnothi__ambiente h3 { font-size: 15px; font-weight: 700; margin: 0 0 8px; color: var(--g-tinta); letter-spacing: 0; text-transform: none; }
    .gnothi__nota { margin: 0; color: var(--g-tenue); font-size: 14px; line-height: 1.55; }
    .gnothi__descargas { display: flex; flex-direction: column; align-items: flex-start; gap: 12px; margin-top: 18px; }

    .gnothi__boton {
      display: inline-flex; align-items: center; gap: 11px; text-decoration: none;
      background: var(--g-acento); color: var(--g-sobre-acento); font-weight: 600; font-size: 15px;
      padding: 13px 24px; border-radius: var(--g-radio); border: 1px solid transparent; transition: background .18s;
    }
    .gnothi__boton:hover { background: var(--g-acento-hondo); color: var(--g-sobre-acento); }
    .gnothi__boton--suave { background: transparent; color: var(--g-acento); border-color: var(--g-linea); }
    .gnothi__boton--suave:hover { background: var(--g-suave); color: var(--g-acento); }
    .gnothi__boton--inerte { background: var(--g-suave); color: var(--g-tenue); border-color: var(--g-linea); cursor: default; pointer-events: none; }
    .gnothi__boton small { display: block; font-size: 11px; font-weight: 400; opacity: .85; letter-spacing: .02em; }
    .gnothi__boton .dos { display: flex; flex-direction: column; line-height: 1.15; }

    .gnothi__pasos { margin: 0; padding: 0; list-style: none; counter-reset: g; }
    .gnothi__pasos li { counter-increment: g; display: grid; grid-template-columns: 30px minmax(0, 1fr); gap: 14px; padding: 13px 0; border-bottom: 1px solid var(--g-linea); margin: 0; }
    .gnothi__pasos li:first-child { padding-top: 0; }
    .gnothi__pasos li:last-child { border-bottom: 0; }
    .gnothi__pasos li::before { content: counter(g); font-size: 12.5px; color: var(--g-acento); padding-top: 3px; font-variant-numeric: tabular-nums; }
    .gnothi__pasos p { margin: 0; font-size: 15px; line-height: 1.55; color: var(--g-tinta); }
    .gnothi__pasos b { font-weight: 700; }
    .gnothi__pasos span { color: var(--g-tenue); }
    .gnothi code { font-family: ui-monospace, "Cascadia Mono", Consolas, monospace; font-size: 13px; background: var(--g-suave); border: 1px solid var(--g-linea); border-radius: 5px; padding: 2px 6px; }

    .gnothi__enlace { margin: 18px 0 0; padding: 14px 16px; background: var(--g-suave); border: 1px solid var(--g-linea); border-radius: calc(var(--g-radio) + 4px); display: grid; grid-template-columns: minmax(0, 1fr) auto; gap: 10px 14px; align-items: center; font-size: 13.5px; color: var(--g-tenue); line-height: 1.5; }
    .gnothi__enlace > span { grid-column: 1 / -1; }
    .gnothi__enlace > code { display: block; overflow-wrap: anywhere; background: var(--g-carta); padding: 7px 10px; }
    .gnothi__enlace button { display: none; font: inherit; font-size: 13px; font-weight: 600; color: var(--g-acento); background: var(--g-carta); border: 1px solid var(--g-linea); border-radius: 999px; padding: 7px 14px; cursor: pointer; }
    .js .gnothi__enlace button { display: inline-block; }
    .gnothi__enlace button:hover { background: var(--g-acento); color: var(--g-sobre-acento); border-color: var(--g-acento); }

    .gnothi__suelta { margin: 24px 0 0; color: var(--g-tenue); font-size: 14px; max-width: 70ch; line-height: 1.55; }
    .gnothi__suelta b { color: var(--g-tinta); font-weight: 700; }
    .gnothi__suelta a { color: var(--g-acento); }

    @media (max-width: 760px) { .gnothi__ambiente { grid-template-columns: 1fr; gap: 22px; } }
  </style>

  <section class="gnothi${c.claseExtra ? ' ' + c.claseExtra : ''}" id="gnothi">
    <h2>${v.titulo}</h2>
    <p class="gnothi__lead">${v.lead(c)}</p>

    <ul class="gnothi__ambientes" role="tablist" aria-label="${v.tablist}">
      <li><button type="button" role="tab" id="tab-android" aria-controls="amb-android" aria-selected="false" data-ambiente="android">${iconos.android} ${tA}</button></li>
      <li><button type="button" role="tab" id="tab-iphone" aria-controls="amb-iphone" aria-selected="false" data-ambiente="iphone">${iconos.iphone} ${tI}</button></li>
      <li><button type="button" role="tab" id="tab-computadora" aria-controls="amb-computadora" aria-selected="false" data-ambiente="computadora">${iconos.computadora} ${tC}</button></li>
    </ul>

    <div class="gnothi__ambiente" id="amb-android" role="tabpanel" aria-labelledby="tab-android" data-ambiente="android">
      <div>
        <h3>${v.android.h3}</h3>
        <p class="gnothi__nota">${v.android.nota(c)}</p>
        <div class="gnothi__descargas">
          <a class="gnothi__boton" href="https://play.google.com/store/apps/details?id=com.ncubo.gnothi.host" rel="noopener">${iconos.play}<span class="dos"><small>${v.android.tienda[0]}</small>${v.android.tienda[1]}</span></a>
          <a class="gnothi__boton gnothi__boton--suave" id="agregar-android" href="https://go.gnothi.app/alta">${v.android.agregar(c)}</a>
        </div>
      </div>
      <div>
        ${pasos(v.android.pasos(c))}
        <div class="gnothi__enlace">
          <span>${v.android.enlace(c)}</span>
          <code id="enlace-movil">https://go.gnothi.app/alta?bundle=…/descargas/movil/${c.modulo}.gmod</code>
          <button type="button" data-copiar="enlace-movil">${v.copiar[0]}</button>
        </div>
      </div>
    </div>

    <div class="gnothi__ambiente" id="amb-iphone" role="tabpanel" aria-labelledby="tab-iphone" data-ambiente="iphone">
      <div>
        <h3>${v.iphone.h3}</h3>
        <p class="gnothi__nota">${v.iphone.nota}</p>
        <div class="gnothi__descargas">
          <span class="gnothi__boton gnothi__boton--inerte" aria-disabled="true"><span class="dos"><small>${v.iphone.tienda[0]}</small>${v.iphone.tienda[1]}</span></span>
          <a class="gnothi__boton gnothi__boton--suave" id="agregar-iphone" href="https://go.gnothi.app/alta">${v.android.agregar(c)}</a>
        </div>
      </div>
      <div>
        ${pasos(v.iphone.pasos(c))}
      </div>
    </div>

    <div class="gnothi__ambiente" id="amb-computadora" role="tabpanel" aria-labelledby="tab-computadora" data-ambiente="computadora">
      <div>
        <h3>${v.computadora.h3}</h3>
        <p class="gnothi__nota">${v.computadora.nota}</p>
        <div class="gnothi__descargas">
          <a class="gnothi__boton" href="descargas/gnothi/Gnothi.Host.zip" download>${iconos.bajar}<span class="dos"><small>${v.computadora.tienda[0]}</small>${v.computadora.tienda[1]}</span></a>
          <a class="gnothi__boton gnothi__boton--suave" href="descargas/${c.modulo}.gmod" download>${v.computadora.modulo(c)}</a>
        </div>
      </div>
      <div>
        ${pasos(v.computadora.pasos(c))}
        <div class="gnothi__enlace">
          <span>${v.computadora.enlace(c)}</span>
          <code id="enlace-escritorio">…/descargas/${c.modulo}.gmod</code>
          <button type="button" data-copiar="enlace-escritorio">${v.copiar[0]}</button>
        </div>
      </div>
    </div>

    <p class="gnothi__suelta">${v.suelta(c)}</p>
  </section>

  <script>
    (function () {
      document.documentElement.classList.add('js');

      // The enrollment link, with the origin this page is served from: the hosted form for the
      // phones, the cargo form for the computer. Both are called ${c.modulo}.gmod (armar-bundle.ps1).
      var origen = location.origin;
      var movil = origen + '/descargas/movil/${c.modulo}.gmod';
      var cargo = origen + '/descargas/${c.modulo}.gmod';
      var alta = 'https://go.gnothi.app/alta?bundle=' + encodeURIComponent(movil);
      ['agregar-android', 'agregar-iphone'].forEach(function (id) { var a = document.getElementById(id); if (a) a.href = alta; });
      var eMovil = document.getElementById('enlace-movil'), eEscritorio = document.getElementById('enlace-escritorio');
      if (eMovil) eMovil.textContent = alta;
      if (eEscritorio) eEscritorio.textContent = cargo;

      // Copy: the button says "${v.copiar[1]}" for a moment. Without a clipboard (plain http off
      // localhost) the text is left selected to copy by hand.
      function seleccionar(nodo) { var r = document.createRange(); r.selectNodeContents(nodo); var s = window.getSelection(); s.removeAllRanges(); s.addRange(r); }
      document.querySelectorAll('[data-copiar]').forEach(function (b) {
        b.addEventListener('click', function () {
          var code = document.getElementById(b.getAttribute('data-copiar')); if (!code) return;
          var listo = function () { var antes = b.textContent; b.textContent = '${v.copiar[1]}'; setTimeout(function () { b.textContent = antes; }, 1600); };
          if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(code.textContent).then(listo, function () { seleccionar(code); });
          else seleccionar(code);
        });
      });

      // The device: proposed by the user agent, changed by the person.
      var tabs = Array.prototype.slice.call(document.querySelectorAll('.gnothi__ambientes [role="tab"]'));
      var paneles = Array.prototype.slice.call(document.querySelectorAll('.gnothi__ambiente'));
      function elegir(nombre) {
        tabs.forEach(function (t) { t.setAttribute('aria-selected', String(t.dataset.ambiente === nombre)); });
        paneles.forEach(function (p) { p.classList.toggle('activo', p.dataset.ambiente === nombre); });
      }
      tabs.forEach(function (t) { t.addEventListener('click', function () { elegir(t.dataset.ambiente); }); });
      var ua = navigator.userAgent || '';
      elegir(/Android/i.test(ua) ? 'android' : /iPhone|iPad|iPod/i.test(ua) ? 'iphone' : 'computadora');
    })();
  </script>
`;
}

// ------------------------------------------------------------------ the four institutions
const instituciones = [
  {
    carpeta: 'Examenes', voz: 'usted', modulo: 'examenes', marca: 'Delfos',
    quien: 'Laboratorio Clínico Delfos', registro: 'su historia de exámenes',
    pregunta: '«¿Cómo viene mi colesterol este año?»',
    tema: { acento: 'var(--app-azul)', acentoHondo: '#1257cc', sobreAcento: '#ffffff', tinta: 'var(--app-navy)', tenue: 'var(--lab-muted)', linea: 'var(--lab-border)', suave: '#eef4fd', carta: '#ffffff', radio: '12px', h2: '1.6rem', pesoTitulo: 800,
            extra: 'margin-top: 56px; padding-top: 40px; border-top: 1px solid #dbe8fa; font-family: "Segoe UI", "Helvetica Neue", Arial, sans-serif;' },
    empalmar(html, sec) {
      // The "Así de simple" steps of the standalone give way to the Gnothi steps; the download
      // card stops saying "not in stores" and its button leads to the steps.
      const a = html.indexOf('    <div class="pasosApp">');
      const b = html.indexOf('    <div class="privacidad">');
      if (a < 0 || b < a) throw new Error('Examenes: no encontré pasosApp/privacidad');
      html = html.slice(0, a) + sec.replace(/\n/g, '\n  ').trimEnd() + '\n\n' + html.slice(b);
      html = reemplazar(html, 'Examenes',
        [`<p class="descargaTexto">No está en tiendas: la descarga directo de esta página, de la
            institución en la que ya confía.</p>`,
         `<p class="descargaTexto">Se usa dentro de Gnothi: gratuita, en Google Play y para Windows.
            Su laboratorio y sus otras instituciones, en la misma aplicación.</p>`],
        [`        <a class="btnDownload" href="/app/">`, `        <a class="btnDownload" href="#gnothi">`],
        [`          Descargar aplicación\n        </a>\n        <p class="compatibilidad">Compatible con Windows, macOS y Linux</p>`,
         `          Usarla con Gnothi\n        </a>\n        <p class="compatibilidad">Android, iPhone y Windows. Abajo, el paso a paso.</p>`]);
      return html;
    },
  },
  {
    carpeta: 'Municipalidad', voz: 'vos', modulo: 'municipalidad', marca: 'MONTEVERDE Municipal Services',
    quien: 'La Municipalidad de Monteverde', registro: 'tu cuenta',
    pregunta: '«¿Por qué me subió bienes inmuebles?»',
    tema: { acento: 'var(--indigo)', acentoHondo: 'var(--indigo-alto)', sobreAcento: 'var(--sobre-indigo)', tinta: 'var(--tinta)', tenue: 'var(--tinta-suave)', linea: 'var(--linea)', suave: 'var(--acento-fondo)', carta: 'var(--papel)', radio: '10px', h2: '26px', pesoTitulo: 750,
            extra: 'background: var(--papel); border: 1px solid var(--linea); border-radius: 14px; padding: 24px; margin: 0 0 22px; box-shadow: var(--sombra);' },
    empalmar(html, sec) {
      const marca = '    <div class="tarjeta abrir">';
      const a = html.indexOf(marca);
      if (a < 0) throw new Error('Municipalidad: no encontré la tarjeta abrir');
      html = html.slice(0, a) + sec.replace(/\n/g, '\n  ').trimEnd() + '\n\n' + html.slice(a);
      return reemplazar(html, 'Municipalidad',
        ['<h1>Tu cuenta con la municipalidad, en tu propia computadora.</h1>', '<h1>Tu cuenta con la municipalidad, en tu propio aparato.</h1>'],
        [`        No en la nuestra. La app se abre en tu navegador, guarda tu cuenta en una carpeta que elegís
        vos, y sigue contestando con la red apagada.`,
         `        No en la nuestra. La app se usa dentro de Gnothi, guarda tu cuenta en una carpeta que elegís
        vos, y sigue contestando con la red apagada.`],
        ['        <h2>Abrila</h2>', '        <h2>Sin Gnothi: abrila suelta</h2>']);
    },
  },
  {
    carpeta: 'Concesionario', voz: 'en', modulo: 'dealership', marca: 'Sotaventos Motors',
    quien: 'Sotaventos Motors', registro: 'your vehicle’s record',
    pregunta: '“Is my car still under warranty?”',
    tema: { acento: 'var(--marca)', acentoHondo: '#16294a', sobreAcento: '#ffffff', tinta: 'var(--tinta)', tenue: 'var(--tinta-suave)', linea: 'var(--borde)', suave: 'var(--papel)', carta: 'var(--tarjeta)', radio: '7px', h2: '26px', pesoTitulo: 700,
            extra: 'background: var(--tarjeta); border: 1px solid var(--borde); border-radius: 10px; padding: 24px; margin: 0 0 22px;' },
    empalmar(html, sec) {
      const marca = '    <div class="tarjeta">\n        <h2>Install it</h2>';
      const a = html.indexOf(marca);
      if (a < 0) throw new Error('Concesionario: no encontré la tarjeta Install it');
      html = html.slice(0, a) + sec.replace(/\n/g, '\n  ').trimEnd() + '\n\n' + html.slice(a);
      return reemplazar(html, 'Concesionario',
        ['<h1>Your car\'s maintenance schedule, on your own computer.</h1>', '<h1>Your car\'s maintenance schedule, on your own device.</h1>'],
        [`        Not on ours. The app installs on your machine, keeps your vehicle's record in a folder you
        pick, and works with the network off.`,
         `        Not on ours. The app runs inside Gnothi, keeps your vehicle's record in a folder you
        pick, and works with the network off.`],
        ['        <h2>Install it</h2>', '        <h2>Without Gnothi: open it on its own</h2>']);
    },
  },
  {
    carpeta: 'Cargas', voz: 'vos', modulo: 'cargas', marca: 'Faro',
    quien: 'Estaciones Faro', registro: 'tu bitácora',
    pregunta: '«¿Cuánto está rindiendo mi carro?»',
    tema: { acento: 'var(--cobre)', acentoHondo: 'var(--cobre-claro)', sobreAcento: '#ffffff', tinta: 'var(--tinta)', tenue: 'var(--tenue)', linea: 'var(--linea)', suave: 'var(--carta)', carta: 'rgba(6, 39, 48, .55)', radio: '8px', h2: '1.7rem', pesoTitulo: 400,
            extra: 'margin: 8px 0 28px; font-family: var(--sans);' },
    empalmar(html, sec) {
      const marca = '        <div class="promesa">';
      const a = html.indexOf(marca);
      if (a < 0) throw new Error('Cargas: no encontré la promesa');
      // The dark page sets its titles in the serif.
      sec = sec.replace('.gnothi h2 { font-size:', '.gnothi h2 { font-family: var(--serif); font-size:');
      html = html.slice(0, a) + sec.replace(/\n/g, '\n      ').trimEnd() + '\n\n' + html.slice(a);
      return reemplazar(html, 'Cargas',
        [`<p style="margin-top:18px"><a class="boton" href="/app/">Instalar Mis cargas</a></p>`,
         `<p style="margin-top:18px"><a class="boton" href="#gnothi">Usarla con Gnothi</a></p>`],
        [`                    La bitácora vive <strong>en una carpeta tuya, en tu computadora</strong>.`,
         `                    La bitácora vive <strong>en una carpeta tuya, en tu propio aparato</strong>.`],
        [`                <b>1 · Instalás la app</b>`, `                <b>1 · Instalás Gnothi y agregás Faro</b>`]);
    },
  },
];

function reemplazar(html, quien, ...pares) {
  for (const [viejo, nuevo] of pares) {
    if (!html.includes(viejo)) throw new Error(`${quien}: no encontré «${viejo.slice(0, 60)}»`);
    html = html.replace(viejo, nuevo);
  }
  return html;
}

for (const c of instituciones) {
  const ruta = `${raiz}/${c.carpeta}/PaginaDeDescarga/descarga/index.html`;
  let html = await readFile(ruta, 'utf8');
  const crlf = html.includes('\r\n');
  html = html.split('\r\n').join('\n');
  if (html.includes('id="gnothi"')) { console.log(`${c.carpeta}: ya tiene la sección, no se toca`); continue; }
  html = c.empalmar(html, seccion(c));
  await writeFile(ruta, crlf ? html.split('\n').join('\r\n') : html, 'utf8');
  console.log(`${c.carpeta}: empalmado (${html.length} bytes)`);
}
