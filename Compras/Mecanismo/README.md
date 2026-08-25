# Puppets.Portador — la copia de Compras

Copiada sin modificar un byte de `Examenes/Mecanismo/Puppets.Portador/`, commit
**`7b60462`** (que a su vez es copia textual de `HolaMundo/Mecanismo/Puppets.Portador/`,
commit `2f51221`).

## Por qué Compras tiene la suya

La convención del equipo ubica el mecanismo compartido en la raíz del repo, sin colgar
de ningún negocio. Hoy todavía no está ahí: el original vive en `HolaMundo/` (que es
material de referencia descartable) y Exámenes lleva su propia copia por esa misma razón.

Con Compras ya somos DOS negocios vivos usando el mismo mecanismo — exactamente el
momento que el README de Exámenes nombra para subir el Portador a la raíz. Mientras esa
mudanza no se acuerde con el equipo, esta copia mantiene a Compras compilando sin
depender ni de `HolaMundo/` ni del árbol de otro negocio.

## Antes de tocar nada acá

**No lo edites.** Es una de las piezas que se copian textual porque ya resolvieron
problemas no obvios: el orden precarga-MEMFS → actor → flush-por-comando, la rotación
del refresh token, el checkpoint que solo avanza.

Mientras las otras copias existan, conviene confirmar que no se separaron:

```bash
diff -r --exclude=obj --exclude=bin --exclude=README.md \
  Examenes/Mecanismo/Puppets.Portador Compras/Mecanismo/Puppets.Portador
```

Sin salida = idénticas. El día que el Portador suba a la raíz, esta copia se borra y
Compras lo referencia de ahí.
