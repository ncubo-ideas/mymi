#!/usr/bin/env python3
"""Genera el guion de compras de Sonia y VERIFICA los invariantes de la wiki.

Por que existe. El guion estaba escrito a mano en Program.cs: seis compras, y con seis
compras "la firma de su semana" no tiene de donde salir. Los invariantes del ecosistema
(Ecosistema/Preguntas.md) piden compras SEMANALES durante veinte meses, con dos totales de
agosto exactos y con cuentas de tickets que hay que poder verificar. Eso no se escribe a
mano: se genera, y el generador es el que se hace responsable de que los numeros cierren.

Los invariantes que sostiene, con su numero en la wiki:

  4  - helado 1x/mes hasta dic 2025, ~1x/semana desde ene 2026
     - embutidos y comidas preparadas ENTRAN en enero 2026
     - los frescos bajan de semanales a quincenales
  13 - agosto 2025 = 181.300 exactos - agosto 2026 = 201.656 exactos
     - tickets con helado: 12 en 2025 - 34 en 2026 (hasta agosto)
  14 - comida de Luna: 25.000 al mes

Si un invariante no se cumple, el script SE CAE. La wiki lo dice sin rodeos: "si el
ecosistema no puede darlas con estos datos, el dato esta mal, no la pregunta".

Uso:  python generar-guion.py        (escribe guion.json al lado)
"""

import json
import os
import sys
from datetime import date, datetime, timedelta

# ---------------------------------------------------------------- el catalogo

# Precio de lista. El guion los mueve con la inflacion propia de cada producto, que es lo
# que la app lee como "su propia inflacion".
CATALOGO = {
    "POLLO-KG":        ("Pechuga de pollo kg",          4350, "Proteinas"),
    "TILAPIA-KG":      ("Filet de tilapia kg",          5200, "Proteinas"),
    "CARNE-MOL":       ("Carne molida kg",              5600, "Proteinas"),
    "HUEVOS-30":       ("Huevos carton 30",             3300, "LacteosYHuevos"),
    "LECHE-1L":        ("Leche semidescremada 1L",        950, "LacteosYHuevos"),
    "QUESO-CREMA":     ("Queso crema 250g",              1980, "LacteosYHuevos"),
    "MANTEQUILLA-250": ("Mantequilla 250g",              2650, "LacteosYHuevos"),
    "BROCOLI":         ("Brocoli unidad",                 920, "FrutasYVerduras"),
    "ESPINACA":        ("Espinaca fresca 250g",          1450, "FrutasYVerduras"),
    "TOMATE-KG":       ("Tomate kg",                     1150, "FrutasYVerduras"),
    "BANANO-KG":       ("Banano kg",                      890, "FrutasYVerduras"),
    "ARROZ-2KG":       ("Arroz grano entero 2kg",        2520, "GranosYAbarrotes"),
    "FRIJOL-900":      ("Frijoles negros 900g",          1150, "GranosYAbarrotes"),
    "PASTA-500":       ("Pasta espagueti 500g",           890, "GranosYAbarrotes"),
    "ACEITE-GIR-1L":   ("Aceite de girasol 1L",          2890, "AceitesYGrasas"),
    "HELADO-1L":       ("Helado de vainilla 1L",         3680, "Azucarados"),
    "SODA-2L":         ("Soda cola 2L",                  1790, "Azucarados"),
    "GALLETA-CHOC":    ("Galletas de chocolate",         1250, "Azucarados"),
    "PAPAS-FAM":       ("Papas fritas familiar",         2100, "Snacks"),
    "JAMON-200":       ("Jamon de pierna 200g",          2350, "Embutidos"),
    "SALCHICHA-PK":    ("Salchichas paquete 8u",         2100, "Embutidos"),
    "MORTADELA-250":   ("Mortadela 250g",                1650, "Embutidos"),
    "POLLO-ROST":      ("Pollo rostizado entero",        6900, "ComidasPreparadas"),
    "LASAGNA-CONG":    ("Lasagna congelada familiar",    5450, "ComidasPreparadas"),
    "PIZZA-CONG":      ("Pizza congelada grande",        4200, "ComidasPreparadas"),
    "PERRO-CROQ-10KG": ("Alimento para perro 10kg",     25000, "Mascotas"),
    "DETERG-5L":       ("Detergente liquido 5L",         6800, "Limpieza"),
    "CLORO-1L":        ("Cloro 1L",                      1100, "Limpieza"),
    "OLLA-ARR-18":     ("Olla arrocera 1.8L",           45900, "Electrohogar"),
}

