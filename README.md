MyProvider — Minimal F# Type Provider scaffold (dotnet, no Paket)

This repository contains a minimal "hello world" F# Type Provider scaffold using only `dotnet` (no Paket), split into a runtime and a design-time project.

Structure
- `src/MyProvider.Runtime` — runtime helpers, multi-targets `net8.0;net10.0`, depends on `YamlDotNet` to demonstrate NuGet dependency resolution.
- `src/MyProvider.DesignTime` — design-time provider assembly, references `FSharp.TypeProviders.SDK` (PrivateAssets=all) and the runtime project.
- `tests/try.fsx` — F# script demonstrating how to load the design-time assembly and access the provided type via FSI (run with `dotnet fsi` after building).
- `build.sh` — helper script to create solutions, restore, and build (no Paket).

Quick start
1. Make the script executable: `chmod +x build.sh`
2. Build the sample:

```bash
./build.sh
```

3. Run the test script (after successful build):

```bash
dotnet fsi tests/try.fsx
```

Expected output:
```
Attempting to use the provided type from loaded design-time assembly...
Provider message: Hello from YamlDotNet
```

How it works
- The type provider (design-time assembly) injects a provided type `MyProvider.Hello` into the namespace.
- The provided type exposes a static property `Message` that calls the runtime helper.
- The runtime helper uses `YamlDotNet` to parse a YAML string and return a message. This demonstrates that NuGet dependencies are correctly resolved at design-time when the provider is loaded.

Notes
- This is a minimal scaffold — it demonstrates design-time/runtime separation and NuGet dependency resolution but does not implement a production-ready provider.
- FSI (dotnet fsi) successfully loads the type provider. In IDEs (VS/Ionide), opening a project that references the design-time project will trigger IDE-based provider loading.
- Packaging: the runtime project is set up to pack with `dotnet pack`. The design-time DLL is included in the package under `build/net8.0/` for IDE discovery (set in `src/MyProvider.Runtime/MyProvider.Runtime.fsproj`).

Building and packing
```bash
# Build (Debug)
dotnet build src/MyProvider.sln -c Debug

# Pack the runtime (includes design-time DLL for IDE discovery)
dotnet pack src/MyProvider.Runtime/MyProvider.Runtime.fsproj -c Debug -o nupkgs

# Run tests via FSI
dotnet fsi tests/try.fsx
```

References
- FSharp.TypeProviders.SDK: https://fsprojects.github.io/FSharp.TypeProviders.SDK/
- Microsoft tutorial: https://learn.microsoft.com/dotnet/fsharp/tutorials/type-providers/creating-a-type-provider

