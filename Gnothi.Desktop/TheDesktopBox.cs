using Gnothi.Host;

namespace Gnothi.Desktop;

// THE BOX ON THE DESKTOP. The twin of Gnothi.Mobile/TheBox.cs, and the same files underneath.
//
// ── TWO THINGS CHANGE BETWEEN PLATFORMS, AND THE SECOND ONE WAS COSTLY TO FIND · 2026-09-25 ─────
//
// The native binary, which was the known one -- `libwasmtime.so` on Android, `wasmtime.dll` here --
// and **THE BORDER'S TRANSPORT**, which was not.
//
// The day the box reached the desktop it came up whole -- engine, substrate, reused `.cwasm`, the
// canary singing 8 of 8 -- and **died on the first module** with
// `DllNotFoundException: Unable to load DLL 'libc'`. The `0x8007007E` in the message is a WINDOWS
// HRESULT and not a wasm error: that gave away that the P/Invoke was being attempted IN THE PROCESS
// and not inside the box. It was `BoxedInstance` talking to the guest over a POSIX FIFO
// (`mkfifo(3)`).
//
// Today `BorderPipe` has both forms -- FIFO on POSIX, named pipe on Windows -- and both open BY
// PATH, which is the only thing `set_stdin_file` accepts, so the guest notices nothing.
//
// TWO THINGS WORTH KEEPING ABOUT HOW IT SHOWED UP:
//
//   1 · **It was seen by comparing, not by running the box alone.** The same module and the same
//       journal in the room and in the box: the room ran it, the box did not. One run of each on
//       its own would not have shown it -- the box's "started up fine".
//
//   2 · **And the opposite had been asserted beforehand**, on a check that did not check that: the
//       six files were reviewed for references to **Android**, none were found, and from there the
//       conclusion was "platform-neutral". **POSIX is not Android.** Looking for one thing and not
//       finding it says nothing about the others.
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