# SU PROPIA INFLACION, por producto y no una general: es el dato que la app promete y que
# ninguna app de super da hoy. Porcentaje anual, aplicado por dias corridos.
INFLACION_ANUAL = {
    "ACEITE-GIR-1L": 0.22,   # el que mas subio: sostiene "que subio en MI canasta"
    "HELADO-1L":     0.09,
    "POLLO-KG":      0.11,
    "LECHE-1L":      0.07,
    "ARROZ-2KG":     0.05,

    # La bolsa de Luna NO sube: el invariante 14 la fija en 25.000 al mes, y una bolsa con
    # inflacion daba 25.000 solo el primer mes. Es el precio de lista de la marca que
    # compra siempre, sostenido a proposito para que el gasto del hogar sea comparable.
    "PERRO-CROQ-10KG": 0.0,
}
INFLACION_POR_DEFECTO = 0.06

PRIMER_SABADO = date(2025, 1, 4)
ULTIMO_SABADO = date(2026, 8, 29)
ARRANQUE_DEL_CAMBIO = date(2026, 1, 1)   # el mes del quiebre, invariante 4

# EL AJUSTE DE LOS DOS AGOSTOS. Los invariantes dan los dos totales al colon, y una
# canasta generada por reglas no cae ahi sola. El cierre se hace con la CANTIDAD de un
# producto de relleno en la ULTIMA compra del mes -pasta en 2025, soda en 2026-, que es
# lo que una persona ajusta de verdad al final de la quincena.
#
# Prefiero que el ajuste se vea aca, con nombre y razon, a que aparezca como una linea
# "varios" que nadie sabe de donde salio.
AJUSTE = {
    (2025, 8): ["PASTA-500", "GALLETA-CHOC", "LECHE-1L", "BANANO-KG"],
    (2026, 8): ["SODA-2L", "GALLETA-CHOC", "PASTA-500", "QUESO-CREMA", "HELADO-1L"],
}


def precio(sku, cuando):
    """El precio de lista movido por la inflacion propia de ese producto."""
    base = CATALOGO[sku][1]
    tasa = INFLACION_ANUAL.get(sku, INFLACION_POR_DEFECTO)
    anios = (cuando - PRIMER_SABADO).days / 365.0
    return round(base * (1 + tasa * anios) / 10) * 10   # al decimal de colon mas cercano


def sabados():
    dia = PRIMER_SABADO
    while dia <= ULTIMO_SABADO:
        yield dia
        dia += timedelta(days=7)


