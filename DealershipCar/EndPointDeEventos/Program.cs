// ANDAMIO DE T1. La narrativa del concesionario se escribe en T8.
//
// La forma de URL que la app consume es UNA y no se negocia:
//     GET {base}/{recurso}/{id}/events?after=N     Authorization: Bearer <token>
//
// Eso no es un detalle: LA APP NO ES CLIENTE DE LOS SERVICIOS GENERALES DEL CONCESIONARIO.
// Sirve eventos y nada mas.

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Results.Text(
    "DealershipCar.EventosMock: todavia no implementado. Se escribe en T8.\n"
    + "Ver DealershipCar/PLAN-OS2-MaintenanceSchedule.md\n"));

app.Run();
