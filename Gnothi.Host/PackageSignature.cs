using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gnothi.Host;

// THE SIGNATURE OF A PACKAGE: that nothing in it changed since it was signed, and by which key.
//
// WHAT IT ADDS TO THE ORIGIN CHECK. The origin check (ModuleEnrollment) asks the origin that served
// or claims a package whether the package is its own, and TLS backs the answer -- while the origin
// is reachable, and while it is still the same origin. A signature stands on its own: it holds
// with the origin down, and it says something the origin cannot -- that THIS package was signed
// with the SAME key as the one already installed. Gnothi remembers the key it accepted first (the
// pin, in installation.json) and says out loud when a later package comes signed by another.
//
// WHAT IS SIGNED. Not the manifest alone -the seal already covers that- but a digest of EVERY file
// in the package: signature.json lists each path with its SHA-256 and carries the signature over
// that list. A package with a file changed, added or removed fails, however good its manifest.
//
// THE KEY TRAVELS WITH THE PACKAGE. signature.json carries the public key, so integrity is checked
// with nothing else in hand; identity -is this key the institution's?- is a separate question,
// answered by the origin's well-known (it publishes its keys) or by the pin. That split is what
// lets a file that arrived through chat be checked at all.
//
// THE ALGORITHM IS ECDSA P-256 WITH SHA-256 ("ES256"), from .NET itself: no library enters the
// Host or the phones for this, and it runs the same under Android, iOS and NativeAOT. The team's
// notes name Ed25519 for the engine's event signatures (BouncyCastle, inside the engine's
// adapters); it is not in the Host's dependency graph and .NET 9 has no Ed25519 of its own. The
// algorithm is named in every signature, so a second one can be added without touching packages
// already signed.
//
// A SIGNATURE PROVES CONTINUITY, NOT VIRTUE: the same author as last time, not a trustworthy one.
// The sheet never says "verified by Gnothi".

// The institution's key pair, as its tooling keeps it. The private half never leaves the file.
public sealed record SigningKey
{
	[JsonPropertyName("id")] public string Id { get; init; } = "";
	[JsonPropertyName("alg")] public string Alg { get; init; } = PackageSignature.Alg;

	// SubjectPublicKeyInfo, base64url. This is what the origin publishes and what the pin keeps.
	[JsonPropertyName("public")] public string Public { get; init; } = "";

	// PKCS#8, base64url. Null in anything published.
	[JsonPropertyName("private")] public string? Private { get; init; }
}

// THE PIN: the key a module was accepted with, written by the Host in installation.json at the
// first install and kept while the key stays the same. `Since` is when it was first accepted, so
// the sheet can say "the same key as last time, since <date>".
public sealed record PackageSigner
{
	[JsonPropertyName("keyId")] public string KeyId { get; init; } = "";
	[JsonPropertyName("public")] public string Public { get; init; } = "";
	[JsonPropertyName("since")] public string Since { get; init; } = "";
}

public enum SignatureState
{
	// No signature.json in the package. Not a failure: a package may come unsigned, and the sheet
	// says so.
	Unsigned,

	// There is a signature and it does not hold: a file changed, added or removed, or the
	// signature does not verify under the key it names. Never installed.
	Invalid,

	// Every file is the one that was signed, under the key in KeyId/Public. Whose key that is,
	// somebody else decides.
	Valid,
}

public sealed record SignatureVerification(SignatureState State, string KeyId, string Public, string Detail)
{
	public static readonly SignatureVerification None = new(SignatureState.Unsigned, "", "", "the package carries no signature.json");
}

public static class PackageSignature
{
	public const string FileName = "signature.json";
	public const string Alg = "ES256";

	// A fresh key pair, P-256. The id is the institution's label for it ("examenes-202609").
	public static SigningKey Generate(string id)
	{
		using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
		return new SigningKey
		{
			Id = id,
			Alg = Alg,
			Public = Base64Url(key.ExportSubjectPublicKeyInfo()),
			Private = Base64Url(key.ExportPkcs8PrivateKey()),
		};
	}

	public static SigningKey ReadKey(string path)
		=> JsonSerializer.Deserialize(File.ReadAllText(path), SignatureJson.Default.SigningKey)
		   ?? throw new InvalidDataException($"unreadable key file: {path}");

