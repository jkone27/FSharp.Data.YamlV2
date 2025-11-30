// Quick test script for FSharp.Data.YamlProviderV2
// Build the design-time and runtime projects first (Debug/net8.0) and then run this with `dotnet fsi`.

// Add the build output folders to the assembly probing paths and reference the runtime and design-time DLLs.
#I "../src/FSharp.Data.YamlProviderV2.Runtime/bin/Debug/net8.0"
#I "../src/FSharp.Data.YamlProviderV2.DesignTime/bin/Debug/net8.0"
#r "Runtime.dll"
#r "DesignTime.dll"

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
    printfn "version: %s" sample.version
    printfn "debug: %s" (sample.debug.ToString())
    printfn ""
    
    // Test nested properties (database)
    printfn "=== Database configuration ==="
    printfn "database.host: %s" Config.database.host
    printfn "database.port: %s" Config.database.port
    printfn "database.name: %s" Config.database.name
    printfn "database.username: %s" Config.database.username
    printfn ""
    
    // Test other nested properties (logging)
    printfn "=== Logging configuration ==="
    printfn "logging.level: %s" Config.logging.level
    printfn "logging.format: %s" Config.logging.format
    printfn ""
    
    // Test ports (nested scalars)
    printfn "=== Port configuration ==="
    printfn "ports.http: %s" Config.ports.http
    printfn "ports.https: %s" Config.ports.https
    printfn ""
    
    printfn "✅ All tests passed!"
with ex ->
    printfn "❌ Error: %s" (ex.Message)
    printfn "%A" ex
