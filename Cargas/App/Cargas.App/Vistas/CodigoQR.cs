using System;
using System.Collections.Generic;
using System.Text;

namespace Cargas.App.Vistas
{
	/// <summary>
	/// UN CODIGO QR, dibujado acá y no pedido a nadie.
	///
	/// La regla de cero CDN de esta app no es solo sobre fuentes: un carnet que necesita
	/// red para dibujarse no sirve en la bomba, que es exactamente donde hace falta. Y
	/// mandar el numero de socio a un generador ajeno seria filtrar el dato que el carnet
	/// existe para proteger.
	///
	/// ALCANCE DELIBERADAMENTE CHICO: modo byte, correccion M, versiones 1 a 3. Son las
	/// tres que tienen UN SOLO bloque de datos, asi que no hace falta el entrelazado de
	/// bloques — la parte del estandar que mas se equivoca uno. Da hasta 42 bytes, y un
	/// numero de socio son doce caracteres. Si algun dia no alcanza, lo que falta es el
	/// entrelazado, no otra cosa.
	///
	/// VERIFICADO CONTRA UN CODIFICADOR DE REFERENCIA INDEPENDIENTE, MODULO POR MODULO,
	/// porque un QR mal armado se ve perfecto y no lo lee ningun telefono: mirarlo no es
	/// una prueba. Ocho contenidos, versiones 1, 2 y 3 y los bordes de cada capacidad
	/// (14/15 y 26/27 bytes), todos identicos a la matriz de la referencia; y los cuatro
	/// realistas ademas se le dieron a leer a un DECODIFICADOR de verdad y volvieron con
	/// su texto. El comando `Cargas.Consola qr <contenido>` imprime la matriz en 0 y 1
	/// para poder repetirlo.
	///
	/// Dos defectos que aparecieron en esa comparacion y que no se ven mirando el dibujo:
	/// la informacion de formato iba TRANSPUESTA -la primera copia baja por la columna 8,
	/// no corre por la fila 8- y la penalizacion N3 barria las columnas con el indice
	/// acotado que no era.
	/// </summary>
	public static class CodigoQR
	{
		// Capacidad en bytes de datos (ya descontados el modo y el contador) por version,
		// con correccion M. Indice 0 = version 1.
		private static readonly int[] CapacidadEnBytes = { 14, 26, 42 };

		// Cuantos codewords de datos y cuantos de correccion lleva cada version con M.
		private static readonly int[] CodewordsDeDatos = { 16, 28, 44 };
		private static readonly int[] CodewordsDeCorreccion = { 10, 16, 26 };

		/// <summary>La matriz del codigo: true es modulo oscuro. Cuadrada.</summary>
		public static bool[,] Matriz(string contenido)
		{
			ArgumentNullException.ThrowIfNull(contenido);

			byte[] datos = Encoding.UTF8.GetBytes(contenido);

			int version = 0;
			while (version < CapacidadEnBytes.Length && datos.Length > CapacidadEnBytes[version])
				version++;

			if (version == CapacidadEnBytes.Length)
				throw new ArgumentException(
					$"Un contenido de {datos.Length} bytes no entra en este codificador "
					+ $"(tope {CapacidadEnBytes[^1]}). Falta el entrelazado de bloques.",
					nameof(contenido));

			byte[] codewords = ConCorreccion(datos, version);
			return Dibujar(codewords, version);
		}

		/// <summary>El mismo codigo, como un &lt;path&gt; de SVG: un solo trazo con un
		/// rectangulo por modulo oscuro. Un path y no mil rects porque el DOM de una
		/// pantalla de carnet no tiene por que llevar mil nodos.</summary>
		public static string TrazoSvg(string contenido) => TrazoSvg(Matriz(contenido));

		/// <summary>El trazo de una matriz ya armada, para que quien necesite el lado y el
		/// dibujo no tenga que codificar dos veces: son ocho mascaras con su penalizacion
		/// cada una, y no es gratis.</summary>
		public static string TrazoSvg(bool[,] matriz)
		{
			ArgumentNullException.ThrowIfNull(matriz);
			int lado = matriz.GetLength(0);

			var trazo = new StringBuilder();
			for (int fila = 0; fila < lado; fila++)
				for (int columna = 0; columna < lado; columna++)
					if (matriz[fila, columna])
						trazo.Append($"M{columna} {fila}h1v1h-1z");

			return trazo.ToString();
		}

		/// <summary>El lado de la matriz, para el viewBox.</summary>
		public static int Lado(string contenido) => Matriz(contenido).GetLength(0);

