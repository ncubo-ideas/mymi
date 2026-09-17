# Cómo correr la caja de wasm

Paso 0 de `F2 · P19`. El resultado medido está en
`Documentacion/Jonathan/F-evidencia/F8-el-paso-0-de-la-caja-de-wasm.md`.

## Lo que hace falta

- El workload: `dotnet workload install wasi-experimental` (ya instalado en esta máquina como
  `wasi-experimental-net9`).
- **wasmtime**, que no viene con nada. Un binario suelto:
  <https://github.com/bytecodealliance/wasmtime/releases> — se probó con la **48.0.2**.
- El motor **`3.0.0-beta.10049`** o cualquiera que traiga el guard de hilos en las DOS piezas.
  La `3.0.0-beta.10029` **no sirve**: trae el guard en `ActorHandler` y no en `Choreography`.

## Publicar

```
dotnet publish Spikes/Lab.WasiCaja/Lab.WasiCaja.csproj -c Release -p:WasmBuildNative=false
```

`WasmBuildNative=false` evita tener que instalar el `wasi-sdk` (el toolchain de C, ~1 GB): usa el
`dotnet.wasm` precompilado del runtime pack. Cuesta tamaño, no funcionalidad.

## Copiar ICU — sí, a mano

```
cp "$(dotnet --list-sdks >/dev/null; echo /c/Program\ Files/dotnet)/packs/Microsoft.NETCore.App.Runtime.Mono.wasi-wasm/9.0.19/runtimes/wasi-wasm/native/icudt.dat" \
   Spikes/Lab.WasiCaja/bin/Release/net9.0/wasi-wasm/AppBundle/
```

`InvariantGlobalization` es una opción administrada; el `dotnet.wasm` precompilado busca ICU igual y
muere en `load_icu_data` antes de la primera línea de C#.

## Correr

```
cd Spikes/Lab.WasiCaja/bin/Release/net9.0/wasi-wasm/AppBundle
mkdir -p datos
wasmtime run -S http --dir .::/ dotnet.wasm Lab.WasiCaja /datos
```

- `-S http` hace falta porque el `dotnet.wasm` de net9 importa `wasi:http`. Sin eso wasmtime se
  niega a instanciar el módulo.
- `--dir .::/` es **el preopen**: lo único que el módulo puede ver del sistema de archivos. Hoy se
  monta todo por comodidad; montar sólo la carpeta concedida es el **paso 2**.

## Si el publish sale en verde y no hay `AppBundle`

Falta `WasiNativeWorkload9`. Se comprueba con:

```
dotnet msbuild Spikes/Lab.WasiCaja/Lab.WasiCaja.csproj -getProperty:IsWasiProject
```

Si devuelve vacío, el SDK de WASI no se importó y el build **no avisa**. Está puesto en el csproj de
este spike, con la explicación al lado.

## La línea base nativa, para comparar

```
dotnet publish Spikes/Lab.WasiCaja/Lab.WasiCaja.csproj -c Release -r win-x64 --self-contained false \
  -p:WasiNativeWorkload9=false -p:PublishTrimmed=false -o <salida>
<salida>/Lab.WasiCaja.exe <carpeta>
```
