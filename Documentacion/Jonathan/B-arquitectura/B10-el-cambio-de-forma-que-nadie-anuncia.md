# B10 · El cambio de forma que nadie anuncia

**Medido el 2026-09-16** en un Xiaomi 2407FPN8EG con Android 16, con un módulo real y datos reales.

**Un módulo puede cambiar de forma en una actualización** —de híbrido a hospedado, de hospedado a
cargo— **y la hoja donde la persona da su consentimiento no lo dice.** Peor: en el bloque que
contesta *«qué cambia»*, **afirma que no cambia nada**.

Es el cambio más grande que un módulo puede tener, y es el único que la hoja no compara.

---

## Lo que se corrió

`municipalidad` estaba instalado en **forma híbrida**: 74 KB, sin motor, su cara en HTML y su
dominio en `dominio/*.dll` corriendo sobre el motor que presta Gnothi. Funcionando, con 23 eventos
en su journal.

Se publicó el mismo módulo en **forma hospedada** —12,2 MB, 226 archivos, con su propio .NET
compilado a wasm— y se abrió el alta.

## Lo que dijo la hoja

> **Actualización** de 2.1.0 a 2.3.0. Tu registro y tus interruptores quedan como están.
>
> **Qué cambia**
> Nada de lo que Gnothi compara: consultas, capacidades, publicador, origen, registro.
> **Cambia la app adentro.**

**«Nada de lo que Gnothi compara.»** Y lo que estaba pasando es que un módulo sin motor se volvía
uno con un runtime de .NET adentro, y su cáscara pasaba de `hybrid` a `webview`.

### Y el matiz, que hay que ser justo

**El bloque «Lo que Gnothi revisó» SÍ describe el paquete nuevo:**

> Trae la app de la institución, publicada: 227 archivos, 19,0 MB.
> Con su propio motor adentro (1,6 MB): un navegador no corre C# sin él.

Así que **la información está en la hoja.** El problema es dónde:

| bloque | qué contesta | qué dice de la forma |
|---|---|---|
| **Qué cambia** | *«¿en qué se diferencia de lo que tengo?»* | **«nada»** — y es falso |
| **Lo que Gnothi revisó** | *«¿qué es esto?»* | lo dice, pero como descripción, no como diferencia |

La persona que quiere saber qué cambia lee el primero. El primero le miente.

## Lo que pasó después de aceptar

    alta: 'municipalidad': update 2.1.0 -> 2.3.0
    alta: 'municipalidad' quedó instalado; 5 módulos

Y en `installation.json`:

```json
"shell": { "type": "webview", … }
```

**La cáscara cambió de `hybrid` a `webview` sin una palabra.**

### Y la consecuencia, que es la parte que importa

Al abrir el módulo, la persona no vio sus datos. Vio esto:

> **DÓNDE GUARDAR**
> [ Elegir una carpeta ] [ Guardar en este navegador ] [ Abrir sin guardar nada ]

**El módulo le está pidiendo que elija dónde guardar, como si fuera la primera vez.** Su cuenta
municipal, que hace un minuto se veía, ya no está en la pantalla.

Y el Host lo registró así:

    comprobación de municipalidad: {"matches":false, "actual":[],
      "detail":"could not query the module: TimeoutException: The operation has timed out."}

**El módulo dejó de contestarle al Host.** Pasó de `matches:true` a `matches:false`.

**El journal NO se perdió** — `files/journals/municipalidad/municipalidad/journal/journal_000001.bin`
sigue ahí, entero.

Dicho sin jerga: **la persona aceptó una actualización que le dijo «tu registro queda como está», y
después de aceptarla su registro no aparece.** Las dos cosas son ciertas a la vez, y por eso la
frase es peligrosa: el registro está, pero la app que le pusieron encima no lo muestra.

### POR QUE no lo muestra — ya está diagnosticado, y NO era el cambio de forma

**Este documento afirmó dos cosas falsas antes de llegar a la verdadera, y las dos se dejan
escritas porque las dos eran cómodas.** La primera decía que «la forma hospedada no alcanza la
carpeta que Gnothi concede» — falso: la hospedada está diseñada justamente para alcanzarla, por el
puente. La segunda, más sutil, era dar por hecho que **el cambio de forma había causado el fallo**.
Tampoco.

La causa está medida y contada entera en **[`E5`](../E-trampas/E5-la-app-hospedada-que-no-sabe-que-lo-esta.md)**.
En corto: la app de municipalidad, al arrancar, **le preguntaba al navegador dónde guardar sin
haberle preguntado antes a Gnothi si estaba hospedada.** El puente estaba puesto —se verificó
sondeando `window.gnothiPuente` en el teléfono— y la app nunca lo llamó. Le faltaba una línea que
las otras cuatro instituciones sí tienen:

