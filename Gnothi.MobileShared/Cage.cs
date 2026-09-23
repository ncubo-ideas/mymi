namespace Gnothi.Host;

// THE CAGE: what a guest can reach from the inside, in ONE PLACE. See B12 and C10.
//
// WHY THIS FILE EXISTS, and it is not tidiness. The canary is good for exactly one thing: building a
// box EXACTLY like the one a module is about to get, putting a probe of Gnothi's inside, and
// checking that the leaks fail. If the canary builds its cage on its own and the instance builds its
// own, **the canary stops saying anything about modules and starts saying something about itself**:
// the two can drift apart without anything failing, and the day they do, the canary will keep
// singing that everything is shut while the real cage has a door open.
//
// The spike left that written as a warning -- «if this is built differently from the one below…» --
// and a warning in a comment is a regression waiting to happen. Here it is a function: there are no
// two paths.
//
// WHAT THIS CLASS DECIDES IS THE CAGE, NOT THE PROTOCOL. What goes in and out -- the FIFO, the
// callback, the argv -- belongs to each caller and differs between the canary and an instance. What
// does NOT differ is what gets mounted and what does not, and that is the only thing that lives here.
internal static class Cage
{
	// WHERE THE GRANTED FOLDER SHOWS UP on the inside: the person's records, and the only place the
	// guest may write.
	internal const string Granted = "/datos";

	// AND WHERE ITS PACKAGE SHOWS UP: the module's own DLLs, READ ONLY. The name is fixed by the
	// Host, not by the module -- the module does not choose where it is, just as it does not choose
	// which folder it was granted.
	internal const string Package = "/modulo";

	// BUILD THE CAGE. Returns the WASI configuration with EVERYTHING that defines what can be
	// reached: two preopens and nothing else. There is no root -- `/` is not a preopen, so it does
	// not exist for the guest -- and that is why the attempts to get out all fail.
	//
	// TWO DOORS, AND THEY ARE NOT THE SAME DOOR. Until the substrate was split from the module there
	// was one: the package travelled inside the .wasm and was never mounted. Now the module's DLLs
	// come from its bundle, so they have to be reachable -- and that is a NEW WAY IN, which is
	// exactly the kind of thing that gets added quietly and checked by nobody.
	//
	//   /datos    the granted folder    read AND write   it is the person's record; the module fills it
	//   /modulo   its own package       READ ONLY        it is what the institution shipped and signed
	//
	// WHY THE PACKAGE IS READ ONLY, and it is not caution: a module that can write its own DLLs can
	// rewrite the code that runs next time, and no signature would catch it -- the package was
	// verified at enrolment, not at every boot. `fsMutable: false` is what makes that impossible
	// instead of merely impolite. The canary checks it: writing into /modulo is one of the attempts
	// that MUST fail, and reading from it is one of the two that MUST work.
	//
	// stderr is inherited on purpose: it is there so whoever reads the device log sees the guest
	// blow up. It is not a channel of the protocol; the protocol is stdout.
	internal static nint Build(string grantedFolder, string packageFolder)
	{
		nint wasi = Wasmtime.wasi_config_new();
		Wasmtime.wasi_config_inherit_stderr(wasi);

		if (!Wasmtime.wasi_config_preopen_dir(wasi, grantedFolder, Granted, true))
			throw new InvalidOperationException($"caja: no pude conceder '{grantedFolder}'");

		// fsMutable: false -- the third argument is what makes this door one-way.
		if (!Wasmtime.wasi_config_preopen_dir(wasi, packageFolder, Package, false))
			throw new InvalidOperationException($"caja: no pude montar el paquete '{packageFolder}'");

		return wasi;
	}

	// ENCLOSE: create the store with that cage in place. It lives here for the same reason as the
	// above -- someone forgetting `set_wasi` on one of the two paths is exactly the divergence this
	// file exists to prevent.
	internal static nint Enclose(nint engine, nint wasi, out nint ctx)
	{
		nint store = Wasmtime.wasmtime_store_new(engine, 0, 0);
		ctx = Wasmtime.wasmtime_store_context(store);
		Wasmtime.wasmtime_context_set_wasi(ctx, wasi);
		Wasmtime.wasmtime_context_set_wasi_http(ctx);
		return store;
	}

	// THE COMPONENT'S ENTRY POINT, the same for every guest: `wasi:cli/run`. The version number is
	// tried newest-first because it is pinned by the SDK the .wasm was built with, not by us.
	private static readonly string[] Worlds = [
		"wasi:cli/run@0.2.0", "wasi:cli/run@0.2.1", "wasi:cli/run@0.2.2", "wasi:cli/run@0.2.3", "wasi:cli/run"];

	internal static byte[] EntryPoint(ref Wasmtime.Instance inst, nint ctx, string who)
	{
		nint instanceIndex = 0;
		foreach (string w in Worlds)
		{
			instanceIndex = Wasmtime.wasmtime_component_instance_get_export_index(ref inst, ctx, 0, w, (nuint)w.Length);
			if (instanceIndex != 0) break;
		}
		if (instanceIndex == 0) throw new InvalidOperationException($"caja: {who} no expone wasi:cli/run");

		nint runIndex = Wasmtime.wasmtime_component_instance_get_export_index(ref inst, ctx, instanceIndex, "run", 3);
		byte[] func = new byte[256];
		if (!Wasmtime.wasmtime_component_instance_get_func(ref inst, ctx, runIndex, func))
			throw new InvalidOperationException($"caja: {who} no tiene run");

		return func;
	}
}
