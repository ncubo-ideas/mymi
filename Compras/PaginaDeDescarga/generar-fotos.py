#!/usr/bin/env python3
"""Recorta las fotos de producto desde la hoja de stickers del diseño.

    python generar-fotos.py "Hoja de stickers 3D de supermercado.png" ../App/Compras.Wasm/wwwroot/assets
    python generar-fotos.py "hoja.png" .  --control     solo dibuja la hoja de control numerada

POR QUE EXISTE. Fernanda entrega los productos en UNA hoja con ciento y pico de stickers,
no en archivos sueltos. Recortarlos a mano es donde se cuelan los errores: un pixel de mas
por lado y el icono queda descentrado contra los que ya estaban.

LA HOJA NO SE COMMITEA, y es a proposito: pesa 2,6 MB, o sea CUATRO VECES el archivo mas
grande del repo. Lo que se commitea es este script con el numero de caja de cada producto
-la tabla ELEGIDOS de abajo-, que es lo que hace falta para volver a sacarlos. La hoja vive
con las entregas de diseño.

POR QUE COMPONENTES CONEXAS Y NO UNA REJILLA. La hoja PARECE una rejilla regular, pero las
filas se tocan: la proyeccion horizontal nunca baja a cero entre las filas 6 y 9 -el pan,
los snacks y las bebidas son altos-, asi que cortar por el minimo partiria algun icono por
la mitad.

Y NO SE VUELVEN A PEGAR LAS PIEZAS. La primera version unia las cajas cercanas, para juntar
los dos ajos o el racimo con su gajo suelto. Con una hoja tan densa esa union es TRANSITIVA
-A cerca de B, B cerca de C- y colapso las 152 cajas en una sola. Quedan separadas: los
pocos iconos partidos no son ninguno de los que usamos, y estan anotados abajo.
"""
import os
import sys

try:
    import numpy as np
    from PIL import Image, ImageDraw
except ImportError:
    sys.exit("Hacen falta pillow y numpy:\n  pip install pillow numpy")

# Separa el icono de la bruma: la hoja trae 446k pixeles con alfa ~2 y rgb pardo, artefacto
# del generador, invisible en pantalla pero suficiente para que la mascara vea contenido en
# todas partes y una proyeccion ingenua no encuentre ninguna calle.
UMBRAL = 40
# Debajo de esto un pixel es bruma y se limpia, para que el png no lleve un rectangulo tenue
# alrededor del icono.
LIMPIAR_DEBAJO_DE = 12
# Una componente mas chica que esto es basura, no un producto.
MINIMO_PX = 400

# QUE CAJA ES CADA PRODUCTO. El numero sale de la hoja de control -correr con --control para
# volver a verla- y se leen por filas, de arriba a abajo y de izquierda a derecha.
#
# Las elecciones que NO son obvias, para que se puedan discutir sin volver a mirar la hoja:
#
#   tilapia    la hoja no trae un filete blanco. #52 es el pescado entero, que es lo mas
#              cercano; #53 es un lomo de salmon y el color enganaria.
#   cloro      #143 es una botella verde. La hoja no trae un envase de cloro; entre esa y
#              el atomizador #141, una botella de litro se parece mas a lo que se compra.
#   olla       #147 es una freidora de aire. Es el unico electrodomestico de mostrador
#              redondo que hay; la alternativa era la licuadora, que se parece menos.
#   lasagna    #113 es una bandeja negra con comida. No hay una lasagna, y una bandeja es
#              exactamente como viene un congelado familiar.
#
# Y FALTA UNA: la pasta. La hoja no trae ningun espagueti -tiene cinco tazones de granos y
# cuatro frascos, ninguno es pasta-, y poner un tazon de garbanzos en su lugar seria peor
# que el hueco. Ese producto sigue mostrando su caja punteada, que dice la verdad.
ELEGIDOS = [
    # frutas y verduras
    (  7, 'espinaca'),
    (  8, 'brocoli'),
    # granos
    ( 17, 'frijol'),
    # lacteos
    ( 38, 'mantequilla'),
    # proteinas
    ( 48, 'carne'),
    ( 49, 'pollo'),
    ( 52, 'tilapia'),
    # embutidos
    ( 58, 'mortadela'),
    ( 60, 'salchicha'),
    ( 63, 'jamon'),
    # snacks y bebidas
    ( 79, 'papas'),
    ( 90, 'aceite'),
    (100, 'soda'),
    # comidas preparadas
    ( 50, 'rostizado'),
    (111, 'pizza'),
    (113, 'lasagna'),
    # limpieza
    (142, 'detergente'),
    (143, 'cloro'),
    # mascotas
    (127, 'perro'),
    # electrohogar
    (147, 'olla'),
]