def canasta_de(dia, indice):
    """Que lleva Sonia ese sabado. Devuelve [(sku, cantidad)].

    La regla del guion, en una linea: en 2025 la canasta es de cocinar en casa -frescos
    todas las semanas, proteina fresca, helado una vez al mes-; desde enero de 2026 entran
    embutidos y comidas preparadas, el helado pasa a semanal y los frescos bajan a
    quincenales. Nadie decidio "comer peor": cambio el ritmo de la casa.

    EL TOTAL SOLO SUBE 11%, y eso disciplina todo lo demas. Los invariantes dan 181.300 en
    agosto 2025 y 201.656 en agosto 2026: la canasta de 2026 NO es la de 2025 con cosas
    encima. Lo que entra desplaza a lo que sale, y por eso la proteina fresca y los frescos
    bajan de verdad en vez de quedarse igual. La primera version de este guion subia 50%
    porque agregaba sin quitar, y eso contradecia el dato.
    """
    despues = dia >= ARRANQUE_DEL_CAMBIO
    lineas = []

    # LA BASE, todas las semanas y en las dos epocas. Es lo que la app reconoce como su
    # firma: lo que no falla.
    lineas.append(("LECHE-1L", 3 if not despues else 1))
    lineas.append(("HUEVOS-30", 1))
    lineas.append(("PASTA-500", 1))

    # FRESCOS: semanales en 2025, quincenales en 2026 (invariante 4). Y cuando van, van
    # con menos: la respuesta que la wiki espera para "en que se me va la plata" dice
    # "los frescos, en cambio, bajaron", asi que tienen que bajar en PLATA y no solo en
    # cuantas veces aparecen.
    if not despues or indice % 2 == 0:
        lineas.append(("BANANO-KG", 2 if not despues else 1))
        lineas.append(("TOMATE-KG", 1))
        if indice % 3 == 0:
            lineas.append(("BROCOLI", 2 if not despues else 1))
        if indice % 4 == 1:
            lineas.append(("ESPINACA", 1))

    # PROTEINA FRESCA. En 2025 es el centro del plato; en 2026 la desplazan los embutidos
    # y las preparadas, que es la otra mitad del cambio.
    if not despues:
        lineas.append(("POLLO-KG", 2 if indice % 2 == 0 else 1))
        if indice % 3 == 1:
            lineas.append(("TILAPIA-KG", 1))
        if indice % 4 == 2:
            lineas.append(("CARNE-MOL", 1))
    else:
        if indice % 3 == 0:
            lineas.append(("POLLO-KG", 1))
        if indice % 6 == 2:
            lineas.append(("CARNE-MOL", 1))

    # GRANOS: quincenal, en las dos epocas.
    if indice % 2 == 0:
        lineas.append(("ARROZ-2KG", 1))
        lineas.append(("FRIJOL-900", 2))

    # ACEITE: cada tres semanas. Es el que mas sube de precio.
    if indice % 3 == 0:
        lineas.append(("ACEITE-GIR-1L", 1))

    # HELADO (invariante 4): una vez al mes en 2025, casi todas las semanas desde enero
    # 2026. Se salta UNA semana del 2026 a proposito: el invariante pide 34 tickets y el
    # calendario tiene 35 sabados hasta agosto, y "casi todas las semanas" es literal.
    if despues:
        if not (dia.year == 2026 and dia.month == 3 and dia.day == 28):
            lineas.append(("HELADO-1L", 1))
    elif dia.day <= 7:
        lineas.append(("HELADO-1L", 1))

    # LO QUE ENTRA EN ENERO DE 2026 (invariante 4).
    if despues:
        lineas.append(("JAMON-200", 1))
        if indice % 3 == 1:
            lineas.append(("SALCHICHA-PK", 1))
        if indice % 4 == 0:
            lineas.append(("MORTADELA-250", 1))

        if indice % 2 == 0:
            lineas.append(("POLLO-ROST", 1))
        if indice % 3 == 1:
            lineas.append(("PIZZA-CONG", 1))
        if indice % 6 == 3:
            lineas.append(("LASAGNA-CONG", 1))

        # queso crema y mantequilla SUBEN: los nombra el cruce del asistente.
        if indice % 2 == 1:
            lineas.append(("QUESO-CREMA", 1))
        if indice % 3 == 0:
            lineas.append(("MANTEQUILLA-250", 1))

        lineas.append(("SODA-2L", 1))
        if indice % 3 == 2:
            lineas.append(("GALLETA-CHOC", 1))
    else:
        # en 2025 los antojos aparecen, pero de a poco
        if indice % 4 == 0:
            lineas.append(("SODA-2L", 1))
        if indice % 6 == 3:
            lineas.append(("GALLETA-CHOC", 1))

    # LIMPIEZA: el detergente al arrancar el mes, el cloro cada tres semanas.
    if dia.day <= 7:
        lineas.append(("DETERG-5L", 1))
    if indice % 3 == 2:
        lineas.append(("CLORO-1L", 1))

    # LUNA (invariante 14): una bolsa al mes, 25.000 exactos.
    if dia.day <= 7:
        lineas.append(("PERRO-CROQ-10KG", 1))

    return lineas


