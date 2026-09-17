using System.Runtime.InteropServices;

namespace Gnothi.Host;

// WASMTIME'S C API, the minimum needed to run a component. See B12 and F8.
//
// WHY BY HAND AND NOT WITH THE `Wasmtime` PACKAGE. The official C# bindings (44.0.0) only load CORE
// MODULES, and WASI's .NET produces a COMPONENT. Measured on 2026-09-16:
//     expected a WebAssembly module but was given a WebAssembly component
// Component model support is still an open issue in that repository; the C API does have it, under
// `include/wasmtime/component/`.
//
// THIS FILE IS SHARED WITH iOS and there is no libwasmtime there. That does no harm: a DllImport
// that is never called does not fail to load. What would fail is opening a module in a box, and on
// iOS that has not been tried yet -- there is no JIT, so wasmtime would have to be interpreted (F8).
internal static class Wasmtime
{
	private const string L = "wasmtime";

	// --- engine and store ------------------------------------------------------------------
	[DllImport(L)] internal static extern nint wasm_engine_new();
	[DllImport(L)] internal static extern void wasm_engine_delete(nint engine);
	[DllImport(L)] internal static extern nint wasmtime_store_new(nint engine, nint data, nint finalizer);
	[DllImport(L)] internal static extern nint wasmtime_store_context(nint store);
	[DllImport(L)] internal static extern void wasmtime_store_delete(nint store);

	// --- errors ----------------------------------------------------------------------------
	[StructLayout(LayoutKind.Sequential)]
	internal struct ByteVec { internal nuint size; internal nint data; }

	[DllImport(L)] internal static extern void wasmtime_error_message(nint error, out ByteVec message);
	[DllImport(L)] internal static extern void wasmtime_error_delete(nint error);
	[DllImport(L)] internal static extern void wasm_byte_vec_delete(ref ByteVec vec);

	// --- WASI, which IS the policy ---------------------------------------------------------
	[DllImport(L)] internal static extern nint wasi_config_new();
	[DllImport(L)] internal static extern void wasi_config_inherit_stderr(nint config);
	[DllImport(L)] internal static extern bool wasi_config_set_argv(nint config, nuint argc, nint[] argv);

	// WHAT THE MODULE SEES OF THE DISK, and that is all of it. It is not a path filter: it is the
	// list of descriptors that exist for the guest. Whatever is not here DOES NOT EXIST, which is why
	// an attempt to get out returns "not found" and not "denied". See B12.
	[DllImport(L)] internal static extern bool wasi_config_preopen_dir(
		nint config,
		[MarshalAs(UnmanagedType.LPUTF8Str)] string hostPath,
		[MarshalAs(UnmanagedType.LPUTF8Str)] string guestPath,
		[MarshalAs(UnmanagedType.I1)] bool fsMutable);

	// THE BORDER, AND THE ASYMMETRY THAT DEFINES IT: the Host HEARS through a callback and SPEAKS
	// through a path. With no input callback, the only way to talk to it while it runs is for that
	// path to be a FIFO. Measured on 2026-09-16 on the phone: it works. See F8.
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate nint GuestWrites(nint data, nint bytes, nuint count);

	[DllImport(L)] internal static extern void wasi_config_set_stdout_custom(
		nint config, GuestWrites callback, nint data, nint finalizer);

	[DllImport(L)] internal static extern bool wasi_config_set_stdin_file(
		nint config, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);

	[DllImport(L)] internal static extern void wasmtime_context_set_wasi(nint context, nint wasi);
	[DllImport(L)] internal static extern void wasmtime_context_set_wasi_http(nint context);

	// --- components ------------------------------------------------------------------------
	[DllImport(L)] internal static extern nint wasmtime_component_new(nint engine, byte[] buf, nuint len, out nint component);
	[DllImport(L)] internal static extern void wasmtime_component_delete(nint component);
	[DllImport(L)] internal static extern nint wasmtime_component_serialize(nint component, out ByteVec ret);
	[DllImport(L)] internal static extern nint wasmtime_component_deserialize_file(
		nint engine, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, out nint component);

	[DllImport(L)] internal static extern nint wasmtime_component_linker_new(nint engine);
	[DllImport(L)] internal static extern void wasmtime_component_linker_delete(nint linker);
	[DllImport(L)] internal static extern nint wasmtime_component_linker_add_wasip2(nint linker);

	// THE MODULE'S NETWORK IS NOT SWITCHED OFF: IT IS NEVER SWITCHED ON. This call sits apart from
	// `add_wasip2` on purpose -- it is wasmtime's doing, not ours -- and today it has to be made
	// because .NET's component IMPORTS wasi:http and will not instantiate without it. A runtime
	// demanding the network capability just to start is the opposite of what this architecture
	// wants, and it is written down in F8.
	[DllImport(L)] internal static extern nint wasmtime_component_linker_add_wasi_http(nint linker);

	[StructLayout(LayoutKind.Sequential)]
	internal struct Instance { internal ulong storeId; internal uint reserved; }

	[DllImport(L)] internal static extern nint wasmtime_component_linker_instantiate(
		nint linker, nint context, nint component, out Instance instance);

	[DllImport(L)] internal static extern nint wasmtime_component_instance_get_export_index(
		ref Instance instance, nint context, nint instanceExportIndex,
		[MarshalAs(UnmanagedType.LPUTF8Str)] string name, nuint nameLen);

	// func_t is passed by value and its union is large: plenty is reserved and it is never read.
	[DllImport(L)] internal static extern bool wasmtime_component_instance_get_func(
		ref Instance instance, nint context, nint exportIndex, byte[] funcOut);

	[DllImport(L)] internal static extern nint wasmtime_component_func_call(
		byte[] func, nint context, nint args, nuint argsSize, nint results, nuint resultsSize);

	// --- convenience -----------------------------------------------------------------------
	internal static string? Message(nint error)
	{
		if (error == 0) return null;
		wasmtime_error_message(error, out ByteVec v);
		string? s = Marshal.PtrToStringUTF8(v.data, (int)v.size);
		wasm_byte_vec_delete(ref v);
		wasmtime_error_delete(error);
		return s;
	}

	// mkfifo(3). There is no managed API for this and nothing more than this is needed.
	[DllImport("libc", SetLastError = true)]
	internal static extern int mkfifo([MarshalAs(UnmanagedType.LPUTF8Str)] string path, uint mode);
}
