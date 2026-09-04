using Android.Security.Keystore;
using Gnothi.Movil;
using Java.Security;
using Javax.Crypto;
using Javax.Crypto.Spec;
using System.Text;

namespace Gnothi.Mobile;

// ANDROID'S SECRET STORE: an encrypted blob in the app's private folder, with a key from the
// **Android Keystore** that is NON-EXPORTABLE -- the key material never enters the app's
// process, it lives in the TEE. What is left on disk is worthless without this device.
//
// WHY NOT EncryptedSharedPreferences: Jetpack security-crypto was DEPRECATED in 1.1.0-alpha07
// (April 2025) in favour of using the Keystore directly, which is what is done here.
//
// THIS WAS HALF OF THE CREDENTIAL CLASS and was split off when the iOS one had to be written.
// The other half -- what those bytes mean -- does not change between platforms and stayed in
// Gnothi.Movil. There is no decision about credentials below: only how a piece of text is
// kept so that it cannot be read from outside.
internal sealed class AndroidSecretStore : ISecretStore
{
	private const string Provider = "AndroidKeyStore";
	private const string Transformation = "AES/GCM/NoPadding";

	// 12 bytes is the nonce GCM expects; it goes in front of the ciphertext, in the clear,
	// which is the normal arrangement.
	private const int NonceLength = 12;

	private readonly string folder;

	internal AndroidSecretStore(string privateFolder)
	{
		folder = Path.Combine(privateFolder, "credentials");
		Directory.CreateDirectory(folder);
	}

	public string? Get(string alias)
	{
		string file = FileFor(alias);
		if (!File.Exists(file)) return null;

		byte[] all = File.ReadAllBytes(file);
		if (all.Length <= NonceLength) return null;

		byte[] nonce = all[..NonceLength];
		byte[] ciphertext = all[NonceLength..];

		Cipher cipher = Cipher.GetInstance(Transformation)!;
		cipher.Init(CipherMode.DecryptMode, Key(alias), new GCMParameterSpec(128, nonce));

		return Encoding.UTF8.GetString(cipher.DoFinal(ciphertext)!);
	}

	public void Save(string alias, string json)
	{
		Cipher cipher = Cipher.GetInstance(Transformation)!;
		cipher.Init(CipherMode.EncryptMode, Key(alias));

		byte[] ciphertext = cipher.DoFinal(Encoding.UTF8.GetBytes(json))!;
		byte[] nonce = cipher.GetIV()!;

		var all = new byte[nonce.Length + ciphertext.Length];
		nonce.CopyTo(all, 0);
		ciphertext.CopyTo(all, nonce.Length);

		File.WriteAllBytes(FileFor(alias), all);
	}

	public void Forget(string alias)
	{
		string file = FileFor(alias);
		if (File.Exists(file)) File.Delete(file);

		try
		{
			KeyStore ks = KeyStore.GetInstance(Provider)!;
			ks.Load(null);
			ks.DeleteEntry(alias);
		}
		catch (Exception) { /* if it was not there, there is nothing to delete */ }
	}

	private string FileFor(string alias) => Path.Combine(folder, $"{alias}.bin");

	// THE KEY NEVER ENTERS THE PROCESS. It is generated inside the Keystore and what comes
	// back is a handle: it can encrypt and decrypt, it cannot be read or copied.
	private static IKey Key(string alias)
	{
		KeyStore ks = KeyStore.GetInstance(Provider)!;
		ks.Load(null);

		if (ks.ContainsAlias(alias)) return ks.GetKey(alias, null)!;

		KeyGenerator generator = KeyGenerator.GetInstance(KeyProperties.KeyAlgorithmAes, Provider)!;
		generator.Init(new KeyGenParameterSpec.Builder(alias, KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
			.SetBlockModes(KeyProperties.BlockModeGcm)!
			.SetEncryptionPaddings(KeyProperties.EncryptionPaddingNone)!
			.Build()!);

		return generator.GenerateKey()!;
	}
}