def armar():
    guion, numero, evento = [], 88000, 300
    for indice, dia in enumerate(sabados()):
        lineas = canasta_de(dia, indice)

        # LA OLLA ARROCERA, con su garantia de 12 meses: es el respaldo que la app muestra
        # y el unico producto del guion con fecha de vencimiento.
        garantias = [0] * len(lineas)
        if dia == date(2026, 5, 9):
            lineas.append(("OLLA-ARR-18", 1))
            garantias.append(12)

        numero += 27
        evento += 1
        guion.append({
            "id": evento,
            "numero": "T-%d" % numero,
            "fecha": datetime(dia.year, dia.month, dia.day, 17, 40).isoformat(),
            "skus": [sku for sku, _ in lineas],
            "descripciones": [CATALOGO[sku][0] for sku, _ in lineas],
            "cantidades": [cantidad for _, cantidad in lineas],
            "preciosUnitarios": [float(precio(sku, dia)) for sku, _ in lineas],
            "categorias": [CATALOGO[sku][2] for sku, _ in lineas],
            "mesesDeGarantia": garantias,
        })

    return guion


# ---------------------------------------------------------------- verificacion

def fecha_de(ticket):
    return datetime.fromisoformat(ticket["fecha"])


def total_de(ticket):
    return sum(c * p for c, p in zip(ticket["cantidades"], ticket["preciosUnitarios"]))


def del_mes(guion, anio, mes):
    return [t for t in guion if fecha_de(t).year == anio and fecha_de(t).month == mes]


def total_del_mes(guion, anio, mes):
    return sum(total_de(t) for t in del_mes(guion, anio, mes))


def tickets_con(guion, sku, anio):
    return sum(1 for t in guion if fecha_de(t).year == anio and sku in t["skus"])


def cerrar_agosto(guion, anio, mes, objetivo):
    """Cierra el total del mes con la ultima compra, y deja dicho como.

    Ajustar solo la CANTIDAD no alcanza: 201.656 casi nunca es divisible por el precio de
    un producto. Se prueban varios candidatos de relleno y, para cada uno, la cantidad que
    deje un precio ENTERO y a menos del 18% del de lista — que es exactamente lo que pasa
    en un super de verdad cuando algo esta en promocion esa semana.

    Prefiero que el ajuste se vea aca, con nombre y razon, a que aparezca como una linea
    "varios" que nadie sabe de donde salio. Y el mes tiene que venir POR DEBAJO del
    objetivo: si se pasa, lo que esta mal es la canasta y hay que arreglarla ahi, no
    tapar la diferencia con un descuento inventado.
    """
    tickets = del_mes(guion, anio, mes)
    ultimo = tickets[-1]

    for sku in AJUSTE[(anio, mes)]:
        lista = precio(sku, fecha_de(ultimo).date())

        # cuanto suma el mes SIN la linea de este candidato
        if sku in ultimo["skus"]:
            i = ultimo["skus"].index(sku)
            sin_la_linea = (total_del_mes(guion, anio, mes)
                            - ultimo["cantidades"][i] * ultimo["preciosUnitarios"][i])
        else:
            i = None
            sin_la_linea = total_del_mes(guion, anio, mes)

        falta = objetivo - sin_la_linea
        if falta <= 0:
            continue

        for cantidad in range(1, 13):
            if falta % cantidad:
                continue
            unitario = falta / cantidad
            if abs(unitario - lista) / lista > 0.18:
                continue

            if i is None:
                ultimo["skus"].append(sku)
                ultimo["descripciones"].append(CATALOGO[sku][0])
                ultimo["categorias"].append(CATALOGO[sku][2])
                ultimo["cantidades"].append(cantidad)
                ultimo["preciosUnitarios"].append(float(unitario))
                ultimo["mesesDeGarantia"].append(0)
            else:
                ultimo["cantidades"][i] = cantidad
                ultimo["preciosUnitarios"][i] = float(unitario)

            return True, "%s x%d a %.0f (lista %.0f) en %s" % (
                CATALOGO[sku][0], cantidad, unitario, lista, ultimo["numero"])

    return False, "el mes no cierra con ningun candidato; revisar la canasta"


