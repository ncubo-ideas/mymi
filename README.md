# Mymi

**English** · [Español](README.es.md)

A person's data universe, on their own device. Each institution hands them a **module** with
their record —their lab, their supermarket—, **Gnothi.Host** brings them together into a single
surface for their AI, and **they** decide what is visible.

> Gnothi is the project's working name, and the code still uses it (`Gnothi.Host`,
> `Gnothi.slnx`). Mymi is the product.

One directory per business, with Spanish names inside. The exception is `Gnothi.Host/`, which is
in English: it knows nothing about any domain, so it had no reason to speak the business's
language.

## What's here

| Folder | What it is |
|---|---|
| `Gnothi.Host/` | **The Host.** An MCP server toward the assistant and an MCP client toward each module. A proxy that **aggregates**: it gathers the tools of N modules into a single list |
| `Modulos/` | One directory per installed module, with its `manifest.json` (written by the **institution**) and its `installation.json` (written by the **Host**) |
| `Examenes/` | The clinical lab business (*exámenes*: lab tests) → [Examenes/README.md](Examenes/README.md), in Spanish |
| `Compras/` | The supermarket business (*compras*: purchases) |
| `HolaMundo/` | The reference example (`Facturas`, invoices). **Deliberately left out of `Gnothi.slnx`**: our domain is based on it, but it is not part of it |
| `.githooks/` | The Conventional Commits hook |
| `.mcp.json` | One MCP server **per business**, so an AI can see several at once |

---

## Setup after cloning

Two lines, once, after cloning. Neither one is optional.

```bash
git config core.hooksPath .githooks
git config core.longpaths true
```

**The first one** enables the hook that checks Conventional Commits before the commit exists.
It is a hook and not a written rule because a rule nobody checks only gets followed for the
first two weeks. The format is explained in `.githooks/commit-msg`.

**The second one** is for Windows long paths, and it deserves its own section.

---

## Cloning on Windows

Windows cuts paths off at **260 characters**. The longest path in this repo is **85**, so the
limit only bites when the **clone root** is deep —a `OneDrive - <long company name>` with nested
folders eats 150 without trying. The checkout fails with `Filename too long`, usually pointing
at the `wwwroot/lib/` of a Blazor WASM app, which is where the longest paths live.

**The cheap way to avoid it: clone into a short root.** `C:\src\mymi` leaves about 240
characters of headroom and the problem doesn't exist.

### If it already failed

`core.longpaths` still works after the failure: the clone is half done, not broken.

```bash
git -C mymi config core.longpaths true
git -C mymi reset --hard
```

And to keep it from failing in the first place, pass it to the clone itself:

```bash
git clone -c core.longpaths=true https://github.com/ncubo-ideas/mymi.git
```

### What `core.longpaths` does NOT fix

That flag talks **to git**. It doesn't talk to MSBuild, Visual Studio or the .NET SDK, which are
still tied to 260. In a Blazor WASM app that matters, because the build generates paths much
deeper than the repo's own:

```
obj/Debug/net9.0/compressed/<hash>/wwwroot/lib/bootstrap/dist/css/bootstrap.min.css.br
```

So with `longpaths` alone the failure just moves from the checkout to the build. The only thing
that also unblocks the tools is enabling it in Windows, **as administrator and per machine**:

```bash
reg add "HKLM\SYSTEM\CurrentControlSet\Control\FileSystem" /v LongPathsEnabled /t REG_DWORD /d 1 /f
```

### And that's why we don't version whole libraries

Each WASM app's `index.html` references **one** Bootstrap file:

```html
<link rel="stylesheet" href="lib/bootstrap/dist/css/bootstrap.min.css" />
```

So that's the only one under version control. Bringing in the whole `dist/` means 44 files and
8.4 MB of third-party build output —RTL variants, source maps, `bootstrap-grid`,
`bootstrap-reboot`, `bootstrap-utilities`, the JS bundle— that nobody references, and they are
also the longest paths in the repo. If another file from the library is needed, add **that one**.

---

## License

[MIT](LICENSE). Third-party components keep their own licenses: see
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
