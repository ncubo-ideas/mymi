#!/usr/bin/env python3
"""
Genera los iconos de la app sin dependencias: PNG escrito a mano con zlib.

    python3 PaginaDeDescarga/generar-iconos.py App/Cargas.Wasm/wwwroot
    python3 PaginaDeDescarga/generar-iconos.py PaginaDeDescarga/descarga --pagina

Vive junto a generar-certificado.sh porque produce ASSETS DE MARCA, que es lo que la
convencion ubica en PaginaDeDescarga. La salida va al wwwroot de la caratula (y a la
raiz de la pagina, con --pagina), que son quienes los sirven.

Se escribe el PNG a mano y no con una libreria porque esta maquina no tiene ninguna -ni
PIL, ni rsvg, ni ImageMagick- y agregar una dependencia de build para tres iconos que
cambian una vez al ano no vale. zlib y struct vienen con Python.

LA MARCA ES LA GOTA EN EL CIRCULO. La gota por la muestra, el circulo por el oraculo.
Es el logo de Laboratorio Clinico Delfos, y el manual es explicito para el icono de
aplicacion: gota y circulo BLANCOS, centrados, sobre #123f6d, sin texto. Solo trazo:
nada se rellena, nada se degrada, y la gota no se separa del circulo.

El area libre pedida es media medida del diametro del circulo; a tamanos chicos el
trazo engorda y el circulo crece, porque un icono no se escala linealmente.
"""

import zlib
import struct
import math

AZUL   = (18, 63, 109)     # #123f6d
BLANCO = (255, 255, 255)

MUESTRAS = 3          # supersampling por eje: 9 muestras por pixel final


def sdf_circulo(px, py, cx, cy, r):
    return math.hypot(px - cx, py - cy) - r


def sdf_triangulo(px, py, vertices):
    """Distancia con signo a un triangulo (negativa adentro), sin importar el sentido.

    El adentro/afuera se decide por conteo de cruces (ray casting) y no por el signo
    de los productos cruzados, para no depender del orden de los vertices.
    """
    minima = float("inf")
    adentro = False
    n = len(vertices)
    for i in range(n):
        ax, ay = vertices[i]
        bx, by = vertices[(i + 1) % n]
        ex, ey = bx - ax, by - ay
        wx, wy = px - ax, py - ay
        t = max(0.0, min(1.0, (wx * ex + wy * ey) / (ex * ex + ey * ey)))
        minima = min(minima, math.hypot(wx - t * ex, wy - t * ey))
        if (ay > py) != (by > py) and px < ax + (py - ay) * (bx - ax) / (by - ay):
            adentro = not adentro
    return -minima if adentro else minima


def geometria(lado):
    """El circulo, la gota y el grosor, en pixeles, con tamano optico."""
    centro = lado / 2.0

    # TAMANO OPTICO. A 512 se respeta el area libre del manual (media medida del
    # diametro alrededor); a 32 un logo "correcto" desaparece, asi que crece.
    if lado <= 48:
        radio = lado * 0.40
        grosor = max(1.6, lado * 0.075)
    elif lado <= 192:
        radio = lado * 0.33
        grosor = lado * 0.055
    else:
        radio = lado * 0.25
        grosor = lado * 0.042

    # La gota, a escala del SVG del manual (circulo r=18; apice en -11, cuenco en
    # +1.4 con radio 6.8).
    apice = (centro, centro - radio * (11.0 / 18.0))
    cuenco = (centro, centro + radio * (1.4 / 18.0))
    radio_cuenco = radio * (6.8 / 18.0)

    return centro, radio, grosor, apice, cuenco, radio_cuenco


def tangentes(apice, cuenco, r):
    """Los dos puntos donde las rectas desde el apice tocan el cuenco."""
    tx, ty = apice
    bx, by = cuenco
    d = math.hypot(tx - bx, ty - by)
    beta = math.acos(min(1.0, r / d))
    angulo = math.atan2(ty - by, tx - bx)
    return [
        (bx + r * math.cos(angulo - beta), by + r * math.sin(angulo - beta)),
        (bx + r * math.cos(angulo + beta), by + r * math.sin(angulo + beta)),
    ]


def dibujar(lado):
    """Devuelve un bytearray RGB de lado*lado."""
    centro, radio, grosor, apice, cuenco, radio_cuenco = geometria(lado)
    p1, p2 = tangentes(apice, cuenco, radio_cuenco)
    triangulo = [apice, p1, p2]

    mitad = grosor / 2.0
    paso = 1.0 / MUESTRAS
    media = MUESTRAS * MUESTRAS

    pixeles = bytearray()
    for y in range(lado):
        fila = bytearray()
        for x in range(lado):
            acum_r = acum_g = acum_b = 0
            for sy in range(MUESTRAS):
                py = y + (sy + 0.5) * paso
                for sx in range(MUESTRAS):
                    px = x + (sx + 0.5) * paso

                    # el anillo del circulo
                    trazo = abs(sdf_circulo(px, py, centro, centro, radio)) <= mitad

                    # el contorno de la gota: la union del cuenco y el triangulo
                    # del apice, trazada. min() de los dos SDF es el SDF de la
                    # union, y |union| <= mitad es su contorno.
                    if not trazo:
                        gota = min(
                            sdf_circulo(px, py, cuenco[0], cuenco[1], radio_cuenco),
                            sdf_triangulo(px, py, triangulo),
                        )
                        trazo = abs(gota) <= mitad

                    color = BLANCO if trazo else AZUL
                    acum_r += color[0]; acum_g += color[1]; acum_b += color[2]

            fila += bytes((acum_r // media, acum_g // media, acum_b // media))
        pixeles += b"\x00" + fila           # filtro 0 por scanline
    return bytes(pixeles)


def escribir_png(ruta, lado, crudo):
    def trozo(tipo, datos):
        return (struct.pack(">I", len(datos)) + tipo + datos
                + struct.pack(">I", zlib.crc32(tipo + datos) & 0xFFFFFFFF))

    cabecera = struct.pack(">IIBBBBB", lado, lado, 8, 2, 0, 0, 0)   # 8 bits, RGB
    with open(ruta, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n")
        f.write(trozo(b"IHDR", cabecera))
        f.write(trozo(b"IDAT", zlib.compress(crudo, 9)))
        f.write(trozo(b"IEND", b""))


if __name__ == "__main__":
    import sys
    destino = sys.argv[1]
    pagina = "--pagina" in sys.argv[2:]

    archivos = ([("favicon.png", 32), ("apple-touch-icon.png", 180)] if pagina
                else [("favicon.png", 32), ("icon-192.png", 192), ("icon-512.png", 512)])

    for nombre, lado in archivos:
        escribir_png(f"{destino}/{nombre}", lado, dibujar(lado))
        print(f"  {nombre}  {lado}x{lado}")