		// ------------------------------------------------------------------ los bits

		private static byte[] ConCorreccion(byte[] datos, int version)
		{
			var bits = new List<bool>();

			// Modo byte (0100) y el contador en 8 bits, que es lo que usan las versiones
			// 1 a 9.
			AgregarBits(bits, 0b0100, 4);
			AgregarBits(bits, datos.Length, 8);
			foreach (byte b in datos) AgregarBits(bits, b, 8);

			int capacidadEnBits = CodewordsDeDatos[version] * 8;

			// Terminador: hasta cuatro ceros, o menos si ya no cabe.
			for (int i = 0; i < 4 && bits.Count < capacidadEnBits; i++) bits.Add(false);

			// Y hasta completar el byte.
			while (bits.Count % 8 != 0) bits.Add(false);

			// El relleno alterna estos dos, que es lo que manda el estandar.
			byte[] relleno = { 0xEC, 0x11 };
			for (int i = 0; bits.Count < capacidadEnBits; i++)
				AgregarBits(bits, relleno[i % 2], 8);

			var codewords = new byte[CodewordsDeDatos[version]];
			for (int i = 0; i < codewords.Length; i++)
			{
				int valor = 0;
				for (int b = 0; b < 8; b++) valor = (valor << 1) | (bits[i * 8 + b] ? 1 : 0);
				codewords[i] = (byte)valor;
			}

			byte[] correccion = ReedSolomon(codewords, CodewordsDeCorreccion[version]);

			var todo = new byte[codewords.Length + correccion.Length];
			Array.Copy(codewords, todo, codewords.Length);
			Array.Copy(correccion, 0, todo, codewords.Length, correccion.Length);
			return todo;
		}

		private static void AgregarBits(List<bool> bits, int valor, int cuantos)
		{
			for (int i = cuantos - 1; i >= 0; i--) bits.Add(((valor >> i) & 1) == 1);
		}

		// --------------------------------------------------- Reed-Solomon en GF(256)

		private static readonly byte[] Exp = new byte[512];
		private static readonly byte[] Log = new byte[256];

		static CodigoQR()
		{
			// El campo del estandar: primitivo 0x11D.
			int x = 1;
			for (int i = 0; i < 255; i++)
			{
				Exp[i] = (byte)x;
				Log[x] = (byte)i;
				x <<= 1;
				if (x >= 256) x ^= 0x11D;
			}
			for (int i = 255; i < 512; i++) Exp[i] = Exp[i - 255];
		}

		private static byte Multiplicar(byte a, byte b) =>
			a == 0 || b == 0 ? (byte)0 : Exp[Log[a] + Log[b]];

		private static byte[] ReedSolomon(byte[] datos, int cuantos)
		{
			// El polinomio generador: (x - a^0)(x - a^1)...
			var generador = new byte[cuantos + 1];
			generador[0] = 1;
			for (int i = 0; i < cuantos; i++)
			{
				for (int j = i + 1; j > 0; j--)
					generador[j] = (byte)(generador[j - 1] ^ Multiplicar(generador[j], Exp[i]));
				generador[0] = Multiplicar(generador[0], Exp[i]);
			}

			var resto = new byte[cuantos];
			foreach (byte dato in datos)
			{
				byte factor = (byte)(dato ^ resto[0]);
				Array.Copy(resto, 1, resto, 0, cuantos - 1);
				resto[cuantos - 1] = 0;

				if (factor != 0)
					for (int i = 0; i < cuantos; i++)
						resto[i] ^= Multiplicar(generador[cuantos - 1 - i], factor);
			}
			return resto;
		}

		// ------------------------------------------------------------- la matriz

		private static bool[,] Dibujar(byte[] codewords, int version)
		{
			int lado = 21 + version * 4;
			var modulos = new bool?[lado, lado];

			PatronesDeBusqueda(modulos, lado);
			Temporizacion(modulos, lado);
			if (version > 0) Alineacion(modulos, lado);

			// El modulo oscuro, que siempre va y siempre en el mismo lugar.
			modulos[lado - 8, 8] = true;

			// La informacion de formato se reserva antes de colocar datos.
			ReservarFormato(modulos, lado);

			ColocarDatos(modulos, codewords, lado);

			// LA MASCARA se elige por penalizacion, como manda el estandar: es lo que
			// evita zonas grandes del mismo color que confunden al lector.
			int mejor = 0;
			int menorPenalizacion = int.MaxValue;
			bool[,] mejorMatriz = null;

			for (int mascara = 0; mascara < 8; mascara++)
			{
				bool[,] candidata = Aplicar(modulos, lado, mascara);
				EscribirFormato(candidata, lado, mascara);

				int penalizacion = Penalizacion(candidata, lado);
				if (penalizacion < menorPenalizacion)
				{
					menorPenalizacion = penalizacion;
					mejor = mascara;
					mejorMatriz = candidata;
				}
			}

			return mejorMatriz;
		}