def verificar(guion):
    """Los invariantes de la wiki. Si uno falla, el dato esta mal, no la pregunta."""
    fallas = []

    def exigir(condicion, que):
        print(("  ok   " if condicion else "  MAL  ") + que)
        if not condicion:
            fallas.append(que)

    ago25 = total_del_mes(guion, 2025, 8)
    ago26 = total_del_mes(guion, 2026, 8)
    exigir(ago25 == 181300, "13 - agosto 2025 = 181.300 (dio %.0f)" % ago25)
    exigir(ago26 == 201656, "13 - agosto 2026 = 201.656 (dio %.0f)" % ago26)

    h25 = tickets_con(guion, "HELADO-1L", 2025)
    h26 = tickets_con(guion, "HELADO-1L", 2026)
    exigir(h25 == 12, "13 - 12 tickets con helado en 2025 (dio %d)" % h25)
    exigir(h26 == 34, "13 - 34 tickets con helado en 2026 (dio %d)" % h26)

    embutidos25 = [t for t in guion if fecha_de(t).year == 2025 and "Embutidos" in t["categorias"]]
    preparadas25 = [t for t in guion if fecha_de(t).year == 2025 and "ComidasPreparadas" in t["categorias"]]
    exigir(not embutidos25, "4 - ningun embutido antes de 2026 (hay %d)" % len(embutidos25))
    exigir(not preparadas25, "4 - ninguna preparada antes de 2026 (hay %d)" % len(preparadas25))

    primer_embutido = min(fecha_de(t) for t in guion if "Embutidos" in t["categorias"])
    exigir(primer_embutido.year == 2026 and primer_embutido.month == 1,
           "4 - los embutidos entran en enero 2026 (entraron %s)" % primer_embutido.date())

    # los frescos: semanales en 2025, quincenales en 2026
    def semanas_con_frescos(anio, mes_hasta):
        tickets = [t for t in guion if fecha_de(t).year == anio and fecha_de(t).month <= mes_hasta]
        con = [t for t in tickets if "FrutasYVerduras" in t["categorias"]]
        return len(con), len(tickets)
    c25, t25 = semanas_con_frescos(2025, 8)
    c26, t26 = semanas_con_frescos(2026, 8)
    exigir(c25 == t25, "4 - frescos en TODAS las compras de 2025 (%d de %d)" % (c25, t25))
    exigir(abs(c26 / t26 - 0.5) < 0.08, "4 - frescos en la mitad de 2026 (%d de %d)" % (c26, t26))

    # Luna: 25.000 en cada mes del guion
    meses = sorted({(fecha_de(t).year, fecha_de(t).month) for t in guion})
    def luna(anio, mes):
        return sum(
            c * p
            for t in del_mes(guion, anio, mes)
            for s, c, p in zip(t["skus"], t["cantidades"], t["preciosUnitarios"])
            if CATALOGO[s][2] == "Mascotas"
        )
    malos = [(a, m, luna(a, m)) for a, m in meses if luna(a, m) != 25000]
    exigir(not malos, "14 - Luna a 25.000 en los %d meses (fallan %d)" % (len(meses), len(malos)))

    return fallas


if __name__ == "__main__":
    guion = armar()

    print("%d compras, de %s a %s" % (len(guion), guion[0]["fecha"][:10], guion[-1]["fecha"][:10]))
    for (anio, mes), objetivo in [((2025, 8), 181300), ((2026, 8), 201656)]:
        ok, detalle = cerrar_agosto(guion, anio, mes, objetivo)
        print("  cierre %d-%02d: %s" % (anio, mes, detalle))

    print("\ninvariantes:")
    fallas = verificar(guion)

    if fallas:
        print("\n%d invariante(s) sin cumplir. NO se escribe el guion." % len(fallas))
        sys.exit(1)

    destino = os.path.join(os.path.dirname(os.path.abspath(__file__)), "guion.json")
    with open(destino, "w", encoding="utf-8") as f:
        json.dump(guion, f, ensure_ascii=False, indent=1)
    print("\nescrito %s" % destino)
