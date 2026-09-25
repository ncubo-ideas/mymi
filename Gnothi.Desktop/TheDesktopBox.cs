using Gnothi.Host;

namespace Gnothi.Desktop;

// THE BOX ON THE DESKTOP. The twin of Gnothi.Mobile/TheBox.cs, and the same files underneath.
//
// ── DOS COSAS CAMBIAN ENTRE PLATAFORMAS, Y LA SEGUNDA COSTO ENCONTRARLA · 2026-09-25 ────────────
//
// El binario nativo, que era la conocida — `libwasmtime.so` en Android, `wasmtime.dll` aca — y **el
// TRANSPORTE DEL BORDE**, que no lo era.
//
// El dia que la caja llego al escritorio arrancaba entera —motor, sustrato, `.cwasm` reusado, el
// canario cantando 8 de 8— y **moria al primer modulo** con
// `DllNotFoundException: Unable to load DLL 'libc'`. El `0x8007007E` del mensaje es un HRESULT de
// WINDOWS y no un error de wasm: eso delato que el P/Invoke se intentaba EN EL PROCESO y no adentro
// de la caja. Era `BoxedInstance` hablandole al invitado por un FIFO de POSIX (`mkfifo(3)`).
//
// Hoy `BorderPipe` tiene las dos formas —FIFO en POSIX, named pipe en Windows— y las dos se abren
// POR RUTA, que es lo unico que `set_stdin_file` acepta, asi que el invitado no se entera.
//
// DOS COSAS QUE VALE GUARDAR DE COMO APARECIO:
//
//   1 · **Se vio comparando, no corriendo la caja sola.** El mismo modulo y el mismo journal en la
//       sala y en la caja: la sala lo corrio, la caja no. Una corrida de cada una por separado no lo
//       habria mostrado — la de la caja «arrancaba bien».
//
//   2 · **Y antes se habia afirmado lo contrario**, con una verificacion que no verificaba eso: se
//       reviso que los seis archivos no tuvieran referencias a **Android**, no se encontraron, y de
//       ahi se concluyo «neutrales de plataforma». **POSIX no es Android.** Buscar una cosa y no
//       encontrarla no dice nada de las demas.
//
// ── WHY IT IS HERE, BECAUSE IT IS NOT THE REASON IT IS ON THE PHONE ─────────────────────────────
//
// On a phone the box answers two things: that the module weighs what it really weighs, and that
// nobody has to trust it to behave. The first is paid for with Google Play's text. **Here only the
// second applies, and it applies whole.**
//
// Today, on this desktop, the institution's dll runs WITH GNOTHI'S PERMISSIONS. It can open another
// module's folder, go out to the network, read the laboratory's journal. Nothing in the system stops
// it -- the only thing that stops it is that it does not do it. Measured on the phone: of nine
// escape attempts, SEVEN work in process and EIGHT OF EIGHT close in the box. That number is not
// Android's, it is wasm's: there are no syscalls, so whatever Gnothi does not mount DOES NOT EXIST.
//
// The stub this replaces said the desktop solves containment "with the child process and the
// operating system's confinement". That is true of the `stdio` form. It is NOT true of `in-process`
// nor of the hybrid's surface, which run inside THIS process. There the hole is the same as on a
// phone.
//
// ── WHAT IT CLAIMS, AND THE LIMIT THAT IS WORTH NAMING ──────────────────────────────────────────
//
// It claims the HYBRID form, same as on the phone: `BoxShell` reads `Shell.Arguments[2]` -- the
// entry dll -- and builds a `WebViewShell` for the face, which is the hybrid package's shape.
//
// AND THE HYBRID IS THE FORM THERE IS, so that is not a limit -- it was written as one here and it
// was wrong. A CARGO module (`in-process`) is not a form a module ships in: it passes NEITHER
// store's text (B11: direct access, "no pasa" in both tables) and it has no face, so there is
// nothing for the person to open. It exists as a shell type because the desktop CLI and the tests
// use it, not because an institution would publish one.
//
// So the box not taking it is not a hole to close. Writing a faceless variant would be building a
// host for something nobody can hand to a person.
//
// ── WHY IT IS NOT BEHIND A FLAG, AND WHY THAT IS STILL SAFE ─────────────────────────────────────
//
// On the phone the box is a lab switch (`--ez caja true`) because there it exists to be MEASURED
// against the hybrid. Here it exists to CONTAIN, and a containment that has to be remembered is not
// containment.
//
// So there is no flag: it is attempted always. What gates it is whether it can actually be done --
// the native library present, the substrate present, and THE CANARY SINGING. If any of those is
// missing, the factory is never registered and every module runs exactly as it ran before, with one
// line in the log saying why. Nobody gets the box by accident, and nobody has to remember it once
// the pieces are there.
//
// AND THE CANARY IS ASKED HERE, at start-up, not when somebody opens a module. See WasmBox.Sings:
// a cage that does not pass has to be a log line and a box that goes unused, never a module that
// fails in the person's hands.
internal static class TheDesktopBox
{
    // THE SUBSTRATE, the same 11 MB file the phone carries. It is wasm: there is nothing
    // platform-specific in it, so it is the SAME artifact and not a build of its own.
    private const string TheWasm = "sustrato.wasm";

