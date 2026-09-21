using System;

namespace Gimnasio.Dominio
{
	// EL HECHO DE QUE LA PERSONA CONTRATO UN PRODUCTO: "Plan Black", y con el, el dia del
	// mes en que le vence cada cuota -"Fecha de vencimiento: Dia 10"-. Los dos vienen en
	// el papel A y B del v3 (N11).
	//
	// EL PRODUCTO ES TEXTO, NO UN CATALOGO: el papel mostro uno. Un catalogo con "Black"
	// y otros inventados seria la fila del Rubro municipal sin recibos que la sostengan.
	// Y no se llama Plan: en el papel "plan" son tres cosas -el producto, la linea del
	// cobro, el plan de ejercicios- y el modelo ya tiene una clase Plan que es la tercera.
	//
	// EL DIA DE VENCIMIENTO ES DEL CALENDARIO DE COBRO -conocimiento / intencion en la
	// grilla-, no de un cobro: cada cobro trae su propia fecha de vencimiento ademas.
	internal sealed class Contratacion
	{
		internal DateTime Fecha { get; }
		internal string Producto { get; }
		internal int DiaDeVencimiento { get; }

		internal Contratacion(DateTime fecha, string producto, int diaDeVencimiento)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(producto);
			if (diaDeVencimiento < 1 || diaDeVencimiento > 31)
				throw new ArgumentOutOfRangeException(nameof(diaDeVencimiento), diaDeVencimiento,
					"El dia de vencimiento es un dia del mes: de 1 a 31.");

			Fecha = fecha.Date;
			Producto = producto.Trim();
			DiaDeVencimiento = diaDeVencimiento;
		}

		internal bool EsElMismoProducto(string producto)
		{
			return !string.IsNullOrWhiteSpace(producto)
				&& string.Equals(Producto, producto.Trim(), StringComparison.OrdinalIgnoreCase);
		}
	}
}
