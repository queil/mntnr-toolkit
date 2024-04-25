namespace Mntnr.Toolkit

open System.IO

[<RequireQualifiedAccess>]
module SdkProj =

    let private supportedExtensions = [ ".csproj"; ".fsproj" ]

    let findProjFile projectDir =
        Directory.EnumerateFiles projectDir
        |> Seq.map FileInfo
        |> Seq.tryFind (fun f -> supportedExtensions |> List.contains f.Extension)
        |> Option.defaultWith (fun () -> failwith $"Directory '{projectDir}' doesn't contain any dotnet project file")
        |> fun f -> f.FullName

    let addProperty propertyXml projFilePath =
        projFilePath |> Xml.appendNode "/Project/PropertyGroup[1]" propertyXml

    let appendItemGroup projFilePath =
        projFilePath |> Xml.appendNode "/Project" "<ItemGroup></ItemGroup>"
        projFilePath

    let appendItem itemXml (itemGroupIndex: int) projFilePath =
        projFilePath |> Xml.appendNode $"/Project/ItemGroup[{itemGroupIndex}]" itemXml
        projFilePath

    let removeProperty xpath projFilePath =
        projFilePath |> Xml.removeNode xpath
        projFilePath

    let changeTfm tfm projFilePath =
        projFilePath |> Xml.replaceNodeText "/Project/PropertyGroup/TargetFramework" tfm
        projFilePath

    let setNugetVer (nuget: string) (newVer: string -> string) projFilePath =
        projFilePath
        |> Xml.replaceNodeText $"/Project/ItemGroup/PackageReference[@Include='{nuget}']/@Version" newVer

        projFilePath
