# Investigación — el porqué de las guías

Lo que respalda a las guías con bibliografía: qué nombre publicado tiene cada cosa que la Metodología
hace, qué dice la literatura de dónde coincidimos y de dónde no, y la corrida del método contra el
dominio de referencia del repo.

**No son guías.** Nadie necesita leer esto para construir una app. Existen para que quien conoce esas
literaturas —modelado conceptual, DDD, Event Modeling, OntoUML— entienda y adopte lo nuestro en la
mitad del tiempo, y para que nosotros tengamos con qué decidir si un concepto se adopta o se deja. El
[glosario](../Guias/glosario.md) es el puente entre esto y las guías: cada término acuñado, al lado del
publicado, con su fuente y su estado.

| | Qué es | Leer si |
|---|---|---|
| [01 · Estado del arte](01-estado-del-arte.md) | ¿Ya estaba documentado el problema de construir dominios que agregan valor? Sí — en cinco literaturas que casi no se citan entre sí. Fowler cap. 3 y 8, Type Object, UFO/OntoUML, Event Modeling, ODI, y por qué **no puede haber algoritmo** | querés saber de dónde sale cada término del glosario |
| [02 · La columna vertebral](02-columna-vertebral.md) | El método de las cuatro pasadas —el espejo, la grilla 2×3, los juicios, la especialización— con sus citas y sus diagramas. **Es el porqué de [la guía](../Guias/columna-vertebral.md)**, escrita el 2026-09-07 en lenguaje llano y enlazando al glosario; acá quedan las citas, los diagramas grandes y el registro de cómo fue cambiando | vas a correr el método o a revisar una corrida |
| [03 · Contraste con los papers](03-contraste-con-los-papers.md) | El método contra los ocho papers de Puppeteer: sin contradicción doctrinal, y tres tensiones en el código —T1 y T2 en el gimnasio, resueltas como incumplimientos nuestros de E24; T3 en Exámenes, resuelta por la decisión de especialización | te preocupa que la Metodología choque con la doctrina del motor |
| [04 · Prueba 1 · Exámenes](04-prueba-1-examenes.md) | El método corrido abiertamente contra `Examenes`, el dominio de referencia. **Once de once** decisiones coinciden desde el 2026-09-07 — la undécima es la que produjo la frontera *juzga vs. parametriza* | querés ver el método aplicado a un dominio que ya existía |

Las corridas contra **sujetos de prueba** —el gimnasio, la municipalidad— no viven acá: viven con el
sujeto, en [`Pruebas/`](../Pruebas/README.md), porque su `BITACORA.md` es el cuaderno de laboratorio y
el tag de cada versión tiene que incluirlo.

*Estos cuatro documentos se escribieron entre el 2026-09-02 y el 2026-09-07 **fuera del repo**, por una
restricción de entonces —"no modifiques nada en los repositorios"— que después se levantó. Entraron el
2026-09-07. Su historia anterior a esa fecha no está en git.*