	public static void WriteKey(string path, SigningKey key)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
		File.WriteAllText(path, JsonSerializer.Serialize(key, SignatureJson.Default.SigningKey));
	}

	// Eight bytes of the SHA-256 of the public key, base64url: short enough to read out loud when
	// two keys have to be told apart.
	public static string Fingerprint(string publicKey)
		=> Base64Url(SHA256.HashData(FromBase64Url(publicKey))[..8]);

	// SIGN a package in place: every file hashed, the list signed, signature.json written. An
	// earlier signature is replaced, not stacked.
	public static void Sign(string packagePath, SigningKey key)
	{
		if (key.Private is null)
			throw new InvalidOperationException($"the key '{key.Id}' has no private half: it can verify, not sign");
		if (!string.Equals(key.Alg, Alg, StringComparison.Ordinal))
			throw new InvalidOperationException($"the key '{key.Id}' is {key.Alg}; this signer speaks {Alg}");

		using ZipArchive zip = ZipFile.Open(packagePath, ZipArchiveMode.Update);
		Find(zip)?.Delete();
		Dictionary<string, string> files = Digests(zip);

		using var ecdsa = ECDsa.Create();
		ecdsa.ImportPkcs8PrivateKey(FromBase64Url(key.Private), out _);
		byte[] signature = ecdsa.SignData(Payload(Alg, key.Id, files), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

		var file = new SignatureFile { Alg = Alg, KeyId = key.Id, Public = key.Public, Files = files, Signature = Base64Url(signature) };
		using Stream entry = zip.CreateEntry(FileName).Open();
		entry.Write(JsonSerializer.SerializeToUtf8Bytes(file, SignatureJson.Default.SignatureFile));
	}

	// VERIFY: does signature.json describe exactly the files that are there, and does its
	// signature hold under the key it names? Nothing is said here about WHOSE key that is.
	public static SignatureVerification Verify(ZipArchive zip)
	{
		ZipArchiveEntry? entry = Find(zip);
		if (entry is null) return SignatureVerification.None;

		SignatureFile? file;
		try
		{
			using Stream content = entry.Open();
			file = JsonSerializer.Deserialize(content, SignatureJson.Default.SignatureFile);
		}
		catch (JsonException e)
		{
			return new SignatureVerification(SignatureState.Invalid, "", "", "signature.json is not valid json: " + e.Message);
		}

		if (file is null || file.Signature.Length == 0 || file.Public.Length == 0)
			return new SignatureVerification(SignatureState.Invalid, file?.KeyId ?? "", file?.Public ?? "", "signature.json is incomplete");
		if (!string.Equals(file.Alg, Alg, StringComparison.Ordinal))
			return new SignatureVerification(SignatureState.Invalid, file.KeyId, file.Public, $"the signature says '{file.Alg}'; this verifier speaks {Alg}");

		// Every file present is listed with the same digest, and nothing listed is missing.
		Dictionary<string, string> actual = Digests(zip);
		string[] added = [.. actual.Keys.Except(file.Files.Keys, StringComparer.Ordinal)];
		string[] missing = [.. file.Files.Keys.Except(actual.Keys, StringComparer.Ordinal)];
		string[] changed = [.. actual.Where(kv => file.Files.TryGetValue(kv.Key, out string? signed) && !string.Equals(signed, kv.Value, StringComparison.Ordinal)).Select(kv => kv.Key)];
		if (added.Length + missing.Length + changed.Length > 0)
			return new SignatureVerification(SignatureState.Invalid, file.KeyId, file.Public,
				$"the files are not the ones that were signed: {added.Length} added, {missing.Length} missing, {changed.Length} changed"
				+ (changed.Length > 0 ? " (" + string.Join(", ", changed.Take(3)) + ")" : ""));

		try
		{
			using var ecdsa = ECDsa.Create();
			ecdsa.ImportSubjectPublicKeyInfo(FromBase64Url(file.Public), out _);
			bool holds = ecdsa.VerifyData(Payload(file.Alg, file.KeyId, file.Files), FromBase64Url(file.Signature),
				HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
			return holds
				? new SignatureVerification(SignatureState.Valid, file.KeyId, file.Public, "")
				: new SignatureVerification(SignatureState.Invalid, file.KeyId, file.Public, "the signature does not verify under the key it names");
		}
		catch (Exception e) when (e is CryptographicException or FormatException)
		{
			return new SignatureVerification(SignatureState.Invalid, file.KeyId, file.Public, "the public key or the signature is malformed: " + e.Message);
		}
	}

	// THE SIGNED BYTES, built the same way on both sides so nothing about JSON layout matters:
	// the algorithm, the key id, then every path with its digest, one per line, in ordinal order.
	internal static byte[] Payload(string alg, string keyId, IReadOnlyDictionary<string, string> files)
	{
		var text = new StringBuilder().Append(alg).Append('\n').Append(keyId).Append('\n');
		foreach (KeyValuePair<string, string> file in files.OrderBy(kv => kv.Key, StringComparer.Ordinal))
			text.Append(file.Key).Append(' ').Append(file.Value).Append('\n');
		return Encoding.UTF8.GetBytes(text.ToString());
	}

	// SHA-256 of every file in the package except the signature itself, keyed by path with
	// forward slashes: the same normalisation the inspection applies.
	private static Dictionary<string, string> Digests(ZipArchive zip)
	{
		var digests = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (ZipArchiveEntry entry in zip.Entries)
		{
			if (entry.Name.Length == 0) continue;
			string path = entry.FullName.Replace('\\', '/');
			if (path.Equals(FileName, StringComparison.OrdinalIgnoreCase)) continue;

			using Stream content = entry.Open();
			digests[path] = Base64Url(SHA256.HashData(content));
		}
		return digests;
	}

	private static ZipArchiveEntry? Find(ZipArchive zip)
		=> zip.Entries.FirstOrDefault(e => e.FullName.Replace('\\', '/').Equals(FileName, StringComparison.OrdinalIgnoreCase));

	//   Gnothi.Host.exe --sign <package.gmod> <key.json> [<key-id>]
	//
	// The publisher-side helper, like --pack: the same code that verifies on the phone signs here,
	// so the two cannot drift. The key file is created when it does not exist -that is the
	// institution's key pair, one per institution, and the demo must not commit a private key- and
	// the origin publishes its public half in its well-known.
	public static int SignFromCli(string[] args)
	{
		if (args.Length < 2)
		{
			Console.Error.WriteLine("[gnothi] usage: --sign <package.gmod> <key.json> [<key-id>]");
			return 2;
		}

		string package = Path.GetFullPath(args[0]);
		string keyPath = Path.GetFullPath(args[1]);
		if (!File.Exists(package))
		{
			Console.Error.WriteLine($"[gnothi] no such package: {package}");
			return 1;
		}

		SigningKey key;
		if (File.Exists(keyPath))
			key = ReadKey(keyPath);
		else
		{
			string id = args.Length > 2 ? args[2] : $"{Path.GetFileNameWithoutExtension(package)}-{DateTime.UtcNow:yyyyMM}";
			key = Generate(id);
			WriteKey(keyPath, key);
			Console.Error.WriteLine($"[gnothi] new key '{id}' ({Fingerprint(key.Public)}) -> {keyPath}");
		}

		Sign(package, key);

		using ZipArchive check = ZipFile.OpenRead(package);
		SignatureVerification verified = Verify(check);
		if (verified.State != SignatureState.Valid)
		{
			Console.Error.WriteLine($"[gnothi] signed, but the signature does not verify: {verified.Detail}");
			return 1;
		}

		Console.Error.WriteLine($"[gnothi] signed {Path.GetFileName(package)} with key '{key.Id}' ({Fingerprint(key.Public)})");
		return 0;
	}

	private static string Base64Url(byte[] bytes)
		=> Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

	private static byte[] FromBase64Url(string text)
	{
		string padded = text.Replace('-', '+').Replace('_', '/');
		padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
		return Convert.FromBase64String(padded);
	}
}

// What signature.json holds. Internal: nobody outside builds one by hand.
internal sealed record SignatureFile
{
	[JsonPropertyName("alg")] public string Alg { get; init; } = "";
	[JsonPropertyName("keyId")] public string KeyId { get; init; } = "";
	[JsonPropertyName("public")] public string Public { get; init; } = "";
	[JsonPropertyName("files")] public Dictionary<string, string> Files { get; init; } = new(StringComparer.Ordinal);
	[JsonPropertyName("signature")] public string Signature { get; init; } = "";
}

// Generated readers and writers, no reflection: this runs on the phones too (see HostJson.cs).
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SignatureFile))]
[JsonSerializable(typeof(SigningKey))]
internal partial class SignatureJson : JsonSerializerContext;
