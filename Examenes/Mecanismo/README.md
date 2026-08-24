# Puppets.Portador — la copia de Exámenes

Copiada sin modificar un byte de `HolaMundo/Mecanismo/Puppets.Portador/`, commit
**`2f51221`**.

## Por qué Exámenes tiene la suya

`Puppets.Portador` es el mecanismo compartido: el bucle de refresh, el token y su
rotación, el checkpoint y el puente de journal. Es igual para todo negocio, y por eso la
convención del equipo lo ubica en la raíz del repo, sin colgar de ninguno.

Hoy no está en la raíz: se movió a `HolaMundo/` junto con el ejemplo de Facturas. Y
**`HolaMundo/` es descartable** — es material de referencia, no un negocio del milestone.
Va a desaparecer.

Por eso Exámenes no podía referenciar hacia adentro de esa carpeta: el día que se borre,
nuestro dominio deja de compilar. La copia no es duplicación permanente, es Exámenes
quedándose con lo que necesita antes de que el ejemplo se vaya.

Que Exámenes esté **basado** en ese ejemplo no lo hace **parte** de él.

## Cuándo esta copia deja de ser la respuesta

Cuando entren los negocios de Kenneth y Geaninna. Ahí sí hay varios negocios vivos
necesitando el mismo mecanismo, y el riesgo que la convención nombra —cuatro versiones
divergiendo— se vuelve real. En ese momento el Portador tiene que subir a la raíz, compartido por todos. **Esta copia
es la candidata a mudarse ahí**: es la única que quedará viva cuando `HolaMundo/` no esté.

## Antes de tocar nada acá

**No lo edites.** Es una de las piezas que se copian textual porque ya resolvieron
problemas no obvios: el orden precarga-MEMFS → actor → flush-por-comando, la rotación del
refresh token, el checkpoint que solo avanza.

Mientras `HolaMundo/` siga existiendo, conviene confirmar que no se separaron:

```bash
diff -r --exclude=obj --exclude=bin --exclude=README.md \
  HolaMundo/Mecanismo/Puppets.Portador Examenes/Mecanismo/Puppets.Portador
```

Sin salida = idénticas. Cuando `HolaMundo/` se borre, este chequeo deja de aplicar y esta
copia pasa a ser el original.
