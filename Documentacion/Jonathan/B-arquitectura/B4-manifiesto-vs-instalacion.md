# B4 · Manifiesto vs instalación: dos archivos porque son dos autoridades

Es el otro de los dos cortes que más fácil se deshacen sin darse cuenta. Y es el que se validó solo,
sin que nadie lo buscara.

---

## El corte

```
Modulos/facturas/manifiesto.json     ← lo escribe LA INSTITUCIÓN
Modulos/facturas/instalacion.json    ← lo escribe EL HOST
```

| | manifiesto | instalación |
|---|---|---|
| **quién lo escribe** | la institución. Algún día lo firma | el Host, al instalar |
| **qué declara** | identidad, rango de sustrato, capacidades pedidas, su lista de consultas | dónde está el binario, qué carpeta se concedió, si está encendido |
| **cambia entre hospedajes** | **no** | **sí** |

## Por qué separados

Si la cáscara y la carpeta vinieran en el manifiesto, **el módulo elegiría cómo se lo ejecuta y dónde
escribe**.

**Conceder es un acto del que concede.** No del que recibe.

Y no es una regla abstracta: el endpoint del publicador tiene que ser **fijo por publicador, registrado en
la instalación, no elegible en runtime**. Si el módulo pudiera decir qué URL traer, **exfiltra codificando
datos en la ruta o el query**.

## La validación que salió sin buscarla

El POC se corrió en dos hospedajes: Windows y un contenedor Linux.

**El contenedor usa el mismo `manifiesto.json`, byte por byte**, porque no tiene ninguna ruta adentro. Lo
único que cambia es el `instalacion.json`, con rutas de Linux y otra carpeta concedida.

> **Lo que declara la institución es portable entre hospedajes; lo que el Host concede no.**

Eso no era el objetivo del ejercicio de Docker. Salió. Y es la mejor evidencia de que el corte no era
arbitrario.

## El eco que vale notar

Cuando el Host se empaquetó como extensión instalable, el formato de bundle pedía declarar un
`user_config`: **el bundle declara *qué* necesita, y la aplicación anfitriona le pregunta a la persona
*con qué valor* y se lo pasa al lanzar.**

Es el mismo corte, un nivel más arriba. Que un formato ajeno haya llegado a la misma forma para su
problema de instalación sugiere que el corte no era una preferencia nuestra.

---

## Qué lleva el manifiesto

Se sabe qué campos van; **la forma no está escrita**, y ahí hay latitud de diseño real.

```json
{
  "manifiesto": "1.0",
  "modulo": "facturas",
  "titulo": "Mis facturas",
  "institucion": "...",

  "sustrato": { "rango": ">=3.0.0 <4.0.0" },

  "capacidades": { "red": false, "carpeta": "propia",
                   "parlante": false, "microfono": false },

  "journal": { "compression": "None", "encryption": "None", "maxFileSize": 4194304 },

  "view": "view.json",
  "load": { "entry": "Facturas.App.dll" },

  "tools": [ { "nombre": "...", "descripcion": "...", "soloLectura": true, "entrada": [] } ]
}
```

### El rango, no una versión exacta

Si un módulo pinea una versión puntual, **cada actualización del Host huerfaniza todos los módulos
instalados**.

Y hay un matiz fino de la especificación: **el autor no declara compatibilidad — el sustrato se la gana.**
El rango que un módulo puede pinear es el que una suite de journals dorados certificó. Eso convierte
«mantenemos compatibilidad hacia atrás» de promesa en **afirmación falsable**.

### Los tres ajustes de journal son campos del manifiesto, no configuración

`compression`, `encryption` y `maxFileSize` se fijan **al crear** el journal, y pedir otro modo después
**lanza excepción**. Si la especificación no lo dice, alguna institución va a descubrir a los dos años que
activar cifrado significa **empezar un journal nuevo**.

### Las capacidades tienen que ser contrato, no recomendación

La restricción que no es negociable: **un módulo que declara «cero red» debe *fallar* en un host que se la
conceda igual.** Sin eso, publicar el formato es regalar la idea sin las salvaguardas que la hacen valer.

En el POC solo se **avisa** —un módulo de facturas que pide micrófono saca un warning— y hacerlo cumplir
necesita el sandbox de capacidades. La distinción está declarada, no escondida.

### `view` y `load`: la presentación y cuál DLL trae la superficie

Dos campos que el manifiesto ganó. **`view`** nombra el JSON de la vista (`view.json`): la presentación
del módulo, **autoría de la institución**, aparte de la lógica. **`load.entry`** dice **cuál DLL trae la
superficie** —la marcada con `[ModuleSurface]`, ver `B2`—; las dependencias del módulo **se resuelven por
adyacencia**, los ensamblados que están **al lado del `entry`** en la misma carpeta, no rastreando un grafo
ni bajando nada. Ver `C7`.

## Qué lleva la instalación

```json
{
  "cascara": { "tipo": "stdio", "comando": "...", "argumentos": [ "..." ] },
  "carpetaConcedida": "...",
  "encendido": true,
  "toolsApagadas": []
}
```

**La cáscara.** Cómo el Host levanta este módulo. Cambiar **solo este bloque** es lo que convierte un
hospedaje en otro, y ni el manifiesto ni el módulo se enteran. Un `tipo` es `stdio` —el proceso aparte de
siempre—; otro es la **cáscara en proceso** (`InProcessShell`): el Host **presta el motor** y **carga las
DLLs** del módulo en su propio proceso, **sin spawnear** nada. Ese es el hospedaje que cierra el bug de
dos escritores de `B3`. Ver `C7`.

**La carpeta concedida.** La única que el módulo puede tocar. Se le pasa **como argumento**, no como
variable de ambiente ni como algo que el módulo pueda deducir del sistema.

**El interruptor.** Es del Host: *si lo implementa el módulo, no es un interruptor.*

**Las consultas apagadas.** El corte fino, que es el que de verdad hace falta: «el total sí, el detalle
no».

## Y una consecuencia práctica del corte

Instalar otro módulo ya no es solo crear una carpeta con esos dos archivos JSON: hoy es un paquete `.gmod`
(un zip) con **manifiesto + `view.json` + las DLLs del módulo** (`X.App.dll` la superficie, `X.Dominio.dll`
el dominio), que se instala con el comando `--install-module` del Host —o, para el comercio, con la custom
action de su MSI (ver `C7`)—. **Pero el corte se mantiene entero:** no se toca la configuración del
asistente, no se recompila el Host, no se registra el módulo en ninguna parte del sistema.

Esa es la diferencia entre *«el asistente tiene N servidores configurados»* y *«la persona tiene N módulos
instalados»*.
