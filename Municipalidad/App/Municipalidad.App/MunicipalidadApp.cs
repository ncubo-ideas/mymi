using System;
using System.IO;
using Choreography.Theater;

namespace Municipalidad.App
{
	/// <summary>
	/// El actor. La caratula lo construye una vez y lo mantiene vivo.
	/// <para>
	/// La semilla va dentro de un upgrade porque OnHydrated corre en CADA hidratacion y la guarda de
	/// upgrade es una-vez-por-actor: asi el arranque es idempotente sin un ejecutable de migracion. Y
	/// las raices se construyen ADENTRO de un comando, como globales de raiz: un campo de C# no se
	/// journaliza y vuelve vacio al siguiente arranque.
	/// </para>
	/// </summary>
	public sealed class MunicipalidadApp : PerformanceV2
	{
		/// <summary>
		/// El nombre del unico actor de una instalacion. Una persona tiene una cuenta con su
		/// municipalidad, asi que no hay clave que descubrir ni prefijo que parsear.
		/// </summary>
		public const string ElActor = "municipalidad";

		public MunicipalidadApp(string nombreDelActor)
			: base(nombreDelActor, ElDominio.Assembly)
		{
		}

		/// <summary>
		/// Si esta app ya se abrio alguna vez sobre esa carpeta. Es lo que le permite a una caratula
		/// de solo lectura contestar que la app nunca se abrio en esta computadora, en vez de reventar.
		/// </summary>
		public static bool HayJournalEn(string carpeta)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(carpeta);

			return Directory.Exists(Path.Combine(carpeta, ElActor, "journal"));
		}

		/// <summary>
		/// Las dos globales de raiz, cada una bajo su propio nombre de upgrade y las dos en UN solo
		/// comando: cada comando deja una entrada en CADA apertura, para siempre.
		/// </summary>
		protected override void OnHydrated()
		{
			Using(@"
				upgrade('semilla') {
					cuenta = Cuenta();
				}
				upgrade('bitacora') {
					bitacora = Bitacora();
				}
			")
			.PerformCommand();
		}
	}
}