		private static void PatronesDeBusqueda(bool?[,] m, int lado)
		{
			foreach ((int fila, int columna) in new[] { (0, 0), (0, lado - 7), (lado - 7, 0) })
			{
				for (int f = -1; f <= 7; f++)
					for (int c = -1; c <= 7; c++)
					{
						int ff = fila + f, cc = columna + c;
						if (ff < 0 || ff >= lado || cc < 0 || cc >= lado) continue;

						bool oscuro = f >= 0 && f <= 6 && c >= 0 && c <= 6
							&& (f == 0 || f == 6 || c == 0 || c == 6
								|| (f >= 2 && f <= 4 && c >= 2 && c <= 4));
						m[ff, cc] = oscuro;
					}
			}
		}

		private static void Temporizacion(bool?[,] m, int lado)
		{
			for (int i = 8; i < lado - 8; i++)
			{
				bool oscuro = i % 2 == 0;
				if (m[6, i] == null) m[6, i] = oscuro;
				if (m[i, 6] == null) m[i, 6] = oscuro;
			}
		}

		private static void Alineacion(bool?[,] m, int lado)
		{
			// Las versiones 2 y 3 llevan UNO solo, y siempre en la misma esquina.
			int centro = lado - 7;
			for (int f = -2; f <= 2; f++)
				for (int c = -2; c <= 2; c++)
					m[centro + f, centro + c] =
						Math.Abs(f) == 2 || Math.Abs(c) == 2 || (f == 0 && c == 0);
		}

		private static void ReservarFormato(bool?[,] m, int lado)
		{
			for (int i = 0; i < 9; i++)
			{
				if (m[8, i] == null) m[8, i] = false;
				if (m[i, 8] == null) m[i, 8] = false;
			}
			for (int i = 0; i < 8; i++)
			{
				if (m[8, lado - 1 - i] == null) m[8, lado - 1 - i] = false;
				if (m[lado - 1 - i, 8] == null) m[lado - 1 - i, 8] = false;
			}
		}

		private static void ColocarDatos(bool?[,] m, byte[] codewords, int lado)
		{
			int bit = 0;
			int total = codewords.Length * 8;
			bool arriba = true;

			for (int columna = lado - 1; columna > 0; columna -= 2)
			{
				// La columna 6 es la de temporizacion: se salta entera.
				if (columna == 6) columna--;

				for (int paso = 0; paso < lado; paso++)
				{
					int fila = arriba ? lado - 1 - paso : paso;

					for (int desplazamiento = 0; desplazamiento < 2; desplazamiento++)
					{
						int c = columna - desplazamiento;
						if (m[fila, c] != null) continue;

						bool oscuro = false;
						if (bit < total)
						{
							oscuro = ((codewords[bit / 8] >> (7 - bit % 8)) & 1) == 1;
							bit++;
						}
						m[fila, c] = oscuro;
					}
				}
				arriba = !arriba;
			}
		}

		// Los modulos de funcion NO se enmascaran; los de datos, si.
		private static bool EsDeFuncion(int fila, int columna, int lado)
		{
			if (fila == 6 || columna == 6) return true;
			if (fila < 9 && columna < 9) return true;
			if (fila < 9 && columna >= lado - 8) return true;
			if (fila >= lado - 8 && columna < 9) return true;

			if (lado > 21)
			{
				int centro = lado - 7;
				if (Math.Abs(fila - centro) <= 2 && Math.Abs(columna - centro) <= 2) return true;
			}
			return false;
		}

		private static bool[,] Aplicar(bool?[,] m, int lado, int mascara)
		{
			var salida = new bool[lado, lado];
			for (int f = 0; f < lado; f++)
				for (int c = 0; c < lado; c++)
				{
					bool valor = m[f, c] ?? false;
					if (!EsDeFuncion(f, c, lado) && Enmascara(mascara, f, c)) valor = !valor;
					salida[f, c] = valor;
				}
			return salida;
		}

		private static bool Enmascara(int mascara, int f, int c) => mascara switch
		{
			0 => (f + c) % 2 == 0,
			1 => f % 2 == 0,
			2 => c % 3 == 0,
			3 => (f + c) % 3 == 0,
			4 => (f / 2 + c / 3) % 2 == 0,
			5 => (f * c) % 2 + (f * c) % 3 == 0,
			6 => ((f * c) % 2 + (f * c) % 3) % 2 == 0,
			_ => ((f + c) % 2 + (f * c) % 3) % 2 == 0,
		};

