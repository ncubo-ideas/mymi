using System;
using Choreography.Theater;
using Puppeteer;

namespace DealershipCar.Dominio.Pruebas
{
	// LO QUE LA PUERTA TODAVIA NO PUEDE CONTESTAR DE LA SEXTA GLOBAL. Es la misma forma que
	// AStayReadWithoutTheDoor tuvo en su primer dia y por la misma razon: SH-3 le agrego a la app la
	// QUINTA global de raiz, sus tres comandos y NINGUNA lectura, y nacio con cuatro miembros. CL-4
	// le agrega la SEXTA -- el libro de costos -- sus DOS comandos y NINGUNA lectura, y nace con
	// cuatro tambien.
	//
	// Y SE VA A ENCOGER EN CL-5, como se encogio el de las estadias en SH-4 y el del perfil en T5 y
	// T6. CL-5 pone las siete lecturas que no cruzan en la puerta, y ese mismo dia CostCountBetween,
	// SpendBetween y EachCostEntryBetween dejan de tener razon de existir: un script crudo que
	// contesta lo que la puerta ya contesta es la superficie de lectura PARALELA que DEC-31 y DEC-35
	// refusaron. Sus llamadores se mudan a CostEntryCountBetween, DescribeSpendBetween y
	// CostEntriesBetween.
	//
	// EL QUE QUEDA ES Vin:
	//
	//   Vin              CostLedger.ReadVin() no es ReadVin() de la puerta, que lee la AGENDA. Es la
	//                    unica lectura que contesta algo PROPIO de la sexta global, asi que es con la
	//                    que se mide su AUSENCIA antes de la migracion (DEC-31), y ninguna lectura de
	//                    CL-5 la contesta -- §7.2 no la lista.
	//
	// LOS OTROS TRES EXISTEN PORQUE LOS DOS COMANDOS NO TIENEN ORACULO. Un comando que escribe donde
	// nadie puede leer esta verde tanto si escribio como si no; eso fue lo que obligo a que el de las
	// estadias naciera con cuatro miembros y se quedara con uno.
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

		internal static int CostCountBetween(PerformanceV2 app, DateTime from, DateTime to)
		{
			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using("@answer = ledger.CostEntryCountBetween(@from, @to);")
				.WithParameters(rented, p => {
					p[Parameter.Out, "answer", typeof(int)] = default;
					p["from", typeof(DateTime)] = from;
					p["to",   typeof(DateTime)] = to;
				})
				.PerformQuery();

				return rented["answer"].GetValue<int>();
			}
		}

		internal static string SpendBetween(PerformanceV2 app, DateTime from, DateTime to)
		{
			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using("@answer = ledger.DescribeSpendBetween(@from, @to);")
				.WithParameters(rented, p => {
					p[Parameter.Out, "answer", typeof(string)] = default;
					p["from", typeof(DateTime)] = from;
					p["to",   typeof(DateTime)] = to;
				})
				.PerformQuery();

				return rented["answer"].GetValue<string>();
			}
		}

		internal static string[] EachCostEntryBetween(PerformanceV2 app, DateTime from, DateTime to)
		{
			using (RentedParameter rented = app.Actor.RentedParameters())
			{
				app.Actor.Using("@answer = ledger.DescribeEachCostEntryBetween(@from, @to);")
				.WithParameters(rented, p => {
					p[Parameter.Out, "answer", typeof(string[])] = default;
					p["from", typeof(DateTime)] = from;
					p["to",   typeof(DateTime)] = to;
				})
				.PerformQuery();

				return rented["answer"].GetValue<string[]>();
			}
		}
	}
}
