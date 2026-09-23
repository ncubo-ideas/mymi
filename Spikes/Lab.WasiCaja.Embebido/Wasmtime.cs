using System;
using System.Runtime.InteropServices;

namespace Lab.WasiCaja.Embebido
{
	// LA C API DE WASMTIME, LO MINIMO PARA CORRER UN COMPONENTE. Paso 3 de F2 · P19.
	//
	// POR QUE A MANO Y NO CON EL PAQUETE `Wasmtime`. Las bindings oficiales de C# (44.0.0) solo
	// cargan MODULOS CORE, y el .NET de WASI produce un COMPONENTE. Medido:
	//     expected a WebAssembly module but was given a WebAssembly component
	// El soporte de component model sigue siendo un issue abierto en ese repositorio. La C API si
	// lo tiene, desde `include/wasmtime/component/`.
	//
	// Y ESTE ES EL MISMO CAMINO QUE HARIA FALTA EN ANDROID: alla wasmtime entra como libreria
	// nativa en el APK igual que aca entra como wasmtime.dll.
	internal static class W
	{
		private const string L = "wasmtime";

		// --- motor y almacen ---------------------------------------------------------------
		[DllImport(L)] internal static extern IntPtr wasm_engine_new();
		[DllImport(L)] internal static extern void wasm_engine_delete(IntPtr engine);
		[DllImport(L)] internal static extern IntPtr wasmtime_store_new(IntPtr engine, IntPtr data, IntPtr finalizer);
		[DllImport(L)] internal static extern IntPtr wasmtime_store_context(IntPtr store);
		[DllImport(L)] internal static extern void wasmtime_store_delete(IntPtr store);

		// --- errores -----------------------------------------------------------------------
		[StructLayout(LayoutKind.Sequential)]
		internal struct ByteVec { internal UIntPtr size; internal IntPtr data; }

		[DllImport(L)] internal static extern void wasmtime_error_message(IntPtr error, out ByteVec message);
		[DllImport(L)] internal static extern void wasmtime_error_delete(IntPtr error);
		[DllImport(L)] internal static extern void wasm_byte_vec_delete(ref ByteVec vec);

		// --- WASI --------------------------------------------------------------------------
		[DllImport(L)] internal static extern IntPtr wasi_config_new();
		[DllImport(L)] internal static extern void wasi_config_inherit_stdout(IntPtr config);
		[DllImport(L)] internal static extern void wasi_config_inherit_stderr(IntPtr config);
		[DllImport(L)] internal static extern void wasi_config_inherit_stdin(IntPtr config);

		// LA ENTRADA DEL INVITADO, y aca esta la asimetria: solo una RUTA o un bufer fijo. Sin
		// callback. Apuntarla a un FIFO es lo unico que permite hablarle mientras corre.
		[DllImport(L)] internal static extern bool wasi_config_set_stdin_file(
			IntPtr config, [MarshalAs(UnmanagedType.LPUTF8Str)] string ruta);
		[DllImport(L)] internal static extern bool wasi_config_set_stdout_file(IntPtr config, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);

		// LA SALIDA DEL INVITADO, POR CALLBACK. Es la mitad que la C API si resuelve: cada escritura
		// del invitado llega a codigo nuestro, sin archivos ni polling.
		// La otra mitad -- hablarle mientras corre -- NO tiene equivalente: stdin solo acepta una
		// ruta o un bufer fijo. De ahi el FIFO.
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		internal delegate IntPtr EscribeElInvitado(IntPtr datos, IntPtr bytes, UIntPtr cuantos);

		[DllImport(L)] internal static extern void wasi_config_set_stdout_custom(
			IntPtr config, EscribeElInvitado callback, IntPtr datos, IntPtr finalizador);
		[DllImport(L)] internal static extern bool wasi_config_set_argv(IntPtr config, UIntPtr argc, IntPtr[] argv);

