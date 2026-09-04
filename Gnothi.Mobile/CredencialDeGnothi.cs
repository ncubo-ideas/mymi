using Android.Security.Keystore;
using Java.Security;
using Javax.Crypto;
using Javax.Crypto.Spec;
using System.Text;
using System.Text.Json;

namespace Gnothi.Mobile;

// LA CREDENCIAL DE GNOTHI ANTE UN PUBLICADOR. Una por módulo, y de Gnothi, no del módulo
// (especificación 13 §3).
//
// TRES COSAS QUE ESTA CLASE EXISTE PARA SOSTENER:
//
//   1. NO VA AL JOURNAL. Un token es contexto de sesión, no historia de la persona. Cuando
//      vivía ahí quedaba en claro, viajaba con cualquier copia del journal y lo leía todo
//      proceso que leyera la carpeta. Eso ya se sacó del lado de la PWA; acá se sostiene.
//   2. NO LA VE EL MÓDULO. Está del otro lado del puente, y con `connect-src 'self'` el
//      módulo tampoco podría usarla aunque la tuviera.
//   3. NO SALE DEL APARATO EN CLARO. La llave que la cifra la genera el **Android Keystore**
//      y es NO EXPORTABLE: el material nunca entra al proceso de la app, vive en el TEE. Lo
//      que se guarda en disco es el blob cifrado; sin el aparato no sirve.
//
// POR QUÉ NO EncryptedSharedPreferences: Jetpack security-crypto quedó DEPRECADO en
// 1.1.0-alpha07 (abril de 2025) a favor de usar el Keystore directo, que es lo que se hace acá.
internal sealed class CredencialDeGnothi
{
	private const string Proveedor = "AndroidKeyStore";
	private const string Transformacion = "AES/GCM/NoPadding";

	// 12 bytes es el nonce que GCM espera; va delante del cifrado, en claro, que es lo normal.
	private const int LargoDelNonce = 12;

	private readonly string alias;
	private readonly string archivo;

	private Guardada? enMemoria;

	internal CredencialDeGnothi(string modulo, string carpetaPrivada)
	{
		// Una llave y un archivo POR MÓDULO: la credencial de un laboratorio no tiene por qué
		// poder descifrar la de otro.
		alias = $"gnothi.credencial.{modulo}";
		archivo = Path.Combine(carpetaPrivada, "credenciales", $"{modulo}.bin");
		Directory.CreateDirectory(Path.GetDirectoryName(archivo)!);
	}

	internal string DeviceKey => Leer().DeviceKey;

	internal string Token => Leer().Token;

	internal string Refresh => Leer().Refresh;

	// La identidad de ESTA instalación de Gnothi ante ESTE publicador. Se genera una sola vez
	// y se guarda; si se perdiera, el publicador vería otro dispositivo.
	internal string AsegurarDeviceKey()
	{
		Guardada actual = Leer();
		if (actual.DeviceKey.Length > 0) return actual.DeviceKey;

		actual.DeviceKey = "device-gnothi-" + Guid.NewGuid().ToString("N")[..16];
		Escribir(actual);
		return actual.DeviceKey;
	}

	internal void Guardar(string token, string refresh)
	{
		Guardada actual = Leer();
		actual.Token = token ?? "";
		actual.Refresh = refresh ?? "";
		Escribir(actual);
	}

	// Al quitar el módulo. En un publicador con revocación (RFC 7009) esto va acompañado de
	// avisarle, para que la concesión muera del otro lado también.
	internal void Olvidar()
	{
		enMemoria = null;
		if (File.Exists(archivo)) File.Delete(archivo);

		try
		{
			KeyStore ks = KeyStore.GetInstance(Proveedor)!;
			ks.Load(null);
			ks.DeleteEntry(alias);
		}
		catch (Exception) { /* si no estaba, no hay nada que borrar */ }
	}

	// ---------------------------------------------------------------- el cifrado

	private Guardada Leer()
	{
		if (enMemoria is not null) return enMemoria;

		if (!File.Exists(archivo)) return enMemoria = new Guardada();

		try
		{
			byte[] todo = File.ReadAllBytes(archivo);
			if (todo.Length <= LargoDelNonce) return enMemoria = new Guardada();

			byte[] nonce = todo[..LargoDelNonce];
			byte[] cifrado = todo[LargoDelNonce..];

			Cipher cifra = Cipher.GetInstance(Transformacion)!;
			cifra.Init(CipherMode.DecryptMode, Llave(), new GCMParameterSpec(128, nonce));

			byte[] claro = cifra.DoFinal(cifrado)!;
			return enMemoria = JsonSerializer.Deserialize<Guardada>(Encoding.UTF8.GetString(claro))
				?? new Guardada();
		}
		catch (Exception ex)
		{
			// Un blob que no se puede descifrar es un blob perdido: pasa si la llave se fue
			// -restaurar un backup en otro aparato, borrar datos de la app-. NO se rompe:
			// Gnothi pide una concesión nueva, que es lo que siempre puede hacer.
			Android.Util.Log.Warn("gnothi", $"credencial ilegible ({ex.GetType().Name}): se pedirá una nueva");
			return enMemoria = new Guardada();
		}
	}

	private void Escribir(Guardada valor)
	{
		enMemoria = valor;

		Cipher cifra = Cipher.GetInstance(Transformacion)!;
		cifra.Init(CipherMode.EncryptMode, Llave());

		byte[] cifrado = cifra.DoFinal(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(valor)))!;
		byte[] nonce = cifra.GetIV()!;

		var todo = new byte[nonce.Length + cifrado.Length];
		nonce.CopyTo(todo, 0);
		cifrado.CopyTo(todo, nonce.Length);

		File.WriteAllBytes(archivo, todo);
	}

	// LA LLAVE NUNCA ENTRA AL PROCESO. Se genera dentro del Keystore y lo que vuelve es un
	// asa: se puede cifrar y descifrar con ella, no leerla ni copiarla.
	private IKey Llave()
	{
		KeyStore ks = KeyStore.GetInstance(Proveedor)!;
		ks.Load(null);

		if (ks.ContainsAlias(alias)) return ks.GetKey(alias, null)!;

		KeyGenerator generador = KeyGenerator.GetInstance(KeyProperties.KeyAlgorithmAes, Proveedor)!;
		generador.Init(new KeyGenParameterSpec.Builder(alias, KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
			.SetBlockModes(KeyProperties.BlockModeGcm)!
			.SetEncryptionPaddings(KeyProperties.EncryptionPaddingNone)!
			.Build()!);

		return generador.GenerateKey()!;
	}

	private sealed class Guardada
	{
		public string DeviceKey { get; set; } = "";

		public string Token { get; set; } = "";

		public string Refresh { get; set; } = "";
	}
}
