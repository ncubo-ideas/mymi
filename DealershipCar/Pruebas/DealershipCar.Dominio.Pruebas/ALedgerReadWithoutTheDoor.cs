using Choreography.Theater;
using Puppeteer;

namespace DealershipCar.Dominio.Pruebas
{
	// LO QUE LA PUERTA NO CONTESTA DE LA SEXTA GLOBAL. Nacio en CL-4 con CUATRO miembros, por la
	// misma razon por la que el de las estadias nacio con cuatro en SH-3: la sexta global de raiz
	// llego con sus DOS comandos y con NINGUNA lectura, y un comando que escribe donde nadie puede
	// leer esta verde tanto si escribio como si no.
	//
	// Y SE ENCOGIO EN CL-5 A UNO, como el de las estadias en SH-4 y el del perfil en T5 y T6. CL-5
	// puso las siete lecturas que no cruzan en la puerta, y ese mismo dia CostCountBetween,
	// SpendBetween y EachCostEntryBetween dejaron de tener razon de existir: un script crudo que
	// contesta lo que la puerta ya contesta es la superficie de lectura PARALELA que DEC-31 y DEC-35
	// refusaron. Sus llamadores se mudaron a CostEntryCountBetween, DescribeSpendBetween y
	// CostEntriesBetween, y la mudanza es REAL y no cosmetica: romper CostEntriesBetween pone rojas
	// las pruebas de los dos comandos, que antes no podian verlo.
	//
	// EL QUE QUEDA ES Vin:
	//
	//   Vin              CostLedger.ReadVin() no es ReadVin() de la puerta, que lee la AGENDA. Es la
	//                    unica lectura que contesta algo PROPIO de la sexta global, asi que es con la
	//                    que se mide su AUSENCIA antes de la migracion (DEC-31), y ninguna de las
	//                    siete de CL-5 la contesta -- §7.2 no la lista.
	//
	// LAS DOS CONDICIONES QUE LO HACEN LEGAL SIGUEN SIENDO LAS MISMAS: vive en Pruebas/, es internal,
	// y NO REPITE NINGUNA LECTURA QUE LA PUERTA TENGA. ESTO NO ES UNA SEGUNDA PUERTA.
	internal static class ALedgerReadWithoutTheDoor
	{
		internal static string Vin(PerformanceV2 app)
		{
			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using("@answer = ledger.ReadVin();")
				.WithParameters(rented, p => {
					p[Parameter.Out, "answer", typeof(string)] = default;
				})
				.PerformQuery();

				return rented["answer"].GetValue<string>();
			}
		}
	}
}
