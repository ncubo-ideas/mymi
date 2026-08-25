using System.Runtime.CompilerServices;

// El dominio es internal por defecto (/puppeteer-domain-modeling E2, E15, E42) y se queda asi.
//
// La guia justifica ese internal diciendo que "el perform comparte el ensamblado". ACA NO
// LO COMPARTE: ARCHITECTURE 3.1 parte Dominio de App a proposito, porque un csproj sin
// referencias es la prueba de que el dominio no cambia al cruzar a wasm.
//
// La justificacion de la guia no aplica; LA REGLA SI, y por una razon mejor: MEDIDO en T0
// (spike S1, ver DealershipCar/SPIKES-OS2.md) que el motor enlaza tipos internal de OTRO
// ensamblado — constructor, metodo y propiedad, en memoria y despues del replay en un
// proceso nuevo. El enlace es por REFLEXION, que es una regla del CLR y no del compilador,
// y a la reflexion el internal no le estorba.
//
// Lo que SI necesita permiso es la mitad de COMPILACION: DealershipCar.App tiene que poder
// nombrar el ensamblado del dominio para pasarselo a PerformanceV2. Para eso son estas dos
// lineas, y para nada mas.
//
// NO agregar mas nombres aca sin una razon escrita. Cada uno es una carátula que puede
// hablarle al dominio por la espalda en vez de entrar por DealershipCarAPI, que es la
// unica puerta.

[assembly: InternalsVisibleTo("DealershipCar.App")]
[assembly: InternalsVisibleTo("DealershipCar.Dominio.Pruebas")]
