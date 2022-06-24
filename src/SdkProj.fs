namespace Mntnr.Toolkit

open System.IO

[<RequireQualifiedAccess>]
module SdkProj =

  let private supportedExtensions = [".csproj"; ".fsproj"]
  let private findProjFileIn projectDir =
    Directory.EnumerateFiles projectDir
    |> Seq.map FileInfo
    |> Seq.tryFind (fun f -> supportedExtensions |> List.contains f.Extension)
    |> Option.defaultWith (fun () -> failwith $"Directory '{projectDir}' doesn't contain any dotnet project file")
    |> fun f -> f.FullName

  let addProperty xmlProperty projectDir =
      findProjFileIn projectDir |> Xml.appendNode "/Project/PropertyGroup[1]" xmlProperty

  let removeProperty xpath projectDir =
      findProjFileIn projectDir |> Xml.removeNode xpath
  
  let changeTfm tfm csprojDir = 
    findProjFileIn csprojDir |> Xml.replaceNodeText "/Project/PropertyGroup/TargetFramework" tfm
