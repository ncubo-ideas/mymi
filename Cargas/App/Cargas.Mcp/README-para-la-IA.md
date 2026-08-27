# Cargas.Mcp — cómo descubre esto una IA

No hace falta un README que le explique a la IA qué operaciones existen: con tools
intencionales, **el schema de cada tool es la documentación**. Este archivo es para
humanos que quieren ver qué va a encontrar el cliente MCP.

- `tools/list` devuelve las cinco tools con su `outputSchema`:
  `mis_cargas` (el ancla: vehículos, placas, completitud, último evento leído),
  `autonomia_estimada`, `rendimiento_vs_habitual`, `lo_que_falta_para_calcular` y
  `dias_hasta_la_proxima_carga`.
- Las `instructions` del servidor traen el workflow: el veredicto es semántico y no se
  reinterpreta por el signo, un dato sospechoso pide registrar la carga que faltó sin
  alarmar, toda proyección se declara, y el Refresh es de la persona.
- Todas las respuestas viajan en `structuredContent`, así los decimales llegan como
  decimales.

El criterio de cada tool es el de Álvaro: si la IA puede contestar algo que ya está en
una pantalla, el demo no probó nada.
