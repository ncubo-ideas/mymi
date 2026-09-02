#!/usr/bin/env python3
"""Convierte la fuente de la marca a un woff2 recortado, para servirla nosotros.

    python generar-fuente.py marca/PlusJakartaSans-VariableFont_wght.ttf ../App/Compras.Wasm/wwwroot/fuentes

POR QUE EXISTE. Plus Jakarta Sans es la tipografia del diseño de Fernanda, y su canvas la
trae de Google Fonts. Esta app promete funcionar "sin nosotros, y sin conexion": una fuente
que se baja de internet se cae el dia que alguien la abra sin red, y la tipografia salta al
fallback. No es una regla de estilo — es una promesa del producto.

LA VARIABLE Y NO LAS ESTATICAS. El zip trae 16 TTF estaticos (92 KB cada uno) y una fuente
VARIABLE con el eje wght de 200 a 800 en un solo archivo. El CSS usa 400, 500, 600, 700 y
800: con las estaticas serian CINCO descargas de 92 KB; con la variable es UNA, y ademas
cualquier peso intermedio sale gratis si algun dia se necesita.

LA ITALICA NO ENTRA. El documento de marca no la pide y la app no la usa; los pocos <em> que
hay los inclina el browser sinteticamente, que a este tamaño no se distingue. Son 174 KB que
no se bajan.

EL RECORTE. Una fuente completa trae 1.186 glifos -griego, cirilico, vietnamita-, y esta app
habla español. Se conservan el latino con sus acentos, la puntuacion general, la moneda -que
incluye el ₡, verificado- y los cuatro simbolos que las pantallas usan de verdad. El resto
se va.

DOS GLIFOS QUE LA FUENTE NO TIENE, y conviene saberlo antes que descubrirlo en pantalla:
❀ (U+2740) y □ (U+25A2). El cuadro se usaba como marcador de "producto sin foto" y se
reemplazo por una caja dibujada en CSS: un glifo que la fuente no trae cae en otra tipografia
y se ve de otra familia justo al lado de la que si carga.
"""

import os
import sys

try:
    from fontTools import subset
    from fontTools.ttLib import TTFont
except ImportError:
    sys.exit("Hacen falta fonttools y brotli para regenerar la fuente:\n"
             "  pip install fonttools brotli\n"
             "El woff2 ya generado esta commiteado, asi que esto solo hace falta si cambia "
             "la tipografia de la marca.")

# LO QUE SE CONSERVA. Cada rango con su razon, para que el proximo que lo toque sepa que
# esta quitando si recorta mas.
RANGOS = [
    "U+0000-00FF",        # latino basico y latin-1: los acentos, la ñ, los signos ¿ ¡ « » ·
    "U+0131",             # i sin punto
    "U+0152-0153",        # Œ œ
    "U+02BB-02BC",        # apostrofos modificadores
    "U+2000-206F",        # puntuacion general: – — ' ' " " … ‹ ›
    "U+20A0-20BF",        # monedas. AQUI ESTA EL ₡ (U+20A1), verificado
    "U+2190",             # ← el "volver" de la ficha de producto
    "U+2192",             # → el "ver todos" y el antes/ahora
    "U+2212",             # − menos matematico
    "U+25B2",             # ▲ subio
    "U+25BC",             # ▼ bajo
    "U+2022",             # • por si alguna lista lo usa
]


def main():
    if len(sys.argv) < 3:
        sys.exit(__doc__)

    origen, destino = os.path.abspath(sys.argv[1]), os.path.abspath(sys.argv[2])
    if not os.path.exists(origen):
        sys.exit("No esta la fuente: " + origen)

    os.makedirs(destino, exist_ok=True)

    # Se avisa de los glifos que no estan, antes de recortar: recortar no los quita -no
    # estaban- pero quien lea la salida tiene que saber en que va a caer el fallback.
    fuente = TTFont(origen)
    cmap = fuente.getBestCmap()
    # Los nombres van SIN el glifo: la consola de Windows es cp1252 y no puede imprimir el
    # caracter del que justamente se esta avisando que falta.
    for nombre, punto in (("la flor", 0x2740), ("el cuadro", 0x25A2)):
        if punto not in cmap:
            print("  aviso: la fuente NO trae %s (U+%04X)" % (nombre, punto))

    ejes = [(a.axisTag, a.minValue, a.maxValue) for a in fuente["fvar"].axes]
    print("  ejes: %s · glifos: %d" % (ejes, len(fuente.getGlyphOrder())))

    salida = os.path.join(destino, "PlusJakartaSans.woff2")
    subset.main([
        origen,
        "--output-file=" + salida,
        "--flavor=woff2",
        "--unicodes=" + ",".join(RANGOS),
        # El eje wght se CONSERVA: sin esto el subset instancia la fuente en un solo peso y
        # se pierde justo lo que la hace variable.
        "--layout-features+=kern,liga",
        "--no-hinting",
        "--desubroutinize",
    ])

    recortada = TTFont(salida)
    print("  %s · %.1f KB · %d glifos · ejes %s" % (
        os.path.basename(salida), os.path.getsize(salida) / 1024,
        len(recortada.getGlyphOrder()),
        [(a.axisTag, a.minValue, a.maxValue) for a in recortada["fvar"].axes]
            if "fvar" in recortada else "NINGUNO (se perdio la variabilidad)"))
    print("  original: %.1f KB" % (os.path.getsize(origen) / 1024))


if __name__ == "__main__":
    main()
