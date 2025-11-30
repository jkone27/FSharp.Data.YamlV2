namespace FSharp.Data.YamlProviderV2.Runtime

open System
open System.Collections.Generic
open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions

/// Runtime YAML parser + DOM
module YamlParsing =

    /// Internal representation
    type YamlValue =
        | Scalar of string
        | Map of Map<string, YamlValue>
        | List of YamlValue list
        | Null



    /// Parsed YAML container (used by the type provider)
    type YamlDocument (root: YamlValue) =
        member _.Root = root

        /// The actual runtime type returned by GetSample / Load
    type Root(doc: YamlDocument) =
        member _.Doc = doc

    /// Create a Root instance from a parsed YamlValue
    let createRoot (yamlValue: YamlValue) =
        Root(YamlDocument(yamlValue))

    /// Convert YamlDotNet object → internal YamlValue
    let rec private convert (v: obj) : YamlValue =
        match v with
        | null -> Null
        | :? string as s -> Scalar s
        | :? int as i -> Scalar (string i)
        | :? float as f -> Scalar (string f)
        | :? bool as b -> Scalar (string b)
        | :? IDictionary<obj,obj> as dict ->
            dict
            |> Seq.map (fun kv -> string kv.Key, convert kv.Value)
            |> Map.ofSeq
            |> Map
        | :? System.Collections.IList as lst ->
            lst
            |> Seq.cast<obj>
            |> Seq.map convert
            |> Seq.toList
            |> List
        | _ -> Scalar (string v)

    /// Parse text
    let parseYaml (text: string) : YamlDocument =
        let deserializer =
            DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build()

        let raw = deserializer.Deserialize<obj>(text)
        YamlDocument(convert raw)

    /// Get a nested key
    let rec getPath (keys: string list) (node: YamlValue) : YamlValue option =
        match keys, node with
        | [], v -> Some v
        | k :: rest, Map m ->
            m |> Map.tryFind k |> Option.bind (fun v -> getPath rest v)
        | _ -> None

    /// Convert scalar to target type
    let convertScalar (targetTy: Type) (scalar: string) : obj =
        if targetTy = typeof<string> then box scalar
        elif targetTy = typeof<int> then box (int scalar)
        elif targetTy = typeof<float> then box (float scalar)
        elif targetTy = typeof<bool> then box (bool.Parse scalar)
        else failwith $"Unsupported scalar conversion to {targetTy.Name}"

    /// Get scalar value from YamlValue
    let tryGetScalar (n: YamlValue) : string option =
        match n with
        | Scalar s -> Some s
        | _ -> None
