# Third-party notices

Mymi is released under the MIT License (see `LICENSE`). That licence covers the
code in this repository. It does **not** cover the third-party components listed
below, which keep their own licences and whose terms travel with them.

Only components **whose bytes are in this repository** are listed here. NuGet and
npm packages are *referenced*, not redistributed: their licences reach the user
through the package itself, not through this repo.

---

## Redistributed in this repository

| component | where | licence | notice file |
|---|---|---|---|
| **jsQR** | `Gnothi.Mobile/Assets/jsqr.js` | Apache 2.0 | `Gnothi.Mobile/Assets/jsqr-LICENSE.txt` ✅ |
| **wasmtime** (prebuilt `libwasmtime.so`, arm64-v8a) | `Gnothi.Mobile/lib/arm64-v8a/` | Apache 2.0 **with LLVM exception** | ⚠️ **missing — see below** |
| **Plus Jakarta Sans**, **Nunito Sans**, **Barlow** | several module bundles | SIL Open Font License 1.1 | partial — see below |

### wasmtime

The Android build carries a prebuilt `libwasmtime.so` (~27 MB). On the desktop it
is **not** redistributed: `Gnothi.Desktop/wasmtime.targets` downloads the official
asset at build time and verifies its SHA256, so that platform redistributes
nothing.

The Android copy does need its licence alongside it, and today it has none.

### Fonts

The SIL OFL requires its text to travel with the font files. Some bundles do carry
it (`Concesionario/.../fonts/OFL.txt`, `Compras/PaginaDeDescarga/marca/OFL-PlusJakartaSans.txt`)
and others do not.

---

## Known gaps, as of 2026-10-13

These are **licence-compliance gaps that exist regardless of which licence Mymi
itself uses**. They were found while adding `LICENSE` and are recorded here rather
than left implicit:

1. **`libwasmtime.so` ships with no licence text.** Apache 2.0 §4 requires the
   licence and any NOTICE to accompany redistribution. Fix: put
   `LICENSE`/`NOTICE` from the wasmtime release next to the `.so`.

2. **Several font files ship with no OFL text.** The OFL requires it in every
   distribution. The files without it today:

   - `Cargas/App/Cargas.Wasm/wwwroot/brand/Barlow-600.woff2`
   - `Cargas/App/Cargas.Wasm/wwwroot/fuentes/PlusJakartaSans-*.woff2`
   - `Compras/App/Compras.Cara/brand/NunitoSans-800.woff2`
   - `Compras/App/Compras.Cara/fuentes/PlusJakartaSans.woff2`
   - `Compras/App/Compras.Wasm/wwwroot/brand/NunitoSans-800.woff2`
   - `Compras/App/Compras.Wasm/wwwroot/fuentes/PlusJakartaSans.woff2`
   - `Compras/PaginaDeDescarga/descarga/fuentes/PlusJakartaSans.woff2`
   - and others — the check is: a font file with no `OFL*` or `*LICENSE*` in its
     own folder.

3. **The copyright holder on `LICENSE` is written as "nCubo".** It should be the
   legal entity that holds the copyright. Confirm before this is published.
