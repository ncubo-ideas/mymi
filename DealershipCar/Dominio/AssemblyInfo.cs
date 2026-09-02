using System.Runtime.CompilerServices;

// QUIEN VE EL INTERIOR DEL DOMINIO. Dos nombres, y cada uno con su razon.
//
// DealershipCar.Frontera : la puerta. Reconoce los nombres de los conjuntos cerrados que
// cruzan el wire, y para eso hay que mirar el conjunto. Rechaza ANTES de performar, que es lo
// unico que niega un nombre sin dejar una entrada permanente en el journal de la persona.
//
// DealershipCar.Dominio.Pruebas : el fixture. Instancia el modelo directo, sin scripts.
//
// DealershipCar.App NO esta aca, y eso es el invariante: la Superficie no puede declarar una
// variable del dominio ni recibirla como parametro, asi que la logica de negocio no se le
// puede mudar y los DTOs no se le pueden pasar al dominio.
[assembly: InternalsVisibleTo("DealershipCar.Frontera")]
[assembly: InternalsVisibleTo("DealershipCar.Dominio.Pruebas")]