```csharp
if (estado.Hospedada) { await AbrirAsync(); return; }
```

**Y por eso una instalación hospedada desde cero habría fallado idéntico.** Cambiar de forma no
causó nada: sólo fue la primera vez que esa app corrió hospedada en un teléfono, y destapó un bug
que estaba ahí desde que se publicó. Municipalidad es la quinta institución; `F6` documenta la forma
hospedada con **cuatro**.

Arreglado y verificado el 2026-09-16 con `2.4.0`: el módulo abre mostrando la cuenta de la persona,
`comprobado.json` queda escrito, y la cáscara sigue siendo `webview`. **Hospedada e híbrida son
compatibles de hecho**, con el mismo journal y sin tocar el Host.

**Lo que este documento sigue afirmando, y que el arreglo NO toca:** la hoja del consentimiento no
compara la forma, y en el bloque que contesta *«qué cambia»* afirma que no cambia nada. Eso era
cierto antes del arreglo y lo sigue siendo. Lo que cambia es el peso del ejemplo: la consecuencia
que se vio ese día no fue culpa del cambio de forma, así que **este documento se queda con el
problema de la hoja, y el bug se lee en `E5`**.

---

## Por qué pasa

Son dos decisiones que por separado están bien.

**1 · La forma se decide por lo que el paquete trae** (`PackageInspector`):

```csharp
(declaresLoad, carriesWeb) switch {
    (true, true)  => Hybrid,
    (true, false) => Cargo,
    _             => Hosted,
}
```

Es correcto: *lo que trae es lo que es*. Un paquete no puede mentir sobre su forma.

**2 · El alta escribe la cáscara desde el paquete nuevo** (`ModuleEnrollment`):

```csharp
(true, true)  => "hybrid",
(true, false) => "in-process",
_             => "webview",
```

También correcto, y arregló un bug: antes el teléfono escribía `"webview"` quemado.

**Pero nadie compara las dos.** `UpdatePlan` compara consultas, capacidades, publicador, origen,
firma y versión. **No compara la forma.** Y como la carpeta concedida se conserva (`priorGranted`),
el cambio es silencioso y reversible en apariencia — hasta que se abre.

---

## Qué habría que hacer

**1 · Que `UpdatePlan` compare la forma**, y que el bloque «Qué cambia» la nombre primero cuando
cambia. Es el renglón más importante de esa hoja, no uno más:

> **Cambia cómo corre.** Tenías un módulo que usa el motor de Gnothi; el paquete nuevo trae su
> propio motor y corre en su propia caja. Tu registro no se toca, pero **puede que la app nueva no
> lo encuentre**.

**2 · Que la frase «tu registro queda como está» deje de aparecer cuando la forma cambia.** Es
verdad sobre los bytes y falsa sobre lo que la persona va a ver, y ésa es la peor clase de frase
verdadera.

**3 · Decidir si el cambio de forma se permite.** Hay tres posturas defendibles y no está escrito
cuál es la nuestra:

- **se permite y se avisa fuerte** — lo mínimo;
- **se permite sólo hacia formas que conserven el acceso al registro** — o sea, el Host verifica
  que la forma nueva sepa leer la carpeta que concedió;
- **no se permite**: un módulo nace con su forma, y cambiarla es desinstalar e instalar, con lo que
  eso significa dicho en voz alta.

**4 · Y de paso, la guardia del motor.** Ya está en `Documentacion/Movil/07`: el instalador de
escritorio rechaza un paquete cargo que traiga `Puppeteer.dll`, pero `ModuleEnrollment` —el alta
desde el enlace, que es por donde entra todo en el teléfono— **nunca consulta esa lista**. Un
híbrido con motor adentro sí se rechaza (`EngineFiles` tiene que ser cero); un hospedado con 12 MB
de motor entra sin que nada lo mencione. No es un bug —la hospedada **necesita** el suyo— pero
significa que la garantía vale para una forma y no para la otra.

---

## Lo que el mismo experimento demostró que SÍ funciona

**Las formas conviven sin problema.** Después de esto, Gnothi quedó con:

| módulo | forma |
|---|---|
| examenes, compras, cargas, dealership | `hybrid` |
| municipalidad | `webview` |

Las cinco tarjetas en la misma pantalla, cada una con su cáscara, elegida por módulo al instalar:

```csharp
"webview"    => new WebViewShell(…)
"hybrid"     => new HybridShell(…)
"in-process" => new InProcessShell(…)
```

**Una cuarta forma sería un `case` más.** Y la institución puede tener un módulo de cada forma a la
vez — eso no es un plan, es lo que corre hoy.

Lo que falta no es soporte: es que la persona sepa cuál le están dando, y cuándo se lo cambian.