		private static void EscribirFormato(bool[,] m, int lado, int mascara)
		{
			// Correccion M es 00, y el resto es el BCH del estandar con su mascara fija.
			int datos = (0b00 << 3) | mascara;
			int bch = datos << 10;
			for (int i = 4; i >= 0; i--)
				if (((bch >> (i + 10)) & 1) == 1) bch ^= 0b10100110111 << i;

			int formato = ((datos << 10) | bch) ^ 0b101010000010010;

			for (int i = 0; i < 15; i++)
			{
				bool oscuro = ((formato >> i) & 1) == 1;

				// La primera copia BAJA POR LA COLUMNA 8 y dobla sobre la fila 8, saltando
				// en las dos patas la linea de temporizacion. Escribirla transpuesta da un
				// codigo que se ve impecable y que ningun lector abre.
				if (i < 6) m[i, 8] = oscuro;
				else if (i == 6) m[7, 8] = oscuro;
				else if (i == 7) m[8, 8] = oscuro;
				else if (i == 8) m[8, 7] = oscuro;
				else m[8, 14 - i] = oscuro;

				// Y la segunda copia, repartida entre las otras dos esquinas.
				if (i < 8) m[8, lado - 1 - i] = oscuro;
				else m[lado - 15 + i, 8] = oscuro;
			}

			m[lado - 8, 8] = true;
		}

		// --------------------------------------------------------- la penalizacion

		private static int Penalizacion(bool[,] m, int lado)
		{
			int total = 0;

			// N1: rachas de cinco o mas del mismo color, en filas y en columnas.
			for (int f = 0; f < lado; f++)
			{
				total += PenalizarRacha(m, lado, f, true);
				total += PenalizarRacha(m, lado, f, false);
			}

			// N2: bloques de 2x2 del mismo color.
			for (int f = 0; f < lado - 1; f++)
				for (int c = 0; c < lado - 1; c++)
					if (m[f, c] == m[f, c + 1] && m[f, c] == m[f + 1, c] && m[f, c] == m[f + 1, c + 1])
						total += 3;

			// N3: el patron que se confunde con uno de busqueda, en los dos sentidos. El
			// indice que corre es SIEMPRE el que va acotado a lado-11; confundirlo con el
			// otro se sale de la matriz.
			bool[] patron = { true, false, true, true, true, false, true, false, false, false, false };
			bool[] invertido = { false, false, false, false, true, false, true, true, true, false, true };

			for (int fija = 0; fija < lado; fija++)
				for (int corre = 0; corre <= lado - 11; corre++)
				{
					if (CoincideEnFila(m, fija, corre, patron)) total += 40;
					if (CoincideEnFila(m, fija, corre, invertido)) total += 40;
					if (CoincideEnColumna(m, fija, corre, patron)) total += 40;
					if (CoincideEnColumna(m, fija, corre, invertido)) total += 40;
				}

			// N4: cuanto se desvia del 50% de modulos oscuros.
			int oscuros = 0;
			for (int f = 0; f < lado; f++)
				for (int c = 0; c < lado; c++)
					if (m[f, c]) oscuros++;

			int porcentaje = oscuros * 100 / (lado * lado);
			total += Math.Abs(porcentaje - 50) / 5 * 10;

			return total;
		}

		private static int PenalizarRacha(bool[,] m, int lado, int indice, bool porFila)
		{
			int total = 0, racha = 1;
			bool anterior = porFila ? m[indice, 0] : m[0, indice];

			for (int i = 1; i < lado; i++)
			{
				bool actual = porFila ? m[indice, i] : m[i, indice];
				if (actual == anterior) racha++;
				else
				{
					if (racha >= 5) total += 3 + (racha - 5);
					racha = 1;
					anterior = actual;
				}
			}
			if (racha >= 5) total += 3 + (racha - 5);
			return total;
		}

		private static bool CoincideEnFila(bool[,] m, int fila, int desde, bool[] patron)
		{
			for (int i = 0; i < patron.Length; i++)
				if (m[fila, desde + i] != patron[i]) return false;
			return true;
		}

		private static bool CoincideEnColumna(bool[,] m, int columna, int desde, bool[] patron)
		{
			for (int i = 0; i < patron.Length; i++)
				if (m[desde + i, columna] != patron[i]) return false;
			return true;
		}
	}
}
