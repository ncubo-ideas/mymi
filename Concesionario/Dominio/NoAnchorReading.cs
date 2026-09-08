using System;

namespace Concesionario.Dominio
{
	/// <summary>El ancla sin mitad de odometro.</summary>
	internal sealed class NoAnchorReading : AnchorReading
	{
		/// <summary>Se llega por <see cref="AnchorReading.None"/>.</summary>
		internal NoAnchorReading()
		{
		}

		/// <summary>No.</summary>
		internal override bool Exists()
		{
			return false;
		}

		/// <summary>Revienta: no hay lectura que devolver.</summary>
		internal override int Value
		{
			get
			{
				throw new DomainException(
					"Este punto base no tiene lectura: pregunte HasReading antes. Devolver cero "
					+ "haria aparecer recorrido todo el odometro desde una completacion que nunca "
					+ "ocurrio.");
			}
		}

		/// <summary>Revienta: sin lectura no hay unidad.</summary>
		internal override DistanceUnit Unit
		{
			get
			{
				throw new DomainException(
					"Este punto base no tiene lectura, asi que tampoco tiene unidad: pregunte "
					+ "HasReading antes.");
			}
		}

		/// <summary>No, en ninguna unidad.</summary>
		internal override bool IsExpressedIn(DistanceUnit unit)
		{
			return false;
		}
	}
}
