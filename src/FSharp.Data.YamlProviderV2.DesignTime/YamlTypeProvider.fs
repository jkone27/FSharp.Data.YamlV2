namespace FSharp.Data.YamlProviderV2.DesignTime

open System
open System.IO
open System.Reflection
open Microsoft.FSharp.Core.CompilerServices
open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Quotations
open FSharp.Data.YamlProviderV2.Runtime.YamlParsing

module Logger =
    let private logFile = 
        let root = Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "provided.log")
        Path.GetFullPath(root)
    
    let log (msg: string) =
        try
            let timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
            let logMsg = $"[{timestamp}] {msg}"
            File.AppendAllText(logFile, logMsg + Environment.NewLine)
        with _ -> ()
    
    let logf fmt = Printf.ksprintf log fmt

module YamlTypeGenerator =
    let inferScalarType (value: string) =
        if String.IsNullOrWhiteSpace value then typeof<string>
        elif Boolean.TryParse value |> fst then typeof<bool>
        elif Int32.TryParse value |> fst then typeof<int>
        elif Double.TryParse value |> fst then typeof<float>
        else typeof<string>

    let makeScalarProperty (key: string) (returnType: Type) =
        ProvidedProperty(
            propertyName = key,
            propertyType = returnType,
            isStatic = false,
            getterCode = fun args ->
                <@@
                    let doc = (%%args.[0] : obj) :?> YamlDocument
                    match getPath [ key ] doc.Root |> Option.bind tryGetScalar with
                    | Some s ->
                        if returnType = typeof<int> then box (try int s with _ -> 0)
                        elif returnType = typeof<float> then box (try float s with _ -> 0.0)
                        elif returnType = typeof<bool> then box (match s.ToLowerInvariant() with "true" -> true | _ -> false)
                        else box s
                    | None -> null
                @@>
        )

    let rec generateTypeForMap (asm: Assembly) (ns: string) (typeName: string) (map: Map<string, YamlValue>) =
        let ty = ProvidedTypeDefinition(asm, ns, typeName, Some typeof<obj>, hideObjectMethods = true, isErased = true)

        for KeyValue(key, value) in map do
            match value with
            | Scalar s ->
                ty.AddMember (makeScalarProperty key (inferScalarType s))

            | Map inner ->
                let nestedName = 
                    if key.Length > 0 then Char.ToUpperInvariant(key.[0]).ToString() + key.Substring(1) + "Config"
                    else "NestedConfig"

                let nestedTy = generateTypeForMap asm ns nestedName inner
                ty.AddMember nestedTy

                ty.AddMember(
                    ProvidedProperty(
                        key,
                        nestedTy,
                        isStatic = false,
                        getterCode = fun args ->
                            <@@
                                let doc = (%% args.[0] : obj) :?> YamlDocument
                                match getPath [ key ] doc.Root with
                                | Some child -> YamlDocument(child)
                                | None -> YamlDocument(Null)
                            @@> :> Expr
                    )
                )

            | List xs ->
                let propType, getterCode =
                    if xs |> List.forall (function Scalar _ -> true | _ -> false) then
                        typeof<string[]>,
                        fun (args: Expr list) ->
                            <@@
                                let doc = (%%args.[0] : obj) :?> YamlDocument
                                match getPath [ key ] doc.Root with
                                | Some (List items) -> items |> List.choose tryGetScalar |> List.toArray
                                | _ -> [||]
                            @@> :> Expr
                    else
                        typeof<obj[]>,
                        fun args ->
                            <@@
                                let doc = (%% args.[0] : obj) :?> YamlDocument
                                match getPath [ key ] doc.Root with
                                | Some (List items) -> items |> List.map (fun v -> match tryGetScalar v with Some s -> box s | None -> box v) |> List.toArray
                                | _ -> [||]
                            @@> :> Expr

                ty.AddMember(ProvidedProperty(key, propType, isStatic = false, getterCode = getterCode))

            | Null -> ()

        ty

[<TypeProvider>]
type YamlProvider(config: TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces(config)

    let ns = "FSharp.Data"
    let asm = Assembly.GetExecutingAssembly()

    do
        let yamlTy = ProvidedTypeDefinition(asm, ns, "YamlProvider", Some typeof<obj>, hideObjectMethods = true)

        yamlTy.DefineStaticParameters(
            parameters = [ ProvidedStaticParameter("FilePath", typeof<string>) ],
            instantiationFunction = fun typeName args ->
                let filePath = args.[0] :?> string
                let full = if Path.IsPathRooted filePath then filePath else Path.Combine(config.ResolutionFolder, filePath) |> Path.GetFullPath
                if not (File.Exists full) then failwithf "YAML file not found: %s" full

                let text = File.ReadAllText full
                let doc = parseYaml text

                let rootTy = ProvidedTypeDefinition(asm, ns, typeName, Some typeof<obj>, hideObjectMethods = true, isErased = true)

                // Generate nested root type from map
                let rootConfigTy =
                    match doc.Root with
                    | Map m -> YamlTypeGenerator.generateTypeForMap asm ns (typeName + ".Root") m
                    | _ -> ProvidedTypeDefinition(asm, ns, typeName + ".Root", Some typeof<obj>, hideObjectMethods = true, isErased = true)

                rootTy.AddMember rootConfigTy

                // Static GetSample method returning the Root type
                let getSample =
                    ProvidedMethod(
                        "GetSample",
                        [],
                        rootConfigTy,
                        isStatic = true,
                        invokeCode = fun _ ->
                            <@@
                                let text = File.ReadAllText(full)
                                let doc = FSharp.Data.YamlProviderV2.Runtime.YamlParsing.parseYaml text
                                YamlDocument(doc.Root) :> obj
                            @@>
                    )
                rootTy.AddMember getSample

                // Static Load(filePath) method returning the Root type
                let load =
                    ProvidedMethod(
                        "Load",
                        [ProvidedParameter("filePath", typeof<string>)],
                        rootConfigTy,
                        isStatic = true,
                        invokeCode = fun args ->
                            <@@
                                let path = (%%args.[0] : obj) :?> string
                                let text = File.ReadAllText(path)
                                let doc = FSharp.Data.YamlProviderV2.Runtime.YamlParsing.parseYaml text
                                YamlDocument(doc.Root) :> obj
                            @@>
                    )
                rootTy.AddMember load

                rootTy
        )

        this.AddNamespace(ns, [ yamlTy ])

[<TypeProviderAssembly>]
do ()
