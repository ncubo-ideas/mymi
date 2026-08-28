#!/usr/bin/env python3
"""Genera los iconos de La Guaria a partir de la flor, y en DOS familias.

    python generar-iconos.py ../App/Compras.Wasm/wwwroot

Vive en PaginaDeDescarga porque produce ASSETS DE MARCA, que es donde la convencion del
equipo los ubica; la salida va al wwwroot de la caratula, que es quien los sirve.

LA MARCA ES LA GUARIA MORADA, la flor nacional. El documento de marca da la receta del
icono de aplicacion sin lugar a dudas: "la flor centrada sobre fondo #2f6b3f, sin texto".
La flor es el archivo que dio Fernanda -su version en tres dimensiones, con el filete
blanco horneado-, y por eso este script COMPONE en vez de dibujar.

DIFIERE DE generar-iconos.py DE EXAMENES en dos cosas, y las dos vale explicarlas:

  1. Ese escribe el PNG a mano con zlib porque la maquina de Renan no tiene PIL. Esta si
     lo tiene, y aca la entrada es un PNG -no un vector-, asi que decodificarlo a mano
     seria escribir un lector de PNG para tres iconos. Los PNG generados SE COMMITEAN, de
     modo que nadie necesita PIL para compilar ni para correr la app: solo para regenerar.
  2. Ese dibuja el logo del laboratorio con distancias con signo, porque es trazo puro y
     escala distinto en cada tamaño. La flor es una ilustracion: se reduce, no se redibuja.

DOS FAMILIAS, Y LA RAZON ES LA QUE MAS SE EQUIVOCA EN LAS PWA:

  icon-N.png            para purpose "any". Cuadrado con esquinas redondeadas, como se ve
                        en una pestaña o en el dialogo de instalar. Es la version que
                        Fernanda diseño.

  icon-N-maskable.png   para purpose "maskable". CUADRADO PLENO, sin redondear, y con el
                        logo mas chico. Android le aplica SU propia mascara -circulo,
                        squircle, gota, segun el telefono-: si le damos la version ya
                        redondeada, la recorta otra vez y quedan las esquinas transparentes
                        comidas. Y la zona segura es un circulo del 80% del lado, asi que
                        un logo cuadrado inscrito ahi mide 0.8/raiz(2) = 56% del lado.

El manifiesto tenia UN SOLO archivo declarado para los dos propositos, que es el error
clasico: en un telefono con mascara circular, el icono se veia recortado.
"""

import os
import sys

try:
    from PIL import Image, ImageDraw
except ImportError:
    sys.exit("Hace falta Pillow para regenerar los iconos:  pip install pillow\n"
             "Los PNG ya generados estan commiteados, asi que esto solo hace falta si "
             "cambia la flor.")

# El verde del icono de aplicacion, del documento de marca (§1).
VERDE = (47, 107, 63)          # #2f6b3f

# Cuanto del lado ocupa la flor en cada familia.
#
# En la version "any" el sistema no recorta nada, asi que la flor puede respirar cerca del
# borde: 72% deja un margen parejo y se sigue reconociendo a 32 px.
#
# En la maskable la zona segura es un circulo del 80% del lado; un cuadrado inscrito en ese
# circulo mide 0.8/raiz(2) = 0.566. Se usa 56% para no quedar pegado al limite.
PARTE_ANY = 0.72
PARTE_MASKABLE = 0.56

# El radio de la esquina, como parte del lado. 22% es la proporcion de un icono de iOS, que
# es la referencia que la gente reconoce como "icono de app".
RADIO = 0.22

TAMANOS = (192, 512)
TAMANO_FAVICON = 32
TAMANO_APPLE = 180   # el que pide iOS para apple-touch-icon


def flor(ruta):
    """La flor, recortada a su contenido visible.

    SE RECORTA POR EL ALFA y no se usa el lienzo tal cual: el archivo original puede traer
    margen transparente asimetrico, y centrar el LIENZO en vez de la FLOR deja el icono
    corrido sin que se note de donde viene.
    """
    imagen = Image.open(ruta).convert("RGBA")
    caja = imagen.getchannel("A").getbbox()
    return imagen.crop(caja) if caja else imagen


def sobre_verde(la_flor, lado, parte, redondear):
    fondo = Image.new("RGBA", (lado, lado), VERDE + (255,))

    # La flor se escala por su lado MAYOR, para que una flor mas ancha que alta no se
    # deforme ni se salga.
    cabe = int(lado * parte)
    ancho, alto = la_flor.size
    escala = cabe / max(ancho, alto)
    nueva = la_flor.resize((max(1, round(ancho * escala)), max(1, round(alto * escala))),
                           Image.LANCZOS)

    fondo.alpha_composite(nueva, ((lado - nueva.width) // 2, (lado - nueva.height) // 2))

    if not redondear:
        return fondo

    # LAS ESQUINAS SE RECORTAN CON UNA MASCARA SUPERMUESTREADA: dibujar el redondeo al
    # tamaño final deja el borde con escalera, y un icono es justamente lo que la gente ve
    # chiquito y nitido.
    escala = 4
    mascara = Image.new("L", (lado * escala, lado * escala), 0)
    ImageDraw.Draw(mascara).rounded_rectangle(
        (0, 0, lado * escala - 1, lado * escala - 1),
        radius=int(lado * escala * RADIO), fill=255)
    fondo.putalpha(mascara.resize((lado, lado), Image.LANCZOS))
    return fondo


def main():
    if len(sys.argv) < 2:
        sys.exit(__doc__)

    destino = os.path.abspath(sys.argv[1])
    if not os.path.isdir(destino):
        sys.exit("No existe la carpeta de salida: " + destino)

    aca = os.path.dirname(os.path.abspath(__file__))
    origen = os.path.join(aca, "marca", "flor.png")
    if not os.path.exists(origen):
        sys.exit("No esta la flor en " + origen
                 + "\nEs el logo.png que dio Fernanda; va commiteado ahi como fuente del icono.")

    la_flor = flor(origen)
    print("flor recortada a %sx%s" % la_flor.size)

    escritos = []
    for lado in TAMANOS:
        for sufijo, parte, redondear in (("", PARTE_ANY, True),
                                         ("-maskable", PARTE_MASKABLE, False)):
            nombre = "icon-%d%s.png" % (lado, sufijo)
            sobre_verde(la_flor, lado, parte, redondear).save(os.path.join(destino, nombre))
            escritos.append(nombre)

    # apple-touch-icon: iOS NO respeta la transparencia -la compone sobre negro- y aplica
    # su propio redondeo. Asi que va plena y sin redondear, como la maskable.
    sobre_verde(la_flor, TAMANO_APPLE, PARTE_MASKABLE, False).save(
        os.path.join(destino, "apple-touch-icon.png"))
    escritos.append("apple-touch-icon.png")

    # El favicon va con la flor MAS GRANDE que la de 192: a 32 px el margen se come el
    # dibujo y la flor deja de reconocerse.
    sobre_verde(la_flor, TAMANO_FAVICON, 0.82, True).save(os.path.join(destino, "favicon.png"))
    escritos.append("favicon.png")

    for nombre in escritos:
        ruta = os.path.join(destino, nombre)
        print("  %-26s %6.1f KB" % (nombre, os.path.getsize(ruta) / 1024))


if __name__ == "__main__":
    main()
