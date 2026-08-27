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

LA MARCA (⚠️ PROVISIONAL) ES EL FARO DE UN SOLO TRAZO: torre, linterna y haz, blancos
sobre el teal petroleo del brief, sin texto y sin relleno. Un faro orienta a quien
viaja, que es lo que la app hace con el carro de Sonia. Pendiente de la especificacion
de Geaninna: cuando llegue, este archivo y los tokens de app.css son el cambio entero.

A tamanos chicos el trazo engorda y el dibujo crece, porque un icono no se escala
linealmente.
"""

import zlib
import struct
import math

TEAL   = (11, 60, 73)      # #0B3C49, el teal petroleo provisional del brief
BLANCO = (255, 255, 255)

MUESTRAS = 3          # supersampling por eje: 9 muestras por pixel final

# La geometria del faro, en el espacio de 40x40 del SVG de la marca (MisCargas.razor
# y descarga/index.html usan el mismo trazo). Todo es contorno o segmento: nada se
# rellena.
TORRE = [(16.0, 34.0), (18.0, 12.0), (22.0, 12.0), (24.0, 34.0)]
LINTERNA = [(17.5, 7.0), (22.5, 7.0), (22.5, 12.0), (17.5, 12.0)]
HACES = [
    ((26.0, 8.0), (32.5, 5.5)),
    ((26.0, 11.0), (33.5, 11.0)),
]


def sdf_poligono(px, py, vertices):
    """Distancia con signo a un poligono (negativa adentro), sin importar el sentido.

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


def distancia_a_segmento(px, py, a, b):
    ax, ay = a
    bx, by = b
    ex, ey = bx - ax, by - ay
    wx, wy = px - ax, py - ay
    t = max(0.0, min(1.0, (wx * ex + wy * ey) / (ex * ex + ey * ey)))
    return math.hypot(wx - t * ex, wy - t * ey)


def geometria(lado):
    """La escala del dibujo y el grosor del trazo, con tamano optico."""
    if lado <= 48:
        alto = lado * 0.86
        grosor = max(1.6, lado * 0.075)
    elif lado <= 192:
        alto = lado * 0.74
        grosor = lado * 0.050
    else:
        alto = lado * 0.62
        grosor = lado * 0.040

    # El dibujo ocupa de y=5.5 a y=34 en el espacio de 40: se escala esa caja y se
    # centra, con el faro apenas a la izquierda porque el haz pesa a la derecha.
    escala = alto / 28.5
    dx = lado / 2.0 - 23.0 * escala
    dy = lado / 2.0 - 19.75 * escala
    return escala, dx, dy, grosor


def dibujar(lado):
    """Devuelve un bytearray RGB de lado*lado."""
    escala, dx, dy, grosor = geometria(lado)

    torre = [(x * escala + dx, y * escala + dy) for x, y in TORRE]
    linterna = [(x * escala + dx, y * escala + dy) for x, y in LINTERNA]
    haces = [((a[0] * escala + dx, a[1] * escala + dy),
              (b[0] * escala + dx, b[1] * escala + dy)) for a, b in HACES]

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

                    # los contornos de la torre y la linterna
                    trazo = abs(sdf_poligono(px, py, torre)) <= mitad
                    if not trazo:
                        trazo = abs(sdf_poligono(px, py, linterna)) <= mitad

                    # los haces, como segmentos con grosor
                    if not trazo:
                        for a, b in haces:
                            if distancia_a_segmento(px, py, a, b) <= mitad:
                                trazo = True
                                break

                    color = BLANCO if trazo else TEAL
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
