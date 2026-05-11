# baldic.mod.json — Specification v1

Schema version: **1**. JSON schema: `schemas/baldic.mod.schema.json`.

## Required fields

| Field | Type | Description |
|---|---|---|
| `schemaVersion` | `integer` | Must be `1`. |
| `id` | `string` | Regex `^[a-z][a-z0-9_]{1,63}$`. Stable, never changes between versions. |
| `version` | `string` | SemVer 2.0, e.g. `1.0.0` or `1.0.0-alpha.1`. |

## Recommended fields

| Field | Type | Description |
|---|---|---|
| `name` | `string` | Human-readable mod name. |
| `description` | `string` | Short description. |
| `authors` | `[{name, contact?}]` | Author list. |
| `license` | `string` | SPDX identifier, e.g. `MIT`. |
| `game` | object | Game compatibility. |
| `game.id` | `string` | Must be `baldis-basics-plus`. |
| `game.versions` | `string[]` | Version ranges, e.g. `[">=0.14.0 <0.15.0"]`. |

## Optional fields

| Field | Type | Description |
|---|---|---|
| `environment` | `"client"` \| `"*"` | Default `"client"`. |
| `loader.versions` | `string[]` | Loader version ranges. |
| `depends` | `{id: range}` | Hard dependencies. Missing or wrong version = fail. |
| `breaks` | `{id: range}` | Hard conflict. Present matching mod = fail. |
| `conflicts` | `{id: range}` | Soft conflict. Loader warns, user may override. |
| `recommends` | `{id: range}` | Suggestion, no enforcement. |
| `suggests` | `{id: range}` | Metadata only, for mod managers. |
| `provides` | `string[]` | Capability aliases, e.g. `["some_api_provider"]`. |
| `assemblies` | `string[]` | Relative paths to managed DLLs inside the package. No path traversal. |
| `entrypoints` | object | See **Entrypoints** below. |
| `patches` | object | See **Patches** below. |
| `resources` | object | See **Resources** below. |
| `custom` | object | Namespaced custom metadata. Unknown keys are ignored by loader. |

## Entrypoints

```json
"entrypoints": {
  "main":         ["MyMod.Namespace.MyInitializer"],
  "assetsLoaded": ["MyMod.Namespace.MyAssetsEntrypoint"],
  "generator":    ["MyMod.Namespace.MyGeneratorEntrypoint"],
  "options":      ["MyMod.Namespace.MyOptionsEntrypoint"],
  "preLaunch":    ["MyMod.Namespace.MyPreLaunchEntrypoint"]
}
```

Each value is a list of fully-qualified type names. The class must implement the
corresponding interface and have a public no-argument constructor:

| Key | Interface |
|---|---|
| `main` | `IBaldicModInitializer` |
| `assetsLoaded` | `IBaldicAssetsLoadedEntrypoint` |
| `generator` | `IBaldicGeneratorEntrypoint` |
| `options` | `IBaldicOptionsEntrypoint` |
| `preLaunch` | `IBaldicPreLaunchEntrypoint` |

API modules may declare additional entrypoint keys.

## Patches

```json
"patches": {
  "harmony": ["MyMod.Patches.MyHarmonyPatch"],
  "cecil": [
    {
      "assembly": "patches/MyMod.Cecil.dll",
      "targets":  ["Assembly-CSharp.dll"]
    }
  ]
}
```

- `harmony`: list of Harmony patch class names in mod's own assemblies.
- `cecil[].assembly`: relative path to a Cecil patcher DLL inside the package.
- `cecil[].targets`: assembly file names to patch pre-load.

## Resources

```json
"resources": {
  "root":         "assets",
  "localization": "assets/localization",
  "assetBundles": "bundles"
}
```

Localization folder layout:
```
assets/localization/English/main.json
assets/localization/Russian/main.json
```

AssetBundle folder layout:
```
bundles/
  windows/mybundle
  linux/mybundle
  macos/mybundle
```

## Dependency semantics

| Field | Semantics |
|---|---|
| `depends` | Absent or version mismatch → **hard fail**, game will not launch. |
| `breaks` | Matching mod present → **hard fail**. |
| `conflicts` | Warning, user can override. |
| `recommends` | Warning only, launch allowed. |
| `suggests` | Metadata only, no runtime effect. |
| `provides` | If two mods provide the same alias the loader checks they are not mutually exclusive. |

## Version range syntax

| Expression | Meaning |
|---|---|
| `*` | Any version |
| `1.0.0` | Exact match |
| `>=1.0.0` | At least 1.0.0 |
| `>=1.0.0 <2.0.0` | 1.x range |
| `>=0.14.0 <0.15.0` | BB+ 0.14.x range |

Multiple space-separated comparators are ANDed.

## Path rules

- All paths in `assemblies`, `patches`, `resources` are **relative** to the package root.
- Absolute paths are rejected.
- `..` (path traversal) is rejected.
- Null bytes are rejected.

## Minimal valid manifest

```json
{
  "schemaVersion": 1,
  "id": "my_mod",
  "version": "1.0.0",
  "game": {
    "id": "baldis-basics-plus",
    "versions": [">=0.14.0 <0.15.0"]
  },
  "assemblies": ["lib/MyMod.dll"],
  "entrypoints": {
    "main": ["MyMod.MyModInitializer"]
  }
}
```
