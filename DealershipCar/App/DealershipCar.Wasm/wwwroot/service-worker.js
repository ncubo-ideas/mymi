// En desarrollo se va siempre a la red y no se habilita soporte offline: cachear haria que un
// cambio no se viera en la primera carga despues de hacerlo. El de produccion es
// service-worker.published.js, que el publish pone en su lugar.
self.addEventListener('fetch', () => { });
