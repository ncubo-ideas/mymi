using System;
using System.Collections.Generic;
using System.Reflection;
using Concesionario.App;
using NUnit.Framework;

namespace Concesionario.Dominio.Pruebas
{
	// LAS PRUEBAS 4, 5 Y 6 DEL BRIEF, EN LA SUPERFICIE DE LA PUERTA. El brief las pide asi: una
	// entrada sin clasificacion se rechaza (4); una duradera sin vida util se rechaza nombrando lo que
	// falta (5); y una recurrente A LA QUE LE PASAN una vida util se decide y se prueba la decision
	// (6).
	//
	// LAS 4 Y LA 6 NO TIENEN GUARDA Y NO DEBEN TENERLA, y decirlo es la mitad honesta. §7.2 fija que
	// la clasificacion la recibe CUAL COMANDO SE LLAMO: no hay ninguna ranura por donde decirla y no
	// hay ninguna vida util en el camino recurrente. Una ley que ninguna llamada puede romper no se
	// defiende con un if, se defiende afirmando la FORMA -- y eso es lo que hay abajo. CL-3 afirmo la
	// mitad del DOMINIO en TheClassificationIsReceivedByWhichCommandWasCalled; esta es la de la
	// PUERTA, que es una superficie distinta y podria haberse escrito mal sola.
	//
	// LA 5 SI TIENE GUARDA, y es una de la PUERTA: una vida util que no es positiva la rechaza
	// AnExpectedLife del dominio, pero para cuando su guarda corre la entrada del comando ya se
	// escribio y el rechazo cicatriza el journal. La misma negativa una llamada antes no cuesta nada.
	// Lo que ese rechazo DICE se mide en TheDoorRefusesACostThatCannotBeSaidBeforePerforming, junto
	// con las otras dos del borde y con el journal byte a byte; aca se afirma que la vida util es
	// OBLIGATORIA POR FORMA, que es lo que ninguna prueba de comportamiento puede ver.
	//
	// SE ENTRA POR REFLEXION Y CONTRA CONJUNTOS EXACTOS, no contra listas de permitidos: un parametro
	// nuevo en cualquiera de los dos comandos se pone rojo con CUALQUIER nombre, que es lo que una
	// lista de nombres prohibidos no haria.
	[TestFixture]
	public sealed class ARecurringCostTakesNoLifeAndADurableOneRequiresIt
	{
		private const string TheRecurring = "RecordRecurringCost";
		private const string TheDurable = "RecordDurableCost";

		// EL COMANDO RECURRENTE, ENTERO. Cuatro parametros y ninguno mas: cuando, cuanto, en que
		// moneda y en que se fue.
		[Test]
		public void TheRecurringCommandTakesTheseFourThingsAndNothingElse()
		{
			Assert.That(TheNamesAndTypesOf(TheRecurring), Is.EqualTo(new[]
			{
				"incurredAt:DateTime", "amount:Decimal", "currency:String", "subject:String",
			}),
				"El comando recurrente gano o perdio un parametro. Los cuatro son lo que un gasto ES; "
				+ "un quinto seria alguien diciendo desde afuera algo que este comando decide.");
		}

		// EL DURADERO, ENTERO. Los mismos cuatro y la vida util, que es el quinto y el ultimo.
		[Test]
		public void TheDurableCommandTakesTheSameFourAndItsExpectedLife()
		{
			Assert.That(TheNamesAndTypesOf(TheDurable), Is.EqualTo(new[]
			{
				"incurredAt:DateTime", "amount:Decimal", "currency:String", "subject:String",
				"expectedLifeMonths:Int32",
			}));
		}

		// LA VIDA UTIL ES OBLIGATORIA POR FORMA, y esta es la unica prueba que lo ve. Un valor por
		// defecto la volveria opcional sin cambiar ninguna llamada existente y sin que ninguna prueba
		// de comportamiento se pusiera roja: quien no la pasara recibiria en silencio la que este
		// modulo eligio, que es L5 en su peor forma.
		[Test]
		public void TheExpectedLifeHasNoDefaultAndCannotBeOmitted()
		{
			ParameterInfo life = TheDoorsCommand(TheDurable).GetParameters()[4];

			Assert.That(life.Name, Is.EqualTo("expectedLifeMonths"));
			Assert.That(life.IsOptional, Is.False,
				"Un parametro opcional es una vida util por defecto con otro nombre.");
			Assert.That(life.HasDefaultValue, Is.False);
		}

