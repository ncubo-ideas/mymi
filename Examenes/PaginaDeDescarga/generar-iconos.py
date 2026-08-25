#!/usr/bin/env python3
"""
Genera los iconos de la app sin dependencias: PNG escrito a mano con zlib.

    python3 PaginaDeDescarga/generar-iconos.py App/Examenes.Wasm/wwwroot

Vive junto a generar-certificado.sh porque produce ASSETS DE MARCA, que es lo que la
convencion ubica en PaginaDeDescarga. La salida va al wwwroot de la caratula, que es
quien los sirve y los declara en su manifest.

Se escribe el PNG a mano y no con una libreria porque esta maquina no tiene ninguna -ni
PIL, ni rsvg, ni ImageMagick- y agregar una dependencia de build para tres iconos que
cambian una vez al ano no vale. zlib y struct vienen con Python.

LA MARCA ES LA SERIE. Tres tramos planos y un salto — la forma exacta de los cuatro
antigenos del guion (3,0 -> 3,1 -> 3,0 -> 5,2). No es decoracion: es lo unico que la app
hace y que una hoja de resultados no puede.

A 32 px la linea se lee como un signo abstracto de "algo subio"; a 512 se lee la serie.
El punto final va en ambar, que en toda la app significa una sola cosa: esto exige
atencion.
"""

import zlib
import struct
import math

PINO  = (31, 64, 52)
PAPEL = (246, 243, 236)
AMBAR = (180, 84, 26)

MUESTRAS = 3          # supersampling por eje: 9 muestras por pixel final


def distancia_a_segmento(px, py, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    largo = dx * dx + dy * dy
    if largo == 0:
        return math.hypot(px - ax, py - ay)
    t = max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / largo))
    return math.hypot(px - (ax + t * dx), py - (ay + t * dy))


def dibujar(lado):
    """Devuelve un bytearray RGB de lado*lado."""
    u = lado / 100.0                      # 1 unidad = 1% del lado

    radio_esquina = 22 * u                # cuadrado redondeado, no circulo

    # TAMANO OPTICO. Un icono no se escala linealmente: a 32 px un trazo "correcto"
    # desaparece. Cuanto mas chico, mas grueso en proporcion y mas lleno el cuadro, que
    # es como se dibujan los iconos de verdad.
    if lado <= 48:
        grosor_pct, radio_pct = 13.0, 14.0
        puntos_pct = [(14, 64), (38, 62), (56, 64), (78, 30)]
    elif lado <= 128:
        grosor_pct, radio_pct = 10.0, 11.0
        puntos_pct = [(16, 65), (39, 62), (58, 65), (79, 30)]
    else:
        grosor_pct, radio_pct = 8.0, 9.5
        puntos_pct = [(18, 66), (40, 63), (59, 66), (79, 30)]

    grosor = max(1.6, grosor_pct * u) / 2.0
    puntos = [(x * u, y * u) for x, y in puntos_pct]
    radio_punto = max(2.4, radio_pct * u)

    pixeles = bytearray()
    paso = 1.0 / MUESTRAS
    media = MUESTRAS * MUESTRAS

    for y in range(lado):
        fila = bytearray()
        for x in range(lado):
            acum_r = acum_g = acum_b = 0
            for sy in range(MUESTRAS):
                py = y + (sy + 0.5) * paso
                for sx in range(MUESTRAS):
                    px = x + (sx + 0.5) * paso

                    # ¿dentro del cuadrado redondeado?
                    cx = min(max(px, radio_esquina), lado - radio_esquina)
                    cy = min(max(py, radio_esquina), lado - radio_esquina)
                    dentro = math.hypot(px - cx, py - cy) <= radio_esquina

                    if not dentro:
                        color = None            # transparente -> se compone sobre papel
                    elif math.hypot(px - puntos[-1][0], py - puntos[-1][1]) <= radio_punto:
                        color = AMBAR           # el punto que exige atencion
                    else:
                        cerca = False
                        for i in range(len(puntos) - 1):
                            ax, ay = puntos[i]
                            bx, by = puntos[i + 1]
                            if distancia_a_segmento(px, py, ax, ay, bx, by) <= grosor:
                                cerca = True
                                break
                        color = PAPEL if cerca else PINO

                    if color is None:
                        acum_r += PAPEL[0]; acum_g += PAPEL[1]; acum_b += PAPEL[2]
                    else:
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
    for nombre, lado in [("favicon.png", 32), ("icon-192.png", 192), ("icon-512.png", 512)]:
        escribir_png(f"{destino}/{nombre}", lado, dibujar(lado))
        print(f"  {nombre}  {lado}x{lado}")