def componentes(mask):
    """Etiqueta componentes conexas (8-vecinos) por corridas + union-find."""
    alto, _ = mask.shape
    padre = [0]

    def raiz(x):
        while padre[x] != x:
            padre[x] = padre[padre[x]]
            x = padre[x]
        return x

    def unir(a, b):
        ra, rb = raiz(a), raiz(b)
        if ra != rb:
            padre[max(ra, rb)] = min(ra, rb)

    previas = []
    por_fila = []
    for y in range(alto):
        fila = mask[y]
        if not fila.any():
            por_fila.append([])
            previas = []
            continue
        d = np.diff(np.concatenate(([0], fila.view(np.int8), [0])))
        actuales = []
        for x0, x1 in zip(np.where(d == 1)[0], np.where(d == -1)[0]):
            padre.append(len(padre))
            etq = len(padre) - 1
            for px0, px1, petq in previas:
                if px0 <= x1 and x0 <= px1:
                    unir(etq, petq)
            actuales.append((x0, x1, etq))
        por_fila.append(actuales)
        previas = actuales

    cajas = {}
    for y, actuales in enumerate(por_fila):
        for x0, x1, etq in actuales:
            r = raiz(etq)
            if r in cajas:
                cy0, cy1, cx0, cx1, n = cajas[r]
                cajas[r] = (min(cy0, y), max(cy1, y + 1), min(cx0, x0), max(cx1, x1), n + (x1 - x0))
            else:
                cajas[r] = (y, y + 1, x0, x1, x1 - x0)
    return list(cajas.values())


def en_orden_de_lectura(cajas):
    """Por filas de arriba a abajo, y dentro de cada fila de izquierda a derecha."""
    cajas = sorted(cajas, key=lambda c: (c[0], c[2]))
    filas = []
    for c in cajas:
        for f in filas:
            # misma fila si los tramos verticales se solapan de verdad, no si se rozan
            solape = min(f[0][1], c[1]) - max(f[0][0], c[0])
            if solape > 0.35 * min(f[0][1] - f[0][0], c[1] - c[0]):
                f.append(c)
                break
        else:
            filas.append([c])
    orden = []
    for f in filas:
        orden.extend(sorted(f, key=lambda c: c[2]))
    return orden


def main():
    if len(sys.argv) < 3:
        sys.exit(__doc__)
    hoja, destino = os.path.abspath(sys.argv[1]), os.path.abspath(sys.argv[2])
    solo_control = '--control' in sys.argv
    if not os.path.exists(hoja):
        sys.exit("No esta la hoja: " + hoja)
    os.makedirs(destino, exist_ok=True)

    im = Image.open(hoja).convert('RGBA')
    arr = np.array(im)
    orden = en_orden_de_lectura(
        [c for c in componentes(arr[:, :, 3] >= UMBRAL) if c[4] >= MINIMO_PX])
    print("  hoja %dx%d  ·  %d iconos" % (im.size[0], im.size[1], len(orden)))

    if solo_control:
        control = Image.new('RGBA', im.size, (255, 255, 255, 255))
        control.alpha_composite(im)
        d = ImageDraw.Draw(control)
        for i, (y0, y1, x0, x1, _) in enumerate(orden, 1):
            d.rectangle([x0, y0, x1, y1], outline=(220, 30, 30, 255), width=2)
            d.text((x0 + 3, y0 + 2), str(i), fill=(180, 0, 0, 255))
        ruta = os.path.join(destino, 'control.png')
        control.convert('RGB').save(ruta)
        print("  hoja de control: %s" % ruta)
        return

    # La bruma se va ANTES de recortar, para que ningun png cargue su rectangulo tenue.
    limpio = arr.copy()
    limpio[:, :, 3][limpio[:, :, 3] < LIMPIAR_DEBAJO_DE] = 0
    fuente = Image.fromarray(limpio, 'RGBA')

    total = 0
    for numero, nombre in ELEGIDOS:
        if numero > len(orden):
            sys.exit("La caja #%d no existe: la hoja cambio y hay que revisar ELEGIDOS "
                     "corriendo con --control." % numero)
        y0, y1, x0, x1, _ = orden[numero - 1]
        ruta = os.path.join(destino, 'p-%s.png' % nombre)
        fuente.crop((x0, y0, x1, y1)).save(ruta)
        kb = os.path.getsize(ruta) / 1024
        total += kb
        print("    #%-3d -> p-%-12s %3dx%-3d %5.1f KB" % (numero, nombre + '.png', x1 - x0, y1 - y0, kb))
    print("  %d fotos, %.0f KB" % (len(ELEGIDOS), total))


if __name__ == '__main__':
    main()
