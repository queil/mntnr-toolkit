namespace Mntnr.Tests

open Mntnr.Toolkit
open System.IO
open Expecto

module Toolkit =

    let private testFinalEol file =
        use stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite)
        stream.Seek(-1, SeekOrigin.End) |> ignore
        let byte = stream.ReadByte()
        byte = 10


    [<Tests>]
    let tests =
        testList
            "Toolkit"
            [

              test "Should create file" {
                  let fileName = "new-file.tmp"
                  File.create fileName [ "Content" ]

                  $"File '{fileName}' should be created"
                  |> Expect.equal (File.Exists fileName) true

                  let lines = File.readLines fileName

                  "File content should be equal to 'Content'"
                  |> Expect.equal (lines |> Seq.toList) [ "Content" ]
              }

              test "Should delete file" {
                  let fileName = "delete-it.tmp"
                  File.create fileName [ "Content" ]

                  $"File '{fileName}' should exist before attempting deletion"
                  |> Expect.equal (File.Exists fileName) true

                  File.delete fileName

                  $"File {fileName} should be deleted"
                  |> Expect.equal (File.Exists fileName) false
              }

              test "Should replace file" {
                  let fileName = "Test.Dockerfile.tmp"

                  File.create
                      fileName
                      [ "FROM my.registry/my/image:0.14.0-alpine"
                        "# A COMMENT"
                        "RUN ls -la"
                        """ENTRYPOINT ["/app/test-9939-3232"]""" ]

                  File.replaceContent
                      fileName
                      (fun lines ->
                          {| projName =
                              lines
                              |> Seq.pick (function
                                  | ParseRegex """ENTRYPOINT \[\"\/app\/(.*)\"\]""" [ name ] -> Some name
                                  | _ -> None) |})
                      (fun vars ->
                          [ $"FROM my.registry/images/{vars.projName}:9.9.9"
                            $"""ENTRYPOINT ["{vars.projName}"]""" ])

                  $"File should have correct content"
                  |> Expect.equal
                      (fileName |> File.readLines |> Seq.toList)
                      [ "FROM my.registry/images/test-9939-3232:9.9.9"
                        """ENTRYPOINT ["test-9939-3232"]""" ]
              }

              test "Should append XML node" {
                  let fileName = "test.csproj.tmp"

                  File.create
                      fileName
                      [ """<Project Sdk="Microsoft.NET.Sdk.Web">"""
                        ""
                        """  <PropertyGroup>"""
                        """  </PropertyGroup>"""
                        ""
                        """</Project>""" ]

                  Xml.appendNode
                      "/Project/PropertyGroup[1]"
                      "<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>"
                      fileName

                  $"File should have correct content"
                  |> Expect.equal
                      (fileName |> File.readLines |> Seq.toList)
                      [ """<Project Sdk="Microsoft.NET.Sdk.Web">"""
                        ""
                        """  <PropertyGroup>"""
                        """    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>"""
                        """  </PropertyGroup>"""
                        ""
                        """</Project>""" ]

                  "File should have a final EOL" |> Expect.isTrue (testFinalEol fileName)
              }

              test "Should not append XML node if already exists" {
                  let fileName = "test68.csproj.tmp"

                  File.create
                      fileName
                      [ """<Project Sdk="Microsoft.NET.Sdk.Web">"""
                        ""
                        """  <PropertyGroup>"""
                        """    <IsPackable>true</IsPackable>"""
                        """  </PropertyGroup>"""
                        ""
                        """</Project>""" ]

                  Xml.appendNode "/Project/PropertyGroup[1]" "<IsPackable>true</IsPackable>" fileName

                  $"File should have correct content"
                  |> Expect.equal
                      (fileName |> File.readLines |> Seq.toList)
                      [ """<Project Sdk="Microsoft.NET.Sdk.Web">"""
                        ""
                        """  <PropertyGroup>"""
                        """    <IsPackable>true</IsPackable>"""
                        """  </PropertyGroup>"""
                        ""
                        """</Project>""" ]

                  "File should have a final EOL" |> Expect.isTrue (testFinalEol fileName)
              }


              test "Should remove csproj property" {
                  let projDir = $"test-files/csproj-remove-prop"
                  mkdir projDir
                  let projFile = $"{projDir}/test.csproj"

                  File.create
                      projFile
                      [ """<Project Sdk="Microsoft.NET.Sdk.Web">"""
                        ""
                        """  <PropertyGroup>"""
                        """    <NodeToRemove>this will be gone</NodeToRemove>"""
                        """  </PropertyGroup>"""
                        ""
                        """</Project>""" ]

                  projDir
                  |> SdkProj.findProjFile
                  |> SdkProj.removeProperty "/Project/PropertyGroup[1]/NodeToRemove"
                  |> ignore

                  "File should have correct content"
                  |> Expect.equal
                      (projFile |> File.readLines |> Seq.toList)
                      [ """<Project Sdk="Microsoft.NET.Sdk.Web">"""
                        ""
                        """  <PropertyGroup>"""
                        """  </PropertyGroup>"""
                        ""
                        """</Project>""" ]

                  "File should have a final EOL" |> Expect.isTrue (testFinalEol projFile)
              }

              test "Should add csproj property" {
                  let projDir = "test-files/csproj"
                  mkdir projDir
                  let projFile = $"{projDir}/test.csproj"

                  File.create
                      projFile
                      [ """<Project Sdk="Microsoft.NET.Sdk.Web">"""
                        ""
                        """  <PropertyGroup>"""
                        """  </PropertyGroup>"""
                        ""
                        """</Project>""" ]

                  projDir
                  |> SdkProj.findProjFile
                  |> SdkProj.addProperty "<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>"

                  $"File should have correct content"
                  |> Expect.equal
                      (projFile |> File.readLines |> Seq.toList)
                      [ """<Project Sdk="Microsoft.NET.Sdk.Web">"""
                        ""
                        """  <PropertyGroup>"""
                        """    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>"""
                        """  </PropertyGroup>"""
                        ""
                        """</Project>""" ]

                  "File should have a final EOL" |> Expect.isTrue (testFinalEol projFile)
              }

              test "Should stream to file" {
                  let fileName = "stream-to.txt.tmp"

                  [ "pear"; "why not" ]
                  |> File.streamTo fileName (function
                      | ParseRegex "p(ear)" [ ear ] -> $"b{ear}"
                      | s -> s)

                  "File should have correct content"
                  |> Expect.equal (fileName |> File.readLines |> Seq.toList) [ "bear"; "why not" ]
              }

              test "Should stream file" {
                  let fileName = "stream.txt.tmp"
                  [ "pear"; "why not" ] |> File.create fileName

                  File.stream fileName (function
                      | ParseRegex "p(ear)" [ ear ] -> $"b{ear}"
                      | s -> s)

                  "File should have correct content"
                  |> Expect.equal (fileName |> File.readLines |> Seq.toList) [ "bear"; "why not" ]
              }

              test "Should append after line with regex captures" {
                  let fileName = "insertAfter.txt.tmp"
                  [ "pear blue"; "why not" ] |> File.create fileName

                  File.stream fileName
                  <| File.insertAfter "^pear (.*)$" (fun [ colour ] -> [ $"bear {colour}"; "static" ])

                  "File should have correct content"
                  |> Expect.equal
                      (fileName |> File.readLines |> Seq.toList)
                      [ "pear blue"; "bear blue"; "static"; "why not" ]
              }

              test "Should append after line simple" {
                  let fileName = "insertLineAfter.txt.tmp"
                  [ "pear blue"; "why not"; "pear blue" ] |> File.create fileName
                  File.insertLineAfter "pear blue" "static" |> File.stream fileName

                  "File should have correct content"
                  |> Expect.equal
                      (fileName |> File.readLines |> Seq.toList)
                      [ "pear blue"; "static"; "why not"; "pear blue"; "static" ]
              }

              test "Should replace XML node contents" {
                  let fileName = "nodeContentsTest.csproj.tmp"

                  File.create
                      fileName
                      [ """<Project Sdk="Microsoft.NET.Sdk.Web">"""
                        ""
                        """  <PropertyGroup>"""
                        """    <TargetFramework>net7.0</TargetFramework>"""
                        """  </PropertyGroup>"""
                        ""
                        """</Project>""" ]

                  Xml.replaceNodeText "/Project/PropertyGroup/TargetFramework" (fun _ -> "net8.0") fileName

                  "File should have updated content"
                  |> Expect.equal
                      (fileName |> File.readLines |> Seq.toList)
                      [ """<Project Sdk="Microsoft.NET.Sdk.Web">"""
                        ""
                        """  <PropertyGroup>"""
                        """    <TargetFramework>net8.0</TargetFramework>"""
                        """  </PropertyGroup>"""
                        ""
                        """</Project>""" ]

                  "File should have a final EOL" |> Expect.isTrue (testFinalEol fileName)
              }

              test "Should change csproj target framework property" {
                  let projDir = "test-files/csproj-tfm"
                  mkdir projDir
                  let projName = $"{projDir}/test.csproj"

                  File.create
                      projName
                      [ """<Project Sdk="Microsoft.NET.Sdk.Web">"""
                        ""
                        """  <PropertyGroup>"""
                        """    <TargetFramework>netcoreapp3.1</TargetFramework>"""
                        """  </PropertyGroup>"""
                        ""
                        """</Project>""" ]

                  projDir
                  |> SdkProj.findProjFile
                  |> SdkProj.changeTfm (fun _ -> "net8.0")
                  |> ignore

                  "File should have updated content"
                  |> Expect.equal
                      (projName |> File.readLines |> Seq.toList)
                      [ """<Project Sdk="Microsoft.NET.Sdk.Web">"""
                        ""
                        """  <PropertyGroup>"""
                        """    <TargetFramework>net8.0</TargetFramework>"""
                        """  </PropertyGroup>"""
                        ""
                        """</Project>""" ]

                  "File should have a final EOL" |> Expect.isTrue (testFinalEol projName)
              }

              test "Should get git remote url" {

                  "Unexpected remote url"
                  |> Expect.equal (Git.relativePath "git@my.git.repo:queil/mntnr-toolkit.git") "queil/mntnr-toolkit"

                  "Unexpected remote url"
                  |> Expect.equal
                      (Git.relativePath "https://git-token:fake-token-value@my.git.repo/queil/mntnr-toolkit.git")
                      "queil/mntnr-toolkit"

                  "Unexpected remote url"
                  |> Expect.equal (Git.relativePath "https://github.com/queil/mntnr-toolkit") "queil/mntnr-toolkit"
              }

              test "Should match remote path" {

                  "Unexpected remote url"
                  |> Expect.equal (Git.remotePathMatches "^queil\/mntnr-toolkit") true

                  "Unexpected remote url"
                  |> Expect.equal (Git.remotePathMatches "^dev\/something") false
              } ]
