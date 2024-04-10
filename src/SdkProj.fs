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

  let addProperty propertyXml projectDir =
      findProjFileIn projectDir |> Xml.appendNode "/Project/PropertyGroup[1]" propertyXml

  let appendItemGroup projectDir =
      findProjFileIn projectDir |> Xml.appendNode "/Project" "<ItemGroup></ItemGroup>"
      projectDir

  let appendItem itemXml (itemGroupIndex:int) projectDir =
      findProjFileIn projectDir |> Xml.appendNode $"/Project/ItemGroup[{itemGroupIndex}]" itemXml
      projectDir

  let removeProperty xpath projectDir =
      findProjFileIn projectDir |> Xml.removeNode xpath
  
  let changeTfm tfm projectDir = 
    findProjFileIn projectDir |> Xml.replaceNodeText "/Project/PropertyGroup/TargetFramework" tfm
    projectDir
