## FSharp.Data.YamlProviderV2

inspired by https://fsprojects.github.io/FSharp.Configuration/YamlConfigProvider.html and https://github.com/baronfel/FSharp.Data.YamlProvider but ported to net8/net10 and might have slight variations.

the type provider sdk was used as a basis, but the project is split in Runtime and DesignTime, tests is just an .fsx script `dotnet fsi tests/try.fsx` to test

`dotnet build` to compile.

if you try from vscode, reload first after each build. a logger is added for debugging, will be disabled in production, `provided.log`

licensed under [MIT](https://choosealicense.com/licenses/mit/).