		// Y EL RECURRENTE NO TIENE DONDE PONERLA. La prueba 6 del brief pregunta que pasa si a una
		// recurrente le pasan una vida util: no pasa nada, porque no hay por donde. No es una guarda
		// que refusa la contradiccion, es que la contradiccion no se puede escribir -- y esa es la
		// decision, dicha en vez de disimulada.
		[Test]
		public void TheRecurringCommandHasNoSlotForAnExpectedLifeAtAll()
		{
			foreach (ParameterInfo each in TheDoorsCommand(TheRecurring).GetParameters())
			{
				Assert.That(each.ParameterType, Is.Not.EqualTo(typeof(int)),
					$"'{each.Name}' es un entero en el comando recurrente. La vida util es el unico "
					+ "entero que un gasto lleva, y una recurrente no tiene ninguna.");
				Assert.That(each.Name.ToLowerInvariant(), Does.Not.Contain("life"));
				Assert.That(each.Name.ToLowerInvariant(), Does.Not.Contain("month"));
			}
		}

		// LA PRUEBA 4 DEL BRIEF, Y NO HAY GUARDA QUE ENSENAR. Ninguno de los dos comandos tiene por
		// donde recibir la clasificacion: ni un parametro de ese tipo -- que la puerta no podria
		// nombrar aunque quisiera, porque vive detras de la cerca del compilador -- ni una ranura con
		// nombre de clase. Un gasto sin clasificacion no se puede registrar porque no se puede DECIR.
		[Test]
		public void NeitherCommandHasAnywhereToSayWhatClassOfCostItIs()
		{
			foreach (string command in new[] { TheRecurring, TheDurable })
				foreach (ParameterInfo each in TheDoorsCommand(command).GetParameters())
				{
					Assert.That(each.ParameterType, Is.Not.EqualTo(typeof(CostClassification)),
						$"'{command}' recibe una clasificacion como tipo.");

					string said = each.Name.ToLowerInvariant();

					Assert.That(said, Does.Not.Contain("class"),
						$"'{command}' tiene un parametro '{each.Name}'. La clasificacion la recibe "
						+ "cual comando se llamo, y una ranura con ese nombre seria alguien de afuera "
						+ "diciendola.");
					Assert.That(said, Does.Not.Contain("kind"));
					Assert.That(said, Does.Not.Contain("recurring"));
					Assert.That(said, Does.Not.Contain("durable"));
				}
		}

		// LOS DOS SON ESCRITURAS Y NO PREGUNTAS QUE ESCRIBEN. Sin este renglon, los censos de arriba
		// pasarian igual si uno de los dos devolviera algo y cayera del otro lado del corte que
		// TheOwnerIsNeverASourceOfAFactAboutTheirOwnVehicle hace.
		[Test]
		public void BothAreCommandsAndNeitherAnswersAnything()
		{
			foreach (string command in new[] { TheRecurring, TheDurable })
				Assert.That(TheDoorsCommand(command).ReturnType, Is.EqualTo(typeof(void)),
					$"'{command}' dejo de ser un comando. Un gasto de la sucursal se REGISTRA; si "
					+ "esto devolviera algo, seria una pregunta que escribe.");
		}

		// EL MIEMBRO, Y REVIENTA SI HAY DOS CON ESE NOMBRE. Devolver el primero seria el defecto que
		// C57 nombra: un item leido por dos sobrecargas se afirma una vez y el barrido reporta una
		// cobertura que no tiene.
		private static MethodInfo TheDoorsCommand(string name)
		{
			var found = new List<MethodInfo>();

			foreach (MethodInfo each in typeof(DealershipCarAPI).GetMethods(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
				if (each.Name == name) found.Add(each);

			Assert.That(found, Has.Count.EqualTo(1),
				$"La puerta tiene {found.Count} miembros llamados '{name}'.");

			return found[0];
		}

		// NOMBRE Y TIPO JUNTOS, y en ORDEN. El orden importa porque es el orden en que el script del
		// DSL los ata: dos parametros del mismo tipo intercambiados pasan un censo de nombres sueltos.
		private static string[] TheNamesAndTypesOf(string command)
		{
			var said = new List<string>();

			foreach (ParameterInfo each in TheDoorsCommand(command).GetParameters())
				said.Add(each.Name + ":" + each.ParameterType.Name);

			return said.ToArray();
		}
	}
}
