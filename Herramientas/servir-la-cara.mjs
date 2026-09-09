// SERVIR LA CARA PARA MIRARLA, sin compilar un telefono.
//
// `ui.html` se abre en cualquier navegador: `gn.call` mira el userAgent y, si no esta dentro de
// Gnothi, contesta con el MOCK del final del archivo. Lo unico que hace falta es servirla por
// HTTP en vez de file://, que es lo que pide cualquier herramienta de captura.
//
//   node Herramientas/servir-la-cara.mjs [puerto]
//
// NO es parte del producto y no entra en ningun bundle: es la forma de ver la pantalla mientras
// se la edita, y de que una captura de la cara sea reproducible por otra persona.
import { createServer } from 'node:http';
import { readFileSync } from 'node:fs';

const puerto = Number(process.argv[2] || 8123);
const cara = new URL('../Gnothi.Mobile/Assets/ui.html', import.meta.url);

createServer((req, res) => {
  res.writeHead(200, { 'content-type': 'text/html; charset=utf-8', 'cache-control': 'no-store' });
  res.end(readFileSync(cara));                       // se relee en cada pedido: F5 y ya
}).listen(puerto, () => console.log(`la cara en http://localhost:${puerto}/`));
