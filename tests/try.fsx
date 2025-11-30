// Quick test script for FSharp.Data.YamlProviderV2
// Build the design-time and runtime projects first (Debug/net8.0) and then run this with `dotnet fsi`.

// Add the build output folders to the assembly probing paths and reference the runtime and design-time DLLs.
#I "../src/FSharp.Data.YamlProviderV2.Runtime/bin/Debug/net8.0"
#I "../src/FSharp.Data.YamlProviderV2.DesignTime/bin/Debug/net8.0"
#r "Runtime.dll"
#r "DesignTime.dll"

// #r "nuget: FSharp.Data.YamlProviderV2, 0.0.1"

open System

// Define the YAML file path as a literal so it can be used in the type provider
[<Literal>]
let YamlPath = __SOURCE_DIRECTORY__ + "/../samples/config.yaml"

// Use the YamlProvider with the file path
type Config = FSharp.Data.YamlProvider<YamlPath>

let sample = Config.GetSample()

printfn "Testing the YAML provider with sample config.yaml..."
printfn ""

try
    // Test top-level scalar properties
    printfn "=== Top-level properties ==="
    printfn "name: %s" sample.name
    printfn "version: %f" sample.version
    printfn "debug: %s" (sample.debug.ToString())
    printfn ""
    
    // Test nested properties (database)
    printfn "=== Database configuration ==="
    printfn "database.host: %s" sample.database.host
    printfn "database.port: %i" sample.database.port
    printfn "database.name: %s" sample.database.name
    printfn "database.username: %s" sample.database.username
    printfn ""
    
    // Test other nested properties (logging)
    printfn "=== Logging configuration ==="
    printfn "logging.level: %s" sample.logging.level
    printfn "logging.format: %s" sample.logging.format
    printfn ""
    
    // Test ports (nested scalars)
    printfn "=== Port configuration ==="
    printfn "ports.http: %i" sample.ports.http
    printfn "ports.https: %i" sample.ports.https
    printfn ""
    
    printfn "✅ All tests passed!"
with ex ->
    printfn "❌ Error: %s" (ex.Message)
    printfn "%A" ex
