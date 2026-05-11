# ADR 001 — Bootstrap approach

**Status:** Accepted  
**Date:** 2026-05-11

## Context

Baldic must inject a managed C# assembly before Baldi's Basics Plus loads its own code,
without requiring BepInEx. Three options were evaluated:

- **A:** Unity Doorstop (external binary bootstrap component).
- **B:** Forked Doorstop.
- **C:** Custom native bootstrap.

## Decision

**Use Unity Doorstop v4 as the external bootstrap component for MVP.**

Config file `doorstop_config.ini` ships with Baldic:

```ini
[UnityDoorstop]
enabled = true
targetAssembly = Baldic/core/Baldic.Bootstrap.Managed.dll
```

The managed entrypoint is `Baldic.Bootstrap.Managed.BaldicBootstrap.Initialize()`.

## Rationale

| Criterion | Doorstop | Fork | Custom |
|---|---|---|---|
| Dev effort | Low | Medium | High |
| Cross-platform (Win/Lin/Mac) | Yes | Yes | Risky |
| Maintenance burden | Low (upstream) | Medium | High |
| Control over bootstrap sequence | Full (via managed code) | Full | Full |
| Dependency risk | LGPL-2.1, well-audited | Lower | None |

Doorstop is a proven, minimal binary used by BepInEx but **not part of** BepInEx —
it is a standalone project. Using it does not introduce BepInEx as a runtime dependency.

The LGPL-2.1 license requires that the Doorstop binary be replaceable; shipping it
separately from Baldic and documenting it in THIRD_PARTY_NOTICES satisfies this.

## Consequences

- `winhttp.dll` (Windows) or `libdoorstop.so` (Linux) must ship alongside Baldic.
- Installer copies these files to the game root.
- `doorstop_config.ini` is created/updated by `baldic install-loader`.
- Uninstaller removes the Doorstop files and restores `doorstop_config.ini` from backup.
- Safe mode (`Baldic/safe-mode.flag`) disables Baldic without removing Doorstop.
- If the `enabled=false` line is in the config, the game launches vanilla.

## Future

After the first stable release, evaluate a forked/minimal Doorstop to reduce
the external dependency and improve error messages on bootstrap failure.