		// EL PARAMETRO QUE IMPORTA ES EL ULTIMO. `fs_mutable` decide si el invitado puede
		// ESCRIBIR en lo montado. El paso 2 midio que el modulo podia reescribir su propio
		// paquete; con esto el paquete va de solo lectura y la carpeta concedida no.
		[DllImport(L)] internal static extern bool wasi_config_preopen_dir(
			IntPtr config,
			[MarshalAs(UnmanagedType.LPUTF8Str)] string hostPath,
			[MarshalAs(UnmanagedType.LPUTF8Str)] string guestPath,
			[MarshalAs(UnmanagedType.I1)] bool fsMutable);

		[DllImport(L)] internal static extern void wasmtime_context_set_wasi(IntPtr context, IntPtr wasi);

		// NO SE LLAMA A `wasmtime_context_set_wasi_http` NI A `add_wasi_http`, y eso ES la
		// politica: la red del modulo no se apaga -- nunca se enciende.

		// --- componentes -------------------------------------------------------------------
		[DllImport(L)] internal static extern IntPtr wasmtime_component_new(IntPtr engine, byte[] buf, UIntPtr len, out IntPtr component);
		[DllImport(L)] internal static extern void wasmtime_component_delete(IntPtr component);

		// PRECOMPILAR. Compilar el componente de 12 MB cuesta 163 MB de memoria y 1,2 s en el
		// telefono, CADA VEZ QUE GNOTHI ARRANCA. Estas dos funciones permiten hacerlo una sola vez
		// -- al instalar el modulo -- y despues abrir el resultado desde disco.
		//
		// EL ARTEFACTO ES ESPECIFICO de la version de wasmtime, del procesador y de las opciones del
		// motor. No viaja en el bundle de la institucion: lo produce el aparato.
		[DllImport(L)] internal static extern IntPtr wasmtime_component_serialize(IntPtr component, out ByteVec ret);
		[DllImport(L)] internal static extern IntPtr wasmtime_component_deserialize_file(
			IntPtr engine, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, out IntPtr component);

		[DllImport(L)] internal static extern IntPtr wasmtime_component_linker_new(IntPtr engine);
		[DllImport(L)] internal static extern void wasmtime_component_linker_delete(IntPtr linker);
		[DllImport(L)] internal static extern IntPtr wasmtime_component_linker_add_wasip2(IntPtr linker);
		[DllImport(L)] internal static extern void wasmtime_component_linker_define_unknown_imports_as_traps(IntPtr linker);
		[DllImport(L)] internal static extern IntPtr wasmtime_component_linker_add_wasi_http(IntPtr linker);
		[DllImport(L)] internal static extern void wasmtime_context_set_wasi_http(IntPtr context);

		[StructLayout(LayoutKind.Sequential)]
		internal struct ComponentInstance { internal ulong storeId; internal uint privado; }

		[DllImport(L)] internal static extern IntPtr wasmtime_component_linker_instantiate(
			IntPtr linker, IntPtr context, IntPtr component, out ComponentInstance instance);

		[DllImport(L)] internal static extern IntPtr wasmtime_component_instance_get_export_index(
			ref ComponentInstance instance, IntPtr context, IntPtr instanceExportIndex,
			[MarshalAs(UnmanagedType.LPUTF8Str)] string name, UIntPtr nameLen);

		// El func_t se pasa por valor y su union es grande; se reserva de sobra y no se lee.
		[DllImport(L)] internal static extern bool wasmtime_component_instance_get_func(
			ref ComponentInstance instance, IntPtr context, IntPtr exportIndex, byte[] funcOut);

		[DllImport(L)] internal static extern IntPtr wasmtime_component_func_call(
			byte[] func, IntPtr context, IntPtr args, UIntPtr argsSize, IntPtr results, UIntPtr resultsSize);

		// --- comodidad ---------------------------------------------------------------------
		internal static string Mensaje(IntPtr error)
		{
			if (error == IntPtr.Zero) return null;
			wasmtime_error_message(error, out ByteVec v);
			string s = Marshal.PtrToStringUTF8(v.data, (int)v.size);
			wasm_byte_vec_delete(ref v);
			wasmtime_error_delete(error);
			return s;
		}
	}
}
