# Prompt para rehacer el icono de la app

Copiá todo lo que sigue a una sesión nueva de Claude Code, parada en la raíz del repo.

---

Necesito el icono de una app, y hay un intento previo que **falló**. Antes de proponer
nada, leé por qué falló: la trampa está buena y es fácil volver a caer.

## Qué es la app

`Examenes/App/Examenes.Wasm` — una app que un **laboratorio clínico ficticio**
("Laboratorios Sotavento") le regala a su paciente. Se instala en la computadora de la
persona como PWA y guarda su expediente de laboratorio en una carpeta suya.

Lo que la app hace y que una hoja de resultados no puede: **leer la serie**. Un antígeno
prostático de 5,2 no dice casi nada; *"venías en 3,0 hace tres años"* sí dice.

Quien la usa es un paciente, probablemente de 50 a 65 años, no un usuario técnico.

## El intento que falló, y por qué

Dibujé la serie: tres tramos planos y un salto, con un punto ámbar al final. Está en
`Examenes/PaginaDeDescarga/generar-iconos.py`. El diagnóstico:

1. **Es genérico.** Una línea ascendente con un punto es el icono de cualquier app de
   analytics, finanzas o fitness. Dice "algo subió". No dice laboratorio, ni salud, ni
   *esto es tuyo*.
2. **Tiene demasiados elementos para 16–32 px.** Tres vértices más un círculo se
   convierten en una mancha. Un favicon aguanta **una silueta**, no una composición.
3. **Era literal, no simbólico.** Traduje el dato a un dibujo del dato. Un icono tiene
   que decir de qué se trata, no graficar el contenido.

**No repitas la línea de tendencia.** Si tu idea se parece a un gráfico, descartala.

## Qué buscar

Una silueta que se lea de un golpe a **16 px** y que diga *laboratorio clínico* o *tu
expediente*. Vale explorar direcciones distintas antes de elegir — la gota, el tubo o
vial, el monograma de la marca, la carpeta/expediente, o algo que combine dos de esas.
Elegí por legibilidad a 16 px, no por lo que se ve lindo a 512.

Criterios, en orden:

1. **Se reconoce a 16 px.** Es el criterio que manda; si falla acá, no importa lo demás.
2. **Dice de qué se trata.** Alguien que ve el icono en el dock sabe que es algo de salud
   o de laboratorio, no una app de bolsa.
3. **Encaja con la marca** (abajo).
4. **Se ve bien a 512** — pero es lo último, no lo primero.

## La marca, que ya existe y no se negocia

Es la misma de la página de descarga (`Examenes/PaginaDeDescarga/descarga/index.html`) y
de la app. Mirá las dos antes de dibujar.

```
--pino    #1f4034   verde institucional, el color de fondo
--papel   #f6f3ec   el trazo sobre el pino
--ambar   #b4541a   ver la regla de abajo
```

**Regla del ámbar, y es importante:** en toda la app el ámbar significa **una sola cosa**,
"esto exige atención". Si aparece en el icono sin significar eso, deja de significarlo en
el resto. Usalo solo si de verdad lo amerita; probablemente no.

Tipografía de la marca: *Instrument Serif* para titulares, *IBM Plex Sans* para el cuerpo,
*IBM Plex Mono* para datos. Si vas por un monograma, la referencia es el serif.

## Restricciones técnicas

- **Esta máquina no tiene PIL, ni rsvg, ni ImageMagick, ni Inkscape.** No los instales.
- Hay un generador que escribe el PNG a mano con `zlib`, con supersampling para
  antialiasing: `Examenes/PaginaDeDescarga/generar-iconos.py`. Reusá esa mecánica; lo que
  hay que reemplazar es la función `dibujar()`.
- **Tamaño óptico, no escalado lineal.** El generador ya lo hace: a 32 px el trazo va más
  grueso y la figura llena más el cuadro que a 512. Mantené ese criterio.
- Hacen falta tres archivos, en `Examenes/App/Examenes.Wasm/wwwroot/`:
  `favicon.png` 32, `icon-192.png` 192, `icon-512.png` 512.
- El manifest (`wwwroot/manifest.webmanifest`) ya los referencia, incluido uno como
  `maskable` — dejá margen de seguridad para que no lo recorten.

```bash
python3 Examenes/PaginaDeDescarga/generar-iconos.py Examenes/App/Examenes.Wasm/wwwroot
```

## Cómo verificar, y esto no es opcional

**Mirá los PNG, no los describas.** Leé los tres archivos generados como imagen. El de
32 px es el que decide.

Y probalo chico de verdad: el `favicon.png` visto a tamaño real, no ampliado. Si tenés
que ampliarlo para entender qué es, no funciona.

Para verlo servido:

```bash
python3 Examenes/PaginaDeDescarga/generar-iconos.py Examenes/App/Examenes.Wasm/wwwroot
MSBuildEnableWorkloadResolver=false dotnet publish Examenes/App/Examenes.Wasm -c Release \
  -o Examenes/PaginaDeDescarga/publicado -p:PublishTrimmed=false
docker compose -f Examenes/docker-compose.yml --project-directory Examenes restart wasm-host
```

Queda en `http://localhost:8090/app/`.

> `MSBuildEnableWorkloadResolver=false` no es adorno: sin eso el SDK pide el workload
> `wasm-tools`, que instalarlo necesita `sudo`.

## Antes de dar por terminado

Mostrame **dos o tres direcciones distintas** renderizadas a 32 px para comparar, no una
sola pulida. La decisión se toma mirando el tamaño chico, lado a lado.

## Commits

Conventional Commits: **tipo en inglés, mensaje en español**, asunto en minúscula y
máximo 72 caracteres. Hay un hook en `.githooks/commit-msg` que lo verifica.
El cuerpo dice **por qué**, no qué.