    internal static void Prepare(Action<string> log)
    {
        // WHERE THE ARTIFACT GOES, and it is not next to the module. The precompiled `.cwasm` is
        // produced by THIS machine and depends on the wasmtime version, on the processor and on the
        // engine options: it travels in no package and is accepted from nobody. The per-user private
        // area is the desktop's equivalent of the phone's FilesDir -- the same place the key and
        // model.cfg live.
        string privateFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Gnothi", "caja");

        string wasm = Path.Combine(AppContext.BaseDirectory, TheWasm);

        if (!File.Exists(wasm))
        {
            log($"caja: no esta {TheWasm} al lado del ejecutable; los modulos corren como siempre");
            return;
        }

        try
        {
            Directory.CreateDirectory(privateFolder);

            WasmBox.Log = log;
            BoxShell.SubstrateWasm = wasm;
            BoxShell.PrivateFolder = privateFolder;

            // COMPILING THE SUBSTRATE IS WHAT NEEDS THE NATIVE LIBRARY, so this is the line that
            // fails when `wasmtime.dll` is not beside the executable -- with a DllNotFoundException,
            // which the catch below turns into one readable line.
            WasmBox.Prepare(wasm, privateFolder);

            // AND THE CANARY BEFORE ANY MODULE. If it does not sing, nothing is registered: the box
            // exists in the process and is used by nobody, which is the right answer when the
            // containment cannot be demonstrated.
            if (!WasmBox.Sings(privateFolder))
            {
                log("caja: el canario NO paso; no se usa la caja y los modulos corren como siempre");
                return;
            }

            BoxShell.Preferred = true;

            // THE SEAM, chained and not assigned -- the desktop already registers a factory for the
            // hybrid and the webview in Program.cs, and this one arrives afterwards. See
            // Module.ChainShellFactory: a field with two owners and no rule cost the box being
            // switched off in silence once already.
            //
            // AND BECAUSE IT CHAINS, THE ORDER IS THE ONE THAT MATTERS: this one is asked FIRST and
            // takes the hybrids; everything it does not claim falls back to the desktop's own
            // factory, which goes on building the hybrid in process exactly as before.
            Module.ChainShellFactory((name, installation) =>
                installation.Shell.Type.Equals("hybrid", StringComparison.OrdinalIgnoreCase)
                    ? new BoxShell(name, installation.Shell, installation.GrantedFolder)
                    : null);

            log("caja: encendida; la superficie de los modulos hibridos corre adentro de wasm");
        }
        catch (Exception ex)
        {
            // IT FAILS CLOSED TOWARDS WHAT ALREADY WORKED. If the box cannot be prepared, Gnothi
            // carries on as always: the modules run in their usual form and nobody loses their
            // records over a containment that could not be set up.
            //
            // The commonest case is `wasmtime.dll` missing, and it is worth the message saying so by
            // name: a DllNotFoundException with a bare "wasmtime" sends people to look for a NuGet
            // package that this repo does not use.
            log(ex is DllNotFoundException
                ? "caja: no se encontro wasmtime.dll al lado del ejecutable; los modulos corren como siempre"
                : $"caja: no se pudo preparar ({ex.GetType().Name}: {ex.Message}); todo sigue igual");

            BoxShell.Preferred = false;
        }
    }
}
